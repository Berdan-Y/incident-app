using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Services;
using Microsoft.Maui.Controls.Maps;
using Shared.Api;
using Shared.Models.Dtos;
using Shared.Models.Enums;
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace Maui.ViewModels;

[QueryProperty(nameof(IncidentId), "id")]
public partial class EditIncidentViewModel : ObservableObject, INotifyPropertyChanged
{
    private readonly IIncidentApi _incidentApi;
    private readonly IGeocodingService _geocodingService;
    private readonly ITokenService _tokenService;
    private Guid _incidentId;
    private string? _reportedByUserId;
    private bool _isLoading;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _address = string.Empty;
    private string _zipCode = string.Empty;
    private Status _status = Status.Todo;
    private Priority _priority = Priority.Unknown;
    private bool _isCreator;
    private bool _isAssignedFieldEmployee;
    private bool _canEditPriority;
    private bool _hasChanges;
    private ObservableCollection<Status> _availableStatuses = new() { Status.Todo, Status.InProgress, Status.Done };

    [ObservableProperty]
    private bool isLoading;

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        Debug.WriteLine($"IsLoading changed to {value}, CanSave: {CanSave}");
    }

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
                HasChanges = true;
            }
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description != value)
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
                HasChanges = true;
            }
        }
    }

    public string Address
    {
        get => _address;
        set
        {
            if (_address != value)
            {
                _address = value;
                OnPropertyChanged(nameof(Address));
                HasChanges = true;
            }
        }
    }

    public string ZipCode
    {
        get => _zipCode;
        set
        {
            if (_zipCode != value)
            {
                _zipCode = value;
                OnPropertyChanged(nameof(ZipCode));
                HasChanges = true;
            }
        }
    }

    public bool HasChanges
    {
        get => _hasChanges;
        set
        {
            if (_hasChanges != value)
            {
                _hasChanges = value;
                OnPropertyChanged(nameof(HasChanges));
                OnPropertyChanged(nameof(CanSave));
                Debug.WriteLine($"HasChanges set to {value}, CanSave: {CanSave}");
            }
        }
    }

    public bool CanSave
    {
        get
        {
            var canSave = !IsLoading && 
                         HasChanges &&
                         !string.IsNullOrWhiteSpace(Title) && 
                         !string.IsNullOrWhiteSpace(Description);
            Debug.WriteLine($"CanSave evaluated: {canSave} (IsLoading: {IsLoading}, HasChanges: {HasChanges}, Title: {!string.IsNullOrWhiteSpace(Title)}, Description: {!string.IsNullOrWhiteSpace(Description)})");
            return canSave;
        }
    }

    [ObservableProperty]
    private bool isAddressValid;

    [ObservableProperty]
    private string addressValidationMessage = string.Empty;

    [ObservableProperty]
    private bool showMap;

    [ObservableProperty]
    private ObservableCollection<Pin> mapPins = new();

    private double? currentLatitude;
    private double? currentLongitude;

    private double? _originalLatitude;
    private double? _originalLongitude;
    private string? _originalAddress;
    private string? _originalZipCode;

    public bool IsCreator
    {
        get => _isCreator;
        private set
        {
            _isCreator = value;
            OnPropertyChanged();
        }
    }

    public bool IsAssignedFieldEmployee
    {
        get => _isAssignedFieldEmployee;
        private set
        {
            _isAssignedFieldEmployee = value;
            OnPropertyChanged();
        }
    }

    public bool CanEditPriority
    {
        get => _canEditPriority;
        private set
        {
            _canEditPriority = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Status> AvailableStatuses
    {
        get => _availableStatuses;
        set
        {
            _availableStatuses = value;
            OnPropertyChanged();
        }
    }

    public string IncidentId
    {
        get => _incidentId.ToString();
        set
        {
            _incidentId = Guid.Parse(value);
            MainThread.BeginInvokeOnMainThread(async () => await LoadIncidentAsync());
        }
    }

    public Status Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                HasChanges = true;
            }
        }
    }

    public Priority Priority
    {
        get => _priority;
        set
        {
            if (_priority != value)
            {
                _priority = value;
                OnPropertyChanged(nameof(Priority));
                HasChanges = true;
            }
        }
    }

    public EditIncidentViewModel(IIncidentApi incidentApi, IGeocodingService geocodingService, ITokenService tokenService)
    {
        Debug.WriteLine("Initializing EditIncidentViewModel");
        _incidentApi = incidentApi;
        _geocodingService = geocodingService;
        _tokenService = tokenService;
        MapPins = new ObservableCollection<Pin>();

        // Initialize properties
        Title = string.Empty;
        Description = string.Empty;
        Address = string.Empty;
        ZipCode = string.Empty;
        AddressValidationMessage = string.Empty;
        Status = Status.Todo;
        Priority = Priority.Unknown;
        HasChanges = false;

        InitializeCommands();
        Debug.WriteLine("EditIncidentViewModel initialized");
    }

    private void InitializeCommands()
    {
        ValidateAddressCommand = new RelayCommand(
            async () =>
            {
                Debug.WriteLine("ValidateAddress command executing");
                await ValidateAddress();
            },
            () => !IsLoading && !string.IsNullOrWhiteSpace(Address) && !string.IsNullOrWhiteSpace(ZipCode)
        );

        SaveCommand = new RelayCommand(
            async () =>
            {
                Debug.WriteLine("Save command executing");
                await SaveAsync();
            },
            () =>
            {
                var can = CanSave;
                Debug.WriteLine($"SaveCommand CanExecute evaluated: {can}");
                return can;
            }
        );

        LoadIncidentCommand = new RelayCommand(
            async () => await LoadIncidentAsync(),
            () => !IsLoading
        );
    }

    public ICommand ValidateAddressCommand { get; private set; }
    public ICommand SaveCommand { get; private set; }
    public ICommand LoadIncidentCommand { get; private set; }

    public async Task LoadIncident(Guid id)
    {
        _incidentId = id;
        await LoadIncidentAsync();
    }

    private async Task LoadIncidentAsync()
    {
        if (_incidentId == Guid.Empty)
            return;

        try
        {
            IsLoading = true;

            var token = _tokenService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                await Shell.Current.DisplayAlert("Error", "Please log in to edit incidents.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            var currentUserRole = jwtToken.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
            var currentUserId = jwtToken.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            var response = await _incidentApi.GetIncidentByIdAsync(_incidentId);
            if (!response.IsSuccessStatusCode || response.Content == null)
            {
                throw new Exception(response.Error?.Content ?? "Failed to load incident");
            }

            var incident = response.Content;
            _reportedByUserId = incident.CreatedBy?.Id.ToString();
            Debug.WriteLine($"Loaded incident with ReportedById: {_reportedByUserId}");

            // Set the properties on the main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Set all properties and force property change notifications
                Title = incident.Title ?? string.Empty;
                OnPropertyChanged(nameof(Title));

                Description = incident.Description ?? string.Empty;
                OnPropertyChanged(nameof(Description));

                Address = incident.Address ?? string.Empty;
                OnPropertyChanged(nameof(Address));

                ZipCode = incident.ZipCode ?? string.Empty;
                OnPropertyChanged(nameof(ZipCode));

                Status = incident.Status;
                OnPropertyChanged(nameof(Status));
                
                Priority = incident.Priority;
                OnPropertyChanged(nameof(Priority));

                // Force refresh of all bindings
                OnPropertyChanged(string.Empty);

                Console.WriteLine($"Set ViewModel properties - Title: {Title}, Description: {Description}, Address: {Address}, ZipCode: {ZipCode}");
                Console.WriteLine($"Set Status: {Status}, Priority: {Priority}");
            });

            // Determine edit permissions
            IsCreator = incident.CreatedBy?.Id.ToString() == currentUserId;
            IsAssignedFieldEmployee = currentUserRole == "FieldEmployee" && 
                                    incident.AssignedTo?.Id.ToString() == currentUserId;
            
            // Only Admin can edit priority
            CanEditPriority = currentUserRole == "Admin";

            Console.WriteLine($"incident.CreatedBy?.Id: {incident.CreatedBy?.Id}, incident.AssignedTo?.Id: {incident.AssignedTo?.Id}");
            Console.WriteLine($"IsCreator: {IsCreator}, IsAssignedFieldEmployee: {IsAssignedFieldEmployee}, CanEditPriority: {CanEditPriority}");

            // If not authorized to edit, go back
            if (!IsCreator && !IsAssignedFieldEmployee)
            {
                await Shell.Current.DisplayAlert("Error", "You are not authorized to edit this incident.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Store original values
            _originalAddress = Address;
            _originalZipCode = ZipCode;
            _originalLatitude = incident.Latitude;
            _originalLongitude = incident.Longitude;
            
            // If we have coordinates, show them on the map
            if (incident.Latitude != 0 && incident.Longitude != 0)
            {
                currentLatitude = incident.Latitude;
                currentLongitude = incident.Longitude;
                await UpdateMapPins(incident.Latitude, incident.Longitude);
                ShowMap = true;
            }

            IsAddressValid = !string.IsNullOrEmpty(Address) && !string.IsNullOrEmpty(ZipCode);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UpdateMapPins(double latitude, double longitude)
    {
        try
        {
            var pin = new Pin
            {
                Location = new Location(latitude, longitude),
                Label = "Incident Location",
                Type = PinType.Place
            };

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MapPins.Clear();
                MapPins.Add(pin);
            });
        }
        catch (Exception ex)
        {
            AddressValidationMessage = $"Error updating map: {ex.Message}";
        }
    }

    private string GetUserFriendlyErrorMessage(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return "Could not validate this address. Please check and try again.";

        // Clean up the error message
        var cleanedMessage = errorMessage
            .Replace("Geocoding Failed:", "")
            .Replace("Geocoding failed:", "")
            .Trim();

        return cleanedMessage.ToUpperInvariant() switch
        {
            "ZERO_RESULTS" => "No matching address found. Please check the address and try again.",
            "INVALID_REQUEST" => "The address information is incomplete. Please provide both street address and zip code.",
            "REQUEST_DENIED" => "Unable to validate address at this time. Please try again later.",
            "OVER_QUERY_LIMIT" => "Address validation service is temporarily unavailable. Please try again later.",
            "UNKNOWN_ERROR" => "An unexpected error occurred. Please try again later.",
            _ => $"Address validation failed: {cleanedMessage}"
        };
    }

    private async Task ValidateAddress()
    {
        Debug.WriteLine($"ValidateAddress called with Address: {Address}, ZipCode: {ZipCode}");
        if (string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(ZipCode))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsAddressValid = false;
                AddressValidationMessage = "Both address and zip code are required";
                ShowMap = false;
                OnPropertyChanged(nameof(IsAddressValid));
                OnPropertyChanged(nameof(AddressValidationMessage));
                OnPropertyChanged(nameof(ShowMap));
            });
            return;
        }

        try
        {
            IsLoading = true;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AddressValidationMessage = "Validating address...";
                OnPropertyChanged(nameof(AddressValidationMessage));
            });

            Debug.WriteLine("Calling geocoding service...");
            var result = await _geocodingService.GeocodeAddressAsync(Address, ZipCode);
            Debug.WriteLine($"Geocoding result - Success: {result.success}, Error: {result.errorMessage}");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (!result.success)
                {
                    IsAddressValid = false;
                    AddressValidationMessage = GetUserFriendlyErrorMessage(result.errorMessage);
                    ShowMap = false;
                }
                else if (result.latitude == null || result.longitude == null)
                {
                    IsAddressValid = false;
                    AddressValidationMessage = "Address found but coordinates are invalid. Please try a different address.";
                    ShowMap = false;
                }
                else
                {
                    IsAddressValid = true;
                    AddressValidationMessage = $"Address validated: {Address}, {ZipCode}";
                    currentLatitude = result.latitude.Value;
                    currentLongitude = result.longitude.Value;
                    await UpdateMapPins(result.latitude.Value, result.longitude.Value);
                    ShowMap = true;
                }

                // Force UI updates
                OnPropertyChanged(nameof(IsAddressValid));
                OnPropertyChanged(nameof(AddressValidationMessage));
                OnPropertyChanged(nameof(ShowMap));
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in ValidateAddress: {ex}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsAddressValid = false;
                AddressValidationMessage = $"Address validation failed: {ex.Message}";
                ShowMap = false;

                // Force UI updates
                OnPropertyChanged(nameof(IsAddressValid));
                OnPropertyChanged(nameof(AddressValidationMessage));
                OnPropertyChanged(nameof(ShowMap));
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        Debug.WriteLine("SaveAsync called");
        try
        {
            IsLoading = true;

            Debug.WriteLine("Creating IncidentPatchDto with only general editable fields");
            var patchDto = new IncidentPatchDto
            {
                Title = Title,
                Description = Description,
                Address = Address,
                ZipCode = ZipCode,
                Latitude = currentLatitude,
                Longitude = currentLongitude
            };

            Debug.WriteLine($"Sending patch for incident {IncidentId} with data: Title={patchDto.Title}, Description={patchDto.Description}, Address={patchDto.Address}, ZipCode={patchDto.ZipCode}");
            var response = await _incidentApi.PatchIncidentAsync(Guid.Parse(IncidentId), patchDto);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Error?.Content ?? "Failed to update incident");
            }

            await Shell.Current.DisplayAlert("Success", "Changes saved successfully.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in SaveAsync: {ex}");
            await Shell.Current.DisplayAlert("Error", "Failed to save changes: " + ex.Message, "OK");
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