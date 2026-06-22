namespace SmartFileOrganizer.Services;

public class RuleDialogService : IRuleDialogService
{
    public bool TryGetRuleInput(
        out string extension,
        out string destinationFolder,
        string? defaultExtension = null,
        string? defaultDestination = null,
        string title = "Add Rule")
    {
        extension = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter a file extension (e.g. .pdf or pdf):",
            title,
            defaultExtension ?? ".pdf");

        if (string.IsNullOrWhiteSpace(extension))
        {
            destinationFolder = string.Empty;
            return false;
        }

        destinationFolder = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter the destination folder name (e.g. Documents):",
            title,
            defaultDestination ?? "Documents");

        return !string.IsNullOrWhiteSpace(destinationFolder);
    }
}
