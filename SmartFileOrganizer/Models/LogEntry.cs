namespace SmartFileOrganizer.Models;

public class LogEntry
{
    public string FileName { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
}
