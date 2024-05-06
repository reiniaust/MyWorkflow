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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
//using Syncfusion.Maui.Inputs;

namespace MyWorkflow;

public partial class MainPage : ContentPage
{
    List<MyTask> tasks;
    MySettings settings;
    string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    string jsonString;
    MyTask rootItem;
    MyTask currentItem;
    MyTask oldItem;
    MyTask cutItem;
    MyTask linkToItem;
    ObservableCollection<MyTask> currentList;
    Random random = new Random();
    int idFrom = 100000000;
    int idTo = 999999999;
    string searchText = "";
    int searchCounter = 0;
    string operation = "";
    //bool deleteConfirmed;
    //bool isRefresh;
    List<DependenceItem> depList = new List<DependenceItem>();
    //List<SfComboBox> depComboList = new List<SfComboBox>();   
    bool editStatus;
    bool loadStatus;
    bool hasNews;

    public MainPage()
	{
		InitializeComponent();

        btnPaste.IsVisible = false;

        try
        {
            jsonString = File.ReadAllText(Path.Combine(docPath, "MyWorkflowSettings.json"));
            settings = JsonSerializer.Deserialize<MySettings>(jsonString);
            StartUp();
        }
        catch (Exception)
        {
            settings = new MySettings();
            startEamilView.IsVisible = true;
            mainView.IsVisible = false;
        }

    }
    private void btnSaveEmail_Clicked(object sender, EventArgs e)
    {
        if (entryEmail.Text != null)
        {
            settings.Email = entryEmail.Text;
            File.WriteAllText(Path.Combine(docPath, "MyWorkflowSettings.json"), JsonSerializer.Serialize(settings));
            StartUp();
        }
    }

    /*
    private void entryText_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (entryText.Text != null && entryText.Text != currentItem.name)
        {
            searchText = entryText.Text;
            searchCounter = 0;
        }
    }
    */

