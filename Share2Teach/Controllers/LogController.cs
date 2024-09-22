using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LogController.Controllers
{
    [ApiController]
    public abstract class BaseLogController<T> : ControllerBase
    {
        protected readonly ILogger<T> _logger;

        public BaseLogController(ILogger<T> logger)
        {
            _logger = logger;
        }
    }
}