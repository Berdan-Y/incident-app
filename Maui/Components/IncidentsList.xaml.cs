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
        Debug.WriteLine("IncidentsList initialized");

        // Add handler for binding context changes
        this.BindingContextChanged += OnBindingContextChanged;
    }

    private void OnBindingContextChanged(object sender, EventArgs e)
    {
        var parentPage = GetParentPage();
        if (parentPage != null)
        {
            Debug.WriteLine($"IncidentsList BindingContext changed. Parent page type: {parentPage.GetType().Name}");
            Debug.WriteLine($"Parent page BindingContext type: {parentPage.BindingContext?.GetType().Name}");

            if (parentPage.BindingContext is BaseIncidentsViewModel vm)
            {
                Debug.WriteLine($"Commands available on parent: ViewDetails={vm.ViewIncidentDetailsCommand != null}, Edit={vm.EditIncidentCommand != null}, Delete={vm.DeleteIncidentCommand != null}");
            }
        }
        else
        {
            Debug.WriteLine("IncidentsList: Could not find parent page");
        }
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
        Debug.WriteLine("OnViewDetailsClicked called");
        if (sender is Button button && button.CommandParameter is IncidentResponseDto incident)
        {
            var parentPage = GetParentPage();
            if (parentPage?.BindingContext is BaseIncidentsViewModel vm)
            {
                Debug.WriteLine($"Executing ViewIncidentDetailsCommand for incident {incident.Id}");
                vm.ViewIncidentDetailsCommand.Execute(incident);
            }
            else
            {
                Debug.WriteLine("Could not find parent page or ViewModel");
            }
        }
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        Debug.WriteLine("OnEditClicked called");
        if (sender is Button button && button.CommandParameter is IncidentResponseDto incident)
        {
            var parentPage = GetParentPage();
            if (parentPage?.BindingContext is BaseIncidentsViewModel vm)
            {
                Debug.WriteLine($"Executing EditIncidentCommand for incident {incident.Id}");
                vm.EditIncidentCommand.Execute(incident);
            }
            else
            {
                Debug.WriteLine("Could not find parent page or ViewModel");
            }
        }
    }

    private void OnDeleteClicked(object sender, EventArgs e)
    {
        Debug.WriteLine("OnDeleteClicked called");
        if (sender is Button button && button.CommandParameter is IncidentResponseDto incident)
        {
            var parentPage = GetParentPage();
            if (parentPage?.BindingContext is BaseIncidentsViewModel vm)
            {
                Debug.WriteLine($"Executing DeleteIncidentCommand for incident {incident.Id}");
                vm.DeleteIncidentCommand.Execute(incident);
            }
            else
            {
                Debug.WriteLine("Could not find parent page or ViewModel");
            }
        }
    }

    private void OnIncidentSelected(object sender, SelectionChangedEventArgs e)
    {
        Debug.WriteLine("OnIncidentSelected called");
        // Reset selection
        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }
}