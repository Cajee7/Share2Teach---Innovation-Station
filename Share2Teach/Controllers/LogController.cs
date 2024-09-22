using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LogController.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseLogController : ControllerBase
    {
        protected readonly ILogger<BaseLogController> _logger;

        public BaseLogController(ILogger<BaseLogController> logger)
        {
            _logger = logger;
        }
    }
}
