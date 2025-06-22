using Microsoft.Maui.Controls;
using System.Diagnostics;
using Maui.ViewModels;
using Shared.Models.Dtos;

namespace Maui.Components;

public partial class IncidentsList : ContentView
{
    public IncidentsList()
    {
        InitializeComponent();

        // Add handler for binding context changes
    }

    private Page GetParentPage()
    {
        Element parent = this;
        while (parent != null && !(parent is Page))
        {
            parent = parent.Parent;
        }
        return parent as Page;
    }

    private void OnViewDetailsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IncidentResponseDto incident)
        {
            var parentPage = GetParentPage();
            if (parentPage?.BindingContext is BaseIncidentsViewModel vm)
            {
                vm.ViewIncidentDetailsCommand.Execute(incident);
            }
        }
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IncidentResponseDto incident)
        {
            var parentPage = GetParentPage();
            if (parentPage?.BindingContext is BaseIncidentsViewModel vm)
            {
                vm.EditIncidentCommand.Execute(incident);
            }
        }
    }

    private void OnFieldEmployeeEditClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IncidentResponseDto incident)
        {
            var parentPage = GetParentPage();
            if (parentPage?.BindingContext is BaseIncidentsViewModel vm)
            {
                vm.FieldEmployeeEditCommand.Execute(incident);
            }
        }
    }

    private void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IncidentResponseDto incident)
        {
            var parentPage = GetParentPage();
            if (parentPage?.BindingContext is BaseIncidentsViewModel vm)
            {
                vm.DeleteIncidentCommand.Execute(incident);
            }
        }
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