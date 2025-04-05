using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Apply token to request
            await ApplyTokenToRequest(request);

            // Send the request
            var response = await base.SendAsync(request, cancellationToken);

            // If unauthorized, try to refresh token and retry once
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Use semaphore to prevent multiple simultaneous refresh attempts
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    // Try to refresh the token
                    bool refreshed = await _refreshToken();
                    if (refreshed)
                    {
                        // Clone the request (since you can't reuse the original)
                        var newRequest = await CloneHttpRequestMessageAsync(request);

                        // Apply the new token
                        await ApplyTokenToRequest(newRequest);

                        // Try again with the new token
                        return await base.SendAsync(newRequest, cancellationToken);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return response;
        }

        private async Task ApplyTokenToRequest(HttpRequestMessage request)
        {
            var token = await _getToken();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            // Copy the request's content (if any)
            if (request.Content != null)
            {
                var ms = new MemoryStream();
                await request.Content.CopyToAsync(ms);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);

                // Copy content headers
                if (request.Content.Headers != null)
                {
                    foreach (var header in request.Content.Headers)
                    {
                        clone.Content.Headers.Add(header.Key, header.Value);
                    }
                }
            }

            // Copy the request headers
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Copy properties
            foreach (var prop in request.Properties)
            {
                clone.Properties.Add(prop);
            }

            return clone;
        }
    }
}
