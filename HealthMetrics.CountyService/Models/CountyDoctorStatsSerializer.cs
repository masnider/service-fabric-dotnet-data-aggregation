using Microsoft.ServiceFabric.Data;
using System;
using System.IO;
using ProtoBuf;

namespace HealthMetrics.CountyService
{
    class CountyDoctorStatsSerializer : IStateSerializer<CountyDoctorStats>
    {
        public CountyDoctorStats Read(BinaryReader binaryReader)
        {
            return Serializer.Deserialize<CountyDoctorStats>(binaryReader.BaseStream);
        }

        public void Write(CountyDoctorStats value, BinaryWriter binaryWriter)
        {
            Serializer.Serialize<CountyDoctorStats>(binaryWriter.BaseStream, value);
        }

        public void Write(CountyDoctorStats baseValue, CountyDoctorStats targetValue, BinaryWriter binaryWriter)
        {
            Write(targetValue, binaryWriter);
        }

        public CountyDoctorStats Read(CountyDoctorStats baseValue, BinaryReader binaryReader)
        {
            return Read(binaryReader);
        }

    }
}
