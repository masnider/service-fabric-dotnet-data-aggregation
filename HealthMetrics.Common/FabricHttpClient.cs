// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net.Http
{
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using HealthMetrics.Common;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using System.Collections.Concurrent;
    using System.IO;
    using Newtonsoft.Json;
    using System.Net.Http.Formatting;
    using System.Net.Http.Headers;

    public static class FabricHttpClient
    {
        private static readonly ConcurrentDictionary<Uri, bool?> addresses;
        private static readonly FabricClient fabricClient;
        private static readonly HttpClient httpClient;
        private static readonly HttpCommunicationClientFactory clientFactory;
        private static readonly JsonSerializer jSerializer;

        static FabricHttpClient()
        {
            addresses = new ConcurrentDictionary<Uri, bool?>();
            fabricClient = new FabricClient();
            HttpClientHandler handler = new HttpClientHandler();

            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }

            httpClient = new HttpClient(handler);
            
            clientFactory = new HttpCommunicationClientFactory(
                ServicePartitionResolver.GetDefault(),
                "endpointName",
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(2));

            jSerializer = new JsonSerializer();  //todo - see if creating this on the fly is better or not 
        }

        public static Task<TReturn> MakeGetRequest<TReturn>(
            Uri serviceName,
            ServicePartitionKey key,
            string endpointName,
            string requestPath
        )
        {
            return MakeHttpRequest<TReturn, string>(
                    serviceName,
                    key,
                    endpointName,
                    requestPath,
                    null,
                    HttpVerb.GET
                    );
        }
        
        public static Task<TReturn> MakeHttpRequest<TReturn, TPayload>(
            Uri serviceName,
            ServicePartitionKey key,
            string endpointName,
            string requestPath,
            TPayload payload,
            HttpVerb verb
        )
        {

            var servicePartitionClient = new ServicePartitionClient<HttpCommunicationClient>(
                clientFactory,
                serviceName,
                key,
                TargetReplicaSelector.Default,
                endpointName,
                new OperationRetrySettings()
                );

            return servicePartitionClient.InvokeWithRetryAsync(
                async client =>
                {
                    if (addresses.TryAdd(client.BaseAddress, true))
                    {
                        //https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
                        //but then http://byterot.blogspot.co.uk/2016/07/singleton-httpclient-dns.html
                        //so we do this per https://github.com/NimaAra/Easy.Common/blob/master/Easy.Common/RestClient.cs
                        ServicePointManager.FindServicePoint(client.BaseAddress).ConnectionLeaseTimeout = 60 * 1000;
                    }

                    Uri newUri = new Uri(client.BaseAddress, requestPath.TrimStart('/'));
                    HttpResponseMessage response = null;

                    switch (verb)
                    {

                        case HttpVerb.GET:
                            response = await httpClient.GetAsync(newUri, HttpCompletionOption.ResponseHeadersRead);
                            break;

                        case HttpVerb.POST:
                            var content = new PushStreamContent((stream, httpContent, httpContext) =>
                            {
                                using (var writer = new StreamWriter(stream))
                                {
                                    jSerializer.Serialize(writer, payload);
                                }
                            });

                            response = await httpClient.PostAsync(newUri, content);
                            break;

                        default:
                            throw new ArgumentException("Unsupported HTTP Verb submitted for HTTP message in HTTPClientExtension");
                    }

                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var streamReader = new StreamReader(stream))
                        {
                            using (JsonReader jsonReader = new JsonTextReader(streamReader))
                            {
                                JsonSerializer serializer = new JsonSerializer();
                                return serializer.Deserialize<TReturn>(jsonReader);
                            }
                        }
                    }
                });
        }
    }
}
