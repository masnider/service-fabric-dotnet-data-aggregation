using Microsoft.ServiceFabric.Data;
using System;
using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;
using HealthMetrics.DoctorService.Models;

namespace HealthMetrics.CountyService.Models
{
    class DoctorCreationRecordSerializer : IStateSerializer<DoctorCreationRecord>
    {

        public DoctorCreationRecordSerializer()
        {
            //https://stackoverflow.com/questions/7372585/protobuf-net-exception-timeout-while-inspecting-metadata
            //https://stackoverflow.com/questions/17096359/is-protobuf-net-thread-safe 
            RuntimeTypeModel.Default.MetadataTimeoutMilliseconds = 300000;
        }

        public DoctorCreationRecord Read(BinaryReader binaryReader)
        {
            return Serializer.Deserialize<DoctorCreationRecord>(binaryReader.BaseStream);
        }

        public void Write(DoctorCreationRecord value, BinaryWriter binaryWriter)
        {
            Serializer.Serialize<DoctorCreationRecord>(binaryWriter.BaseStream, value);
        }

        public void Write(DoctorCreationRecord baseValue, DoctorCreationRecord targetValue, BinaryWriter binaryWriter)
        {
            Write(targetValue, binaryWriter);
        }

        public DoctorCreationRecord Read(DoctorCreationRecord baseValue, BinaryReader binaryReader)
        {
            return Read(binaryReader);
        }

    }
}
