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
        }
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await ApplyTokenToRequest(request);
            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    bool refreshed = await _refreshToken();
                    if (refreshed)
                    {
                        var newRequest = await CloneHttpRequestMessageAsync(request);
                        await ApplyTokenToRequest(newRequest);
                        var newResponse = await base.SendAsync(newRequest, cancellationToken);
                        return newResponse;
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
