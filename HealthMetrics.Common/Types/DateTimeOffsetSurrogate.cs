using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthMetrics.Common
{
    //System.InvalidOperationException: No serializer defined for type: System.DateTimeOffset
    //Code to solve both issues from: https://stackoverflow.com/a/39090557/4852187 
    [ProtoContract]
    public class DateTimeOffsetSurrogate
    {
        [ProtoMember(1)]
        public string DateTimeString { get; set; }

        public static implicit operator DateTimeOffsetSurrogate(DateTimeOffset value)
        {
            return new DateTimeOffsetSurrogate { DateTimeString = value.ToString("o") };
        }

        public static implicit operator DateTimeOffset(DateTimeOffsetSurrogate value)
        {
            try
            {
                return DateTimeOffset.Parse(value.DateTimeString, null, DateTimeStyles.RoundtripKind);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to parse date time value: " + value.DateTimeString, ex);
            }
        }
    }
}
