// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.Common
{
    using System.Runtime.Serialization;
    using HealthMetrics.Common;
    using ProtoBuf;

    [DataContract]
    [ProtoContract]
    public struct DoctorStatsViewModel
    {
        public DoctorStatsViewModel(int patientCount, long healthReportCount, HealthIndex averageHealthIndex, string doctorName)
        {
            this.PatientCount = patientCount;
            this.HealthReportCount = healthReportCount;
            this.AverageHealthIndex = averageHealthIndex;
            this.DoctorName = doctorName;
        }

        [DataMember]
        [ProtoMember(1)]
        public int PatientCount { get; private set; }

        [DataMember]
        [ProtoMember(2)]
        public long HealthReportCount { get; private set; }

        [DataMember]
        [ProtoMember(3)]
        public HealthIndex AverageHealthIndex { get; private set; }

        [DataMember]
        [ProtoMember(4)]
        public string DoctorName { get; private set; }
    }
}