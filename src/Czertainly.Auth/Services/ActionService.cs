using Czertainly.Auth.Common.Services;
using Czertainly.Auth.Data.Contracts;
using Czertainly.Auth.Models.Dto;
using Czertainly.Auth.Models.Mappings;

namespace Czertainly.Auth.Services
{
    public class ActionService : CrudService<Models.Entities.Action, ActionDto, ActionDto>, IActionService
    {

        public ActionService(IRepositoryManager repositoryManager, ILogger<ActionService> logger)
            : base(repositoryManager, repositoryManager.Action, ActionEntityMapper.Instance, logger)
        {
        }
    }
}
