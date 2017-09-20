using Microsoft.ServiceFabric.Data;
using System;
using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;
using HealthMetrics.DoctorService.Models;

namespace HealthMetrics.CountyService.Models
{
    class PatientRegistrationRecordSerializer : IStateSerializer<PatientRegistrationRecord>
    {

        public PatientRegistrationRecordSerializer()
        {
            //https://stackoverflow.com/questions/7372585/protobuf-net-exception-timeout-while-inspecting-metadata
            //https://stackoverflow.com/questions/17096359/is-protobuf-net-thread-safe 
            RuntimeTypeModel.Default.MetadataTimeoutMilliseconds = 300000;
        }

        public PatientRegistrationRecord Read(BinaryReader binaryReader)
        {
            return Serializer.Deserialize<PatientRegistrationRecord>(binaryReader.BaseStream);
        }

        public void Write(PatientRegistrationRecord value, BinaryWriter binaryWriter)
        {
            Serializer.Serialize<PatientRegistrationRecord>(binaryWriter.BaseStream, value);
        }

        public void Write(PatientRegistrationRecord baseValue, PatientRegistrationRecord targetValue, BinaryWriter binaryWriter)
        {
            Write(targetValue, binaryWriter);
        }

        public PatientRegistrationRecord Read(PatientRegistrationRecord baseValue, BinaryReader binaryReader)
        {
            return Read(binaryReader);
        }

    }
}
