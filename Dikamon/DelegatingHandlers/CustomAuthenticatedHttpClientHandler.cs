using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Dikamon.DelegatingHandlers
{
    public class CustomAuthenticatedHttpClientHandler : DelegatingHandler
    {
        private readonly Func<Task<string>> _getToken;
        private readonly Func<Task<bool>> _refreshToken;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public CustomAuthenticatedHttpClientHandler(
            Func<Task<string>> getToken,
            Func<Task<bool>> refreshToken)
        {
            _getToken = getToken;
            _refreshToken = refreshToken;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                await ApplyTokenToRequest(request);
                var response = await base.SendAsync(request, cancellationToken);

                // Log all responses
                Debug.WriteLine($"[HTTP] {request.Method} {request.RequestUri} -> {response.StatusCode}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Debug.WriteLine($"[AUTH] Unauthorized response for {request.RequestUri}");
                    await _semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        bool refreshed = await _refreshToken();
                        Debug.WriteLine($"[AUTH] Token refresh result: {refreshed}");

                        if (refreshed)
                        {
                            var newRequest = await CloneHttpRequestMessageAsync(request);
                            await ApplyTokenToRequest(newRequest);
                            var newResponse = await base.SendAsync(newRequest, cancellationToken);
                            Debug.WriteLine($"[AUTH] Retried request result: {newResponse.StatusCode}");
                            return newResponse;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[AUTH] Error during token refresh: {ex.Message}");
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HTTP-ERROR] Exception in SendAsync: {ex.Message}");
                throw;
            }
        }

        private async Task ApplyTokenToRequest(HttpRequestMessage request)
        {
            try
            {
                var token = await _getToken();
                Debug.WriteLine($"[AUTH] Applying token to request: {!string.IsNullOrEmpty(token)}");

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                else
                {
                    Debug.WriteLine("[AUTH] No token available to apply to request");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AUTH] Error applying token to request: {ex.Message}");
            }
        }

        private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            if (request.Content != null)
            {
                var ms = new MemoryStream();
                await request.Content.CopyToAsync(ms);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);
                if (request.Content.Headers != null)
                {
                    foreach (var header in request.Content.Headers)
                    {
                        clone.Content.Headers.Add(header.Key, header.Value);
                    }
                }
            }
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            foreach (var prop in request.Properties)
            {
                clone.Properties.Add(prop);
            }

            return clone;
        }
    }
}