using System.Threading.Tasks;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.DataTransferObjects;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Jobs;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Services;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Api.Controllers
{
    /// <summary>
    /// Index queue controller.
    /// </summary>
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/queues/{projectId}/{branchName}")]
    public class QueuesController : Controller
    {
        private readonly IQueuesService _queuesService;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="queuesService"></param>
        public QueuesController(IQueuesService queuesService)
        {
            _queuesService = queuesService;
        }

        /// <summary>
        /// Get status of index for specific project and branch.
        /// </summary>
        /// <param name="projectId">Project id.</param>
        /// <param name="branchName">Branch name.</param>
        /// <returns>Status index.</returns>
        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IndexStatusDto))]
        public async Task<IndexStatusDto> Get(string projectId, string branchName)
        {
            var queueStatus = await _queuesService.GetQueueStatusAsync(projectId, branchName);
            return queueStatus;
        }

        /// <summary>
        /// Reindex all documentations from specific project and branch.
        /// </summary>
        /// <param name="projectId">Project id.</param>
        /// <param name="branchName">Branch name.</param>
        /// <returns>If added to queue with success then method returns 200 (Ok).</returns>
        [HttpPost]
        [ProducesResponseType(200)]
        public IActionResult Post(string projectId, string branchName)
        {
            BackgroundJob.Enqueue<IIndexRefreshJob>(x => x.RefreshIndexAsync(null, projectId, branchName));
            return Ok();
        }
    }
}
