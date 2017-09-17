// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.Common
{
    //using System.Runtime.Serialization;
    //using HealthMetrics.Common;
    using ProtoBuf;
    using System;

    [ProtoContract]
    public struct DoctorStatsViewModel
    {
        //public DoctorStatsViewModel(Guid doctorId, int countyId, int patientCount, long healthReportCount, HealthIndex averageHealthIndex, string doctorName)
        public DoctorStatsViewModel(Guid doctorId, int countyId, int patientCount, long healthReportCount, HealthIndex averageHealthIndex)
        {
            this.DoctorId = doctorId;
            this.countyId = countyId;
            this.PatientCount = patientCount;
            this.HealthReportCount = healthReportCount;
            this.AverageHealthIndex = averageHealthIndex;
            //this.DoctorName = doctorName;
        }

        [ProtoMember(1)]
        public Guid DoctorId { get; private set; }

        [ProtoMember(2)]
        public int countyId { get; private set; }

        [ProtoMember(3)]
        public int PatientCount { get; private set; }

        [ProtoMember(4)]
        public long HealthReportCount { get; private set; }

        [ProtoMember(5)]
        public HealthIndex AverageHealthIndex { get; private set; }

        //[ProtoMember(6)]
        //public string DoctorName { get; private set; }
    }
}