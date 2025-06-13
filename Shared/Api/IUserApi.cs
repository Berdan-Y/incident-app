using Refit;
using Shared.Models.Dtos;

namespace Shared.Api;

public interface IUserApi
{
    [Get("/api/users")]
    Task<IApiResponse<List<UserDto>>> GetUsersAsync();
} 