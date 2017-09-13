using Microsoft.ServiceFabric.Data;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthMetrics.NationalService.Models
{
    class NationalCountyStatsSerializer : IStateSerializer<NationalCountyStats>
    {
        public NationalCountyStatsSerializer()
        {
            //https://stackoverflow.com/questions/7372585/protobuf-net-exception-timeout-while-inspecting-metadata
            //https://stackoverflow.com/questions/17096359/is-protobuf-net-thread-safe 
            RuntimeTypeModel.Default.MetadataTimeoutMilliseconds = 300000;
        }

        public NationalCountyStats Read(BinaryReader binaryReader)
        {
            return Serializer.Deserialize<NationalCountyStats>(binaryReader.BaseStream);
        }

        public void Write(NationalCountyStats value, BinaryWriter binaryWriter)
        {
            Serializer.Serialize<NationalCountyStats>(binaryWriter.BaseStream, value);
        }

        public void Write(NationalCountyStats baseValue, NationalCountyStats targetValue, BinaryWriter binaryWriter)
        {
            Write(targetValue, binaryWriter);
        }

        public NationalCountyStats Read(NationalCountyStats baseValue, BinaryReader binaryReader)
        {
            return Read(binaryReader);
        }

    }
}
