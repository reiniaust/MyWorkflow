using System.Collections.ObjectModel;
using MyWorkflow.Maui.Models;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Asana.Net;
using System.Text.Json;


namespace MyWorkflow.Maui;

public partial class MainPage : ContentPage
{
    List<MyTask> tasks;
    string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    string jsonString;
    MyTask currentItem;
    ObservableCollection<MyTask> currentList;
    Random random = new Random();

    public MainPage()
	{
		InitializeComponent();



        try
        {
            jsonString = File.ReadAllText(Path.Combine(docPath, "MyWorkflowData.json"));
            //tasks = new List<MyTask>(JsonSerializer.Deserialize<MyTask[]>(jsonString));
            tasks = new List<MyTask>(JsonSerializer.Deserialize<MyTask[]>(jsonString));
        }
        catch (Exception)
        {
            tasks = new List<MyTask>();
        }
        currentItem = tasks.Find(x => x.gid == "");
        if (currentItem == null)
        {
            currentItem = new MyTask() { gid = "" };
            tasks.Add(currentItem);
        }

        foreach (var task in tasks.Where(x => x.name != null && x.name.Contains("AccessToken:")))
        {
            ReadAsana(task);
        }

        LoadCurrentList();
	}

    private void myListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        currentItem = e.SelectedItem as MyTask;
        LoadCurrentList();

        //Shell.Current.GoToAsync(nameof(MainPage));
    }

    private void btnBack_Clicked(object sender, EventArgs e)
    {
        currentItem = tasks.Find(x => x.gid == currentItem.parentid);   
        LoadCurrentList();
    }

    private void btnAdd_Clicked(object sender, EventArgs e)
    {
        string id = random.Next(100000000, 999999999).ToString();
        MyTask item = new MyTask()
        {
            gid = id,
            parentid = currentItem.gid,
            name = entryText.Text
        };
        tasks.Add(item);
        entryText.Text = "";

        SaveItems();

        LoadCurrentList();
    }

    private void Delete_Clicked(object sender, EventArgs e)
    {
        var menuItem = sender as MenuItem;
        MyTask item = menuItem.CommandParameter as MyTask;
        item = tasks.Find(x => x.gid == item.gid);
        if (item != null)
        {
            tasks.Remove(item);
            SaveItems();
            LoadCurrentList();
        }
    }

    private void LoadCurrentList()
    {
        currentList = new ObservableCollection<MyTask>(tasks.Where(i => i.parentid == currentItem.gid));
        myListView.ItemsSource = currentList;
        entryText.Text = currentItem.name;
        if (currentItem.gid != "")
        {
            btnBack.IsEnabled = true;
        }
        else { 
            btnBack.IsEnabled = false; 
        }
        btnAdd.IsEnabled = false;
    }

    private void SaveItems()
    {
        File.WriteAllText(Path.Combine(docPath, "MyWorkflowData.json"), JsonSerializer.Serialize(tasks));
    }

    private void entryText_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (entryText.Text != null && entryText.Text != currentItem.name)
        {
            btnAdd.IsEnabled = true;
        }
        else
        {
            btnAdd.IsEnabled = false;
        }
    }

    private void entryText_Focused(object sender, FocusEventArgs e)
    {
        var entry = sender as Entry;

        entry.CursorPosition = 0;
        entry.SelectionLength = entry.Text == null ? 0 : entry.Text.Length;
    }

    private async Task ReadAsana(MyTask rootTask)
    {

        /*
        IAsanaApiClient client = AsanaApiClientFactory.Create("2/1204359925204139/1206050122835689:21d739ce4de9c0ce99be94e5fa9eb73a");
        var tasks = await client.GetTasks("1206029338454980");

        // Iterate over the tasks and print their names
        foreach (var task in tasks)
        {
            Console.WriteLine(task.Name);
        }

        string accessToken = "2/1204359925204139/1206050122835689:21d739ce4de9c0ce99be94e5fa9eb73a";
        string projectId = "1206029338454980";
        */
        string accessToken = rootTask.name.Split("AccessToken: ")[1].Split(",")[0];
        string projectId = rootTask.name.Split("ProjectId: ")[1].Split(")")[0];

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            // Get project tasks
            string url = $"https://app.asana.com/api/1.0/projects/{projectId}/tasks";
            HttpResponseMessage response = await client.GetAsync(url);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                AsanaTasksResponse tasksResponse = JsonSerializer.Deserialize<AsanaTasksResponse>(responseContent);
                foreach (var task in tasksResponse.data)
                {
                    task.parentid = rootTask.gid;
                    tasks.Add(task);
                    await ReadSubTasks(task);
                }
                LoadCurrentList();
            }
            else
            {
                //Console.WriteLine($"Request failed with status code {response.StatusCode}");
                //Console.WriteLine(responseContent);
            }

            async Task ReadSubTasks(MyTask task)
            {
                url = $"https://app.asana.com/api/1.0/tasks/{task.gid}/subtasks";
                response = await client.GetAsync(url);
                responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    AsanaTasksResponse tasksResponse = JsonSerializer.Deserialize<AsanaTasksResponse>(responseContent);
                    foreach (var subtask in tasksResponse.data)
                    {
                        subtask.parentid = task.gid;
                        tasks.Add(subtask);
                        ReadSubTasks(subtask);
                    }
                }
            }
        }
    }
}

