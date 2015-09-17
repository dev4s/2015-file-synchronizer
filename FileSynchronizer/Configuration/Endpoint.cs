using System.Collections.Generic;

namespace FileSynchronizer.Configuration
{
    public class Endpoint
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public Protocol Protocol { get; set; }

        public string Username { get; set; }

        public string EncryptedPass { get; set; }

        public string RemoteDirectory { get; set; }

        public IEnumerable<Destination> Destinations { get; set; }
    }
}