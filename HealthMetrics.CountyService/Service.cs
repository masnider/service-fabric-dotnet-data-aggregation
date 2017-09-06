// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.CountyService
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using HealthMetrics.Common;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Newtonsoft.Json;
    using Web.Service;
    using System.Net.Http;

    public class Service : StatefulService
    {
        internal const string ServiceTypeName = "HealthMetrics.CountyServiceType";
        internal const string ConfigSectionName = "HealthMetrics.CountyService.Settings";
        internal const string CountyNameDictionaryName = "CountyNames";
        internal const string CountyHealthDictionaryName = "{0}-Health";
        object ConfigPackageLockObject = new object();

        private KeyedCollection<string, ConfigurationProperty> configPackageSettings;

        private readonly HealthIndexCalculator indexCalculator;

        public Service(StatefulServiceContext serviceContext) : base(serviceContext)
        {
            InitConfig();
            this.indexCalculator = new HealthIndexCalculator(serviceContext);
        }

        public Service(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica)
            : base(serviceContext, reliableStateManagerReplica)
        {
            InitConfig();
            this.indexCalculator = new HealthIndexCalculator(serviceContext);
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.ServiceMessage(this, "CountyService starting data processing.");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {

                    //every interval seconds, grab the counties and send them to national
                    await Task.Delay(TimeSpan.FromSeconds(int.Parse(this.GetSetting("UpdateFrequency"))), cancellationToken);

                    IReliableDictionary<int, string> countyNamesDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<int, string>>(CountyNameDictionaryName);

                    IList<KeyValuePair<int, string>> countyNames = new List<KeyValuePair<int, string>>();

                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        IAsyncEnumerator<KeyValuePair<int, string>> enumerator = (await countyNamesDictionary.CreateEnumerableAsync(tx)).GetAsyncEnumerator();

                        while (await enumerator.MoveNextAsync(cancellationToken))
                        {
                            countyNames.Add(enumerator.Current);
                        }

                        await tx.CommitAsync();
                    }

                    foreach (KeyValuePair<int, string> county in countyNames)
                    {
                        IReliableDictionary<Guid, CountyDoctorStats> countyHealth =
                            await
                                this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, CountyDoctorStats>>(
                                    string.Format(CountyHealthDictionaryName, county.Key));

                        int totalDoctorCount = 0;
                        int totalPatientCount = 0;
                        long totalHealthReportCount = 0;
                        //double priorAvg = 0;
                        //double expandedAverage = 0;
                        //double newTotal = 0;

                        IList<KeyValuePair<Guid, CountyDoctorStats>> records = new List<KeyValuePair<Guid, CountyDoctorStats>>();

                        using (ITransaction tx = this.StateManager.CreateTransaction())
                        {
                            IAsyncEnumerable<KeyValuePair<Guid, CountyDoctorStats>> healthRecords = await countyHealth.CreateEnumerableAsync(tx, EnumerationMode.Unordered);

                            IAsyncEnumerator<KeyValuePair<Guid, CountyDoctorStats>> enumerator = healthRecords.GetAsyncEnumerator();

                            while (await enumerator.MoveNextAsync(cancellationToken))
                            {
                                records.Add(enumerator.Current);
                            }

                            await tx.CommitAsync();
                        }
                            
                        foreach (KeyValuePair<Guid, CountyDoctorStats> item in records)
                        {
                            
                            //expandedAverage = priorAvg * totalDoctorCount;
                            //newTotal = expandedAverage + item.Value.AverageHealthIndex.GetValue();

                            totalDoctorCount++;
                            totalPatientCount += item.Value.PatientCount;
                            totalHealthReportCount += item.Value.HealthReportCount;


                            //priorAvg = newTotal / totalHealthReportCount;

                        }

                        HealthIndex avgHealth = this.indexCalculator.ComputeAverageIndex(records.Select(x => x.Value.AverageHealthIndex));

                        CountyStatsViewModel payload = new CountyStatsViewModel(totalDoctorCount, totalPatientCount, totalHealthReportCount, avgHealth);

                        ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting("NationalServiceName"));

                        ServicePrimer primer = new ServicePrimer();
                        await primer.WaitForStatefulService(serviceUri.ToUri(), CancellationToken.None);

                        await FabricHttpClient.MakePostRequest<string, CountyStatsViewModel>(
                            serviceUri.ToUri(),
                            new ServicePartitionKey(),
                            "ServiceEndpoint",
                            "/national/health/" + county.Key,
                            payload,
                            cancellationToken
                            );

                    }
                }
                catch (TimeoutException te)
                {
                    // transient error. Retry.
                    ServiceEventSource.Current.ServiceMessage(
                        this,
                        "CountyService encountered an exception trying to send data to National Service: TimeoutException in RunAsync: {0}",
                        te.ToString());
                }
                catch (FabricNotReadableException)
                {
                    // transient error. Retry.
                }
                catch (FabricTransientException fte)
                {
                    // transient error. Retry.
                    ServiceEventSource.Current.ServiceMessage(
                        this,
                        "CountyService encountered an exception trying to send data to National Service: FabricTransientException in RunAsync: {0}",
                        fte.ToString());
                }
                catch (FabricNotPrimaryException)
                {
                    // not primary any more, time to quit.
                    return;
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.ServiceMessage(this, "{0}", ex.ToString());
                    throw;
                }
            }
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(
                    (initParams) =>
                        new HttpCommunicationListener(
                            "healthcounty",
                            new Startup(this.StateManager, new HealthIndexCalculator(this.Context)),
                            this.Context), "ServiceEndpoint")
            };
        }

        private void UpdateConfigSettings(ConfigurationSettings configSettings)
        {
            lock (ConfigPackageLockObject)
            {
                this.configPackageSettings = configSettings.Sections[ConfigSectionName].Parameters;
            }
        }

        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            this.UpdateConfigSettings(e.NewPackage.Settings);
        }

        private string GetSetting(string key)
        {
            lock (ConfigPackageLockObject)
            {
                return this.configPackageSettings[key].Value;
            }
        }

        private void InitConfig()
        {
            ConfigurationPackage configPackage = this.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            this.Context.CodePackageActivationContext.ConfigurationPackageModifiedEvent
                += this.CodePackageActivationContext_ConfigurationPackageModifiedEvent;

            this.UpdateConfigSettings(configPackage.Settings);
        }
    }
}