using System.Collections.Generic;
using System.Threading.Tasks;
using Bibliotheca.Server.Depository.Abstractions.DataTransferObjects;
using Bibliotheca.Server.Indexer.Abstractions.DataTransferObjects;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Services
{
    public interface IGatewayService
    {
        Task UploadDocumentIndex(string projectId, string branchName, DocumentIndexDto documentIndex);

        Task<string> GetDocumentContentAsync(string projectId, string branchName, BaseDocumentDto document);

        Task<ProjectDto> GetProjectAsync(string projectId);

        Task RemoveIndexAsync(string projectId, string branchName);

        Task<IList<BaseDocumentDto>> GetAllDocumentsAsync(string projectId, string branchName);
    }
}