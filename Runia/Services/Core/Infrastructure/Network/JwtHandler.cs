using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Runia.Services.Core.Infrastructure.Network;

public class JwtHandler(ITokenStore tokenStore, ILogger<JwtHandler> logger) : DelegatingHandler
{
    private readonly ITokenStore _tokenStore = tokenStore;
    private readonly ILogger<JwtHandler> _logger = logger;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            var (access, _, _) = await _tokenStore.GetTokensAsync();
            if (!string.IsNullOrWhiteSpace(access))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogDebug(ex, "JwtHandler: failed to read tokens");
        }

        try
        {
            return await base.SendAsync(request, ct);
        }
        catch (OperationCanceledException ex)
        {
            // This covers TaskCanceledException (a subtype) when HttpClient times out or the request is cancelled.
            // Log a warning so callers and diagnostics show the cause, then rethrow to preserve cancellation semantics.
            _logger.LogWarning(ex, "JwtHandler: HTTP request was canceled for {Method} {Uri}", request.Method, request.RequestUri);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JwtHandler: unexpected exception sending HTTP request {Method} {Uri}", request.Method, request.RequestUri);
            throw;
        }
    }
}
