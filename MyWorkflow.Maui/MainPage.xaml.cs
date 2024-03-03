using System.Collections.ObjectModel;
using MyWorkflow.Maui.Models;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Asana.Net;
using System.Text.Json;


namespace MyWorkflow.Maui;

public partial class MainPage : ContentPage
{
    List<MyItem> items;
    List<AsanaTask> tasks;
    string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    string jsonString;
    MyItem currentItem;
    ObservableCollection<MyItem> currentList;

    public MainPage()
	{
		InitializeComponent();



        try
        {
            jsonString = File.ReadAllText(Path.Combine(docPath, "MyWorkflowData.json"));
            //items = new List<MyItem>(JsonSerializer.Deserialize<MyItem[]>(jsonString));
            tasks = new List<AsanaTask>(JsonSerializer.Deserialize<AsanaTask[]>(jsonString));
        }
        catch (Exception)
        {
            items = new List<MyItem>();
            tasks = new List<AsanaTask>();
        }
        currentItem = items.Find(x => x.Id == 0);
        if (currentItem == null)
        {
            currentItem = new MyItem() { Id = 0 };
            items.Add(currentItem);
        }

        ReadAsana();

        LoadCurrentList();
	}

    private void myListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        currentItem = e.SelectedItem as MyItem;
        LoadCurrentList();

        //Shell.Current.GoToAsync(nameof(MainPage));
    }

    private void btnBack_Clicked(object sender, EventArgs e)
    {
        currentItem = items.Find(x => x.Id == currentItem.ParenteId);   
        LoadCurrentList();
    }

    private void btnAdd_Clicked(object sender, EventArgs e)
    {
        int id = 1;
        if (items.Count > 0)
        {
            id = items.Max(i => i.Id) + 1;
        }
        MyItem item = new MyItem()
        {
            Id = id,
            ParenteId = currentItem.Id,
            Text = entryText.Text
        };
        items.Add(item);
        entryText.Text = "";

        SaveItems();

        LoadCurrentList();
    }

    private void Delete_Clicked(object sender, EventArgs e)
    {
        var menuItem = sender as MenuItem;
        MyItem item = menuItem.CommandParameter as MyItem;
        item = items.Find(x => x.Id == item.Id);
        if (item != null)
        {
            items.Remove(item);
            SaveItems();
            LoadCurrentList();
        }
    }

    private void LoadCurrentList()
    {
        currentList = new ObservableCollection<MyItem>(items.Where(i => i.ParenteId == currentItem.Id));
        myListView.ItemsSource = currentList;
        entryText.Text = currentItem.Text;
        if (currentItem.Id != 0)
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
        File.WriteAllText(Path.Combine(docPath, "MyWorkflowData.json"), JsonSerializer.Serialize(items));
    }

    private void entryText_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (entryText.Text != null && entryText.Text != currentItem.Text)
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

    private async Task ReadAsana()
    {

        /*
        IAsanaApiClient client = AsanaApiClientFactory.Create("2/1204359925204139/1206050122835689:21d739ce4de9c0ce99be94e5fa9eb73a");
        var tasks = await client.GetTasks("1206029338454980");

        // Iterate over the tasks and print their names
        foreach (var task in tasks)
        {
            Console.WriteLine(task.Name);
        }
        */

        string accessToken = "2/1204359925204139/1206050122835689:21d739ce4de9c0ce99be94e5fa9eb73a";
        string projectId = "1206029338454980";

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
                    url = $"https://app.asana.com/api/1.0/tasks/{task.gid}/subtasks";
                    response = await client.GetAsync(url);
                    responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        tasksResponse = JsonSerializer.Deserialize<AsanaTasksResponse>(responseContent);
                    }
                }
            }
            else
            {
                //Console.WriteLine($"Request failed with status code {response.StatusCode}");
                //Console.WriteLine(responseContent);
            }
        }
    }
}

