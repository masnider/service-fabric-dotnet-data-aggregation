// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandActor
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Linq;
    using System.Threading.Tasks;
    using HealthMetrics.BandActor.Interfaces;
    using HealthMetrics.Common;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;
    using System.Net.Http;
    using HealthMetrics.DoctorService.Models;
    using Microsoft.ServiceFabric.Services.Client;
    using System.Threading;

    internal class BandActor : Actor, IBandActor, IRemindable
    {
        private const string GenerateHealthDataAsyncReminder = "GenerateHealthDataAsync";
        private const string GenerateAndSendHealthReportReminderName = "SendHealthReportAsync";
        private const string RegisterPatientReminderName = "RegisterPatientReminder";
        private readonly TimeSpan TimeWindow = TimeSpan.FromMinutes(2);

        private Uri doctorServiceUri;
        private CryptoRandom random = new CryptoRandom();
        private HealthIndexCalculator indexCalculator;
        private IActorReminder registrationReminder = null;
        private ServicePartitionKey doctorServicePartitionKey = null; //TODO repopulate these on recovery during onactivateasync if there is state

        public BandActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public async Task<BandDataViewModel> GetBandDataAsync()
        {
            try
            {
                //check to see if the patient name is set
                //if not this actor object hasn't been initialized
                //and we can skip the rest of the checks
                ConditionalValue<string> PatientInfoResult = await this.StateManager.TryGetStateAsync<string>("PatientName");

                if (PatientInfoResult.HasValue)
                {
                    ConditionalValue<CountyRecord> CountyInfoResult = await this.StateManager.TryGetStateAsync<CountyRecord>("CountyInfo");
                    ConditionalValue<Guid> DoctorInfoResult = await this.StateManager.TryGetStateAsync<Guid>("DoctorId");
                    ConditionalValue<HealthIndex> HeatlthInfoResult = await this.StateManager.TryGetStateAsync<HealthIndex>("HealthIndex");
                    ConditionalValue<List<HeartRateRecord>> HeartRateRecords =
                        await this.StateManager.TryGetStateAsync<List<HeartRateRecord>>("HeartRateRecords");

                    HealthIndexCalculator ic = this.indexCalculator;

                    HealthIndex healthIndex = ic.ComputeIndex(HeatlthInfoResult.Value);

                    return new BandDataViewModel(
                        DoctorInfoResult.Value,
                        this.Id.GetGuidId(),
                        PatientInfoResult.Value,
                        CountyInfoResult.Value,
                        healthIndex,
                        HeartRateRecords.Value);
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("Exception inside band actor {0}|{1}|{2}", this.Id, this.Id.Kind, e));
            }

            throw new ArgumentException(string.Format("No band actor state {0}|{1}", this.Id, this.Id.Kind));
        }

        public async Task NewAsync(BandInfo info)
        {
            await this.StateManager.SetStateAsync<CountyRecord>("CountyInfo", info.CountyInfo);
            await this.StateManager.SetStateAsync<Guid>("DoctorId", info.DoctorId);
            await this.StateManager.SetStateAsync<HealthIndex>("HealthIndex", info.HealthIndex);
            await this.StateManager.SetStateAsync<string>("PatientName", info.PersonName);
            await this.StateManager.SetStateAsync<List<HeartRateRecord>>("HeartRateRecords", new List<HeartRateRecord>());
            this.doctorServicePartitionKey = new ServicePartitionKey(HashUtil.getLongHashCode(info.DoctorId.ToString()));
            await this.RegisterRegistrationReminder();

            ActorEventSource.Current.ActorMessage(this, "Band created. ID: {0}, Name: {1}, Doctor ID: {2}", this.Id, info.PersonName, info.DoctorId);
        }

        async Task IRemindable.ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            switch (reminderName)
            {
                case RegisterPatientReminderName:
                    await this.RegisterPatientReminder();
                    await RegisterHealthReportReminder();
                    await this.UnregisterReminderAsync(this.registrationReminder);
                    this.registrationReminder = null;
                    break;

                case GenerateAndSendHealthReportReminderName:
                    await this.GenerateAndSendHealthReportAsync();
                    break;

                default:
                    ActorEventSource.Current.Message("Reminder {0} is not implemented on BandActor.", reminderName);
                    break;
            }

            return;
        }

        private async Task RegisterPatientReminder()
        {
            ConditionalValue<Guid> DoctorIdResult = await this.StateManager.TryGetStateAsync<Guid>("DoctorId");
            var docIdStr = DoctorIdResult.Value.ToString();

            var prr = new PatientRegistrationRecord(
                await this.StateManager.GetStateAsync<string>("PatientName"),
                this.Id.GetGuidId(),
                await this.StateManager.GetStateAsync<HealthIndex>("HealthIndex")
                );

            await FabricHttpClient.MakePostRequest<string, PatientRegistrationRecord>(
                    this.doctorServiceUri,
                    new ServicePartitionKey(HashUtil.getLongHashCode(docIdStr)),
                    "ServiceEndpoint",
                    "/doctor/new/patient/" + docIdStr,
                    prr,
                    SerializationSelector.PBUF,
                    CancellationToken.None
                    );
        }

        protected override Task OnActivateAsync()
        {
            ConfigurationPackage configPackage = this.ActorService.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            this.indexCalculator = new HealthIndexCalculator(this.ActorService.Context);
            this.UpdateConfigSettings(configPackage.Settings);
            this.ActorService.Context.CodePackageActivationContext.ConfigurationPackageModifiedEvent +=
                this.CodePackageActivationContext_ConfigurationPackageModifiedEvent;
            ActorEventSource.Current.ActorMessage(this, "Band activated. ID: {0}", this.Id);
            return Task.FromResult(true);
        }

        private async Task GenerateAndSendHealthReportAsync()
        {
            try
            {
                ConditionalValue<Guid> DoctorIdResult = await this.StateManager.TryGetStateAsync<Guid>("DoctorId"); 
                string docIdStr = DoctorIdResult.Value.ToString();
                HeartRateRecord record = new HeartRateRecord((float)this.random.NextDouble());

                await this.SaveHealthDataAsync(record);

                await FabricHttpClient.MakePostRequest<string, HeartRateRecord>(
                    this.doctorServiceUri,
                    this.doctorServicePartitionKey,
                    "ServiceEndpoint",
                    "doctor/health/" + this.Id.GetGuidId(),
                    record,
                    SerializationSelector.PBUF,
                    CancellationToken.None
                    );
                
                ActorEventSource.Current.Message("Health info sent from band {0} to doctor {1}", this.Id, DoctorIdResult.Value);

            }
            catch (Exception e)
            {
                ActorEventSource.Current.Message(
                    "Band Actor failed to send health data to doctor. Exception: {0}",
                    (e is AggregateException) ? e.InnerException.ToString() : e.ToString());
            }

            return;
        }

        private void UpdateConfigSettings(ConfigurationSettings configSettings)
        {
            KeyedCollection<string, ConfigurationProperty> parameters = configSettings.Sections["HealthMetrics.BandActor.Settings"].Parameters;
            this.doctorServiceUri = new ServiceUriBuilder(parameters["DoctorServiceInstanceName"].Value).ToUri();
        }

        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            this.UpdateConfigSettings(e.NewPackage.Settings);
        }

        private async Task SaveHealthDataAsync(HeartRateRecord newRecord)
        {
            ConditionalValue<List<HeartRateRecord>> HeartRateRecords = await this.StateManager.TryGetStateAsync<List<HeartRateRecord>>("HeartRateRecords");

            if (HeartRateRecords.HasValue)
            {
                List<HeartRateRecord> records = HeartRateRecords.Value;
                records = records.Where(x => DateTimeOffset.UtcNow - ((DateTimeOffset)x.Timestamp).ToUniversalTime() <= this.TimeWindow).ToList();
                records.Add(newRecord);
                await this.StateManager.SetStateAsync<List<HeartRateRecord>>("HeartRateRecords", records);
            }
            return;
        }

        private async Task RegisterHealthReportReminder()
        {
            await this.RegisterReminderAsync(GenerateAndSendHealthReportReminderName, null, TimeSpan.FromSeconds(this.random.Next(1, 30)), TimeSpan.FromSeconds(1));
        }

        private async Task RegisterRegistrationReminder()
        {
            this.registrationReminder = await this.RegisterReminderAsync(RegisterPatientReminderName, null, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1));
        }
    }
}