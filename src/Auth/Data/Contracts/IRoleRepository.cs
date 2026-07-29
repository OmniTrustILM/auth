using Auth.Common.Data.Repositories;
using Auth.Models.Entities;

namespace Auth.Data.Contracts
{
    public interface IRoleRepository : IBaseRepository<Role>
    {
        Task<IEnumerable<Role>> GetUserRolesAsync(Guid userUuid);
    }
}
