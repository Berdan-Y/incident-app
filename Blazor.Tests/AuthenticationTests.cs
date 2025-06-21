using Bunit;
using Xunit;
using Moq;
using Blazor.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Blazor.Pages;
using Microsoft.AspNetCore.Components;
using Shared.Models.Dtos;
using Shared.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Blazor.Tests;

public class AuthenticationTests : TestContext
{
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly Mock<ISnackbar> _snackbarMock;
    private readonly TestNavigationManager _navigationManager;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private string? _lastSnackbarMessage;
    private Severity _lastSnackbarSeverity;

    public AuthenticationTests()
    {
        // Create all mocks first
        _authServiceMock = new Mock<IAuthenticationService>();
        _snackbarMock = new Mock<ISnackbar>();
        _tokenServiceMock = new Mock<ITokenService>();

        // Setup snackbar mock to capture messages
        _snackbarMock
            .Setup(x => x.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
            .Callback((string message, Severity severity, Action<SnackbarOptions> _, string __) =>
            {
                _lastSnackbarMessage = message;
                _lastSnackbarSeverity = severity;
            });

        // Register all services first
        Services.AddSingleton(_authServiceMock.Object);
        Services.AddSingleton(_snackbarMock.Object);
        Services.AddSingleton(_tokenServiceMock.Object);
        Services.AddMudServices();
        TestNavigationManager.Register(Services);

        // Then get the navigation manager
        _navigationManager = (TestNavigationManager)Services.GetRequiredService<NavigationManager>();

        // Mock JSInterop calls
        JSInterop.SetupVoid("mudPopover.initialize", _ => true);
        JSInterop.SetupVoid("mudPopover.connect", _ => true);
        JSInterop.SetupVoid("mudPopover.disconnect", _ => true);
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
        JSInterop.SetupVoid("mudKeyInterceptor.disconnect", _ => true);
        JSInterop.SetupVoid("mudSelectState.restoreState", _ => true);
        JSInterop.SetupVoid("mudSelectState.saveState", _ => true);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldNavigateToHome()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "password123" };
        _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(true);
        _authServiceMock.Setup(x => x.IsAdmin())
            .Returns(true);

        var cut = RenderComponent<Login>();
        var form = cut.FindComponent<MudForm>();
        var emailInput = cut.FindComponent<MudTextField<string>>().Find("input");
        var passwordInput = cut.FindComponents<MudTextField<string>>()[1].Find("input");
        var loginButton = cut.FindComponent<MudButton>();

        // Act
        await cut.InvokeAsync(() => emailInput.Change(loginDto.Email));
        await cut.InvokeAsync(() => passwordInput.Change(loginDto.Password));
        
        // Validate the form
        await cut.InvokeAsync(() => form.Instance.Validate());

        // Set up a TaskCompletionSource to track when navigation occurs
        var navigationOccurred = new TaskCompletionSource<bool>();
        _navigationManager.LocationChanged += (sender, args) => navigationOccurred.SetResult(true);

        await cut.InvokeAsync(() => loginButton.Instance.OnClick.InvokeAsync());

        // Wait for navigation to occur with a timeout
        await Task.WhenAny(navigationOccurred.Task, Task.Delay(TimeSpan.FromSeconds(10)));

        // Assert
        _authServiceMock.Verify(x => x.LoginAsync(It.Is<LoginDto>(dto => 
            dto.Email == loginDto.Email && 
            dto.Password == loginDto.Password)), Times.Once);
        Assert.Equal("http://localhost/", _navigationManager.Uri);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldShowError()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "invalid@example.com", Password = "wrongpassword" };
        _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(false);

        var cut = RenderComponent<Login>();
        var form = cut.FindComponent<MudForm>();
        var emailInput = cut.FindComponent<MudTextField<string>>().Find("input");
        var passwordInput = cut.FindComponents<MudTextField<string>>()[1].Find("input");
        var loginButton = cut.FindComponent<MudButton>();

