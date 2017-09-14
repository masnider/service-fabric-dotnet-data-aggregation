using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HealthMetrics.DoctorService.Models;
using HealthMetrics.Common;

namespace HealthMetrics.DoctorService.Controllers
{
    [Route("doctor")]
    public class DoctorController : Controller
    {
        [HttpPost]
        [Route("health/{personId}")]
        public async Task ReportPatientHealthAsync(Guid personId, [FromBody]HeartRateRecord latestHeartRateRecord)
        {

        }

        [HttpPost]
        [Route("new/{doctorId}")]
        public async Task NewDoctorAsync(Guid doctorId, [FromBody]DoctorCreationRecord record)
        {
            Console.WriteLine(doctorId);
            return;
        }

        [HttpPost]
        [Route("new/patient/{doctorId}")]
        public async Task RegisterPatientAsync(Guid doctorId, [FromBody]PatientRegistrationRecord record)
        {

        }
    }
}
