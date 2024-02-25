using System.Collections.ObjectModel;
using MyWorkflow.Maui.Models;

namespace MyWorkflow.Maui;

public partial class MainPage : ContentPage
{
    ObservableCollection<MyItem> items = new ObservableCollection<MyItem>();

    public MainPage()
	{
		InitializeComponent();

        myListView.ItemsSource = items;
	}

    private void btnAdd_Clicked(object sender, EventArgs e)
    {
        items.Add(new MyItem() { Text = entryText.Text });
        entryText.Text = "";
    }
}

