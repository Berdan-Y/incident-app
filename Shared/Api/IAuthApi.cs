using Refit;
using Shared.Models;
using Shared.Models.Dtos;

namespace Shared.Api;

public interface IAuthApi
{
    [Post("/api/Auth/login")]
    Task<IApiResponse<LoginResponseDto>> LoginAsync([Body] LoginDto loginDto);

    [Post("/api/Auth/register")]
    Task<IApiResponse<string>> RegisterAsync([Body] RegisterDto registerDto);

    [Post("/api/Auth/logout")]
    Task<IApiResponse<bool>> LogoutAsync();
}