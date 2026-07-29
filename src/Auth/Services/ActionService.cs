using Auth.Common.Services;
using Auth.Data.Contracts;
using Auth.Models.Dto;
using Auth.Models.Mappings;

namespace Auth.Services
{
    public class ActionService : CrudService<Models.Entities.Action, ActionDto, ActionDto>, IActionService
    {

        public ActionService(IRepositoryManager repositoryManager, ILogger<ActionService> logger)
            : base(repositoryManager, repositoryManager.Action, ActionEntityMapper.Instance, logger)
        {
        }
    }
}
