using Microsoft.Maui.Devices;

namespace Maui.Services;

public interface IApiConfigurationService
{
    string BaseUrl { get; }
}

public class ApiConfigurationService : IApiConfigurationService
{
    public string BaseUrl { get; }

    public ApiConfigurationService()
    {
        BaseUrl = DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5007"  // Android emulator special DNS
            : "http://localhost:5007"; // iOS and other platforms
    }
} 