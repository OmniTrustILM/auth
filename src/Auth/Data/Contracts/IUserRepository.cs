using Auth.Common.Data.Repositories;
using Auth.Models.Entities;

namespace Auth.Data.Contracts
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<IEnumerable<User>> GetRoleUsersAsync(Guid roleUuid);
    }
}
