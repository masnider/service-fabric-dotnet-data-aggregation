// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandCreationService
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using HealthMetrics.BandActor.Interfaces;
    using HealthMetrics.Common;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Microsoft.ServiceFabric.Services.Runtime;
    using System.Net.Http;
    using HealthMetrics.DoctorService.Models;
    using Microsoft.ServiceFabric.Services.Client;

    public class Service : StatelessService
    {
        // This is the name of the ServiceType that is registered with FabricRuntime. 
        // This name must match the name defined in the ServiceManifest. If you change
        // this name, please change the name of the ServiceType in the ServiceManifest.
        public const string ServiceTypeName = "HealthMetrics.BandCreationServiceType";

        private static FabricClient fabricClient = new FabricClient();
        private Uri ActorServiceUri;
        private Uri DoctorServiceUri;
        private int NumberOfCreationThreads;
        private int MaxBandsToCreatePerServiceInstance;

        private ConcurrentDictionary<int, ServicePartitionClient<HttpCommunicationClient>> communicationClientDictionary =
            new ConcurrentDictionary<int, ServicePartitionClient<HttpCommunicationClient>>();

        public Service(StatelessServiceContext serviceContext) : base(serviceContext)
        {
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            ConfigurationSettings configSettings = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config").Settings;
            KeyedCollection<string, ConfigurationProperty> serviceParameters = configSettings.Sections["HealthMetrics.BandCreationService.Settings"].Parameters;

            this.NumberOfCreationThreads = int.Parse(serviceParameters["NumberOfCreationThreads"].Value);
            this.MaxBandsToCreatePerServiceInstance = int.Parse(serviceParameters["MaxBandsToCreatePerServiceInstance"].Value);
            this.ActorServiceUri = new ServiceUriBuilder(serviceParameters["BandActorServiceName"].Value).ToUri();
            this.DoctorServiceUri = new ServiceUriBuilder(serviceParameters["DoctorServiceInstanceName"].Value).ToUri();

            string dataPath = FabricRuntime.GetActivationContext().GetDataPackageObject("Data").Path;
            BandActorGenerator bag = new BandActorGenerator(configSettings, dataPath);

            bag.Prepare();

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < this.NumberOfCreationThreads; i++)
            {
                tasks.Add(Task.Run(() => this.CreateBandActorTask(bag, cancellationToken), cancellationToken));
            }

            ServiceEventSource.Current.ServiceMessage(this, "Band Creation has begun.");
            await Task.WhenAll(tasks);
            ServiceEventSource.Current.ServiceMessage(this, "Band Creation has completed.");
        }

        private async Task CreateBandActorTask(BandActorGenerator bag, CancellationToken cancellationToken)
        {
            CryptoRandom random = new CryptoRandom();

            while (!cancellationToken.IsCancellationRequested && this.MaxBandsToCreatePerServiceInstance > 0)
            {
                bool created = false;
                while (!created && !cancellationToken.IsCancellationRequested)
                {
                    ActorId bandActorId;
                    Guid doctorId;
                    int randomCountyId = -1;
                    string doctorName = null;

                    randomCountyId = random.Next(0, bag.doctorsPerCounty.Keys.Count);
                    doctorName = bag.GetRandomName(random);

                    CountyRecord randomCountyRecord = bag.doctorsPerCounty.Keys.ElementAt(randomCountyId);
                    BandInfo bandActorInfo = bag.GetRandomHealthStatus(randomCountyRecord, random);

                    try
                    {
                        bandActorId = new ActorId(Guid.NewGuid());
                        doctorId = bandActorInfo.DoctorId;
                        //doctorId = new ActorId(bandActorInfo.DoctorId);

                        var dcr = new DoctorCreationRecord(doctorName, doctorId, randomCountyRecord);
                        ServicePartitionKey key = new ServicePartitionKey(HashUtil.getLongHashCode(bandActorInfo.DoctorId.ToString()));

                        await FabricHttpClient.MakePostRequest<string, DoctorCreationRecord>(
                            this.DoctorServiceUri,
                            key,
                            "ServiceEndpoint",
                            "/doctor/new/" + doctorId,
                            dcr,
                            SerializationSelector.PBUF,
                            cancellationToken
                            );

                        //IDoctorActor docActor = ActorProxy.Create<IDoctorActor>(doctorActorId, this.DoctorServiceUri);
                        //await docActor.NewAsync(doctorName, randomCountyRecord);

                        IBandActor bandActor = ActorProxy.Create<IBandActor>(bandActorId, this.ActorServiceUri);
                        await bandActor.NewAsync(bandActorInfo);

                        ServiceEventSource.Current.Message("Actor created {0} verifying...", bandActorId);

                        created = true;
                    }

                    catch (Exception e)
                    {
                        ServiceEventSource.Current.ServiceMessage(this, "Failed to iniitalize band or doctor. {0}", e.ToString());
                    }
                }

                this.MaxBandsToCreatePerServiceInstance--;

                ServiceEventSource.Current.ServiceMessage(this, "Created Actors, {0} remaining", this.MaxBandsToCreatePerServiceInstance);

                await Task.Delay(100, cancellationToken);
            }
        }
    }
}