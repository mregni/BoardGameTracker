namespace BoardGameTracker.Common.DTOs;

public class BggConfigStatusDto
{
    public bool IsConfigured { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }
}
