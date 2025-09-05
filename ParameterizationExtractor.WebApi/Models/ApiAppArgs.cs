using Quipu.ParameterizationExtractor.Common;

namespace Quipu.ParameterizationExtractor.WebApi.Models
{
    public class ApiAppArgs : IAppArgs
    {
        public string DBName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string PathToPackage { get; set; } = string.Empty;
        public string ConnectionName { get; set; } = "DefaultConnection";
        public string OutputFolder { get; set; } = string.Empty;
        public bool Interactive { get; set; } = false;

        public static ApiAppArgs Create(string outputFolder, string connectionName = "DefaultConnection")
        {
            return new ApiAppArgs
            {
                OutputFolder = outputFolder,
                ConnectionName = connectionName,
                Interactive = false
            };
        }
    }
}
