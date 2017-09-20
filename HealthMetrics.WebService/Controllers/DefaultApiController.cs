// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.WebService.Controllers
{
    using System;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Fabric.Query;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using HealthMetrics.Common;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Query;
    using Microsoft.AspNetCore.Mvc;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using HealthMetrics.NationalService.Models;
    using HealthMetrics.CountyService;
    using System.Net;
    using Microsoft.ServiceFabric.Services.Client;
    using HealthMetrics.BandActor.Interfaces;

    [Route("api")]
    public class DefaultApiController : Controller
    {
        private readonly KeyedCollection<string, ConfigurationProperty> configPackageSettings;
        private readonly string BandServiceName = "BandActorServiceName";

        public DefaultApiController()
        {
            this.configPackageSettings = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config").Settings.Sections["HealthMetrics.WebService.Settings"].Parameters;
        }

        [HttpGet]
        [Route("settings/{setting}")]
        public Task<string> GetSettingValue(string setting)
        {
            return Task.FromResult<string>(this.GetSetting(setting));
        }

        [HttpGet]
        [Route("national/health")]
        public async Task<List<CountyHealth>> GetNationalHealth()
        {
            try
            {
                ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting("NationalServiceInstanceName"));

                var result = await FabricHttpClient.MakeGetRequest<List<CountyHealth>>(
                    serviceUri.ToUri(),
                    new ServicePartitionKey(),
                    "ServiceEndpoint",
                    "/national/health",
                    CancellationToken.None
                    );

                return result;
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.Message("Exception in Web API Controller getting national health {0}", e);
                throw;
            }
        }

        [HttpGet]
        [Route("national/stats")]
        public async Task<NationalStatsViewModel> GetNationalStats()
        {
            try
            {

                ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting("NationalServiceInstanceName"));
                var key = new ServicePartitionKey();
                var result = await FabricHttpClient.MakeGetRequest<NationalStatsViewModel>(
                    serviceUri.ToUri(),
                    key,
                    "ServiceEndpoint",
                    "/national/stats",
                    CancellationToken.None
                    );

                return result;
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.Message("Exception in Web API Controller getting national stats {0}", e);
                throw;
            }
        }

        /// <summary>
        /// List of {doctor ID, average patient health}
        /// </summary>
        /// <param name="countyId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("county/{countyId}/doctors/")]
        public async Task<IEnumerable<KeyValuePair<Guid, CountyDoctorStats>>> GetDoctors(int countyId)
        {
            try
            {
                ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting("CountyServiceInstanceName"));

                var result = await FabricHttpClient.MakeGetRequest<IEnumerable<KeyValuePair<Guid, CountyDoctorStats>>>(
                    serviceUri.ToUri(),
                    new ServicePartitionKey(countyId),
                    "ServiceEndpoint",
                    "/county/doctors/" + countyId,
                    CancellationToken.None
                    );

                return result;
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.Message("Exception in Web API Controller getting county {0} doctors: {1}", countyId, e);
                throw;
            }
        }

        [HttpGet]
        [Route("county/{countyId}/health/")]
        public async Task<HealthIndex> GetCountyHealth(int countyId)
        {

            try
            {
                ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting("CountyServiceInstanceName"));

                var result = await FabricHttpClient.MakeGetRequest<HealthIndex>(
                    serviceUri.ToUri(),
                    new ServicePartitionKey(countyId),
                    "ServiceEndpoint",
                    "/county/health/" + countyId,
                    CancellationToken.None
                    );

                return result;
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.Message("Exception in Web API Controller getting county {0} health {1}", countyId, e);
                throw;
            }
        }

        /// <summary>
        /// Doctor Id
        /// County Record
        ///     County Name
        ///     County Id
        ///     County Health
        /// Health Status
        /// Heart Rate[]
        /// </summary>
        /// <param name="bandId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("patients/{bandId}")]
        public async Task<BandDataViewModel> GetPatientData(Guid bandId)
        {
            try
            {
                ActorId bandActorId = new ActorId(bandId);
                ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting(BandServiceName));

                IBandActor actor = ActorProxy.Create<IBandActor>(bandActorId, serviceUri.ToUri());
                var result = await actor.GetBandDataAsync();


                return result;
            }
            catch (AggregateException ae)
            {
                ServiceEventSource.Current.Message("Exception in Web ApiController {0}", ae.InnerException);
                throw ae.InnerException;
            }
        }

        [HttpGet]
        [Route("GetIds")]
        public async Task<KeyValuePair<string, string>> GetPatientId()
        {
            return await this.GetRandomIdsAsync();
        }

        private string GetSetting(string key)
        {
            return this.configPackageSettings[key].Value;
        }

        private async Task<KeyValuePair<string, string>> GetRandomIdsAsync()
        {
            ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting(BandServiceName));
            Uri fabricServiceName = serviceUri.ToUri();

            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            CancellationToken token = cts.Token;
            FabricClient fc = new FabricClient();
            ServicePartitionList partitions = await fc.QueryManager.GetPartitionListAsync(fabricServiceName);

            string doctorId = null;

            while (!token.IsCancellationRequested && doctorId == null)
            {
                try
                {
                    foreach (Partition p in partitions)
                    {
                        long partitionKey = ((Int64RangePartitionInformation)p.PartitionInformation).LowKey;
                        token.ThrowIfCancellationRequested();
                        ContinuationToken queryContinuationToken = null;
                        IActorService proxy = ActorServiceProxy.Create(fabricServiceName, partitionKey);
                        PagedResult<ActorInformation> result = await proxy.GetActorsAsync(queryContinuationToken, token);
                        foreach (ActorInformation info in result.Items)
                        {
                            token.ThrowIfCancellationRequested();

                            ActorId bandActorId = info.ActorId;
                            IBandActor bandActor = ActorProxy.Create<IBandActor>(bandActorId, fabricServiceName);

                            try
                            {
                                BandDataViewModel data = await bandActor.GetBandDataAsync();
                                doctorId = data.DoctorId.ToString();
                                return new KeyValuePair<string, string>(bandActorId.ToString(), data.DoctorId.ToString());
                            }
                            catch (Exception e)
                            {
                                ServiceEventSource.Current.Message("Exception when obtaining actor ID. No State? " + e.ToString());
                                continue;
                            }

                        }
                        //otherwise we will bounce around other partitions until we find an actor
                    }
                }
                catch (Exception e)
                {
                    ServiceEventSource.Current.Message("Exception when obtaining actor ID: " + e.ToString());
                    continue;
                }
            }

            throw new InvalidOperationException("Couldn't find actor within timeout");
        }
    }
}