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
    MyItem currentItem = new MyItem() { Id = 0 };
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
        currentList = new ObservableCollection<MyItem>(items.Where(i => i.ParenteId == currentItem.Id));

        myListView.ItemsSource = currentList;
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

        File.WriteAllText(Path.Combine(docPath, "MyWorkflowData.json"), JsonSerializer.Serialize(items));

        currentList.Add(item);
    }

    private void myListView_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        currentItem = e.Item as MyItem;
        currentList = new ObservableCollection<MyItem>(items.Where(i => i.ParenteId == currentItem.Id));
        myListView.ItemsSource = currentList;
        entryText.Text = currentItem.Text;
    }
}

