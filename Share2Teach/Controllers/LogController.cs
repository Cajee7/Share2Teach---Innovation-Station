using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LogController.Controllers
{
    /// <summary>
    /// Abstract base class for logging controllers.
    /// Provides logging functionality to derived controllers.
    /// </summary>
    /// <typeparam name="T">The type of the class that is using the logger.</typeparam>
    [ApiController]
    public abstract class BaseLogController<T> : ControllerBase
    {
        /// <summary>
        /// The logger instance used for logging events and messages.
        /// </summary>
        protected readonly ILogger<T> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseLogController{T}"/> class.
        /// </summary>
        /// <param name="logger">An instance of <see cref="ILogger{T}"/> used for logging.</param>
        public BaseLogController(ILogger<T> logger)
        {
            _logger = logger;
        }
    }
}
