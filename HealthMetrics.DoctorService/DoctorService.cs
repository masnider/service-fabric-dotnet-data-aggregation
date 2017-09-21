using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Data;
using HealthMetrics.Common;
using Microsoft.ServiceFabric.Data.Collections;
using HealthMetrics.DoctorService.Models;
using System.Collections.Concurrent;
using System.Net.Http;
using BladeRuiner.Common.ServiceUtilities;
using Microsoft.ServiceFabric.Services.Client;
using HealthMetrics.CountyService.Models;

namespace HealthMetrics.DoctorService
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class DoctorService : StatefulService
    {

        private static readonly string DoctorRegistrationDictionaryName = "DoctorRegistrationDictionaryName";
        private static readonly string DoctorPatientDictionaryName = "Doctor_{0}_Patients";
        private static readonly string DoctorMetadataDictionaryName = "Doctor_{0}_Metadata";
        private readonly ServiceConfigReader scr;
        private readonly Uri CountyServiceUri;
        private HealthIndexCalculator indexCalculator;

        public DoctorService(StatefulServiceContext context)
            : base(context)
        {
            this.indexCalculator = new HealthIndexCalculator(context);
            this.scr = new ServiceConfigReader("Config");
            ServiceUriBuilder serviceUriBuilder = new ServiceUriBuilder(this.scr["HealthMetrics.DoctorService.Settings"]["CountyServiceInstanceName"]);
            this.CountyServiceUri = serviceUriBuilder.ToUri();
            this.StateManager.TryAddStateSerializer<DoctorCreationRecord>(new DoctorCreationRecordSerializer());
            this.StateManager.TryAddStateSerializer<PatientRegistrationRecord>(new PatientRegistrationRecordSerializer());
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[]
            {
                new ServiceReplicaListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return new WebHostBuilder()
                                    .UseKestrel()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<StatefulServiceContext>(serviceContext)
                                            .AddSingleton<IReliableStateManager>(this.StateManager))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                                    .UseUrls(url)
                                    .Build();
                    }), "ServiceEndpoint")
            };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    ConcurrentDictionary<int, List<KeyValuePair<Guid, string>>> countyDoctorMap = new ConcurrentDictionary<int, List<KeyValuePair<Guid, string>>>();

                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        var doctorDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, DoctorCreationRecord>>(DoctorRegistrationDictionaryName);
                        var enumerator = (await doctorDictionary.CreateEnumerableAsync(tx)).GetAsyncEnumerator();

                        while (await enumerator.MoveNextAsync(cancellationToken))
                        {

                            var doctorListItem = enumerator.Current;
                            Guid doctorId = doctorListItem.Key;
                            int countyId = doctorListItem.Value.CountyInfo.CountyId;
                            string name = doctorListItem.Value.DoctorName;

                            //TODO: Evaluate if this will correctly always add, or if it will get overwritten
                            countyDoctorMap.AddOrUpdate(
                                countyId,
                                new List<KeyValuePair<Guid, string>>() { new KeyValuePair<Guid, string>(doctorId, name) },
                                (id, existingList) =>
                                    {
                                        existingList.Add(new KeyValuePair<Guid, string>(doctorId, name));
                                        return existingList;
                                    }
                             );

                        }

                        await tx.CommitAsync();
                    }

                    foreach (KeyValuePair<int, List<KeyValuePair<Guid, string>>> info in countyDoctorMap) //should actually be able to do these in parallel
                    {
                        List<DoctorStatsViewModel> countyDoctorStats = new List<DoctorStatsViewModel>();

                        foreach (var docInfo in info.Value) //these should go in parallel too
                        {

                            int patientCount = 0;
                            long healthReportCount = 0;

                            string doctorMetadataDictionaryName = String.Format(DoctorMetadataDictionaryName, docInfo.Key);

                            var doctorMetadataDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(doctorMetadataDictionaryName);

                            using (ITransaction tx = this.StateManager.CreateTransaction())
                            {
                                var reportCountResult = await doctorMetadataDictionary.TryGetValueAsync(tx, "HealthReportCount");
                                if (reportCountResult.HasValue)
                                {
                                    healthReportCount = reportCountResult.Value;
                                }

                                var patientCountResult = await doctorMetadataDictionary.TryGetValueAsync(tx, "PatientCount");
                                if (patientCountResult.HasValue)
                                {
                                    patientCount = (int)patientCountResult.Value;
                                }

                                await tx.CommitAsync();
                            }

                            HealthIndex avgHealthIndex = await GetAveragePatientHealthInfoAsync(docInfo.Key, cancellationToken);
                            countyDoctorStats.Add(new DoctorStatsViewModel(docInfo.Key, info.Key, patientCount, healthReportCount, avgHealthIndex, docInfo.Value));
                        }

                        await FabricHttpClient.MakePostRequest<string, List<DoctorStatsViewModel>>(
                            this.CountyServiceUri,
                            new ServicePartitionKey(info.Key),
                            "ServiceEndpoint",
                            "county/health/",
                            countyDoctorStats,
                            SerializationSelector.PBUF,
                            cancellationToken
                            );
                    }
                }
                catch (TimeoutException te)
                {
                    // transient error. Retry.
                    ServiceEventSource.Current.ServiceMessage(
                        this.Context,
                        "DoctorService encountered an exception trying to send data to County Service: TimeoutException in RunAsync: {0}",
                        te.ToString());
                }
                catch (FabricNotReadableException fnre)
                {
                    // transient error. Retry.
                    ServiceEventSource.Current.ServiceMessage(
                        this.Context,
                        "DoctorService encountered an exception trying to send data to County Service: FabricNotReadableException in RunAsync: {0}",
                        fnre.ToString());
                }
                catch (FabricTransientException fte)
                {
                    // transient error. Retry.
                    ServiceEventSource.Current.ServiceMessage(
                        this.Context,
                        "DoctorService encountered an exception trying to send data to County Service: FabricTransientException in RunAsync: {0}",
                        fte.ToString());
                }
                catch (HttpRequestException hre)
                {
                    ServiceEventSource.Current.ServiceMessage(
                        this.Context,
                        "DoctorService encountered an exception trying to send data to County Service: HttpRequestException in RunAsync: {0}",
                        hre.ToString());
                }
                catch (FabricNotPrimaryException)
                {
                    // not primary any more, time to quit.
                    return;
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, "{0}", ex.ToString());
                    throw;
                }

            }
        }


        private async Task<HealthIndex> GetAveragePatientHealthInfoAsync(Guid doctorId, CancellationToken ct)
        {

            var doctorPatientDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, PatientRegistrationRecord>>(String.Format(DoctorPatientDictionaryName, doctorId));
            IList<HealthIndex> healthReports = new List<HealthIndex>();


            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                var enumerator = (await doctorPatientDictionary.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(ct))
                {
                    healthReports.Add(enumerator.Current.Value.PatientHealthIndex);
                }

                await tx.CommitAsync();
            }

            if (healthReports.Count > 0)
            {
                return this.indexCalculator.ComputeAverageIndex(healthReports);
            }
            else
            {
                return this.indexCalculator.ComputeIndex(-1);
            }

        }
    }
}