        // Store initial URI
        var initialUri = _navigationManager.Uri;

        // Act
        await cut.InvokeAsync(() => emailInput.Change(loginDto.Email));
        await cut.InvokeAsync(() => passwordInput.Change(loginDto.Password));
        
        // Set up snackbar mock before clicking the button
        var snackbarMessage = string.Empty;
        var snackbarSeverity = Severity.Normal;
        var snackbarCalled = new TaskCompletionSource<bool>();
        
        _snackbarMock
            .Setup(x => x.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
            .Callback((string message, Severity severity, Action<SnackbarOptions> _, string __) =>
            {
                snackbarMessage = message;
                snackbarSeverity = severity;
                snackbarCalled.SetResult(true);
            });

        // Validate the form
        await cut.InvokeAsync(() => form.Instance.Validate());
        await cut.InvokeAsync(() => loginButton.Instance.OnClick.InvokeAsync());

        // Wait for the snackbar to be called with a timeout
        await Task.WhenAny(snackbarCalled.Task, Task.Delay(TimeSpan.FromSeconds(5)));

        // Assert
        _authServiceMock.Verify(x => x.LoginAsync(It.Is<LoginDto>(dto => 
            dto.Email == loginDto.Email && 
            dto.Password == loginDto.Password)), Times.Once);
        Assert.Equal("Invalid email or password", snackbarMessage);
        Assert.Equal(Severity.Error, snackbarSeverity);

        // Verify we didn't navigate by checking the URI hasn't changed
        Assert.Equal(initialUri, _navigationManager.Uri);
    }

