using Auth.Common.Models.Dto;
using Auth.Models.Dto;
using Auth.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers
{
    [ApiVersion("1.0")]
    [Route("auth/actions")]
    [ApiController]
    public class ActionsController : ControllerBase
    {
        private readonly IActionService _actionService;

        public ActionsController(IActionService actionService)
        {
            _actionService = actionService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<ActionDto>>> GetActionsAsync()
        {
            var result = await _actionService.GetAsync(new QueryRequestDto());

            return Ok(result);
        }
    }
}
