namespace SampleApi.Host.Configuration
{
    using System.Collections.Generic;

    /*
     * Application specific settings
     */
    public class ApiConfiguration
    {
        public int Port { get; set; }

        public string SslCertificateFileName { get; set; }

        public string SslCertificatePassword { get; set; }

        public bool UseProxy { get; set; }

        public string ProxyUrl { get; set; }
    }
}
