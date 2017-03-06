using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bibliotheca.Server.Depository.Abstractions.DataTransferObjects;
using Bibliotheca.Server.Indexer.Abstractions.DataTransferObjects;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.DataTransferObjects;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Exceptions;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Services
{
    public class QueuesService : IQueuesService
    {
        private readonly IGatewayService _gatewayService;

        private readonly ILogger _logger;

        private readonly IDistributedCache _cache;

        public QueuesService(
            IGatewayService gatewayService, 
            ILoggerFactory loggerFactory,
            IDistributedCache cache)
        {
            _gatewayService = gatewayService;
            _logger = loggerFactory.CreateLogger<QueuesService>();
            _cache = cache;
        }

        public async Task AddToQueueAsync(string projectId, string branchName)
        {
            var cacheKey = GetCacheKey(projectId, branchName);

            try
            { 
                var value = await _cache.GetAsync(cacheKey);
                if (value != null)
                {
                    throw new QueueForBranchExistsException($"Queue for project: '{projectId}' and branch '{branchName}' already exists.");
                }

                var queueStatus = await AddQueueStatusToCache(projectId, branchName, cacheKey);

                var project = await _gatewayService.GetProjectAsync(projectId);
                var allDocuments = await _gatewayService.GetAllDocumentsAsync(projectId, branchName);
                queueStatus.NumberOfAllDocuments = allDocuments.Count;

                await _gatewayService.RemoveIndexAsync(projectId, branchName);

                foreach (var document in allDocuments)
                {
                    queueStatus.NumberOfIndexedDocuments++;
                    if (!IsIndexable(document))
                    {
                        await UpdateQueueStatusInCache(queueStatus, cacheKey);
                        continue;
                    }

                    _logger.LogInformation($"Indexing file: {document.Uri}");
                    var documentContent = await _gatewayService.GetDocumentContentAsync(projectId, branchName, document);
                    var documentIndex = CreateDocumentIndex(projectId, branchName, project, document, documentContent);

                    await _gatewayService.UploadDocumentIndex(projectId, branchName, documentIndex);
                    await UpdateQueueStatusInCache(queueStatus, cacheKey);
                }

                _logger.LogInformation($"Reindexing finished");
            }
            finally
            {
                await RemoveQueueStatusFromCache(cacheKey);
            }
        }

        public async Task<IndexStatusDto> GetQueueStatusAsync(string projectId, string branchName)
        {
            var cacheKey = GetCacheKey(projectId, branchName);
            var value = await _cache.GetAsync(cacheKey);

            IndexStatusDto queueStatus = null;
            if(value != null)
            {
                var objectString = Encoding.UTF8.GetString(value);
                queueStatus = JsonConvert.DeserializeObject<QueueStatusDto>(objectString);
            }
            else
            {
                queueStatus = new IndexStatusDto { IndexStatus = IndexStatusEnum.Unknown };
            }

            return queueStatus;
        }

        private async Task RemoveQueueStatusFromCache(string cacheKey)
        {
            await _cache.RemoveAsync(cacheKey);
        }

        private async Task<QueueStatusDto> AddQueueStatusToCache(string projectId, string branchName, string cacheKey)
        {
            var queueStatus = new QueueStatusDto
            {
                ProjectId = projectId,
                BranchName = branchName,
                StartTime = DateTime.UtcNow,
                NumberOfIndexedDocuments = 0,
                NumberOfAllDocuments = null,
                IndexStatus = IndexStatusEnum.Indexing
            };
            var serialoizedObject = JsonConvert.SerializeObject(queueStatus);
            var objectBytes = Encoding.UTF8.GetBytes(serialoizedObject);

            await _cache.SetAsync(cacheKey, objectBytes, new DistributedCacheEntryOptions());
            return queueStatus;
        }

        private async Task UpdateQueueStatusInCache(QueueStatusDto queueStatus, string cacheKey)
        {
            var serialoizedObject = JsonConvert.SerializeObject(queueStatus);
            var objectBytes = Encoding.UTF8.GetBytes(serialoizedObject);

            await _cache.SetAsync(cacheKey, objectBytes, new DistributedCacheEntryOptions());
        }

        private string GetCacheKey(string projectId, string branchName)
        {
            return $"{projectId}#{branchName}";
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