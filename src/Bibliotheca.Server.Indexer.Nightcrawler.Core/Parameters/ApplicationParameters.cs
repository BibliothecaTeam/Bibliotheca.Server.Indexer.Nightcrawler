namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Parameters
{
    public class ApplicationParameters
    {
        public string SecureToken { get; set; }
        
        public string OAuthAuthority { get; set; }

        public string OAuthAudience { get; set; }

        public string CacheConfiguration { get; set; }
        
        public string CacheInstanceName { get; set; }

        public ServiceDiscovery ServiceDiscovery { get; set; }
    }
}