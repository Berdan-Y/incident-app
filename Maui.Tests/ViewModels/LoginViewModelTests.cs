using Maui.Core.ViewModels;
using Moq;
using Refit;
using Shared.Api;
using Shared.Models.Dtos;
using Shared.Services;
using Xunit;

namespace Maui.Tests.ViewModels;

public class TestLoginViewModel : LoginViewModelBase
{
    public TestLoginViewModel(IAuthApi authApi, ITokenService tokenService)
        : base(authApi, tokenService)
    {
    }
}

public class LoginViewModelTests
{
    private readonly Mock<IAuthApi> _authApiMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly TestLoginViewModel _viewModel;

    public LoginViewModelTests()
    {
        _authApiMock = new Mock<IAuthApi>();
        _tokenServiceMock = new Mock<ITokenService>();
        _viewModel = new TestLoginViewModel(_authApiMock.Object, _tokenServiceMock.Object);
    }

    [Fact]
    public async Task LoginAsync_WithEmptyCredentials_ReturnsError()
    {
        // Arrange
        _viewModel.Email = string.Empty;
        _viewModel.Password = string.Empty;

        // Act
        var result = await _viewModel.LoginAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Please enter both email and password", result.Message);
        Assert.False(_viewModel.IsLoading);
        _authApiMock.Verify(x => x.LoginAsync(It.IsAny<LoginDto>()), Times.Never);
        _tokenServiceMock.Verify(x => x.SetTokenAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        _viewModel.Email = "test@example.com";
        _viewModel.Password = "password123";

        var expectedResponse = new LoginResponseDto
        {
            Token = "test-token",
            Roles = new List<string> { "User" }
        };

        _authApiMock.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(new ApiResponse<LoginResponseDto>(
                new HttpResponseMessage(System.Net.HttpStatusCode.OK),
                expectedResponse,
                new RefitSettings()));

        // Act
        var result = await _viewModel.LoginAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Login successful! Welcome back.", result.Message);
        Assert.False(_viewModel.IsLoading);
        Assert.Equal(string.Empty, _viewModel.Email);
        Assert.Equal(string.Empty, _viewModel.Password);

        _authApiMock.Verify(x => x.LoginAsync(It.Is<LoginDto>(dto =>
            dto.Email == "test@example.com" &&
            dto.Password == "password123")), Times.Once);

        _tokenServiceMock.Verify(x => x.SetTokenAsync("test-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.SetRolesAsync("User"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsError()
    {
        // Arrange
        _viewModel.Email = "test@example.com";
        _viewModel.Password = "wrongpassword";

        var errorResponse = new ApiResponse<LoginResponseDto>(
            new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized),
            null,
            new RefitSettings());

        _authApiMock.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(errorResponse);

        // Act
        var result = await _viewModel.LoginAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Unknown error occurred", result.Message);
        Assert.False(_viewModel.IsLoading);
        _tokenServiceMock.Verify(x => x.SetTokenAsync(It.IsAny<string>()), Times.Never);
        _tokenServiceMock.Verify(x => x.SetRolesAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenApiThrowsException_ReturnsError()
    {
        // Arrange
        _viewModel.Email = "test@example.com";
        _viewModel.Password = "password123";

        _authApiMock.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        var result = await _viewModel.LoginAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Equal("An unexpected error occurred: Network error", result.Message);
        Assert.False(_viewModel.IsLoading);
        _tokenServiceMock.Verify(x => x.SetTokenAsync(It.IsAny<string>()), Times.Never);
        _tokenServiceMock.Verify(x => x.SetRolesAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithNullResponse_ReturnsError()
    {
        // Arrange
        _viewModel.Email = "test@example.com";
        _viewModel.Password = "password123";

        var nullResponse = new ApiResponse<LoginResponseDto>(
            new HttpResponseMessage(System.Net.HttpStatusCode.OK),
            null,
            new RefitSettings());

        _authApiMock.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(nullResponse);

        // Act
        var result = await _viewModel.LoginAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Login response did not contain a valid token", result.Message);
        Assert.False(_viewModel.IsLoading);
        _tokenServiceMock.Verify(x => x.SetTokenAsync(It.IsAny<string>()), Times.Never);
        _tokenServiceMock.Verify(x => x.SetRolesAsync(It.IsAny<string>()), Times.Never);
    }
} 