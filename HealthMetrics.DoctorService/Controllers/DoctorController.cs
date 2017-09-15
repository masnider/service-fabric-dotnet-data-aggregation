using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HealthMetrics.DoctorService.Models;
using HealthMetrics.Common;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace HealthMetrics.DoctorService.Controllers
{
    [Route("doctor")]
    public class DoctorController : Controller
    {

        private readonly IReliableStateManager StateManager;
        private static readonly string DoctorRegistrationDictionaryName = "DoctorRegistrationDictionaryName";
        private static readonly string MetadataDictionaryName = "MetadataDictionaryName";
        private static readonly string DoctorMetadataDictionaryName = "Doctor_{0}_Metadata";
        private static readonly string DoctorPatientDictionaryName = "Doctor_{0}_Patients";

        public DoctorController(IReliableStateManager stateManager)
        {
            this.StateManager = stateManager;
        }

        [HttpPost]
        [Route("health/{doctorId}/{personId}")]
        public async Task ReportPatientHealthAsync(Guid doctorId, Guid personId, [FromBody]HeartRateRecord latestHeartRateRecord)
        {
            var doctorMetadataDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(String.Format(DoctorMetadataDictionaryName, doctorId));
            var metadataDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(MetadataDictionaryName);

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await metadataDictionary.AddOrUpdateAsync(tx, "HealthReportCount", 1, (key, value) => value + 1);
                await doctorMetadataDictionary.AddOrUpdateAsync(tx, "HealthReportCount", 1, (key, value) => value + 1);
                await tx.CommitAsync();
            }

            return;
        }

        [HttpPost]
        [Route("new/{doctorId}")]
        public async Task NewDoctorAsync(Guid doctorId, [FromBody]DoctorCreationRecord record)
        {
            var doctorDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, DoctorCreationRecord>>(DoctorRegistrationDictionaryName);
            var metadataDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(MetadataDictionaryName);

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                if (!((await doctorDictionary.TryGetValueAsync(tx, doctorId)).HasValue))
                {
                    await metadataDictionary.AddOrUpdateAsync(tx, "DoctorCount", 1, (key, value) => value + 1);
                    await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, PatientRegistrationRecord>>(tx, String.Format(DoctorPatientDictionaryName, doctorId));
                    await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(tx, String.Format(DoctorMetadataDictionaryName, doctorId));
                    await doctorDictionary.SetAsync(tx, doctorId, record);
                }

                await tx.CommitAsync();
            }
            return;
        }

        [HttpPost]
        [Route("new/patient/{doctorId}")]
        public async Task RegisterPatientAsync(Guid doctorId, [FromBody]PatientRegistrationRecord record)
        {
            var doctorPatientDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, PatientRegistrationRecord>>(String.Format(DoctorPatientDictionaryName, doctorId));
            var doctorMetadataDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(String.Format(DoctorMetadataDictionaryName, doctorId));
            var metadataDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(MetadataDictionaryName);

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                if (!(await doctorPatientDictionary.TryGetValueAsync(tx, record.PatientId)).HasValue)
                {
                    await metadataDictionary.AddOrUpdateAsync(tx, "PatientCount", 1, (key, value) => value + 1);
                    await doctorPatientDictionary.SetAsync(tx, record.PatientId, record);
                    await doctorMetadataDictionary.AddOrUpdateAsync(tx, "PatientCount", 1, (key, value) => value + 1);
                }

                await tx.CommitAsync();
            }
            return;
        }
    }
}
