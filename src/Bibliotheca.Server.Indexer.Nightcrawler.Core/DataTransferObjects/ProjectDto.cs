using System.Collections.Generic;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.DataTransferObjects
{
    public class ProjectDto
    {
        public ProjectDto()
        {
            Tags = new List<string>();
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string DefaultBranch { get; set; }

        public List<string> Tags { get; private set; }

        public string Group { get; set; }

        public string ProjectSite { get; set; }
    }
}