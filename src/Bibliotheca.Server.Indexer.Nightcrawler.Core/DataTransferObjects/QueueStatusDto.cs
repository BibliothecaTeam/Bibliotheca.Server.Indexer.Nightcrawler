using System;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.DataTransferObjects
{
    public class QueueStatusDto
    {
        public string ProjectId { get; set; }

        public string BranchName { get; set; }

        public DateTime StartTime { get; set; }

        public int NumberOfIndexedDocuments { get; set; }

        public int? NumberOfAllDocuments { get; set; }
    }
}