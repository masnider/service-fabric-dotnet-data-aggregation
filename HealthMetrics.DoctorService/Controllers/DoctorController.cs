using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HealthMetrics.DoctorService.Models;
using HealthMetrics.Common;
using Microsoft.ServiceFabric.Data;

namespace HealthMetrics.DoctorService.Controllers
{
    [Route("doctor")]
    public class DoctorController : Controller
    {

        private readonly IReliableStateManager StateManager;
        private static readonly string DoctorRegistrationDictionaryName = "DoctorRegistrationDictionaryName";
        private static readonly string PatientReportDictionaryName = "PatientReportDictionaryName";
        private static readonly string MetadataDictionaryName = "MetadataDictionaryName";

        public DoctorController(IReliableStateManager stateManager)
        {
            this.StateManager = stateManager;
        }

        [HttpPost]
        [Route("health/{personId}")]
        public async Task ReportPatientHealthAsync(Guid personId, [FromBody]HeartRateRecord latestHeartRateRecord)
        {
            Console.WriteLine(personId);
            return;
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
            Console.WriteLine(doctorId);
            return;
        }
    }
}
