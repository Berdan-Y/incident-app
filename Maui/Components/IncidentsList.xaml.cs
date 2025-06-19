using Microsoft.Maui.Controls;

namespace Maui.Components;

public partial class IncidentsList : ContentView
{
    public IncidentsList()
    {
        InitializeComponent();
    }

    private void OnIncidentSelected(object sender, SelectionChangedEventArgs e)
    {
        // Reset selection
        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }
} 