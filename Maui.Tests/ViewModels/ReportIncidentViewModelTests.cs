using Maui.Core.ViewModels;
using Moq;
using Refit;
using Shared.Api;
using Shared.Models.Dtos;
using System.Net.Http.Headers;
using Xunit;
using Shared.Services;
using System.ComponentModel;

namespace Maui.Tests.ViewModels;

public class TestReportIncidentViewModel : ReportIncidentViewModelBase
{
    public TestReportIncidentViewModel(
        IIncidentApi incidentApi,
        ITokenService tokenService,
        IGeocodingService geocodingService)
        : base(incidentApi, tokenService, geocodingService)
    {
    }
}

public class MockTokenService : ITokenService
{
    private bool _isLoggedIn;
    private bool _isInitializing;
    private List<string> _roles = new();
    private string? _userId;
    private string? _userName;
    private string? _email;
    private string? _token;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LoggedIn;
    public event EventHandler? LoggedOut;

    public bool IsLoggedIn => _isLoggedIn;
    public bool IsInitializing => _isInitializing;
    public List<string> Roles => _roles;

    public void SetIsLoggedIn(bool value)
    {
        _isLoggedIn = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoggedIn)));
    }

    public void SetUserId(string? userId)
    {
        _userId = userId;
    }

    public string? GetEmail() => _email;
    public List<string> GetRoles() => _roles;
    public string? GetToken() => _token;
    public string? GetUserId() => _userId;
    public string? GetUserName() => _userName;
    public bool HasRole(string role) => _roles.Contains(role);

    public Task LogoutAsync()
    {
        _isLoggedIn = false;
        _roles.Clear();
        _token = null;
        _userId = null;
        _userName = null;
        _email = null;
        LoggedOut?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task SetRolesAsync(string roles)
    {
        _roles = roles.Split(',').ToList();
        return Task.CompletedTask;
    }

    public Task SetTokenAsync(string token)
    {
        _token = token;
        _isLoggedIn = true;
        LoggedIn?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}

public class ReportIncidentViewModelTests
{
    private readonly Mock<IIncidentApi> _incidentApiMock;
    private readonly MockTokenService _tokenService;
    private readonly Mock<IGeocodingService> _geocodingServiceMock;
    private readonly TestReportIncidentViewModel _viewModel;

    public ReportIncidentViewModelTests()
    {
        _incidentApiMock = new Mock<IIncidentApi>();
        _tokenService = new MockTokenService();
        _geocodingServiceMock = new Mock<IGeocodingService>();

        _viewModel = new TestReportIncidentViewModel(
            _incidentApiMock.Object,
            _tokenService,
            _geocodingServiceMock.Object);
    }

    [Fact]
    public async Task SubmitReport_WithEmptyTitle_ReturnsError()
    {
        // Arrange
        _viewModel.Title = string.Empty;
        _viewModel.Description = "Test description";

        // Act
        var result = await _viewModel.SubmitReportAsync();

        // Assert
        Assert.False(result.success);
        Assert.Equal("Title is required.", result.message);
        Assert.False(_viewModel.IsLoading);
        _incidentApiMock.Verify(x => x.CreateIncidentAsync(It.IsAny<MultipartFormDataContent>()), Times.Never);
    }

    [Fact]
    public async Task SubmitReport_WithEmptyDescription_ReturnsError()
    {
        // Arrange
        _viewModel.Title = "Test Title";
        _viewModel.Description = string.Empty;

        // Act
        var result = await _viewModel.SubmitReportAsync();

        // Assert
        Assert.False(result.success);
        Assert.Equal("Description is required.", result.message);
        Assert.False(_viewModel.IsLoading);
        _incidentApiMock.Verify(x => x.CreateIncidentAsync(It.IsAny<MultipartFormDataContent>()), Times.Never);
    }

    [Fact]
    public async Task SubmitReport_WithCurrentLocationAndNoCoordinates_ReturnsError()
    {
        // Arrange
        _viewModel.Title = "Test Title";
        _viewModel.Description = "Test Description";
        _viewModel.UseCurrentLocation = true;
        _viewModel.Latitude = 0;
        _viewModel.Longitude = 0;

        // Act
        var result = await _viewModel.SubmitReportAsync();

        // Assert
        Assert.False(result.success);
        Assert.Equal("Location could not be determined. Please try again or use manual entry.", result.message);
        Assert.False(_viewModel.IsLoading);
        _incidentApiMock.Verify(x => x.CreateIncidentAsync(It.IsAny<MultipartFormDataContent>()), Times.Never);
    }

    [Fact]
    public async Task SubmitReport_WithManualLocationAndNoAddress_ReturnsError()
    {
        // Arrange
        _viewModel.Title = "Test Title";
        _viewModel.Description = "Test Description";
        _viewModel.UseManualLocation = true;
        _viewModel.Address = string.Empty;
        _viewModel.Zipcode = string.Empty;

        // Act
        var result = await _viewModel.SubmitReportAsync();

        // Assert
        Assert.False(result.success);
        Assert.Equal("Address and Zipcode are required when setting location manually.", result.message);
        Assert.False(_viewModel.IsLoading);
        _incidentApiMock.Verify(x => x.CreateIncidentAsync(It.IsAny<MultipartFormDataContent>()), Times.Never);
    }

    [Fact]
    public async Task SubmitReport_WithValidManualLocation_ValidatesAddress()
    {
        // Arrange
        _viewModel.Title = "Test Title";
        _viewModel.Description = "Test Description";
        _viewModel.UseManualLocation = true;
        _viewModel.Address = "123 Test St";
        _viewModel.Zipcode = "12345";

        _geocodingServiceMock.Setup(x => x.GeocodeAddressAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((true, 40.0, -70.0, null));

        var response = new ApiResponse<IncidentResponseDto>(
            new HttpResponseMessage(System.Net.HttpStatusCode.OK),
            new IncidentResponseDto 
            { 
                Title = "Test Title",
                Description = "Test Description",
                Status = Shared.Models.Enums.Status.Todo,
                Priority = Shared.Models.Enums.Priority.Low
            },
            new RefitSettings());

        _incidentApiMock.Setup(x => x.CreateIncidentAsync(It.IsAny<MultipartFormDataContent>()))
            .Returns(Task.FromResult<IApiResponse<IncidentResponseDto>>(response));

        // Act
        var result = await _viewModel.SubmitReportAsync();

        // Assert
        Assert.True(result.success);
        Assert.Equal("Incident report submitted successfully!", result.message);
        Assert.False(_viewModel.IsLoading);
        Assert.Equal(40.0, _viewModel.Latitude);
        Assert.Equal(-70.0, _viewModel.Longitude);
        _geocodingServiceMock.Verify(x => x.GeocodeAddressAsync("123 Test St", "12345"), Times.Once);
        _incidentApiMock.Verify(x => x.CreateIncidentAsync(It.IsAny<MultipartFormDataContent>()), Times.Once);
    }

    [Fact]
    public async Task SubmitReport_WithInvalidAddress_ReturnsError()
    {
        // Arrange
        _viewModel.Title = "Test Title";
        _viewModel.Description = "Test Description";
        _viewModel.UseManualLocation = true;
        _viewModel.Address = "Invalid Address";
        _viewModel.Zipcode = "12345";

        _geocodingServiceMock.Setup(x => x.GeocodeAddressAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, null, null, "Invalid address"));

        // Act
        var result = await _viewModel.SubmitReportAsync();

        // Assert
        Assert.False(result.success);
        Assert.Equal("Invalid address", result.message);
        Assert.False(_viewModel.IsLoading);
        _incidentApiMock.Verify(x => x.CreateIncidentAsync(It.IsAny<MultipartFormDataContent>()), Times.Never);
    }

    [Fact]
    public async Task SubmitReport_WithPhotos_IncludesPhotosInRequest()
    {
        // Arrange
        _viewModel.Title = "Test Title";
        _viewModel.Description = "Test Description";
        _viewModel.UseCurrentLocation = true;
        _viewModel.Latitude = 40.0;
        _viewModel.Longitude = -70.0;

        var photoData = new byte[] { 1, 2, 3, 4, 5 };
        _viewModel.Photos.Add(new PhotoItemBase(photoData, "test.jpg", "image/jpeg"));

        var response = new ApiResponse<IncidentResponseDto>(
            new HttpResponseMessage(System.Net.HttpStatusCode.OK),
            new IncidentResponseDto 
            { 
                Title = "Test Title",
                Description = "Test Description",
                Status = Shared.Models.Enums.Status.Todo,
                Priority = Shared.Models.Enums.Priority.Low
            },
            new RefitSettings());

        _incidentApiMock.Setup(x => x.CreateIncidentAsync(It.IsAny<MultipartFormDataContent>()))
            .Returns(Task.FromResult<IApiResponse<IncidentResponseDto>>(response));

        // Act
        var result = await _viewModel.SubmitReportAsync();

        // Assert
        Assert.True(result.success);
        Assert.Equal("Incident report submitted successfully!", result.message);
        Assert.False(_viewModel.IsLoading);
        _incidentApiMock.Verify(x => x.CreateIncidentAsync(It.Is<MultipartFormDataContent>(content => 
            content.Any(c => c.Headers.ContentDisposition != null && 
                           c.Headers.ContentDisposition.Name != null && 
                           c.Headers.ContentDisposition.Name.Contains("photos")))), Times.Once);
    }

    [Fact]
    public async Task SubmitReport_WhenAnonymous_DoesNotIncludeUserId()
    {
        // Arrange
        _viewModel.Title = "Test Title";
        _viewModel.Description = "Test Description";
        _viewModel.UseCurrentLocation = true;
        _viewModel.Latitude = 40.0;
        _viewModel.Longitude = -70.0;
        _viewModel.IsAnonymous = true;

        _tokenService.SetIsLoggedIn(true);
        _tokenService.SetUserId("user-id");

        var response = new ApiResponse<IncidentResponseDto>(
            new HttpResponseMessage(System.Net.HttpStatusCode.OK),
            new IncidentResponseDto 
            { 
                Title = "Test Title",
                Description = "Test Description",
                Status = Shared.Models.Enums.Status.Todo,
                Priority = Shared.Models.Enums.Priority.Low
            },
            new RefitSettings());

        _incidentApiMock.Setup(x => x.CreateIncidentAsync(It.IsAny<MultipartFormDataContent>()))
            .Returns(Task.FromResult<IApiResponse<IncidentResponseDto>>(response));

        // Act
        var result = await _viewModel.SubmitReportAsync();

        // Assert
        Assert.True(result.success);
        _incidentApiMock.Verify(x => x.CreateIncidentAsync(It.Is<MultipartFormDataContent>(content => 
            content.Any(c => c.Headers.ContentDisposition != null && 
                           c.Headers.ContentDisposition.Name != null && 
                           c.Headers.ContentDisposition.Name.Contains("incident")))), Times.Once);
    }

    [Fact]
    public async Task ValidateAndGeocodeAddress_WithValidAddress_UpdatesCoordinates()
    {
        // Arrange
        _viewModel.Address = "123 Test St";
        _viewModel.Zipcode = "12345";

        _geocodingServiceMock.Setup(x => x.GeocodeAddressAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((true, 40.0, -70.0, null));

        // Act
        var result = await _viewModel.ValidateAndGeocodeAddressAsync();

        // Assert
        Assert.True(result.success);
        Assert.Equal("Address validated successfully!", result.message);
        Assert.Equal(40.0, _viewModel.Latitude);
        Assert.Equal(-70.0, _viewModel.Longitude);
        Assert.Equal("Address validated successfully!", _viewModel.LocationStatus);
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public async Task ValidateAndGeocodeAddress_WithInvalidAddress_ReturnsError()
    {
        // Arrange
        _viewModel.Address = "Invalid Address";
        _viewModel.Zipcode = "12345";

        _geocodingServiceMock.Setup(x => x.GeocodeAddressAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, null, null, "Invalid address"));

        // Act
        var result = await _viewModel.ValidateAndGeocodeAddressAsync();

        // Assert
        Assert.False(result.success);
        Assert.Equal("Invalid address", result.message);
        Assert.False(_viewModel.IsLoading);
    }
} 