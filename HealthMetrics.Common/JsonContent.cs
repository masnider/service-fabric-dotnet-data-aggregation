using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HealthMetrics.Common
{
    public class JsonContent : HttpContent
    {
        public object SerializationTarget { get; private set; }
        private byte[] data;
        private long size = -1;
        public JsonContent(object serializationTarget)
        {
            try
            {
                SerializationTarget = serializationTarget;
                var header = new MediaTypeHeaderValue("application/json");
                data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(SerializationTarget));
                size = data.Length;
            }
            catch (Exception e)
            {
                var z = e;
                throw;
            }
        }
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            try
            {
                await stream.WriteAsync(data, 0, (int)size);
                await stream.FlushAsync();
            }
            catch (Exception e)
            {
                var z = e;
                throw;
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = size;
            return true;
        }
    }
}