using System.ComponentModel.DataAnnotations;

namespace Quipu.ParameterizationExtractor.WebApi.Models
{
    public class ExtractionRequest
    {
        [Required]
        public string ConnectionString { get; set; } = string.Empty;
        
        [Required]
        public string Configuration { get; set; } = string.Empty;
        
        [Required]
        public ConfigurationType ConfigType { get; set; }
        
        public string? OutputFileName { get; set; }
    }

    public enum ConfigurationType
    {
        DSL,
        JSON,
        XML
    }

    public class ExtractionResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public byte[]? ZipFileContent { get; set; }
        public string? FileName { get; set; }
    }
}