    [Fact]
    public async Task Register_WithValidData_ShouldRegisterAndNavigateToHome()
    {
        // Arrange
        var registerDto = new RegisterDto 
        { 
            Email = "newuser@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe",
            Role = Shared.Models.Enums.Role.Member
        };

        _authServiceMock.Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync(true);

        // Initialize navigation manager with base URI
        var navManager = Services.GetRequiredService<NavigationManager>();
        navManager.NavigateTo(navManager.BaseUri + "register");

        var cut = RenderComponent<Register>();
        var form = cut.FindComponent<MudForm>();
        var inputs = cut.FindComponents<MudTextField<string>>();
        var emailField = inputs[2].Instance;

        // Set up snackbar mock
        var snackbarMessages = new List<(string Message, Severity Severity)>();
        
        _snackbarMock
            .Setup(x => x.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
            .Callback((string message, Severity severity, Action<SnackbarOptions> _, string __) =>
            {
                snackbarMessages.Add((message, severity));
            });

        // Set up navigation tracking
        var navigationOccurred = new TaskCompletionSource<bool>();
        _navigationManager.LocationChanged += (sender, args) => 
        {
            if (args.Location == _navigationManager.BaseUri)
            {
                navigationOccurred.SetResult(true);
            }
        };

        // Act - Fill in fields and trigger validation
        await cut.InvokeAsync(() => emailField.ValueChanged.InvokeAsync(registerDto.Email));
        await cut.InvokeAsync(() => emailField.Validate());

        await cut.InvokeAsync(() => inputs[0].Instance.ValueChanged.InvokeAsync(registerDto.FirstName));
        await cut.InvokeAsync(() => inputs[0].Instance.Validate());

        await cut.InvokeAsync(() => inputs[1].Instance.ValueChanged.InvokeAsync(registerDto.LastName));
        await cut.InvokeAsync(() => inputs[1].Instance.Validate());

        await cut.InvokeAsync(() => inputs[3].Instance.ValueChanged.InvokeAsync(registerDto.Password));
        await cut.InvokeAsync(() => inputs[3].Instance.Validate());

        await cut.InvokeAsync(() => inputs[4].Instance.ValueChanged.InvokeAsync(registerDto.Password));
        await cut.InvokeAsync(() => inputs[4].Instance.Validate());

        // Validate the entire form
        await cut.InvokeAsync(() => form.Instance.Validate());

        // Wait for validation to complete
        await Task.Delay(100);

        // Click the register button
        var registerButton = cut.FindComponent<MudButton>();
        await cut.InvokeAsync(() => registerButton.Instance.OnClick.InvokeAsync());

        // Wait for navigation with a timeout
        await Task.WhenAny(navigationOccurred.Task, Task.Delay(TimeSpan.FromSeconds(5)));

        // Wait a bit for any final snackbar messages
        await Task.Delay(100);

        // Assert
        _authServiceMock.Verify(x => x.RegisterAsync(It.Is<RegisterDto>(dto => 
            dto.Email == registerDto.Email && 
            dto.Password == registerDto.Password &&
            dto.FirstName == registerDto.FirstName &&
            dto.LastName == registerDto.LastName)), Times.Once);

        Assert.True(form.Instance.IsValid);
        
        var successMessage = snackbarMessages.LastOrDefault();
        Assert.Equal("User registered successfully", successMessage.Message);
        Assert.Equal(Severity.Success, successMessage.Severity);
        
        Assert.Equal(navManager.BaseUri, navManager.Uri);
    }

    [Fact]
    public async Task Register_WithInvalidData_ShouldShowError()
    {
        // Arrange
        var registerDto = new RegisterDto 
        { 
            Email = "invalid",
            Password = "short",
            FirstName = "Random",
            LastName = "Name",
            Role = Shared.Models.Enums.Role.Member
        };

        _authServiceMock.Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync(false);

        // Initialize navigation manager with base URI
        var navManager = Services.GetRequiredService<NavigationManager>();
        navManager.NavigateTo(navManager.BaseUri + "register");

        var cut = RenderComponent<Register>();
        var form = cut.FindComponent<MudForm>();
        var inputs = cut.FindComponents<MudTextField<string>>();
        var emailField = inputs[2].Instance;

        // Set up snackbar mock
        var snackbarMessages = new List<(string Message, Severity Severity)>();
        
        _snackbarMock
            .Setup(x => x.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
            .Callback((string message, Severity severity, Action<SnackbarOptions> _, string __) =>
            {
                snackbarMessages.Add((message, severity));
            });

        // Act - Fill in email field first and trigger validation
        await cut.InvokeAsync(() => emailField.ValueChanged.InvokeAsync(registerDto.Email));
        await cut.InvokeAsync(() => emailField.Validate());

        // Fill in remaining fields
        await cut.InvokeAsync(() => inputs[0].Instance.ValueChanged.InvokeAsync(registerDto.FirstName));
        await cut.InvokeAsync(() => inputs[1].Instance.ValueChanged.InvokeAsync(registerDto.LastName));
        await cut.InvokeAsync(() => inputs[3].Instance.ValueChanged.InvokeAsync(registerDto.Password));
        await cut.InvokeAsync(() => inputs[4].Instance.ValueChanged.InvokeAsync(registerDto.Password));

        // Validate the form
        await cut.InvokeAsync(() => form.Instance.Validate());

        // Click the register button
        var registerButton = cut.FindComponent<MudButton>();
        await cut.InvokeAsync(() => registerButton.Instance.OnClick.InvokeAsync());

        // Wait a bit for messages
        await Task.Delay(100);

        // Assert
        _authServiceMock.Verify(x => x.RegisterAsync(It.IsAny<RegisterDto>()), Times.Never);
        
        var errorMessage = snackbarMessages.FirstOrDefault();
        Assert.Equal("Please enter a valid email address.", errorMessage.Message);
        Assert.Equal(Severity.Warning, errorMessage.Severity);

        // Verify validation error messages
        Assert.True(emailField.HasErrors);
        Assert.Contains("invalid email", emailField.ErrorText?.ToLower() ?? "");

        // Verify we didn't navigate
        Assert.Equal(navManager.BaseUri + "register", navManager.Uri);
    }
} 