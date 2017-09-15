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
        [Route("county/health")]
        public async Task Post([FromBody] IList<DoctorStatsViewModel> stats)
        {

            try
            {                
                IReliableDictionary<int, string> countyNameDictionary =
                    await this.stateManager.GetOrAddAsync<IReliableDictionary<int, string>>(Service.CountyNameDictionaryName);


                foreach (var stat in stats)
                {
                    IReliableDictionary<Guid, CountyDoctorStats> countyHealth =
                        await
                            this.stateManager.GetOrAddAsync<IReliableDictionary<Guid, CountyDoctorStats>>(
                                string.Format(Service.CountyHealthDictionaryName, stat.DoctorId));

                    using (ITransaction tx = this.stateManager.CreateTransaction())
                    {
                        await
                            countyHealth.SetAsync(
                                tx,
                                stat.DoctorId,
                                new CountyDoctorStats(stat.PatientCount, stat.HealthReportCount, stat.DoctorName, stat.AverageHealthIndex));

                        // Add the county only if it does not already exist.
                        ConditionalValue<string> getResult = await countyNameDictionary.TryGetValueAsync(tx, stat.countyId);

                        if (!getResult.HasValue)
                        {
                            await countyNameDictionary.AddAsync(tx, stat.countyId, String.Empty);
                        }

                        // finally, commit the transaction and return a result
                        await tx.CommitAsync();
                    }
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