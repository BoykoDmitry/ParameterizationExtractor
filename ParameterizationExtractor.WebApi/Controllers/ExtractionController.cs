using Microsoft.AspNetCore.Mvc;
using Quipu.ParameterizationExtractor.WebApi.Models;
using Quipu.ParameterizationExtractor.WebApi.Services;

namespace Quipu.ParameterizationExtractor.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExtractionController : ControllerBase
    {
        private readonly IExtractionService _extractionService;
        private readonly ILogger<ExtractionController> _logger;

        public ExtractionController(IExtractionService extractionService, ILogger<ExtractionController> logger)
        {
            _extractionService = extractionService;
            _logger = logger;
        }

        /// <summary>
        /// </summary>
        [HttpPost("extract")]
        public async Task<IActionResult> ExtractAsync([FromBody] ExtractionRequest request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Received extraction request for config type: {ConfigType}", request.ConfigType);

            var result = await _extractionService.ProcessExtractionAsync(request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return File(result.ZipFileContent!, "application/zip", result.FileName);
        }

        /// <summary>
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// </summary>
        [HttpGet("formats")]
        public IActionResult GetFormats()
        {
            var formats = new
            {
                supported_formats = new[] { "DSL", "JSON", "XML" },
                examples = new
                {
                    dsl = @"for script ""example"" take from ""Users"" where ""Active = 1"" consider FK for ""Users"" and UniqueColumns ""Id"" build sql with asIs",
                    json = new
                    {
                        scripts = new[]
                        {
                            new
                            {
                                scriptName = "example",
                                rootRecords = new[]
                                {
                                    new { tableName = "Users", where = "Active = 1", processingOrder = 0 }
                                },
                                tablesToProcess = new[]
                                {
                                    new
                                    {
                                        tableName = "Users",
                                        uniqueColumns = new[] { "Id" },
                                        extractStrategy = "FK",
                                        sqlBuildOptions = new[] { "AsIsInserts" }
                                    }
                                }
                            }
                        }
                    },
                    xml = @"<Package><Scripts><SourceForScript ScriptName=""example""><RootRecords><RecordsToExtract TableName=""Users"" Where=""Active = 1"" ProcessingOrder=""0"" /></RootRecords><TablesToProcess><TableToExtract TableName=""Users""><UniqueColumns><string>Id</string></UniqueColumns></TableToExtract></TablesToProcess></SourceForScript></Scripts></Package>"
                }
            };

            return Ok(formats);
        }
    }
}
