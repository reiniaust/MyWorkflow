﻿using System.Collections.ObjectModel;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Asana.Net;
using System.Text.Json;
//using Newtonsoft.Json;
using RestSharp;
using System.Globalization;
using System.Threading.Tasks;
//using Google.Android.Material.Tabs;
//using Syncfusion.Maui.Inputs;

namespace MyWorkflow;

public partial class MainPage : ContentPage
{
    List<MyTask> tasks;
    List<MyTask> filteredTasks;
    MySettings settings;
    string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    string jsonString;
    MyTask rootItem;
    MyTask currentItem;
    MyTask oldItem;
    MyTask cutItem;
    MyTask linkToItem;
    MyTask userRootItem;
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
    List<MyTask> synonyms;
    List<MyTask> backList;


    public MainPage()
    {
        InitializeComponent();

        //btnPaste.IsVisible = false;

        try
        {
            jsonString = File.ReadAllText(Path.Combine(docPath, "MyWorkflowSettings.json"));
            settings = JsonSerializer.Deserialize<MySettings>(jsonString);
        }
        catch (Exception)
        {
            settings = new MySettings();
            startEamilView.IsVisible = true;
            mainView.IsVisible = false;
        }
        if (mainView.IsVisible)
        {
            StartUp();
        }

    }
    private void StartUp()
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

            // verwaiste Datensätze löschen
            tasks = tasks.Where(x => ItemPathLeftToRight(x) != null).ToList();
            tasks = tasks.Where(x => !(x.notes != null && x.notes.Contains("[nicht aktualisiert"))).ToList();
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
        currentItem.name = "Home";
        rootItem = currentItem;

        userRootItem = tasks.Find(x => x.gid == "-1");
        if (userRootItem == null)
        {
            userRootItem = new MyTask() { gid = "-1", parentid = "", name = "Benutzer" };
            tasks.Add(userRootItem);
        }

        backList = new List<MyTask>();
        editStatus = false;


        ReadAllAssana();

        ReadControlling();

        ReadEmails();

        List<MyTask> userList = tasks.Where(x => x.parentid == userRootItem.gid).ToList();
        userList.Add(new MyTask() { gid = "", name = " " });
        userList = userList.OrderBy(x => x.name).ToList();
        if (pickUserFilter.ItemsSource == null || pickUserFilter.ItemsSource.Count != userList.Count)
        {
            pickUserFilter.ItemsSource = userList;
            pickUserFilter.ItemDisplayBinding = new Binding("name");
        }
        pickUserAssignee.ItemsSource = userList;
        pickUserAssignee.ItemDisplayBinding = new Binding("name");
        if (currentItem.assignee != null)
        {
            pickUserAssignee.SelectedItem = userList.Find(x => x.gid == currentItem.assignee.gid);
        }

        filteredTasks = tasks;
        LoadCurrentList();
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

