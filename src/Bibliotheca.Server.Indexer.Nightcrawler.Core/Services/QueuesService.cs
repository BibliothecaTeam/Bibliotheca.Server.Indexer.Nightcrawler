using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bibliotheca.Server.Depository.Abstractions.DataTransferObjects;
using Bibliotheca.Server.Indexer.Abstractions.DataTransferObjects;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Services
{
    public class QueuesService : IQueuesService
    {
        private readonly IGatewayService _gatewayService;

        private readonly ILogger _logger;

        public QueuesService(
            IGatewayService gatewayService, 
            ILoggerFactory loggerFactory)
        {
            _gatewayService = gatewayService;
            _logger = loggerFactory.CreateLogger<QueuesService>();
        }

        public async Task AddToQueue(string projectId, string branchName)
        {
            var project = await _gatewayService.GetProjectAsync(projectId);
            var allDocuments = await _gatewayService.GetAllDocumentsAsync(projectId, branchName);

            await _gatewayService.RemoveIndexAsync(projectId, branchName);

            foreach(var document in allDocuments)
            {
                if(!IsIndexable(document))
                {
                    continue;
                }

                _logger.LogInformation($"Indexing file: {document.Uri}");
                var documentContent = await _gatewayService.GetDocumentContentAsync(projectId, branchName, document);
                var documentIndex = CreateDocumentIndex(projectId, branchName, project, document, documentContent);

                await _gatewayService.UploadDocumentIndex(projectId, branchName, documentIndex);
            }

            _logger.LogInformation($"Reindexing finished");
        }

        private bool IsIndexable(BaseDocumentDto document)
        {
            var fileExtension = Path.GetExtension(document.Uri);
            if(fileExtension == ".md")
            {
                return true;
            }

            return false;
        }

        private DocumentIndexDto CreateDocumentIndex(string projectId, string branchName, ProjectDto project, BaseDocumentDto document, string documentContent)
        {
            string indexed = ClearContent(documentContent);
            string title = ClearTitle(document.Uri);

            var documentIndexDto = new DocumentIndexDto
            {
                Content = indexed,
                BranchName = branchName,    
                ProjectName = project.Name,
                ProjectId = projectId,
                Title = title,
                Url = document.Uri,
                Id = ToAlphanumeric(document.Uri),
                Tags = project.Tags.ToArray()
            };

            return documentIndexDto;
        }

        private string ClearTitle(string url)
        {
            var title = Path.GetFileNameWithoutExtension(url);
            title = Regex.Replace(title, "[^a-zA-Z0-9- _]", string.Empty);
            title = title.Replace("-", " ");
            title = title.Replace("_", " ");
            title = UppercaseFirst(title);
            return title;
        }

        private static string ClearContent(string content)
        {
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(content);
            var indexed = htmlDocument.DocumentNode.InnerText;

            return indexed;
        }

        public string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public string ToAlphanumeric(string s)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9-]");
            return rgx.Replace(s, string.Empty);
        }
    }
}