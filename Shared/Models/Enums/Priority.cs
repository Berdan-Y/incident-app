using System.ComponentModel;

namespace Shared.Models.Enums;

public enum Priority
{
    [Description("Unknown")]
    Unknown = 0,
    [Description("Low")]
    Low = 1,
    
    [Description("Medium")]
    Medium = 2,
    
    [Description("High")]
    High = 3,

    [Description("Critical")]
    Critical = 4
}

public static class PriorityExtensions
{
    public static Priority[] Values => (Priority[])Enum.GetValues(typeof(Priority));
}