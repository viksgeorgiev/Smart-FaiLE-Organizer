namespace SmartFileOrganizer.Services;

public interface IRuleDialogService
{
    bool TryGetRuleInput(
        out string extension,
        out string destinationFolder,
        string? defaultExtension = null,
        string? defaultDestination = null,
        string title = "Add Rule");
}
