using Auth.Common.Data.Repositories;

namespace Auth.Data.Contracts
{
    public interface IActionRepository : IBaseRepository<Models.Entities.Action>
    {
        Task<Models.Entities.Action?> GetActionByNameAsync(string actionName);

        Task<Dictionary<TKey, Models.Entities.Action>> GetActionsMapAsync<TKey>(Func<Models.Entities.Action, TKey> keySelector) where TKey : notnull;

    }
}
