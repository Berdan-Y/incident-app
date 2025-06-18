using Maui.Services;
using Shared.Api;
using Shared.Models.Dtos;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui.ViewModels;

[QueryProperty(nameof(IncidentId), "id")]
public class IncidentDetailsViewModel : INotifyPropertyChanged
{
    private readonly IIncidentApi _incidentApi;
    private readonly ITokenService _tokenService;
    private IncidentResponseDto? _incident;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private string _incidentId = string.Empty;

    public IncidentResponseDto? Incident
    {
        get => _incident;
        set
        {
            _incident = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public string IncidentId
    {
        get => _incidentId;
        set
        {
            _incidentId = value;
            LoadIncidentAsync().ConfigureAwait(false);
        }
    }

    public ICommand RefreshCommand { get; }

    public IncidentDetailsViewModel(IIncidentApi incidentApi, ITokenService tokenService)
    {
        _incidentApi = incidentApi;
        _tokenService = tokenService;

        RefreshCommand = new Command(
            execute: async () => await LoadIncidentAsync(),
            canExecute: () => !IsLoading
        );
    }

    private async Task LoadIncidentAsync()
    {
        if (string.IsNullOrEmpty(IncidentId))
        {
            ErrorMessage = "Invalid incident ID";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var token = _tokenService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "Please log in to view incident details.";
                return;
            }

            var response = await _incidentApi.GetIncidentByIdAsync(Guid.Parse(IncidentId));

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                Incident = response.Content;
                ErrorMessage = string.Empty;
            }
            else if (response.Error != null)
            {
                ErrorMessage = $"Failed to load incident: {response.Error.Content}";
                System.Diagnostics.Debug.WriteLine($"Error loading incident: {response.Error.Content}");
            }
            else
            {
                ErrorMessage = "Failed to load incident. Please try again later.";
                System.Diagnostics.Debug.WriteLine("Error loading incident: Unknown error");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Exception in LoadIncidentAsync: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 