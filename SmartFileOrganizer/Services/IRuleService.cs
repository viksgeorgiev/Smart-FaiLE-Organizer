using SmartFileOrganizer.Models;

namespace SmartFileOrganizer.Services;

public interface IRuleService
{
    Task<List<OrganizationRule>> LoadRulesAsync();

    Task SaveRulesAsync(IEnumerable<OrganizationRule>? rules);
}
