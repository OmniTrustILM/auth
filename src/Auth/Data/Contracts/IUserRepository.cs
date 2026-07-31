using Auth.Common.Data;
using Auth.Common.Data.Repositories;
using Auth.Models.Entities;

namespace Auth.Data.Contracts
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<IEnumerable<User>> GetRoleUsersAsync(Guid roleUuid);

        /// <summary>
        /// Members of a named group, sorted but not paged. Group membership lives in a single serialized column, so the
        /// filter cannot be pushed into the query and every user is read to apply it; the caller pages the result.
        /// </summary>
        Task<List<User>> GetGroupMembersAsync(string groupName, QueryStringParameters parameters);
    }
}
