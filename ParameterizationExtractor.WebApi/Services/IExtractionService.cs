using Quipu.ParameterizationExtractor.WebApi.Models;

namespace Quipu.ParameterizationExtractor.WebApi.Services
{
    public interface IExtractionService
    {
        Task<ExtractionResponse> ProcessExtractionAsync(ExtractionRequest request, CancellationToken cancellationToken = default);
    }
}
