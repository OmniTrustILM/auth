using Auth.Common.Data;
using Auth.Common.Services;
using Auth.Models.Dto;

namespace Auth.Services
{
    public interface IActionService : ICrudService<ActionDto, ActionDto>
    {
    }
}
