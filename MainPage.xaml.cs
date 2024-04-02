using System.Collections.ObjectModel;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Asana.Net;
using System.Text.Json;
using RestSharp;
using System.ComponentModel.Design;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;

namespace MyWorkflow;

public partial class MainPage : ContentPage
{
    List<MyTask> tasks;
    string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    string jsonString;
    MyTask rootItem;
    MyTask currentItem;
    ObservableCollection<MyTask> currentList;
    Random random = new Random();
    int idFrom = 100000000;
    int idTo = 999999999;
    string searchText = "";
    int searchCounter = 0;
    bool deleteConfirmed;
    //bool isRefresh;

    public MainPage()
	{
		InitializeComponent();

        StartUp();
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
            //btnAdd.IsEnabled = false;
            //btnUpdate.IsEnabled = false;
        }
    }
    private void editorNotes_TextChanged(object sender, TextChangedEventArgs e)
    {
        /*
        currentItem.notes = editorNotes.Text;
        btnUpdate.IsEnabled = true;
        */
    }

    private void datePick_Due_on_DateSelected(object sender, DateChangedEventArgs e)
    {
        /*
        if (!isRefresh)
        {
            //currentItem.due_on = datePick_Due_on.Date.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            //btnUpdate.IsEnabled = true;
        }
        else
        {
            isRefresh = false;
        }
        */
        entry_Due_on.Text = datePick_Due_on.Date.ToString();

    }

    private void checkCompleted_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        currentItem.completed = checkCompleted.IsChecked;
        //btnUpdate.IsEnabled = true;
    }

    private void myListView_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        currentItem = e.Item as MyTask;
        LoadCurrentList();
    }

    private void btnHome_Clicked(object sender, EventArgs e)
    {
        StartUp();
    }

    private void btnSearch_Clicked(object sender, EventArgs e)
    {
        int i = 0;
        foreach (var task in tasks.Where(x => isSearchInTitem(x))
            .OrderByDescending(x => x.modified_at == null ? x.created_at : x.modified_at))
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

    bool isSearchInTitem(MyTask item)
    {
        bool found = false;
        var words = searchText.Split(' ');
        foreach (var word in words)
        {
            //if (searchText == "" || (item.name != null && item.name.Contains(words[0], StringComparison.OrdinalIgnoreCase)))
            if (searchText == "" || (searchIn(item).Contains(word, StringComparison.OrdinalIgnoreCase)))
            {
                found = true; break;
            }
        }
        if (found && words.Length > 1)
        {
            foreach (var word in words)
            {
                if (!(ItemPath(item) + searchIn(item)).Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    found = false; break;
                }
            }
        }
        return found;
    }

    string searchIn(MyTask item) {
        string searchIn = "";
        if (item.name != null)
        {
            searchIn += item.name;
        }
        if (item.notes != null)
        {
            searchIn += item.notes;
        }
        if (item.due_on != null && !item.completed)
        {
            searchIn += "Termine" + item.due_on;
        }
        return searchIn;
    }

    private void btnBack_Clicked(object sender, EventArgs e)
    {
        if (currentItem.parentid != null)
        {
            currentItem = tasks.Find(x => x.gid == currentItem.parentid);   
            LoadCurrentList();
        }
    }

    private void btnAdd_Clicked(object sender, EventArgs e)
    {
        if (currentItem.name == entryText.Text)
        {
            lblStatus.Text = "Bitte erst den Titel eingeben bzw. ändern.";
        }
        else
        {
            string id = random.Next(idFrom, idTo).ToString();
            MyTask item = new MyTask()
            {
                gid = id,
                parentid = currentItem.gid,
                created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss", CultureInfo.InvariantCulture)
            };
            setItem(item);
            /*
            if (datePick_Due_on.Date != DateTime.Today)
            {
                item.due_on = datePick_Due_on.Date.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            }
            */
            tasks.Add(item);
            entryText.Text = "";
            editorNotes.Text = "";
            entry_Due_on.Text = "";

            SaveTask("add", item);
        }
    }

    private void btnUpdate_Clicked(object sender, EventArgs e)
    {
        setItem(currentItem);
        currentItem.modified_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss", CultureInfo.InvariantCulture);
        SaveTask("update", currentItem);
    }

    private void btnDelete_Clicked(object sender, EventArgs e)
    {
        if (deleteConfirmed)
        {
            tasks.Remove(currentItem);
            SaveTask("delete", currentItem);
        }
        else
        {
            lblStatus.Text = "Zur Bestätigung erneut Löschen anwählen.";
            deleteConfirmed = true;
        }
    }


    private void setItem(MyTask item)
    {
        /*
        if (datePick_Due_on.Date != DateTime.Today)
        {
            item.due_on = datePick_Due_on.Date.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");

        }
        */
        item.name = entryText.Text;
        item.notes = editorNotes.Text;
        if (entry_Due_on.Text != "")
        {
            item.due_on = DateTime.Parse(entry_Due_on.Text).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        }
        else
        {
            item.due_on = null;
        }
    }


    private void StartUp()
    {
        searchText = "";
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
        rootItem = currentItem;

        ReadAllAssana();

        LoadCurrentList();
    }

    void ReadAllAssana()
    {
        // Asana Tasks löschen
        tasks = tasks.Where(x => x.gid.Length <= idTo.ToString().Length).ToList();
        foreach (var task in tasks.Where(x => x.notes != null && x.notes.Contains("AccessToken:")))
        {
            ReadAsana(task);
        }
    }
    private void LoadCurrentList()
    {
        deleteConfirmed = false;
        tasks.ForEach(x => x.next_due_on = null);
        GetNextDateFromItems(rootItem);

        currentList = new ObservableCollection<MyTask>(tasks.Where(i => i.parentid == currentItem.gid).OrderBy(x => x.OrderDate));
        foreach (var task in currentList)
        {
            if (tasks.Find(x => x.parentid == task.gid) != null)
            {
                task.NameInList = "+ " + task.name;
            }
            else
            {
                task.NameInList = task.name;
            }
        }
        myListView.ItemsSource = currentList;
        entryText.Text = currentItem.name;
        editorNotes.Text = currentItem.notes;
        //isRefresh = true;
        if (currentItem.due_on != null)
        {
            datePick_Due_on.Date = DateTime.Parse(currentItem.due_on);
            entry_Due_on.Text = datePick_Due_on.Date.ToString();
        }
        else
        {
            datePick_Due_on.Date = DateTime.Today;
            entry_Due_on.Text = "";
        }
        checkCompleted.IsChecked = currentItem.completed;

        lblStatus.Text = ItemPath(currentItem);
        if (currentItem.gid != "")
        {
            btnBack.IsEnabled = true;
        }
        else { 
            //btnBack.IsEnabled = false; 
        }
        //btnAdd.IsEnabled = false;
        //btnUpdate.IsEnabled = false;
        //entryText.Focus();
    }

    private void SaveTask(string operation, MyTask item)
    {
        SaveItems();
        //if (currentItem.notes != null && currentItem.notes.Contains("AccessToken:") || currentItem.gid.Length > idTo.ToString().Length)
        if (currentItem.gid.Length > idTo.ToString().Length)
        {
            SaveAsanaTask(operation, item);
        }

        if (operation == "update" || operation == "delete")
        {
            if (currentItem.gid != "")
            {
                currentItem = tasks.Find(x => x.gid == currentItem.parentid);
            }
        }

        LoadCurrentList();
    }

    private void SaveItems()
    {
        File.WriteAllText(Path.Combine(docPath, "MyWorkflowData.json"), JsonSerializer.Serialize(tasks));
        File.WriteAllText(Path.Combine(docPath, "MyWorkflowData" + DateTime.Today.ToString().Split(" ")[0].Replace("/", "") + ".json"), JsonSerializer.Serialize(tasks));
    }

    private void entryText_Focused(object sender, FocusEventArgs e)
    {
        Dispatcher.Dispatch(() =>
        {
            var entry = sender as Entry;

            entry.CursorPosition = 0;
            entry.SelectionLength = entry.Text == null ? 0 : entry.Text.Length;
        });

    }



    private async Task ReadAsana(MyTask rootTask)
    {

        string accessToken = rootTask.notes.Split("AccessToken: ")[1].Split(",")[0];
        string projectId = rootTask.notes.Split("ProjectId: ")[1];

        var options = new RestClientOptions($"https://app.asana.com/api/1.0/tasks?project={projectId}");
        var client = new RestClient(options);
        var request = new RestRequest("");
        request.AddHeader("accept", "application/json");
        request.AddHeader("authorization", $"Bearer {accessToken}");
        var response = await client.GetAsync(request);

        /*
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            // Get project tasks
            string url = $"https://app.asana.com/api/1.0/projects/{projectId}/tasks";
            HttpResponseMessage response = await client.GetAsync(url);
            string responseContent = await response.Content.ReadAsStringAsync();
            */

            if (response.IsSuccessStatusCode)
            {
                AsanaTasksResponse tasksResponse = JsonSerializer.Deserialize<AsanaTasksResponse>(response.Content);
                foreach (var task in tasksResponse.data)
                {
                    options = new RestClientOptions($"https://app.asana.com/api/1.0/tasks/{task.gid}");
                    client = new RestClient(options);
                    request = new RestRequest("");
                    request.AddHeader("accept", "application/json");
                    request.AddHeader("authorization", $"Bearer {accessToken}");
                    response = await client.GetAsync(request);
                    AsanaTaskResponse taskResponse = JsonSerializer.Deserialize<AsanaTaskResponse>(response.Content);
                    task.notes = taskResponse.data.notes;
                    task.created_at = taskResponse.data.created_at;
                    task.modified_at = taskResponse.data.modified_at;
                    task.due_on = taskResponse.data.due_on;
                    task.completed = taskResponse.data.completed;
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
                var options = new RestClientOptions($"https://app.asana.com/api/1.0/tasks/{task.gid}/subtasks");
                var client = new RestClient(options);
                var request = new RestRequest("");
                request.AddHeader("accept", "application/json");
                request.AddHeader("authorization", $"Bearer {accessToken}");
                var response = await client.GetAsync(request);

                /*
                url = $"https://app.asana.com/api/1.0/tasks/{task.gid}/subtasks";
                response = await client.GetAsync(url);
                responseContent = await response.Content.ReadAsStringAsync();
                */

                if (response.IsSuccessStatusCode)
                {
                    AsanaTasksResponse tasksResponse = JsonSerializer.Deserialize<AsanaTasksResponse>(response.Content);
                    foreach (var subtask in tasksResponse.data)
                    {
                        options = new RestClientOptions($"https://app.asana.com/api/1.0/tasks/{subtask.gid}");
                        client = new RestClient(options);
                        request = new RestRequest("");
                        request.AddHeader("accept", "application/json");
                        request.AddHeader("authorization", $"Bearer {accessToken}");
                        response = await client.GetAsync(request);
                        AsanaTaskResponse taskResponse = JsonSerializer.Deserialize<AsanaTaskResponse>(response.Content);
                        subtask.notes = taskResponse.data.notes;
                        subtask.created_at = taskResponse.data.created_at;
                        subtask.modified_at = taskResponse.data.modified_at;
                        subtask.due_on = taskResponse.data.due_on;
                        subtask.completed = taskResponse.data.completed;
                        subtask.parentid = task.gid;
                        tasks.Add(subtask);
                        ReadSubTasks(subtask);
                    }
                }
                else 
                {
                    lblStatus.Text = response.Content.ToString();
                }
            }
        //}
    }
    private async Task SaveAsanaTask(string operation, MyTask task)
    {
        MyTask rootTask = task;
        string accessToken = "";
        string projectId = "";
        while (rootTask != null && accessToken == "")
        {
            if (rootTask.notes != null && rootTask.notes.Contains("AccessToken:"))
            {
                accessToken = rootTask.notes.Split("AccessToken: ")[1].Split(",")[0];
                projectId = rootTask.notes.Split("ProjectId: ")[1];
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
            string beginStr = "{\"data\":{\"name\":\"" + task.name 
                + "\",\"notes\":\"" + task.notes
                + "\",\"due_on\":\"" + task.due_on 
                + "\",\"completed\":\"" + task.completed;
            if (operation == "add")
            {
                if (currentItem.notes.Contains("AccessToken:"))
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
                lblStatus.Text = response.Content.ToString();
            }
            else
            {
                AsanaTaskResponse taskResponse = JsonSerializer.Deserialize<AsanaTaskResponse>(response.Content);
                task.gid = taskResponse.data.gid;
            }
        }
        if (operation == "update")
        {
            var response = await client.PutAsync(request);
            if (!response.IsSuccessful)
            { 
                lblStatus.Text = response.Content.ToString();
            }
        }
        if (operation == "delete")
        {
            var response = await client.DeleteAsync(request);
        }


    }

    public string ItemPath(MyTask task)
    {
        string path = "";
        while (task != null && task.parentid != null)
        {
            task = tasks.FirstOrDefault(i => i.gid == task.parentid);
            if (task != null && task.name != null)
            {
                path += " < " + task.name;
            }
        }
        return path;
    }

    void GetNextDateFromItems(MyTask item)
    {
        foreach (var subItem in tasks.Where(x => x.parentid == item.gid))
        {
            GetNextDateFromItems(subItem);
            if (!subItem.completed && subItem.next_due_on != null)
            {
                if (item.next_due_on == null)
                {
                    item.next_due_on = subItem.next_due_on;
                }
                else
                {
                    if (DateTime.Parse(subItem.next_due_on) < DateTime.Parse(item.next_due_on))
                    {
                        item.next_due_on = subItem.next_due_on;
                    }
                }
            }
        }
        if (item.due_on != null)
        {
            if (item.next_due_on != null)
            {
                if (DateTime.Parse(item.due_on) < DateTime.Parse(item.next_due_on))
                {
                    item.next_due_on = item.due_on;
                }
            }
            else
            {
                item.next_due_on = item.due_on;
            }

        }
    }

    class Test
    {
        public string Name { get; set; }
        public string TextColor { get; set; }
    }

}

