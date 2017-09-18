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
        //private static readonly string MetadataDictionaryName = "MetadataDictionaryName";
        private static readonly string DoctorMetadataDictionaryName = "Doctor_{0}_Metadata";
        private static readonly string DoctorPatientDictionaryName = "Doctor_{0}_Patients";

        public DoctorController(IReliableStateManager stateManager)
        {
            this.StateManager = stateManager;
        }

        [HttpPost]
        [Route("new/{doctorId}")]
        public async Task NewDoctorAsync(Guid doctorId, [FromBody]DoctorCreationRecord record)
        {
            try
            {
                var doctorDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, DoctorCreationRecord>>(DoctorRegistrationDictionaryName);
                //var metadataDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(MetadataDictionaryName);

                //create the dictionary which holds patients for this doctor
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, PatientRegistrationRecord>>(String.Format(DoctorPatientDictionaryName, doctorId));
                //create the dictionary which holds metadatafor this doctor
                await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(String.Format(DoctorMetadataDictionaryName, doctorId));


                //long totalDoctorCount = -1;

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    if (!((await doctorDictionary.TryGetValueAsync(tx, doctorId)).HasValue))
                    {
                        //increase the total number of doctors
                        //totalDoctorCount = await metadataDictionary.AddOrUpdateAsync(tx, "DoctorCount", 1, (key, value) => value + 1);

                        //add this doctor to the list of doctors
                        await doctorDictionary.SetAsync(tx, doctorId, record);
                    }

                    await tx.CommitAsync();
                }
            }
            catch (Exception e)
            {
                var z = e;
                throw;
            }
            return;
        }

        [HttpPost]
        [Route("new/patient/{doctorId}")]
        public async Task RegisterPatientAsync(Guid doctorId, [FromBody]PatientRegistrationRecord record)
        {

            try
            {
                var doctorPatientDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, PatientRegistrationRecord>>(String.Format(DoctorPatientDictionaryName, doctorId));
                var doctorMetadataDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(String.Format(DoctorMetadataDictionaryName, doctorId));
                //var metadataDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(MetadataDictionaryName);

                //long totalPatientCount = -1;
                long doctorPatientCount = -1;

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    if (!(await doctorPatientDictionary.TryGetValueAsync(tx, record.PatientId)).HasValue)
                    {
                        //totalPatientCount = await metadataDictionary.AddOrUpdateAsync(tx, "PatientCount", 1, (key, value) => value + 1);
                        await doctorPatientDictionary.SetAsync(tx, record.PatientId, record);
                        doctorPatientCount = await doctorMetadataDictionary.AddOrUpdateAsync(tx, "PatientCount", 1, (key, value) => value + 1);
                    }

                    await tx.CommitAsync();
                }
            }
            catch (Exception e)
            {
                var z = e;
                throw;
            }



            return;
        }


        [HttpPost]
        [Route("health/{doctorId}/{personId}")]
        public async Task ReportPatientHealthAsync(Guid doctorId, Guid personId, [FromBody]HeartRateRecord latestHeartRateRecord)
        {
            try
            {
                var doctorMetadataDictionaryName = String.Format(DoctorMetadataDictionaryName, doctorId);
                var doctorMetadataDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(doctorMetadataDictionaryName);
                //var metadataDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(MetadataDictionaryName);

                //long totalReports = -1;
                long doctorReports = -1;

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    //totalReports = await metadataDictionary.AddOrUpdateAsync(tx, "HealthReportCount", 1, (key, value) => value + 1);
                    doctorReports = await doctorMetadataDictionary.AddOrUpdateAsync(tx, "HealthReportCount", 1, (key, value) => value + 1);
                    await tx.CommitAsync();
                }
            }
            catch (Exception e)
            {
                var z = e;
                throw;
            }
            return;
        }
    }
}
