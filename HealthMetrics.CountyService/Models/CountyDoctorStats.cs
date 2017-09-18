// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.CountyService
{
    using HealthMetrics.Common;
    using ProtoBuf;
    using System.Runtime.Serialization;

    [DataContract]
    [ProtoContract]
    public struct CountyDoctorStats
    {
        public CountyDoctorStats(int patientCount, long healthReportCount, string doctorName, HealthIndex averageHealthIndex)
        {
            this.DoctorName = doctorName;
            this.PatientCount = patientCount;
            this.HealthReportCount = healthReportCount;
            this.AverageHealthIndex = averageHealthIndex;
        }


        [DataMember]
        [ProtoMember(1)]
        public string DoctorName { get; private set; }

        [DataMember]
        [ProtoMember(2)]
        public int PatientCount { get; private set; }

        [DataMember]
        [ProtoMember(3)]
        public long HealthReportCount { get; private set; }

        [DataMember]
        [ProtoMember(4)]
        public HealthIndex AverageHealthIndex { get; private set; }
    }
}