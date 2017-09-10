using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace HealthMetrics.Common
{
    class ProtobufMediaFormatter : MediaTypeFormatter
    {
        public ProtobufMediaFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/x-protobuf"));
        }

        public override bool CanReadType(Type type)
        {
            return false;
        }

        public override bool CanWriteType(Type type)
        {
            return true;
        }
    }
}
