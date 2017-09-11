using System;
using System.Collections.Generic;
using System.IO;
//using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace HealthMetrics.Common
{
    public class ProtoContent : HttpContent
    {
        public object SerializationTarget { get; private set; }
        private byte[] data;
        private long size = -1;
        public ProtoContent(object serializationTarget)
        {
            try
            {
                SerializationTarget = serializationTarget;
                this.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
                MemoryStream ms = new MemoryStream();
                Serializer.Serialize(ms, SerializationTarget);
                ms.Flush();

                // https://stackoverflow.com/a/4960973/4852187
                // Otherwise - https://github.com/svn2github/protobuf-net/blob/master/protobuf-net/ProtoReader.cs#L1320
                data = ms.GetBuffer();
                size = ms.Length;
               
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
                throw z;
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = size;
            return true;
        }
    }
}