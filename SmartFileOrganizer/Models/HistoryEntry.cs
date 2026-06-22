namespace SmartFileOrganizer.Models;

public class HistoryEntry
{
    public DateTime Timestamp { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
