using SmartFileOrganizer.Data;
using SmartFileOrganizer.Models;

namespace SmartFileOrganizer.Services;

public class RuleService
{
    private readonly JsonStorageService _storage;

    public RuleService(JsonStorageService storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<List<OrganizationRule>> LoadRulesAsync()
    {
        AppDataPaths.EnsureAppFolderExists();
        var rules = await _storage.LoadAsync<OrganizationRule>(AppDataPaths.RulesFilePath);

        return rules
            .Where(rule => rule is not null &&
                           !string.IsNullOrWhiteSpace(rule.Extension) &&
                           !string.IsNullOrWhiteSpace(rule.DestinationFolder))
            .ToList();
    }

    public async Task SaveRulesAsync(IEnumerable<OrganizationRule>? rules)
    {
        if (rules is null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        AppDataPaths.EnsureAppFolderExists();
        await _storage.SaveAsync(AppDataPaths.RulesFilePath, rules);
    }
}
