// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.CountyService
{
    using HealthMetrics.Common;
    using ProtoBuf;

    [ProtoContract]
    public struct CountyDoctorStats
    {
        public CountyDoctorStats(int patientCount, long healthReportCount, HealthIndex averageHealthIndex)
        {
            this.PatientCount = patientCount;
            this.HealthReportCount = healthReportCount;
            this.AverageHealthIndex = averageHealthIndex;
        }

        [ProtoMember(1)]
        public int PatientCount { get; private set; }

        [ProtoMember(2)]
        public long HealthReportCount { get; private set; }

        [ProtoMember(3)]
        public HealthIndex AverageHealthIndex { get; private set; }
    }
}