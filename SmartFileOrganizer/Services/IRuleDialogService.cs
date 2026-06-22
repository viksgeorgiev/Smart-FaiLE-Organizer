namespace SmartFileOrganizer.Services;

public interface IRuleDialogService
{
    bool TryGetRuleInput(out string extension, out string destinationFolder);
}
