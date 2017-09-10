// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.CountyService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using HealthMetrics.Common;
    using HealthMetrics.DoctorActor.Interfaces;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using System.Net.Http;
    using ProtoBuf;
    using System.IO;
    using ProtoBuf.Meta;

    /// <summary>
    /// Default controller.
    /// </summary>
    public class CountyHealthController : ApiController
    {
        /// <summary>
        /// Reliable object state manager.
        /// </summary>
        private readonly IReliableStateManager stateManager;

        private readonly HealthIndexCalculator indexCalculator;

        /// <summary>
        /// Initializes a new instance of the DefaultController class.
        /// </summary>
        /// <param name="stateManager">Reliable object state manager.</param>
        public CountyHealthController(IReliableStateManager stateManager, HealthIndexCalculator indexCalculator)
        {
            this.stateManager = stateManager;
            this.indexCalculator = indexCalculator;
            RuntimeTypeModel.Default.MetadataTimeoutMilliseconds = 300000;
        }

        [HttpGet]
        [Route("county/health/{countyId}")]
        public async Task<HealthIndex> Get(int countyId)
        {
            IReliableDictionary<Guid, CountyDoctorStats> countyHealth =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<Guid, CountyDoctorStats>>(
                    string.Format(Service.CountyHealthDictionaryName, countyId));

            IList<KeyValuePair<Guid, CountyDoctorStats>> doctorStats = new List<KeyValuePair<Guid, CountyDoctorStats>>();

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                IAsyncEnumerator<KeyValuePair<Guid, CountyDoctorStats>> enumerator = (await countyHealth.CreateEnumerableAsync(tx)).GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    doctorStats.Add(enumerator.Current);
                }
            }

            if (doctorStats.Count > 0)
            {
                return this.indexCalculator.ComputeAverageIndex(doctorStats.Select(x => x.Value.AverageHealthIndex));
            }

            return this.indexCalculator.ComputeIndex(-1);
        }

        /// <summary>
        /// Saves health info for a county.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("county/health/{countyId}/{doctorId}")]
        //0: public async Task Post()
        //1: public async  Task<IHttpActionResult> Post()
        //2: public async Task Post(HttpRequestMessage message)
        //3: public async Task Post([FromUri] int countyId, [FromUri] Guid doctorId, HttpRequestMessage message)
        //4: public async Task<IHttpActionResult> Post([FromUri] int countyId, [FromUri] Guid doctorId, [FromBody] DoctorStatsViewModel stats)
        public async Task Post([FromUri] int countyId, [FromUri] Guid doctorId, [FromBody] DoctorStatsViewModel stats)
        {

            try
            {
                //works with 2
                //DoctorStatsViewModel dsvm;
                //using (MemoryStream s = new MemoryStream(await message.Content.ReadAsByteArrayAsync()))
                //{
                //    dsvm = Serializer.Deserialize<DoctorStatsViewModel>(s);
                //}
                
                IReliableDictionary<int, string> countyNameDictionary =
                    await this.stateManager.GetOrAddAsync<IReliableDictionary<int, string>>(Service.CountyNameDictionaryName);

                IReliableDictionary<Guid, CountyDoctorStats> countyHealth =
                    await
                        this.stateManager.GetOrAddAsync<IReliableDictionary<Guid, CountyDoctorStats>>(
                            string.Format(Service.CountyHealthDictionaryName, countyId));

                using (ITransaction tx = this.stateManager.CreateTransaction())
                {
                    await
                        countyHealth.SetAsync(
                            tx,
                            doctorId,
                            new CountyDoctorStats(stats.PatientCount, stats.HealthReportCount, stats.DoctorName, stats.AverageHealthIndex));

                    // Add the county only if it does not already exist.
                    ConditionalValue<string> getResult = await countyNameDictionary.TryGetValueAsync(tx, countyId);

                    if (!getResult.HasValue)
                    {
                        await countyNameDictionary.AddAsync(tx, countyId, String.Empty);
                    }

                    // finally, commit the transaction and return a result
                    await tx.CommitAsync();
                }

                return;
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.Message("Exception in CountyHealthController {0}", e);
                throw;
            }
        }
    }
}