using System.Collections.ObjectModel;
using MyWorkflow.Maui.Models;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Asana.Net;
using System.Text.Json;
using RestSharp;
using System.ComponentModel.Design;

namespace MyWorkflow.Maui;

public partial class MainPage : ContentPage
{
    List<MyTask> tasks;
    string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    string jsonString;
    MyTask currentItem;
    ObservableCollection<MyTask> currentList;
    Random random = new Random();
    int idFrom = 100000000;
    int idTo = 999999999;
    string searchText = "";
    int searchCounter = 0;

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

    private void entryText_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (entryText.Text != null && entryText.Text != currentItem.name)
        {
            searchText = entryText.Text;
            searchCounter = 0;

            btnAdd.IsEnabled = true;
            if (currentItem.gid != "")
            {
                btnUpdate.IsEnabled = true;
            }
        }
        else
        {
            btnAdd.IsEnabled = false;
            btnUpdate.IsEnabled = false;
        }
    }

    private void myListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        currentItem = e.SelectedItem as MyTask;
        LoadCurrentList();

        //Shell.Current.GoToAsync(nameof(MainPage));
    }

    private void btnSearch_Clicked(object sender, EventArgs e)
    {
        int i = 0;
        foreach (var task in tasks.Where(x => searchText == "" || (x.name != null && x.name.Contains(searchText, StringComparison.OrdinalIgnoreCase))))
        {
            if (i == searchCounter)
            {
                currentItem = task;
            }
            i += 1;
        }
        searchCounter++;
        LoadCurrentList();
    }

    private void btnBack_Clicked(object sender, EventArgs e)
    {
        currentItem = tasks.Find(x => x.gid == currentItem.parentid);   
        LoadCurrentList();
    }

    private void btnAdd_Clicked(object sender, EventArgs e)
    {
        string id = random.Next(idFrom, idTo).ToString();
        MyTask item = new MyTask()
        {
            gid = id,
            parentid = currentItem.gid,
            name = entryText.Text
        };
        tasks.Add(item);
        entryText.Text = "";

        SaveTask("add", item);

    }

    private void btnUpdate_Clicked(object sender, EventArgs e)
    {
        currentItem.name = entryText.Text;
        SaveTask("update", currentItem);
    }


    private void Delete_Clicked(object sender, EventArgs e)
    {
        var menuItem = sender as MenuItem;
        MyTask item = menuItem.CommandParameter as MyTask;
        item = tasks.Find(x => x.gid == item.gid);
        if (item != null)
        {
            tasks.Remove(item);
            SaveTask("delete", item);
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
        btnUpdate.IsEnabled = false;
    }

    private void SaveTask(string operation, MyTask item)
    {
        if (currentItem.name.Contains("AccessToken:") || currentItem.gid.Length > idTo.ToString().Length)
        {
            SaveAsanaTask(operation, item);
        }
        else
        {
            SaveItems();
        }

        if (operation == "update")
        {
            currentItem = tasks.Find(x => x.gid == currentItem.parentid);
        }

        LoadCurrentList();
    }

    private void SaveItems()
    {
        // Asana Tasks löschen
        tasks = tasks.Where(x => x.gid.Length <= idTo.ToString().Length).ToList();

        File.WriteAllText(Path.Combine(docPath, "MyWorkflowData.json"), JsonSerializer.Serialize(tasks));
    }

    private void entryText_Focused(object sender, FocusEventArgs e)
    {
        var entry = sender as Entry;

        entry.CursorPosition = 0;
        entry.SelectionLength = entry.Text == null ? 0 : entry.Text.Length;
    }

    private async Task ReadAsana(MyTask rootTask)
    {

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
                else 
                {
                    lblMessage.Text = response.Content.ToString();
                }
            }
        }
    }
    private async Task SaveAsanaTask(string operation, MyTask task)
    {
        MyTask rootTask = task;
        string accessToken = "";
        string projectId = "";
        while (rootTask != null && accessToken == "")
        {
            if (rootTask.name.Contains("AccessToken:"))
            {
                accessToken = rootTask.name.Split("AccessToken: ")[1].Split(",")[0];
                projectId = rootTask.name.Split("ProjectId: ")[1].Split(")")[0];
            }
            rootTask = tasks.Find(x => x.gid == rootTask.parentid);
        }

        string url = "https://app.asana.com/api/1.0/tasks";
        if (operation == "update" || operation == "delete")
        {
            url += "/" + task.gid;
        }
        var options = new RestClientOptions(url);
        var client = new RestClient(options);
        var request = new RestRequest("");
        request.AddHeader("accept", "application/json");
        request.AddHeader("authorization", "Bearer " + accessToken);

        if (operation == "add" || operation == "update")
        {
            string beginStr = "{\"data\":{\"name\":\"" + task.name;
            if (operation == "add")
            {
                if (currentItem.name.Contains("AccessToken:"))
                {
                    request.AddJsonBody(beginStr + "\",\"projects\":[\"" + projectId + "\"]}}", false);
                }
                else
                {
                    request.AddJsonBody(beginStr + "\",\"parent\":\"" + currentItem.gid + "\"}}", false);
                }
            }
            else
            {
                request.AddJsonBody(beginStr + "\"}}", false);
            }
        }

        if (operation == "add")
        {
            var response = await client.PostAsync(request);
            if (!response.IsSuccessful)
            {
                lblMessage.Text = response.Content.ToString();
            }
        }
        if (operation == "update")
        {
            var response = await client.PutAsync(request);
            if (!response.IsSuccessful)
            { 
                lblMessage.Text = response.Content.ToString();
            }
        }
        if (operation == "delete")
        {
            var response = await client.DeleteAsync(request);
        }


    }

}

