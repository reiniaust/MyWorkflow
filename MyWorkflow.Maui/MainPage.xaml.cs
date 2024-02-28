using System.Collections.ObjectModel;
using MyWorkflow.Maui.Models;
using System.Text.Json;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace MyWorkflow.Maui;

public partial class MainPage : ContentPage
{
    List<MyItem> items;
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
            items = new List<MyItem>(JsonSerializer.Deserialize<MyItem[]>(jsonString));
        }
        catch (Exception)
        {
            items = new List<MyItem>();
        }
        currentItem = items.Find(x => x.Id == 0);
        if (currentItem == null)
        {
            currentItem = new MyItem() { Id = 0 };
            items.Add(currentItem);
        }


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

        currentList.Add(item);
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
            btnBack.IsVisible = true;
        }
        else { 
            btnBack.IsVisible = false; 
        }
    }

    private void SaveItems()
    {
        File.WriteAllText(Path.Combine(docPath, "MyWorkflowData.json"), JsonSerializer.Serialize(items));
    }
}

