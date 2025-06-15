using System.ComponentModel;

namespace Maui.Services;

public class AuthService : INotifyPropertyChanged
{
    private readonly ITokenService _tokenService;
    private bool _isAuthenticated;

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set
        {
            if (_isAuthenticated != value)
            {
                _isAuthenticated = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAuthenticated)));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public AuthService(ITokenService tokenService)
    {
        _tokenService = tokenService;
        _tokenService.LoggedIn += (s, e) => IsAuthenticated = true;
        _tokenService.LoggedOut += (s, e) => IsAuthenticated = false;
        
        // Set initial state
        IsAuthenticated = _tokenService.IsLoggedIn;
    }
} 