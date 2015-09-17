using System.Collections.Generic;
using System.Configuration;

namespace FileSynchronizer.Configuration
{
    public class ServiceSettings
    {
        private ServiceSettings() {}

        public static ServiceSettings GetSettings()
        {
            return (ServiceSettings)(dynamic)ConfigurationManager.GetSection("serviceSettings");
        }

        public IEnumerable<Endpoint> Endpoints { get; set; }
    }
}