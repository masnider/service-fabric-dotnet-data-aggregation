using HealthMetrics.Common;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthMetrics.DoctorService.Models
{
    [ProtoContract]
    public struct DoctorCreationRecord
    {
        [ProtoMember(1)]
        public string DoctorName { get; private set; }

        [ProtoMember(2)]
        public Guid DoctorId { get; private set; }

        [ProtoMember(3)]
        public CountyRecord CountyInfo { get; private set; }

        public DoctorCreationRecord(string name, Guid id, CountyRecord countyInfo)
        {
            this.DoctorName = name;
            this.DoctorId = id;
            this.CountyInfo = countyInfo;
        }
    }
}
