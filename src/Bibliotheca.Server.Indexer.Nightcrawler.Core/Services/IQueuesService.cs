using System.Threading.Tasks;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Services
{
    public interface IQueuesService
    {
        Task AddToQueue(string projectId, string branchName);
    }
}