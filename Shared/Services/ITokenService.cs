using System.ComponentModel;

namespace Shared.Services;

public interface ITokenService : INotifyPropertyChanged
{
    bool IsLoggedIn { get; }
    string? GetUserId();
    Task SetTokenAsync(string token);
    Task SetRolesAsync(string roles);
}