using Microsoft.Extensions.DependencyInjection; // Не забудь этот using
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using Runia.Services.Auth;

namespace Runia.Services.Core.Infrastructure.Network
{
    public class JwtHandler(IServiceProvider serviceProvider, ILogger<JwtHandler> logger) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            // Достаем AuthService только тогда, когда реально летит запрос
            var auth = serviceProvider.GetRequiredService<AuthService>();
            var access = auth.AccessToken;
            var hasToken = !string.IsNullOrWhiteSpace(access);
            var length = access?.Length ?? 0;
            logger.LogInformation("DEBUG_AUTH: Sending request to {Uri}. Token present: {HasToken}. Token length: {Len}.",
                request.RequestUri,
                hasToken,
                length);

            if (!string.IsNullOrWhiteSpace(access))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
            }
            else
            {
                request.Headers.Authorization = null;
            }

            try
            {
                return await base.SendAsync(request, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "JwtHandler: Error sending request to {Uri}", request.RequestUri);
                throw;
            }
        }
    }
}
