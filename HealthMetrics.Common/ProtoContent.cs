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
        //    protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        //    {
        //        throw new NotImplementedException();
        //    }

        //    protected override bool TryComputeLength(out long length)
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

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
                data = ms.GetBuffer();
                size = ms.Length;
                
                //4.6
                //if (!ms.TryGetBuffer(out data))
                //{
                //    throw new ArgumentException("Tried to get buffer for protobuf and failed");
                //}

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