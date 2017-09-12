// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandActor.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using HealthMetrics.Common;
    using ProtoBuf;

    [DataContract]
    [ProtoContract]
    public struct BandDataViewModel
    {
        public BandDataViewModel(
            Guid doctorId,
            Guid bandId,
            string patientName,
            CountyRecord countyInfo,
            HealthIndex healthIndexValue,
            IEnumerable<HeartRateRecord> heartRateHistory)
        {
            this.DoctorId = doctorId;
            this.PersonId = bandId;
            this.PersonName = patientName; //used
            this.CountyInfo = countyInfo; //used
            this.HealthIndexValue = healthIndexValue; //used
            this.HeartRateHistory = heartRateHistory; //used
        }

        [DataMember]
        public Guid DoctorId { get; private set; }

        [DataMember]
        public Guid PersonId { get; private set; }

        [DataMember]
        [ProtoMember(1)]
        public string PersonName { get; private set; }

        [DataMember]
        [ProtoMember(2)]
        public CountyRecord CountyInfo { get; private set; }

        [DataMember]
        [ProtoMember(3)]
        public HealthIndex HealthIndexValue { get; private set; }

        [DataMember]
        [ProtoMember(4)]
        public IEnumerable<HeartRateRecord> HeartRateHistory { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}|{1}|{2}|{3}|{4}", this.DoctorId, this.PersonId, this.PersonName, this.CountyInfo, this.HealthIndexValue);
        }
    }
}