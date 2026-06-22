using SmartFileOrganizer.Data;
using SmartFileOrganizer.Models;

namespace SmartFileOrganizer.Services;

public class RuleService
{
    private readonly JsonStorageService _storage;

    public RuleService(JsonStorageService storage)
    {
        _storage = storage;
    }

    public async Task<List<OrganizationRule>> LoadRulesAsync()
    {
        AppDataPaths.EnsureAppFolderExists();
        return await _storage.LoadAsync<OrganizationRule>(AppDataPaths.RulesFilePath);
    }

    public async Task SaveRulesAsync(IEnumerable<OrganizationRule> rules)
    {
        AppDataPaths.EnsureAppFolderExists();
        await _storage.SaveAsync(AppDataPaths.RulesFilePath, rules);
    }
}
