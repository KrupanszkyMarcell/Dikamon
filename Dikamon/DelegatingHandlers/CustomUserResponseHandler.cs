using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dikamon.Models;


namespace Dikamon.DelegatingHandlers
{
    public class CustomUserResponseHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var successModel = Newtonsoft.Json.JsonConvert.DeserializeObject<Users>(json);
            }
            else if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var json = await response.Content.ReadAsStringAsync();
                var errorModel = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorMessage>(json);
            }

            return response;
        }
    }
}