    private void searchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        searchText = searchBar.Text;
        searchCounter = 0;
    }


    private void editorNotes_TextChanged(object sender, TextChangedEventArgs e)
    {
        //setEditStatus();
    }

    private void datePick_Due_on_DateSelected(object sender, DateChangedEventArgs e)
    {
        entry_Due_on.Text = datePick_Due_on.Date.ToString();
        entry_Due_on.Text = entry_Due_on.Text.Substring(0, entry_Due_on.Text.Length - 3) ; // Sekunden von Datum/Uhrzeit abschneiden
        setEditStatus();
    }

    private void checkCompleted_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        setEditStatus();
    }

    private void myListView_ItemTapped(object sender, ItemTappedEventArgs e)
    {

        MyTask item = e.Item as MyTask;
        if (operation == "deleteLink")
        {
            currentItem.dependencies.Remove(item.gid);
            operation = "";
            SaveTask("update", currentItem);
        }
        else
        {
            currentItem = item;
        }
        LoadCurrentList();
    }

    private void btnRefresh_Clicked(object sender, EventArgs e)
    {
        StartUp();
    }

    /*
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
    */
    private void searchBar_SearchButtonPressed(object sender, EventArgs e)
    {
        if (searchBar.Text == "*")
        {
            searchText = "";
        }
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
                if (!(ItemPathLeftToRight(item) + searchIn(item)).Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    found = false; break;
                }
            }
        }
        return found;
    }

    string searchIn(MyTask item) {
        string searchIn = "";
        searchIn += item.name;
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
        SetStatus("Hinzufügen");
        editStatus = true;
        operation = "add";
        setButtonStatus();
        entryText.Text = "";
        editorNotes.Text = "";
        entry_Due_on.Text = "";
        entryText.Focus();

    }


    private void btnEdit_Clicked(object sender, EventArgs e)
    {
        SetStatus("Ändern");
        editStatus = true;
        operation = "update";
        setButtonStatus();
    }

    private void btnDelete_Clicked(object sender, EventArgs e)
    {
        editStatus = true;
        operation = "delete";
        setButtonStatus();
        SetStatus("Zum Löschen auf Speichern klicken.");
    }

    private void btnSave_Clicked(object sender, EventArgs e)
    {
        string id = random.Next(idFrom, idTo).ToString();
        MyTask item = null;
        if (operation == "add")
        {
            item = new MyTask()
            {
                gid = id,
                parentid = currentItem.gid,
                created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss", CultureInfo.InvariantCulture)
            };
            setItem(item);
        }
        if (operation == "update")
        {
            item = currentItem;
            setItem(item);
            item.modified_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss", CultureInfo.InvariantCulture);
        }

        if (item != null && item.notes != null && item.notes.Contains("TaskId"))
        {
            item.gid = item.notes.Split("TaskId: ")[1];
        }

        if (operation == "delete")
        {
            item = currentItem;
            tasks.Remove(item);
        }
        if (linkToItem != null)
        {
            operation = "setDependence";
            item = linkToItem;
            linkToItem.dependencies.Add(currentItem.gid);
            currentItem = linkToItem;
            linkToItem = null;
        }

        SaveTask(operation, item);

        editStatus = false;
        operation = "";
        LoadCurrentList();
    }

    private void btnCancel_Clicked(object sender, EventArgs e)
    {
        operation = "";
        editStatus = false;
        cutItem = null;
        linkToItem = null;
        setButtonStatus();
    }

    private void btnExpandMore_Clicked(object sender, EventArgs e)
    {
        stackMoreButtons.IsVisible = true;
        btnExpandMore.IsVisible = false;
        btnExpandLess.IsVisible = true;
    }

    private void btnExpandLess_Clicked(object sender, EventArgs e)
    {
        stackMoreButtons.IsVisible = false;
        btnExpandLess.IsVisible = false;
        btnExpandMore.IsVisible = true;
    }


    private void btnCut_Clicked(object sender, EventArgs e)
    {
            cutItem = currentItem;
            btnCut.IsVisible = false;
            btnPaste.IsVisible = true;
            currentItem = tasks.Find(x => x.gid == currentItem.parentid);
            LoadCurrentList();
            currentList.Remove(cutItem);
    }

    private void btnAddLink_Clicked(object sender, EventArgs e)
    {
        SetStatus("Abhängigkeit hinzufügen: Punkt auswählen und auf Speichern klicken.");
        linkToItem = currentItem;
        editStatus = true;
        setButtonStatus();
    }

    private void btnDeleteLink_Clicked(object sender, EventArgs e)
    {
        SetStatus("Zu löschende Abhängigkeit anklicken.");
        operation = "deleteLink";
    }

    private void btnPaste_Clicked(object sender, EventArgs e)
    {
        MyTask item = null;
        if (cutItem != null) {
            item = cutItem;
            cutItem.parentid = currentItem.gid;
            btnCut.IsVisible = true;
            cutItem = null;
        }
        btnPaste.IsVisible = false;
        currentItem = item;
        SaveTask("update", item);
    }

    void setEditStatus()
    {
        if (!editStatus && !loadStatus)
        {
            editStatus = true;
            operation = "update";
            setButtonStatus();
        }
    }

    void setButtonStatus()
    {
        searchBar.IsVisible = !editStatus;
        entryText.IsVisible = editStatus;
        editorNotes.IsVisible = editStatus || currentItem.notes != "";
        stack_Due_on_Completed.IsVisible = editStatus || currentItem.due_on != null;

        btnBack.IsVisible = !editStatus || linkToItem != null;
        //btnSearch.IsVisible = !editStatus || linkToItem != null;
        if (currentItem.gid == "")
        {
            btnBack.IsVisible = false;
            btnEdit.IsVisible = false;
            btnExpandMore.IsVisible = false;
            btnExpandLess.IsVisible = false;
            btnCut.IsVisible = false;
            btnAddLink.IsVisible = false;
            btnDeleteLink.IsVisible = false;
            btnDelete.IsVisible = false;
        }
        else
        {
            btnRefresh.IsVisible = !editStatus || linkToItem != null;
            btnEdit.IsVisible = !editStatus;
            if (!editStatus)
            {
                btnExpandMore.IsVisible = !stackMoreButtons.IsVisible;
                btnExpandLess.IsVisible = stackMoreButtons.IsVisible;
            }
            btnCut.IsVisible = !editStatus;
            btnAddLink.IsVisible = !editStatus && linkToItem == null;
            btnDeleteLink.IsVisible = !editStatus && linkToItem != null;
            btnDelete.IsVisible = !editStatus;
        }

        btnAdd.IsVisible = !editStatus;
        btnSave.IsVisible = editStatus;
        btnCancel.IsVisible = editStatus;
    }


    private void setItem(MyTask item)
    {
        oldItem = item.Clone() as MyTask;

        item.name = entryText.Text.TrimEnd();
        item.notes = editorNotes.Text;
        if (entry_Due_on.Text != "")
        {
            item.due_on = DateTime.Parse(entry_Due_on.Text).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        }
        else
        {
            item.due_on = null;
        }
        currentItem.completed = checkCompleted.IsChecked;

        /*
        item.dependencies.Clear();
        foreach (var comboDep in depComboList)
        {
            if (comboDep.SelectedValue != null)
            {
                item.dependencies.Add(comboDep.SelectedValue.ToString());
            }
        }
        */
    }

    private void setItemPropertys(MyTask fromItem, MyTask toItem)
    {
        toItem.name = fromItem.name;
        toItem.notes = fromItem.notes;
        toItem.due_on = fromItem.due_on;
        toItem.completed = fromItem.completed;
        if (toItem.modified_at != fromItem.modified_at)
        {
            toItem.modified_at = fromItem.modified_at;
            hasNews = true;
        }
    }

    private void StartUp()
    {
        try
        {
            startEamilView.IsVisible = false;
            mainView.IsVisible = true;

            searchText = "";
                    startEamilView.IsVisible = false;
        mainView.IsVisible = true;

        searchText = "";
        searchBar.Text = "*";
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
        editStatus = false;

        ReadAllAssana();

        LoadCurrentList();

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
            editStatus = false;

            ReadAllAssana();

            LoadCurrentList();

        }
        catch (Exception e)
        {
            SetStatus(e.Message);
        }
    }

    void ReadAllAssana()
    {
        // Asana Tasks löschen
        //tasks = tasks.Where(x => x.gid.Length <= idTo.ToString().Length || x.parentid == "").ToList();
        foreach (var task in tasks.Where(x => x.notes != null && x.notes.Contains("AccessToken:")))
        {
            ReadAsana(task);
        }
    }
    private void LoadCurrentList()
    {
        SetStatus("");

        loadStatus = true;

        if (currentItem == null)
        {
            currentItem = tasks.Find(x => x.gid == "");
        }

        tasks.ForEach(x =>
        {
            if (x.name == null) x.name = "";
            x.name = x.name.TrimEnd();
            x.next_due_on = null;
            depList.Add(new DependenceItem() { Id = x.gid, NameAndPath = x.name + ItemPathRightToLeft(x) });
        });

        // Abhängigkeiten anzeigen
        List<MyTask> dependList = new List<MyTask>();
        if (currentItem.dependencies.Count > 0)
        {
            lblDepend.IsVisible = true;
            listViewDepend.IsVisible = true;
            foreach (var id in currentItem.dependencies)
            {
                MyTask item = tasks.Find(x => x.gid == id);
                dependList.Add(item);
            }
            listViewDepend.ItemsSource = dependList;
            btnDeleteLink.IsVisible = true;
        }
        else 
        { 
            lblDepend.IsVisible = false; 
            listViewDepend.IsVisible = false;
            btnDeleteLink.IsVisible = false;
        }

        // hiervon abhängig
        dependList = tasks.Where(x => x.dependencies.Contains(currentItem.gid)).ToList();
        if (dependList.Count > 0)
        {
            lblDependTo.IsVisible = true;
            listViewDependTo.IsVisible = true;
            listViewDependTo.ItemsSource = dependList;
        }
        else
        {
            lblDependTo.IsVisible = false;
            listViewDependTo.IsVisible = false;
        }

        if (cutItem == null)
        {
            setButtonStatus();
        }

        GetNextDateFromItems(rootItem);

        currentList = new ObservableCollection<MyTask>(tasks.Where(i => i.parentid == currentItem.gid).OrderBy(x => x.OrderDate));
        foreach (var task in currentList)
        {
            if (task.notes != null && task.notes != "" || tasks.Find(x => x.parentid == task.gid) != null)
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

        lblPath.Text = ItemPathLeftToRight(currentItem);
        if (currentItem.gid != "")
        {
            btnBack.IsEnabled = true;
        }
        //editorNotes.Focus();
        loadStatus = false;
    }

    private void SaveTask(string operation, MyTask item)
    {
        if (currentItem.notes != null && currentItem.notes.Contains("AccessToken:") || currentItem.gid.Length > idTo.ToString().Length)
        //if (currentItem.gid.Length > idTo.ToString().Length)
        {
            SaveAsanaTask(operation, item);
        }
        else
        {
            tasks.Add(item);
        }
        SaveItems();

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
        SetTasksToNotRefreshed(rootTask, "[nicht aktualisiert " + rootTask.gid + "]");

        string accessToken = rootTask.notes.Split("AccessToken: ")[1].Split(",")[0];
        var options = new RestClientOptions();
        string taskId = null;
        if (rootTask.notes.Contains("ProjectId"))
        {
            string projectId = rootTask.notes.Split("ProjectId: ")[1];
            options = new RestClientOptions($"https://app.asana.com/api/1.0/tasks?project={projectId}");
        }
        else
        {
            if (rootTask.notes.Contains("TaskId"))
            {
                taskId = rootTask.notes.Split("TaskId: ")[1];
                options = new RestClientOptions($"https://app.asana.com/api/1.0/tasks/{taskId}");
            }
        }

        var client = new RestClient(options);
        var request = new RestRequest("");
        request.AddHeader("accept", "application/json");
        request.AddHeader("authorization", $"Bearer {accessToken}");
        var response = await client.GetAsync(request);

        if (response.IsSuccessStatusCode)
        {
            if (rootTask.notes.Contains("ProjectId"))
            {
                AsanaTasksResponse tasksResponse = JsonSerializer.Deserialize<AsanaTasksResponse>(response.Content);
                foreach (var task in tasksResponse.data)
                {
                    options = new RestClientOptions($"https://app.asana.com/api/1.0/tasks/{task.gid}");
                    client = new RestClient(options);
                    /*
                    request = new RestRequest("");
                    request.AddHeader("accept", "application/json");
                    request.AddHeader("authorization", $"Bearer {accessToken}");
                    */
                    response = await client.GetAsync(request);
                    AsanaTaskResponse taskResponse = JsonSerializer.Deserialize<AsanaTaskResponse>(response.Content);
                    task.notes = taskResponse.data.notes;
                    task.created_at = taskResponse.data.created_at;
                    task.modified_at = taskResponse.data.modified_at;
                    task.due_on = taskResponse.data.due_on;
                    task.completed = taskResponse.data.completed;
                    task.parentid = rootTask.gid;

                    var updTask = tasks.Find(x => x.gid == task.gid);
                    if (updTask != null)
                    {
                        setItemPropertys(task, updTask);
                    }
                    else
                    {
                        tasks.Add(task);
                        hasNews = true;
                    }

                    await ReadSubTasks(task, null);
                }
            }
            else
            {
                await ReadSubTasks(rootTask, taskId);
            }

            //List<MyTask> test = tasks.Where(x => (x.notes != null && x.notes.Contains("[nicht aktualisiert " + rootTask.gid + "]"))).ToList();
            tasks = tasks.Where(x => !(x.notes != null && x.notes.Contains("[nicht aktualisiert " + rootTask.gid + "]"))).ToList();

            SaveItems();

            if (!editStatus)
            {
                // Nicht neu laden, während editiert wird
                LoadCurrentList();
            }

            if (hasNews)
            {
                SetStatus("Es gibt Neuigkeiten. Dazu einfach das Suchsymbol betätigen.");
                hasNews = false;
            }
        }

        void SetTasksToNotRefreshed(MyTask item, string text)
        {
            foreach (var subItem in tasks.Where(x => x.parentid == item.gid))
            {
                subItem.notes += text;
                SetTasksToNotRefreshed(subItem, text);
            }
        }

        async Task ReadSubTasks(MyTask task, string taskId)
        {
            if (taskId == null)
                taskId = task.gid;
            var options = new RestClientOptions($"https://app.asana.com/api/1.0/tasks/{taskId}/subtasks");
            var client = new RestClient(options);
            var request = new RestRequest("");
            request.AddHeader("accept", "application/json");
            request.AddHeader("authorization", $"Bearer {accessToken}");
            var response = await client.GetAsync(request);

            if (response.IsSuccessStatusCode)
            {
                AsanaTasksResponse tasksResponse = JsonSerializer.Deserialize<AsanaTasksResponse>(response.Content);
                foreach (var subtask in tasksResponse.data)
                {
                    string url = $"https://app.asana.com/api/1.0/tasks/{subtask.gid}";
                    options = new RestClientOptions(url);
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

                    // Abhängigkeiten lesen
                    url += "/dependencies";
                    options = new RestClientOptions(url);
                    client = new RestClient(options);
                    response = await client.GetAsync(request);
                    AsanaDependenciesResponse depResponse = JsonSerializer.Deserialize<AsanaDependenciesResponse>(response.Content);
                    for (int i = 0; i < depResponse.data.Length; i++)
                    {
                        subtask.dependencies.Add(depResponse.data[i].gid);
                    } 

                    var updTask = tasks.Find(x => x.gid == subtask.gid);
                    if (updTask != null)
                    {
                        setItemPropertys(subtask, updTask);
                    }
                    else
                    {
                        tasks.Add(subtask);
                    }

                    ReadSubTasks(subtask, null);
                }
            }
            else 
            {
                SetStatus(response.Content.ToString());
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
            if (rootTask.notes != null && rootTask.notes.Contains("AccessToken:"))
            {
                accessToken = rootTask.notes.Split("AccessToken: ")[1].Split(",")[0];
                if (rootTask.notes.Contains("ProjectId"))
                {
                    projectId = rootTask.notes.Split("ProjectId: ")[1];
                }
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

        // Historie / Änderungsprotokoll            
        //if (!currentItem.notes.Contains("AccessToken:"))
        {
            string text = "";
            if (operation == "add")
            {
                text += "Erstellt am " + DateTime.Now.ToShortDateString() + " von " + settings.Email + ".";
                MyTask parent = task;
                while (parent != null && parent.parentid != null)
                {
                    parent = tasks.FirstOrDefault(i => i.gid == parent.parentid);
                    if (parent.notes != null && parent.notes.Contains(text))
                    {
                        text = "";
                    }
                }
            }

            if (oldItem != null && operation == "update")
            {
                if (task.name != oldItem.name)
                {
                    text += "Betreff geändert von " + settings.Email + " am " + DateTime.Now.ToShortDateString()
                        + " von '" + oldItem.name + "' auf '" + task.name + "'. ";
                }
                if (oldItem.due_on == null && task.due_on != null)
                {
                    text += "Termin eingetragen von " + settings.Email + " am " + DateTime.Now.ToShortDateString() + ". ";
                }
                if (oldItem.due_on != null && task.LocalDateString(task.due_on) != task.LocalDateString(oldItem.due_on))
                {
                    text += "Termin geändert von " + settings.Email + " am " + DateTime.Now.ToShortDateString()
                        + " von '" + task.LocalDateString(oldItem.due_on) + "' auf '" + task.LocalDateString(task.due_on) + "'. ";
                }
                if (!oldItem.completed && task.completed)
                {
                    text += "Punkt erledigt von " + settings.Email + " am " + DateTime.Now.ToShortDateString() + ". ";
                }
                if (oldItem.completed && !task.completed)
                {
                    text += "Punkt auf unerledigt von " + settings.Email + " am " + DateTime.Now.ToShortDateString() + ". ";
                }
                oldItem = null;
            }
            if (text != "")
            {
                task.notes += text + " ";
            }
        }


        if (operation == "add" || operation == "update")
        {
            string beginStr = "{\"data\":{\"name\":\"" + task.name 
                + "\",\"notes\":\"" + task.notes
                + "\",\"due_on\":\"" + task.due_on 
                + "\",\"completed\":\"" + task.completed;
            if (operation == "add")
            {
                if (currentItem.notes.Contains("AccessToken:") && projectId != "")
                {
                    request.AddJsonBody(beginStr + "\",\"projects\":[\"" + projectId + "\"]}}", false);
                }
                else
                {
                    string parentItem;
                    if (currentItem.notes.Contains("TaskId: "))
                    {
                        parentItem = currentItem.notes.Split("TaskId: ")[1];
                    }
                    else
                    {
                        parentItem = currentItem.gid;
                    }
                    request.AddJsonBody(beginStr + "\",\"parent\":\"" + parentItem + "\"}}", false);
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
                SetStatus(response.Content.ToString());
            }
            else
            {
                AsanaTaskResponse taskResponse = JsonSerializer.Deserialize<AsanaTaskResponse>(response.Content);
                task.gid = taskResponse.data.gid;
                tasks.Add(task);
                LoadCurrentList();
            }
        }
        if (operation == "update")
        {
            var response = await client.PutAsync(request);
            if (!response.IsSuccessful)
            { 
                SetStatus(response.Content.ToString());
            }
        }
        if (operation == "delete")
        {
            var response = await client.DeleteAsync(request);
        }

        // Abhängigkeiten speichern
        if (operation == "setDependence")
        {
            url = "https://app.asana.com/api/1.0/tasks/" + task.gid + "/addDependencies";
            options = new RestClientOptions(url);
            client = new RestClient(options);
            request.AddJsonBody("{\"data\":{\"dependencies\":" + JsonSerializer.Serialize(task.dependencies) +"}}", false);
            var response = await client.PostAsync(request);
            if (!response.IsSuccessful)
            {
                SetStatus(response.Content.ToString());
            }
        }

    }

    public string ItemPathLeftToRight(MyTask task)
    {
        string path = task.name;
        while (task != null && task.parentid != null)
        {
            task = tasks.FirstOrDefault(i => i.gid == task.parentid);
            if (task != null && task.name != "")
            {
                path = task.name + ">" + path;
            }
        }
        return path;
    }

    public string ItemPathRightToLeft(MyTask task)
    {
        string path = "";
        while (task != null && task.parentid != null)
        {
            task = tasks.FirstOrDefault(i => i.gid == task.parentid);
            if (task != null && task.name != "")
            {
                path += "<" + task.name;
            }
        }
        return path;
    }


    void GetNextDateFromItems(MyTask item)
    {
        if (IsResponsible(item))
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
    }

    /// <summary>
    /// Prüfen, ob man selber zustämdig ist. Wenn nicht, dann den Termin ignorieren
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    bool IsResponsible(MyTask item)
    {
        bool isResponsible = true;
        foreach (var id in item.dependencies)
        {
            MyTask depItem = tasks.Find(x => x.gid == id);
            if (depItem.notes != null && depItem.notes.Contains("@") && !depItem.notes.Contains(settings.Email))
            {
                isResponsible = false;
            }
        }
        return isResponsible;
    }

    void SetStatus(string text)
    {
        if (text == "")
            lblStatus.IsVisible = false;
        else
            lblStatus.IsVisible = true;

        lblStatus.Text = text;
    }

}

