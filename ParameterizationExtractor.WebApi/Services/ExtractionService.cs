using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using Quipu.ParameterizationExtractor.WebApi.Models;
using Quipu.ParameterizationExtractor.Logic.Interfaces;
using Quipu.ParameterizationExtractor.DSL.Connector;
using Quipu.ParameterizationExtractor.Logic.Configs;
using Quipu.ParameterizationExtractor.Configs;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Quipu.ParameterizationExtractor.WebApi.Services
{
    public class ExtractionService : IExtractionService
    {
        private readonly ILogger<ExtractionService> _logger;
        private readonly IDSLConnector _dslConnector;
        private readonly PackageProcessor _packageProcessor;
        private readonly IServiceProvider _serviceProvider;

        public ExtractionService(
            ILogger<ExtractionService> logger,
            IDSLConnector dslConnector,
            PackageProcessor packageProcessor,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _dslConnector = dslConnector;
            _packageProcessor = packageProcessor;
            _serviceProvider = serviceProvider;
        }

        public async Task<ExtractionResponse> ProcessExtractionAsync(ExtractionRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Processing extraction request with config type: {ConfigType}", request.ConfigType);

                IPackage package = request.ConfigType switch
                {
                    ConfigurationType.DSL => ParseDSLConfiguration(request.Configuration),
                    ConfigurationType.JSON => ParseJSONConfiguration(request.Configuration),
                    ConfigurationType.XML => ParseXMLConfiguration(request.Configuration),
                    _ => throw new ArgumentException($"Unsupported configuration type: {request.ConfigType}")
                };

                var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var scopedAppArgs = scope.ServiceProvider.GetRequiredService<IAppArgs>();
                    if (scopedAppArgs is ApiAppArgs apiArgs)
                    {
                        apiArgs.OutputFolder = tempDir;
                        apiArgs.ConnectionName = "DefaultConnection";
                    }
                    
                    Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", request.ConnectionString);
                    
                    var scopedPackageProcessor = scope.ServiceProvider.GetRequiredService<PackageProcessor>();
                    await scopedPackageProcessor.ExecuteAsync(cancellationToken, package);

                    var zipContent = await CreateZipFromDirectoryAsync(tempDir);
                    var fileName = request.OutputFileName ?? $"extraction_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";

                    return new ExtractionResponse
                    {
                        Success = true,
                        ZipFileContent = zipContent,
                        FileName = fileName
                    };
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
            catch (DSLParseException ex)
            {
                _logger.LogError(ex, "DSL parsing error");
                return new ExtractionResponse
                {
                    Success = false,
                    ErrorMessage = $"DSL parsing error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing extraction request");
                return new ExtractionResponse
                {
                    Success = false,
                    ErrorMessage = $"Processing error: {ex.Message}"
                };
            }
        }

        private IPackage ParseDSLConfiguration(string dslContent)
        {
            return _dslConnector.Parse(dslContent);
        }

        private IPackage ParseJSONConfiguration(string jsonContent)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var packageData = JsonSerializer.Deserialize<PackageData>(jsonContent, options);
            return ConvertToPackage(packageData);
        }

        private IPackage ParseXMLConfiguration(string xmlContent)
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, xmlContent);
                var configSerializer = new ConfigSerializer(_dslConnector);
                return configSerializer.GetPackage(tempFile);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        private IPackage ConvertToPackage(PackageData packageData)
        {
            var package = new Package();
            
            foreach (var scriptData in packageData.Scripts)
            {
                var script = new SourceForScript
                {
                    ScriptName = scriptData.ScriptName
                };

                foreach (var rootRecord in scriptData.RootRecords)
                {
                    script.RootRecords.Add(new RecordsToExtract
                    {
                        TableName = rootRecord.TableName,
                        Where = rootRecord.Where,
                        ProcessingOrder = rootRecord.ProcessingOrder
                    });
                }

                foreach (var table in scriptData.TablesToProcess)
                {
                    var tableToExtract = new TableToExtract
                    {
                        TableName = table.TableName,
                        UniqueColumns = table.UniqueColumns?.ToList() ?? new List<string>()
                    };

                    tableToExtract.ExtractStrategy = table.ExtractStrategy?.ToLower() switch
                    {
                        "onlyonetableextract" or "onetable" => new OnlyOneTableExtractStrategy(),
                        "fkdependencyextract" or "fk" => new FKDependencyExtractStrategy(),
                        "parentsextract" or "parents" => new OnlyParentExtractStrategy(),
                        "childrenextract" or "children" => new OnlyChildrenExtractStrategy(),
                        _ => new FKDependencyExtractStrategy()
                    };

                    tableToExtract.SqlBuildStrategy = new SqlBuildStrategy
                    {
                        AsIsInserts = table.SqlBuildOptions?.Contains("AsIsInserts") ?? false,
                        NoInserts = table.SqlBuildOptions?.Contains("NoInserts") ?? false,
                        ThrowExecptionIfNotExists = table.SqlBuildOptions?.Contains("ThrowExceptionIfNotExists") ?? false
                    };

                    script.TablesToProcess.Add(tableToExtract);
                }

                package.Scripts.Add(script);
            }

            return package;
        }

        private async Task<byte[]> CreateZipFromDirectoryAsync(string directoryPath)
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(directoryPath, file);
                    var entry = archive.CreateEntry(relativePath);
                    
                    using var entryStream = entry.Open();
                    using var fileStream = File.OpenRead(file);
                    await fileStream.CopyToAsync(entryStream);
                }
            }
            
            return memoryStream.ToArray();
        }
    }

    public class PackageData
    {
        public List<ScriptData> Scripts { get; set; } = new();
    }

    public class ScriptData
    {
        public string ScriptName { get; set; } = string.Empty;
        public List<RootRecordData> RootRecords { get; set; } = new();
        public List<TableData> TablesToProcess { get; set; } = new();
    }

    public class RootRecordData
    {
        public string TableName { get; set; } = string.Empty;
        public string Where { get; set; } = string.Empty;
        public int ProcessingOrder { get; set; }
    }

    public class TableData
    {
        public string TableName { get; set; } = string.Empty;
        public string[]? UniqueColumns { get; set; }
        public string? ExtractStrategy { get; set; }
        public string[]? SqlBuildOptions { get; set; }
        public string[]? Exclude { get; set; }
    }
}
