using System.Threading.Tasks;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.DataTransferObjects;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Services
{
    public interface IQueuesService
    {
        Task AddToQueueAsync(string projectId, string branchName);
        
        Task<QueueStatusDto> GetQueueStatusAsync(string projectId, string branchName);
    }
}