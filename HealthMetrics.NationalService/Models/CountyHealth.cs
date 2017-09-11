// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService.Models
{
    using System.Runtime.Serialization;
    using HealthMetrics.Common;
    using ProtoBuf;

    [DataContract]
    [ProtoContract]
    public struct CountyHealth
    {
        [DataMember]
        [ProtoMember(1)]
        public int Id { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public HealthIndex Health { get; set; }
    }
}