using Bunit;
using Xunit;
using Moq;
using Blazor.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using MudBlazor.Utilities;
using Blazor.Pages;
using Microsoft.AspNetCore.Components;
using Shared.Models.Dtos;
using Shared.Models;
using Shared.Models.Enums;
using Shared.Api;
using System.Net.Http;
using Refit;
using Bunit.Extensions;
using System.Threading;
using System.Linq;

namespace Blazor.Tests;

public class IncidentManagementTests : TestContext
{
    private readonly Mock<IIncidentApi> _incidentApiMock;
    private readonly Mock<ISnackbar> _snackbarMock;
    private readonly NavigationManager _navigationManager;
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly Mock<IUserApi> _userApiMock;
    private readonly Mock<IGoogleMapsService> _googleMapsServiceMock;

    public IncidentManagementTests()
    {
        // Create all mocks first
        _incidentApiMock = new Mock<IIncidentApi>();
        _snackbarMock = new Mock<ISnackbar>();
        _authServiceMock = new Mock<IAuthenticationService>();
        _userApiMock = new Mock<IUserApi>();
        _googleMapsServiceMock = new Mock<IGoogleMapsService>();

        // Setup snackbar mock to capture messages without returning a concrete instance
        _snackbarMock
            .Setup(x => x.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
            .Callback((string message, Severity severity, Action<SnackbarOptions> _, string __) => { });

        // Register all services first
        Services.AddSingleton(_incidentApiMock.Object);
        Services.AddSingleton(_snackbarMock.Object);
        Services.AddSingleton(_authServiceMock.Object);
        Services.AddSingleton(_userApiMock.Object);
        Services.AddSingleton(_googleMapsServiceMock.Object);
        Services.AddMudServices();

        // Configure the navigation manager with a proper base address
        var navigationManager = new TestNavigationManager();
        navigationManager.NavigateTo("http://localhost/", false);
        Services.AddSingleton<NavigationManager>(navigationManager);
        _navigationManager = navigationManager;

        // Setup all required MudBlazor JSInterop calls
        JSInterop.SetupVoid("mudPopover.initialize", _ => true);
        JSInterop.SetupVoid("mudPopover.connect", _ => true);
        JSInterop.SetupVoid("mudPopover.disconnect", _ => true);
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
        JSInterop.SetupVoid("mudKeyInterceptor.disconnect", _ => true);
        JSInterop.SetupVoid("mudKeyInterceptor.updatekey", _ => true);
        JSInterop.SetupVoid("mudSelect.setOptionValue", _ => true);
        JSInterop.SetupVoid("mudSelect.setSelectedValue", _ => true);
        JSInterop.SetupVoid("mudSelectState.restoreState", _ => true);
        JSInterop.SetupVoid("mudSelectState.saveState", _ => true);
        JSInterop.SetupVoid("mudElement.getBoundingClientRect", _ => true);
        JSInterop.SetupVoid("mudElement.focus", _ => true);
        JSInterop.SetupVoid("mudElement.scroll", _ => true);
        JSInterop.SetupVoid("mudElement.scrollTo", _ => true);
        JSInterop.SetupVoid("mudElement.scrollIntoView", _ => true);

        // Setup return values for element measurements using a simple object
        JSInterop.Setup<object>("mudElementRef.getBoundingClientRect", _ => true)
            .SetResult(new
            {
                top = 0,
                left = 0,
                bottom = 100,
                right = 100,
                width = 100,
                height = 100
            });
    }

    [Fact]
    public async Task AllIncidents_UpdateStatus_ShouldUpdateIncidentStatus()
    {
        // Arrange
        var incidents = new List<IncidentResponseDto>
        {
            new IncidentResponseDto
            {
                Id = Guid.NewGuid(),
                Title = "Test Incident",
                Description = "Test Description",
                Status = Status.Todo,
                Priority = Priority.Medium,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var apiResponse = new Refit.ApiResponse<List<IncidentResponseDto>>(
            new HttpResponseMessage(System.Net.HttpStatusCode.OK),
            incidents,
            new RefitSettings());

        _incidentApiMock.Setup(x => x.GetIncidentsAsync())
            .ReturnsAsync(apiResponse);

        _incidentApiMock.Setup(x => x.UpdateIncidentStatusAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(new Refit.ApiResponse<IncidentDto>(
                new HttpResponseMessage(System.Net.HttpStatusCode.OK),
                new IncidentDto(),
                new RefitSettings()));

        // Setup the GetIncidentByIdAsync mock that will be called by the details component
        _incidentApiMock.Setup(x => x.GetIncidentByIdAsync(incidents[0].Id))
            .ReturnsAsync(new Refit.ApiResponse<IncidentResponseDto>(
                new HttpResponseMessage(System.Net.HttpStatusCode.OK),
                incidents[0],
                new RefitSettings()));

        var cut = RenderComponent<AllIncidents>();
        
        // Wait for the component to render and data to load
        try 
        {
            // Wait for the incidents to be loaded and rendered
            cut.WaitForState(() => cut.FindAll(".mud-card").Any(), TimeSpan.FromSeconds(5));
            
            // Then wait for the status chip to be rendered
            cut.WaitForState(() => cut.FindComponents<MudChip>().Any(), TimeSpan.FromSeconds(5));
        }
        catch (Exception ex) when (ex.GetType().Name == "WaitForFailedException")
        {
            var currentState = $"Component failed to render within timeout. Current component state: " +
                             $"Cards found: {cut.FindAll(".mud-card").Count}, " +
                             $"Chips found: {cut.FindComponents<MudChip>().Count}";
            throw new Exception(currentState, ex);
        }
        
        // Navigate to incident details to update status
        var detailsButton = cut.FindAll("button").First(b => b.TextContent.Contains("View Details"));
        await cut.InvokeAsync(() => detailsButton.Click());
        
        // Verify navigation occurred
        Assert.EndsWith($"incident/{incidents[0].Id}", _navigationManager.Uri);
        
        // Render the details component
        var detailsComponent = RenderComponent<IncidentDetails>(parameters => parameters
            .Add(p => p.Id, incidents[0].Id));
            
        // Wait for the status select to be rendered in the details view
        try
        {
            detailsComponent.WaitForState(() => detailsComponent.FindComponents<MudSelect<Status>>().Any(), TimeSpan.FromSeconds(5));
        }
        catch (Exception ex) when (ex.GetType().Name == "WaitForFailedException")
        {
            var currentState = $"Details component failed to render within timeout. Current component state: " +
                             $"Select components found: {detailsComponent.FindComponents<MudSelect<Status>>().Count}";
            throw new Exception(currentState, ex);
        }
        
        // Update the status
        var statusSelect = detailsComponent.FindComponents<MudSelect<Status>>().First();
        await detailsComponent.InvokeAsync(async () => 
        {
            await statusSelect.Instance.ValueChanged.InvokeAsync(Status.InProgress);
        });

        // Wait a bit for the update to complete
        await Task.Delay(100);

        // Assert
        _incidentApiMock.Verify(x => x.UpdateIncidentStatusAsync(
            incidents[0].Id,
            (int)Status.InProgress), Times.Once);
    }

    [Fact]
    public async Task IncidentDetails_UpdatePriority_ShouldUpdateIncidentPriority()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var incident = new IncidentResponseDto
        {
            Id = incidentId,
            Title = "Test Incident",
            Description = "Test Description",
            Status = Status.Todo,
            Priority = Priority.Low,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _incidentApiMock.Setup(x => x.GetIncidentByIdAsync(incidentId))
            .ReturnsAsync(new Refit.ApiResponse<IncidentResponseDto>(
                new HttpResponseMessage(System.Net.HttpStatusCode.OK),
                incident,
                new RefitSettings()));

        var cut = RenderComponent<IncidentDetails>(parameters => parameters
            .Add(p => p.Id, incidentId));

        // Act
        var prioritySelect = cut.FindComponents<MudSelect<Priority>>().First();
        await cut.InvokeAsync(() => prioritySelect.Instance.ValueChanged.InvokeAsync(Priority.High));

        // Assert
        _incidentApiMock.Verify(x => x.UpdateIncidentPriorityAsync(
            incidentId,
            (int)Priority.High), Times.Once);
    }

    [Fact]
    public async Task IncidentDetails_UpdateAssignee_ShouldUpdateIncidentAssignee()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var newAssigneeId = Guid.NewGuid();
        var incident = new IncidentResponseDto
        {
            Id = incidentId,
            Title = "Test Incident",
            Description = "Test Description",
            Status = Status.Todo,
            Priority = Priority.Low,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var fieldEmployees = new List<UserDto>
        {
            new UserDto { Id = newAssigneeId.ToString(), Role = "FieldEmployee" }
        };

        _incidentApiMock.Setup(x => x.GetIncidentByIdAsync(incidentId))
            .ReturnsAsync(new Refit.ApiResponse<IncidentResponseDto>(
                new HttpResponseMessage(System.Net.HttpStatusCode.OK),
                incident,
                new RefitSettings()));

        _userApiMock.Setup(x => x.GetUsersAsync())
            .ReturnsAsync(new Refit.ApiResponse<List<UserDto>>(
                new HttpResponseMessage(System.Net.HttpStatusCode.OK),
                fieldEmployees,
                new RefitSettings()));

        var cut = RenderComponent<IncidentDetails>(parameters => parameters
            .Add(p => p.Id, incidentId));

        // Wait for the component to render
        try
        {
            cut.WaitForState(() => 
            {
                var assigneeSelect = cut.FindComponents<MudSelect<UserDto>>().Any();
                return assigneeSelect;
            }, TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            throw new Exception($"Could not find assignee select component. Current markup: {cut.Markup}", ex);
        }

        // Find the MudSelect component for assignee
        var assigneeSelect = cut.FindComponents<MudSelect<UserDto>>().FirstOrDefault();
        if (assigneeSelect == null)
        {
            throw new Exception("Could not find assignee select component after waiting.");
        }

        // Trigger the value change
        await cut.InvokeAsync(async () => 
        {
            await assigneeSelect.Instance.ValueChanged.InvokeAsync(fieldEmployees[0]);
        });

        // Assert
        _incidentApiMock.Verify(x => x.AssignIncidentAsync(
            incidentId,
            newAssigneeId), Times.Once);
    }

    [Fact]
    public async Task IncidentDetails_UpdateAllInformation_ShouldUpdateIncidentDetails()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var incident = new IncidentResponseDto
        {
            Id = incidentId,
            Title = "Test Incident",
            Description = "Old description",
            Status = Status.Todo,
            Priority = Priority.Low,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var incidentResponse = new Refit.ApiResponse<IncidentResponseDto>(
            new HttpResponseMessage(System.Net.HttpStatusCode.OK),
            incident,
            new RefitSettings());

        _incidentApiMock.Setup(x => x.GetIncidentByIdAsync(incidentId))
            .ReturnsAsync(incidentResponse);

        var cut = RenderComponent<IncidentDetails>(parameters => parameters
            .Add(p => p.Id, incidentId));

        // Wait for the component to render and data to load with better error handling
        try
        {
            // First wait for the basic component structure to render
            cut.WaitForState(() => cut.FindAll(".mud-grid").Any(), TimeSpan.FromSeconds(5));
            
            // Then wait for the select components to be rendered
            cut.WaitForState(() => 
            {
                var selects = cut.FindComponents<MudSelect<Status>>();
                return selects.Any() && selects.Count >= 1;
            }, TimeSpan.FromSeconds(5));
        }
        catch (Exception ex) when (ex.GetType().Name == "WaitForFailedException")
        {
            var currentState = $"Component failed to render within timeout. Current component state: " +
                             $"Grid elements found: {cut.FindAll(".mud-grid").Count}, " +
                             $"Status Selects found: {cut.FindComponents<MudSelect<Status>>().Count}, " +
                             $"Priority Selects found: {cut.FindComponents<MudSelect<Priority>>().Count}";
            throw new Exception(currentState, ex);
        }

        // Get the select components after we're sure they exist
        var statusSelect = cut.FindComponents<MudSelect<Status>>().First();
        var prioritySelect = cut.FindComponents<MudSelect<Priority>>().First();
        
        // Update the values within InvokeAsync to ensure proper state management
        await cut.InvokeAsync(async () => 
        {
            await statusSelect.Instance.ValueChanged.InvokeAsync(Status.InProgress);
            await Task.Delay(100); // Give time for the first update to process
            await prioritySelect.Instance.ValueChanged.InvokeAsync(Priority.High);
        });

        // Wait a bit for all updates to complete
        await Task.Delay(100);

        // Assert
        _incidentApiMock.Verify(x => x.UpdateIncidentStatusAsync(
            incidentId,
            (int)Status.InProgress), Times.Once);

        _incidentApiMock.Verify(x => x.UpdateIncidentPriorityAsync(
            incidentId,
            (int)Priority.High), Times.Once);
    }
} 