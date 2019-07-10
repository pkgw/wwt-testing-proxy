using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace testingproxy.Controllers
{
    [ApiController]
    public class ProxyController : ControllerBase
    {
        static readonly HttpClientHandler handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false
        };

        static readonly HttpClient client = new HttpClient(handler);

        const string PREFIX = "http://www.worldwidetelescope.org/";

        [Route("{*path}")]
        [HttpGet]
        public async Task Get(string path)
        {
            Uri uri = new Uri(PREFIX + path + HttpContext.Request.QueryString.Value);
            //System.Diagnostics.Debug.Print("\nURL: " + uri.ToString() + "\n");

            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                int code = (int) response.StatusCode;
                HttpContext.Response.StatusCode = code;

                foreach (KeyValuePair<string, IEnumerable<string>> pair in response.Headers)
                {
                    HttpContext.Response.Headers[pair.Key] = (String[]) pair.Value;
                }

                foreach (KeyValuePair<string, IEnumerable<string>> pair in response.Content.Headers)
                {
                    HttpContext.Response.Headers[pair.Key] = (String[])pair.Value;
                }

                // Lame hardcoding -- rewrite absolute-URL redirects to various WWT domains
                // to continue going through our proxy.
                if (code >= 300 && code < 400 && response.Headers.Location.IsAbsoluteUri)
                {
                    string host = response.Headers.Location.Host;

                    if (host == "worldwidetelescope.org" ||
                        host == "www.worldwidetelescope.org" ||
                        host == "cdn.worldwidetelescope.org" ||
                        host == "content.worldwidetelescope.org")
                    {
                        var builder = new UriBuilder(response.Headers.Location);
                        builder.Host = "";
                        builder.Scheme = "";
                        builder.Port = 0;
                        HttpContext.Response.Headers["Location"] = builder.ToString();
                    }
                }

                await response.Content.CopyToAsync(HttpContext.Response.Body);
            }
        }
    }
}
