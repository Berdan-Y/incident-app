using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Models.Dtos;

namespace Maui.ViewModels;

public abstract partial class BaseIncidentsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage;

    [ObservableProperty]
    private string emptyMessage;

    [ObservableProperty]
    private ObservableCollection<IncidentResponseDto> incidents;

    public ICommand ViewIncidentDetailsCommand { get; protected set; }

    protected BaseIncidentsViewModel()
    {
        Incidents = new ObservableCollection<IncidentResponseDto>();
    }
} 