using DomainCopilot.Application.Abstractions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DomainCopilot.Application.AI;

public sealed class FallbackChatCompletionService : IChatCompletionService
{
    private readonly IChatCompletionProvider _primary;
    private readonly IChatCompletionProvider _fallback;
    private readonly ILogger<FallbackChatCompletionService> _logger;

    public FallbackChatCompletionService(
        IChatCompletionProvider primary,
        IChatCompletionProvider fallback,
        ILogger<FallbackChatCompletionService> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await _primary.CompleteAsync(request, cancellationToken);
        }
        catch (AIProviderUnavailableException exception) when (!cancellationToken.IsCancellationRequested && _primary.ProviderId != _fallback.ProviderId)
        {
            _logger.LogWarning(exception, "Primary AI provider {PrimaryProvider} failed; falling back to {FallbackProvider}.",
                _primary.ProviderId, _fallback.ProviderId);
            var response = await _fallback.CompleteAsync(request, cancellationToken);
            return response with { IsDegraded = true };
        }
        catch (HttpRequestException exception) when (!cancellationToken.IsCancellationRequested && _primary.ProviderId != _fallback.ProviderId)
        {
            _logger.LogWarning(exception, "Primary AI provider {PrimaryProvider} is unreachable; falling back to {FallbackProvider}.",
                _primary.ProviderId, _fallback.ProviderId);
            var response = await _fallback.CompleteAsync(request, cancellationToken);
            return response with { IsDegraded = true };
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested && _primary.ProviderId != _fallback.ProviderId)
        {
            _logger.LogWarning(exception, "Primary AI provider {PrimaryProvider} timed out; falling back to {FallbackProvider}.",
                _primary.ProviderId, _fallback.ProviderId);
            var response = await _fallback.CompleteAsync(request, cancellationToken);
            return response with { IsDegraded = true };
        }
        catch (JsonException exception) when (!cancellationToken.IsCancellationRequested && _primary.ProviderId != _fallback.ProviderId)
        {
            _logger.LogWarning(exception, "Primary AI provider {PrimaryProvider} returned an invalid response; falling back to {FallbackProvider}.",
                _primary.ProviderId, _fallback.ProviderId);
            var response = await _fallback.CompleteAsync(request, cancellationToken);
            return response with { IsDegraded = true };
        }
    }
}
