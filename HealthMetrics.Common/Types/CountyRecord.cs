// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.Common
{
    using ProtoBuf;
    using System.Runtime.Serialization;

    [DataContract]
    [ProtoContract]
    public struct CountyRecord
    {
        [DataMember]
        [ProtoMember(1)]
        public string CountyName { get; private set; }

        [DataMember]
        [ProtoMember(2)]
        public int CountyId { get; private set; }

        [DataMember]
        [ProtoMember(3)]
        public double CountyHealth { get; private set; }

        public CountyRecord(string name, int id, double countyHealth)
        {
            this.CountyName = name;
            this.CountyId = id;
            this.CountyHealth = countyHealth;
        }

        public long GetLongPartitionKey()
        {
            return HashUtil.getLongHashCode(this.ToString());
        }

        public static bool operator ==(CountyRecord a, CountyRecord b)
        {
            if (a.CountyHealth == b.CountyHealth
                && a.CountyId == b.CountyId
                && a.CountyName == b.CountyName)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool operator !=(CountyRecord a, CountyRecord b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return string.Format("[{0}|{1}]", this.CountyId, this.CountyHealth);
        }

        public override bool Equals(object obj)
        {
            return (this == (CountyRecord)obj);
        }

        public override int GetHashCode()
        {
            return HashUtil.getIntHashCode(this.ToString());
        }
    }
}