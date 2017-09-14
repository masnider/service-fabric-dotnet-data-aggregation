using HealthMetrics.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthMetrics.DoctorService.Models
{
    public class PatientRegistrationRecord
    {
        public Guid PatientId { get; private set; }
        public string PatientName { get; private set; }
        public HealthIndex PatientHealthIndex { get; private set; }

        public PatientRegistrationRecord(string name, Guid id, HealthIndex healthIndex)
        {
            this.PatientName = name;
            this.PatientId = id;
            this.PatientHealthIndex = healthIndex;
        }

    }
}