    private void searchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        searchText = searchBar.Text;
        searchCounter = 0;
    }


    private void editorNotes_TextChanged(object sender, TextChangedEventArgs e)
    {
        setEditStatus();
    }

    private void datePick_Due_on_DateSelected(object sender, DateChangedEventArgs e)
    {
        entry_Due_on.Text = datePick_Due_on.Date.ToString();
        entry_Due_on.Text = entry_Due_on.Text.Substring(0, entry_Due_on.Text.Length - 3); // Sekunden von Datum/Uhrzeit abschneiden
    }

    private void entry_Due_on_TextChanged(object sender, TextChangedEventArgs e)
    {
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

    // Suchen
    private void searchBar_SearchButtonPressed(object sender, EventArgs e)
    {
        searchText = searchText.Replace("*", "");

        MyTask synRoot = tasks.Find(x => x.name == "Synonyme");
        if (synRoot != null)
        {
            synonyms = tasks.Where(x => x.parentid == synRoot.gid).ToList();
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
            if (searchText == "" || wordFound(searchIn(item), word))
            {
                found = true; break;
            }
        }
        if (found && words.Length > 1)
        {
            foreach (var word in words)
            {
                if (!(wordFound((ItemPathLeftToRight(item) + searchIn(item)), word)))
                {
                    found = false; break;
                }
            }
        }
        return found;
    }

    bool isUserFilterInItem(MyTask item, string userId)
    {
        bool found = false;
        while (item != null && item.gid != "")
        {
            if (item.assignee != null && item.assignee.gid == userId || item.due_on != null && !item.completed && item.assignee == null)
            {
                found = true;
                break;
            }
            item = tasks.FirstOrDefault(i => i.gid == item.parentid);
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

    bool wordFound(string text, string word)
    {
        bool found;
        found = text.Contains(word, StringComparison.OrdinalIgnoreCase);
        if (!found)
        {
            MyTask synItem = synonyms.Find(x => x.name.Contains(word, StringComparison.OrdinalIgnoreCase));
            if (synItem != null)
            {
                foreach (string synWord in synItem.name.Split("|"))
                {
                    found = text.Contains(synWord, StringComparison.OrdinalIgnoreCase);
                    if (found)
                    {
                        break;
                    }
                }
            }
        }
        return found;
    }

    private void btnBack_Clicked(object sender, EventArgs e)
    {
        backList.RemoveAt(backList.Count - 1);
        currentItem = backList[backList.Count - 1];
        backList.RemoveAt(backList.Count - 1);
        LoadCurrentList();
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
        SetStatus("Neuer Punkt");
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
        MyTask item = null;
        if (operation == "add")
        {
            item = newItem();
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

    private void btnCopy_Clicked(object sender, EventArgs e)
    {
        string clipboard = currentItem.name;
        if (currentItem.notes != null && currentItem.notes != "" && !currentItem.notes.StartsWith("Erstellt am"))
        {
            clipboard += System.Environment.NewLine + currentItem.notes;
        }

        currentList = new ObservableCollection<MyTask>(tasks.Where(i => i.parentid == currentItem.gid).OrderBy(x => x.OrderDate));
        foreach (var task in currentList)
        {
            clipboard += System.Environment.NewLine + "- " + task.name;
            if (task.notes != null && task.notes != "" && !task.notes.StartsWith("Erstellt am"))
            {
                clipboard += System.Environment.NewLine + "  " + task.notes;
            }
        }

        Clipboard.Default.SetTextAsync(clipboard);

        SetStatus("Der Eintrag wurde in die Zwischenablage kopiert.");
    }

    private void btnCut_Clicked(object sender, EventArgs e)
    {
        cutItem = currentItem;
        btnCut.IsVisible = false;
        //btnPaste.IsVisible = true;
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

    private async void btnPaste_Clicked(object sender, EventArgs e)
    {
        if (cutItem != null) {
            MyTask item = null;
            item = cutItem;
            cutItem.parentid = currentItem.gid;
            btnCut.IsVisible = true;
            cutItem = null;
            //btnPaste.IsVisible = false;
            currentItem = item;
            SaveTask("update", item);
        }
        else
        {
            string clipboard = await Clipboard.GetTextAsync();
            addFromClipboard(clipboard);
        }
    }

    void addFromClipboard(string clipboard)
    {
        MyTask item = null;
        
        if (clipboard != null)
        {
            string[] clipChildren = clipboard.Split("-");
            if (clipChildren.Length > 0)
            {
                string[] clipbArray = clipChildren[0].Split(System.Environment.NewLine);
                if (clipbArray.Length > 0)
                {
                    item = newItem();
                    item.name = clipbArray[0];
                    if (clipbArray.Length > 1)
                    {
                        item.notes = clipbArray[1];
                    }
                    SaveTask("add", item);
                    for (int i = 1; i < clipChildren.Length; i++)
                    {
                        currentItem = item;
                        addFromClipboard(clipChildren[i].Trim());
                    }
                }
            }
        }
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
        searchBar.IsVisible = !editStatus || linkToItem != null;
        entryText.IsVisible = editStatus;
        editorNotes.IsVisible = editStatus || currentItem.notes != "";
        stack_Due_on_Completed.IsVisible = editStatus || currentItem.due_on != null;

        btnBack.IsVisible = !editStatus || linkToItem != null;
        //btnSearch.IsVisible = !editStatus || linkToItem != null;
        if (currentItem.gid == "")
        {
            //btnBack.IsVisible = false;
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
            btnCut.IsVisible = !editStatus;
            btnCopy.IsVisible = !editStatus;
            btnAddLink.IsVisible = !editStatus && linkToItem == null;
            btnDeleteLink.IsVisible = !editStatus && currentItem.dependencies.Count > 0;
            btnDelete.IsVisible = !editStatus;
        }

        btnAdd.IsVisible = !editStatus;
        btnPaste.IsVisible = !editStatus;
        btnSave.IsVisible = editStatus;
        btnCancel.IsVisible = editStatus;
        btnExpandMore.IsVisible = !stackMoreButtons.IsVisible;
        btnExpandLess.IsVisible = stackMoreButtons.IsVisible;
    }

    MyTask newItem()
    {
        return new MyTask()
        {
            gid = random.Next(idFrom, idTo).ToString(),
            parentid = currentItem.gid,
            created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss", CultureInfo.InvariantCulture)
        };
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

        item.assignee = null;
        if (pickUserAssignee.SelectedItem != null)
        {
            MyTask user = (MyTask)pickUserAssignee.SelectedItem;
            if (user.gid != "")
            {
                item.assignee = new() { gid = user.gid, name = user.name };
            }
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
        toItem.assignee = fromItem.assignee;
        toItem.completed = fromItem.completed;
        if (toItem.modified_at != fromItem.modified_at)
        {
            toItem.modified_at = fromItem.modified_at;
            hasNews = true;
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

        MyTask user = (MyTask)pickUserFilter.SelectedItem;
        if (user == null || user.gid == "")
        {
            filteredTasks = tasks;
        }
        else
        {
            filteredTasks = tasks.Where(x => isUserFilterInItem(x, user.gid)).ToList();
            List<MyTask> newTasks = new List<MyTask>();
            newTasks = newTasks.Concat(filteredTasks).ToList();
            foreach (var item in newTasks)
            {
                MyTask parent = tasks.FirstOrDefault(i => i.gid == item.parentid);
                while (parent != null && parent.parentid != null)
                {
                    parent = tasks.FirstOrDefault(i => i.gid == parent.parentid);
                    if (filteredTasks.Find(x => x.gid == parent.gid) == null)
                    {
                        filteredTasks.Add(parent);
                    }
                }
            }
            settings.LastUserId = user.gid;
        }


        backList.Add(currentItem);

        lblCurrentTitle.Text = currentItem.name;

        tasks.ForEach(x =>
        {
            if (x.name == null) x.name = "";
            x.name = x.name.TrimEnd();
            x.next_due_on = null;
            depList.Add(new DependenceItem() { Id = x.gid, NameAndPath = x.name + ItemPathRightToLeft(x) });
            // Vermeidung eines Absturzes durch Endlosschleife
            if(x.parentid == x.gid)
            {
                x.parentid = "";
            }
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

        GetSummary(rootItem);

        currentList = new ObservableCollection<MyTask>(filteredTasks.Where(i => i.parentid == currentItem.gid).OrderBy(x => x.OrderDate));
        foreach (var task in currentList)
        {
            if (task.notes != null && task.notes != "" && !task.notes.StartsWith("Erstellt am") || tasks.Find(x => x.parentid == task.gid) != null)
            {
                // Plus anzeigen, wenn Unterpunkte oder Inhalt hinterlegt ist
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

        // Pfad mit Buttons anzeigen
        PathButtons();


        if (backList.Count() >1)
        {
            btnBack.IsEnabled = true;
        }
        else
        {
            btnBack.IsEnabled = false;
        }
        //editorNotes.Focus();
        loadStatus = false;
    }

    private void PathButtons()
    {
        List<Button> buttons = new List<Button>();
        MyTask item = currentItem;
        while (item != null && item.name != "Home")
        {
            item = tasks.FirstOrDefault(i => i.gid == item.parentid);
            string btnText = item.name;
            if (btnText.Length > 15)
            {
                btnText = btnText.Substring(0, 12) + "...";
            }
            if (item.name == "")
            {
                btnText = "Home";
            }

            Button button = new Button() { Text = btnText };
            button.AutomationId = item.gid;
            button.Clicked += btnPath_Clicked;
            buttons.Add(button);
        }

        stackPath.Clear();
        for (int i = buttons.Count - 1; i >= 0; i--)
        {
            stackPath.Add(buttons[i]);
        }
    }

    private void btnPath_Clicked(object sender, EventArgs e)
    {
        Button btn = sender as Button;
        currentItem = tasks.Find(x => x.gid == btn.AutomationId);
        LoadCurrentList();
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
            if (operation == "add")
		{
    			tasks.Add(item);
		}
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
                    task.assignee = taskResponse.data.assignee;
                    if (task.assignee != null && tasks.Find(x => x.gid == task.assignee.gid) == null)
                    {
                        tasks.Add(new() {parentid = userRootItem.gid, gid = task.assignee.gid, name = task.assignee.name });
                    }
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
                filteredTasks = tasks;
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
            foreach (var subItem in tasks.Where(x => x.parentid == item.gid && x.gid.Length > idTo.ToString().Length))
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
                    subtask.assignee = taskResponse.data.assignee;
                    subtask.completed = taskResponse.data.completed;
                    subtask.parentid = task.gid;

                    // Abhängigkeiten lesen
                    /*
                    url += "/dependencies";
                    options = new RestClientOptions(url);
                    client = new RestClient(options);
                    response = await client.GetAsync(request);
                    AsanaDependenciesResponse depResponse = JsonSerializer.Deserialize<AsanaDependenciesResponse>(response.Content);
                    for (int i = 0; i < depResponse.data.Length; i++)
                    {
                        subtask.dependencies.Add(depResponse.data[i].gid);
                    } 
                    */
                    subtask.dependencies = new List<string>();
                    if (subtask.notes != null && subtask.notes.Contains("|"))
                    {
                        string[] depIds = subtask.notes.Split("|")[1].Split(",");
                        for (int i = 0; i < depIds.Length; i++)
                        {
                            subtask.dependencies.Add(depIds[i]);
                        }
                        subtask.notes = subtask.notes.Split("|")[0];
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

        // Abhängigkeiten speichern
        for (int i = 0; i < task.dependencies.Count; i++)
        {
            if (i == 0)
            {
                task.notes += "|";
            }
            task.notes += task.dependencies[i];
            if (i < task.dependencies.Count - 1)
            {
                task.notes += ",";
            }
        }


        if (operation == "add" || operation == "update")
        {
            string beginStr = "{\"data\":{\"name\":\"" + task.name 
                + "\",\"notes\":\"" + task.notes
                + "\",\"due_on\":\"" + task.due_on
                + "\",\"assignee\":\"" + task.assignee 
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
                filteredTasks = tasks;
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
        /*
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
        */

    }
    public string ItemPathIdsLeftToRight(MyTask task)
    {
        string path = "";
        while (task != null && task.parentid != null)
        {
            task = tasks.FirstOrDefault(i => i.gid == task.parentid);
            if (task != null)
            {
                path = task.gid + path + ",";
            }
        }
        return path;
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
        if (task == null) { path = null; }
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

    /// <summary>
    /// Nächstze offene Termine hochreichen
    /// </summary>
    /// <param name="item"></param>
    void GetNextDateFromItems(MyTask item)
    {
        try
        {
            if (IsResponsible(item))
            {
                foreach (var subItem in filteredTasks.Where(x => x.parentid == item.gid))
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
                        DateTime due_on;
                        if (DateTime.TryParse(item.due_on, out due_on))
                        {
                            if (due_on < DateTime.Parse(item.next_due_on))
                            {
                                item.next_due_on = item.due_on;
                            }
                        }
                    }
                    else
                    {
                        item.next_due_on = item.due_on;
                    }
                }
            }
        }
        catch (Exception)
        {
        }
    }

    // Summen hochreichen
    void GetSummary(MyTask item)
    {
        double summary = 0;
        string unit = "";
        foreach (var subItem in filteredTasks.Where(x => x.parentid == item.gid))
        {
            GetSummary(subItem);
            if (subItem.name != null)
            {
                var split = subItem.name.Split(" ");
                double n;
                if (split.Length > 2 && double.TryParse(split[split.Length - 2], out n) && unit != null)
                {
                    summary += n;
                    unit = split[split.Length - 1];
                }
                else
                {
                    unit = null;
                    break;
                };
            }
        }
        if (summary != 0 && unit != null && item.name != null)
        {
            var split = item.name.Split(" ");
            if (split[split.Length - 1] == unit)
            {
                item.name = "";
                for (int i = 0; i < split.Length - 2; i++)
                {
                    item.name += split[i] + " ";
                }
            }
            summary = Math.Round(summary, 2);
            item.name = item.name.TrimEnd() + " " + summary + " " + unit;
        }
    }


    /// <summary>
    /// Prüfen, ob man selber zuständig ist. Wenn nicht, dann den Termin ignorieren
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    bool IsResponsible(MyTask item)
    {
        bool isResponsible = true;
        foreach (var id in item.dependencies)
        {
            MyTask depItem = tasks.Find(x => x.gid == id);
            if (depItem != null && depItem.notes != null && depItem.notes.Contains("@") && !depItem.notes.Contains(settings.Email))
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


    public async Task ReadControlling()
    {
        var options = new RestClientOptions();
        options = new RestClientOptions("http://localhost:1024/view/v_AuftraegeOffen");

        var client = new RestClient(options);
        var request = new RestRequest("");
        request.AddHeader("accept", "application/json");
        var response = await client.GetAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var kdRoot = tasks.FirstOrDefault(i => i.name == "Kunden");
            if (kdRoot != null)
            {
                foreach (var item in tasks.Where(x => x.parentid == kdRoot.gid && x.name.Contains(":")))
                {
                    var prjNr = item.name.Split(":")[0];
                    MyTask offenRoot;
                    offenRoot = tasks.FirstOrDefault(i => i.parentid == item.gid && i.name.StartsWith("Offen"));
                    if (offenRoot == null)
                    {
                        offenRoot = new() {
                            gid = random.Next(idFrom, idTo).ToString(),
                            parentid = item.gid,
                            name = "Offene Aufträge"
                        };
                        tasks.Add(offenRoot);
                    }

                    tasks = tasks.Where(x => x.parentid != offenRoot.gid).ToList(); // löschen

                    dynamic dynJson = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                    foreach (var i1 in dynJson)
                    {
                        foreach (var i2 in i1)
                        {
                            foreach (var i3 in i2)
                            {
                                foreach (var i4 in i3)
                                {
                                    if (i4.ProjektNr == prjNr)
                                    {
                                        string auftrNr = "Auftrag " + i4.HotlineNr + ": ";
                                        MyTask auftr;
                                        auftr = tasks.FirstOrDefault(i => i.name.StartsWith(auftrNr));
                                        if (auftr == null)
                                        {
                                            auftr = new();
                                            auftr.gid = random.Next(idFrom, idTo).ToString();
                                            tasks.Add(auftr);
                                        }
                                        auftr.parentid = offenRoot.gid;
                                        auftr.name = auftrNr + i4.Titel;
                                        if (auftr.name.Length > 100)
                                        {
                                            auftr.name = auftr.name.Substring(0, 99) + "...";
                                            auftr.notes = "..." + auftr.name.Substring(100);
                                        }
                                        auftr.name += " " + i4.AngebDm + " €";
                                        auftr.created_at = MyDateConvert(i4.BestellDatum);
                                        if (i4.AendDatum != null)
                                        {
                                            auftr.modified_at = MyDateConvert(i4.AendDatum);
                                        }
                                        if (i4.Termin != null)
                                        {
                                            auftr.due_on = MyDateConvert(i4.Termin);
                                        }
                                    }
                                }
                                break;
                            }
                            break;
                        }
                        break;
                    }
                }
            }
        }

        // MitarbeiterKtl
        options = new RestClientOptions("http://localhost:1024/view/v_Ktl_TtkOhneAuftr_Ab2015_TextLaengeGr50");
        client = new RestClient(options);
        response = await client.GetAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var ktlRoot = tasks.FirstOrDefault(i => i.name.StartsWith("Buchungen") && ItemPathLeftToRight(i).Contains("Service"));
            if (ktlRoot != null)
            {
                dynamic dynJson = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                foreach (var i1 in dynJson)
                {
                    foreach (var i2 in i1)
                    {
                        foreach (var i3 in i2)
                        {
                            int maxId = 1362941;
                            foreach (MyTask buchung in tasks.Where(x => x.parentid == ktlRoot.gid))
                            {
                                if (int.Parse(buchung.gid) > maxId)
                                {
                                    maxId = int.Parse(buchung.gid);
                                }
                            }

                            foreach (var i4 in i3)
                            {
                                string ktlId = i4.SatzId;
                                if (int.Parse(ktlId) > maxId)
                                {
                                    MyTask ttk = new();
                                    ttk.gid = i4.SatzId;
                                    ttk.parentid = ktlRoot.gid;
                                    ttk.name = i4.Kuerzel + ": " + i4.Taetigkeit;
                                    string kd = i4.Kunde;
                                    if (kd != null)
                                    {
                                        ttk.name += ": " + kd;
                                    }
                                    string dauer = i4.DauerStd;
                                    dauer = dauer.Replace(".", ",");
                                    ttk.name += ": " + Math.Round(float.Parse(dauer),2) + " Std";
                                    ttk.notes = i4.Text;
                                    ttk.created_at = MyDateConvert(i4.Datum);
                                    tasks.Add(ttk);
                                }
                            }
                            break;
                        }
                        break;
                    }
                    break;
                }
            }
        }

        SaveItems();

        string MyDateConvert(string dateString)
        {
            string[] d = ((string)dateString).Split("/");
            DateTime date = DateTime.Parse(d[1] + "." + d[0] + "." + d[2]);
            return date.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'").Split(" ")[0];
        }
    }

    public async Task ReadEmails()
    {
        MyTask mailSettings = tasks.Find(x => x.name == "Posteingang (IMAP)");
        if (mailSettings != null)
        {
            string[] settings = mailSettings.notes.Split(",");
            var emailReaderService = new EmailReaderService();
            var emails = await emailReaderService.GetEmailsAsync(settings[0], int.Parse(settings[1]), settings[2].Trim(), settings[3].Trim());

            foreach (var email in emails)
            {
                if (email.From.ToString().Split("<").Count() > 1)
                {
                    MyTask mailFrom = tasks.Find(x => x.name.Contains(email.From.ToString().Split("<")[1]));
                    if (mailFrom != null)
                    {
                        MyTask mailRoot = tasks.Find(x => x.parentid == mailFrom.gid && x.name == "E-Mails");
                        if (mailRoot == null) { 
                            mailRoot = new()
                            {
                                gid = random.Next(idFrom, idTo).ToString(),
                                parentid = mailFrom.gid,
                                name = "E-Mails"
                            };
                            tasks.Add(mailRoot);
                        }
                        MyTask mail = new()
                        {
                            gid = random.Next(idFrom, idTo).ToString(),
                            parentid = mailRoot.gid,
                            created_at = email.Date.ToString("yyyy-MM-ddTHH\\:mm\\:ss", CultureInfo.InvariantCulture),
                            name = email.Subject,
                            notes = email.Body.ToString().Split("Mit freundl")[0],
                        };
                        if (mail.notes.Split("8bit").Count() > 1)
                        {
                            mail.notes = mail.notes.Split("8bit")[1].Trim();
                        }
                        if (mail.notes.Split("printable").Count() > 1)
                        {
                            mail.notes = mail.notes.Split("printable")[1].Trim();
                        }
                        tasks.Add(mail);
                    }
                }
            }
        }
    }

    private void pickUserFilter_SelectedIndexChanged(object sender, EventArgs e)
    {
        LoadCurrentList();
    }
}

