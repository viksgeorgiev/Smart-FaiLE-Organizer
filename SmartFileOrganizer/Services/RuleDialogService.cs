namespace SmartFileOrganizer.Services;

public class RuleDialogService : IRuleDialogService
{
    public bool TryGetRuleInput(out string extension, out string destinationFolder)
    {
        extension = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter a file extension (e.g. .pdf or pdf):",
            "Add Rule",
            ".pdf");

        if (string.IsNullOrWhiteSpace(extension))
        {
            destinationFolder = string.Empty;
            return false;
        }

        destinationFolder = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter the destination folder name (e.g. Documents):",
            "Add Rule",
            "Documents");

        return !string.IsNullOrWhiteSpace(destinationFolder);
    }
}
