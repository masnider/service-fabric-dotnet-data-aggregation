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
            string doctorDictionaryName = String.Format(DoctorPatientDictionaryName, doctorId);
            string doctorMetadataDictionaryName = String.Format(DoctorMetadataDictionaryName, doctorId);

            try
            {
                //doctordictionary is just a list of GuidId to the actual doctor information
                var doctorDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, DoctorCreationRecord>>(DoctorRegistrationDictionaryName);

                //create the dictionary which holds patients for this doctor
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, PatientRegistrationRecord>>(doctorDictionaryName);

                //create the dictionary which holds metadata for this doctor
                await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(doctorMetadataDictionaryName);

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    if (!((await doctorDictionary.TryGetValueAsync(tx, doctorId)).HasValue))
                    {
                        //add this doctor to the list of doctors
                        await doctorDictionary.SetAsync(tx, doctorId, record);
                    }

                    await tx.CommitAsync();
                }
            }
            catch (Exception e)
            {
                // transient error. Retry.
                ServiceEventSource.Current.Message("Exception in DoctorController, NewDoctor {0}", e.ToString());
                throw;
            }

            ServiceEventSource.Current.Message("Successfully registered doctor {0}. PL: {1} D: {2}", doctorId, doctorDictionaryName, doctorMetadataDictionaryName);
            return;
        }

        [HttpPost]
        [Route("new/patient/{doctorId}")]
        public async Task RegisterPatientAsync(Guid doctorId, [FromBody]PatientRegistrationRecord record)
        {
            string doctorPatientDictionaryName = String.Format(DoctorPatientDictionaryName, doctorId);
            string doctorMetadataDictionaryName = String.Format(DoctorMetadataDictionaryName, doctorId);
            long doctorPatientCount = -1;

            try
            {
                var doctorPatientDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, PatientRegistrationRecord>>(doctorPatientDictionaryName);
                var doctorMetadataDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(doctorMetadataDictionaryName);

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    if (!(await doctorPatientDictionary.TryGetValueAsync(tx, record.PatientId)).HasValue)
                    {
                        await doctorPatientDictionary.SetAsync(tx, record.PatientId, record);
                        doctorPatientCount = await doctorMetadataDictionary.AddOrUpdateAsync(tx, "PatientCount", 1, (key, value) => value + 1);
                    }

                    await tx.CommitAsync();
                }
            }
            catch (Exception e)
            {
                // transient error. Retry.
                ServiceEventSource.Current.Message("Exception in DoctorController RegisterPatient {0}", e.ToString());
                throw;
            }

            ServiceEventSource.Current.Message("Successfully registered patient {0}. PL: {1} D: {2} Count {3}", doctorId, doctorPatientDictionaryName, doctorMetadataDictionaryName, doctorPatientCount);
            return;
        }


        [HttpPost]
        [Route("health/{doctorId}/{personId}")]
        public async Task ReportPatientHealthAsync(Guid doctorId, Guid personId, [FromBody]HeartRateRecord latestHeartRateRecord)
        {
            string doctorMetadataDictionaryName = String.Format(DoctorMetadataDictionaryName, doctorId);
            long doctorHealthReportCount = -1;

            try
            {
                var doctorMetadataDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(doctorMetadataDictionaryName);

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    doctorHealthReportCount = await doctorMetadataDictionary.AddOrUpdateAsync(tx, "HealthReportCount", 1, (key, value) => value + 1);
                    await tx.CommitAsync();
                }
            }
            catch (Exception e)
            {
                // transient error. Retry.
                ServiceEventSource.Current.Message("Exception in DoctorController Report Patient Health {0}", e.ToString());
                throw;
            }

            ServiceEventSource.Current.Message("Successfully handled patient health reprot D_ID {0} P_ID {1} Count {2}", doctorId, personId, doctorHealthReportCount);
            return;
        }
    }
}