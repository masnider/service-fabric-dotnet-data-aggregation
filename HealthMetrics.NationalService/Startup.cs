// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService
{
    using System.Collections.Concurrent;
    using System.Web.Http;
    using Microsoft.ServiceFabric.Data;
    using Owin;
    using Web.Service;
    using WebApiContrib.Formatting;
    using System.Net.Http.Formatting;
    using System.Linq;

    /// <summary>
    /// OWIN configuration
    /// </summary>
    public class Startup : IOwinAppBuilder
    {
        private readonly IReliableStateManager objectManager;
        private readonly ConcurrentBag<int> updatedCounties;
        private readonly ConcurrentDictionary<string, long> statsDictionary;

        public Startup(IReliableStateManager objectManager, ConcurrentBag<int> updatedCounties, ConcurrentDictionary<string, long> statsDictionary)
        {
            this.objectManager = objectManager;
            this.updatedCounties = updatedCounties;
            this.statsDictionary = statsDictionary;
        }

        /// <summary>
        /// Configures the app builder using Web API.
        /// </summary>
        /// <param name="appBuilder"></param>
        public void Configuration(IAppBuilder appBuilder)
        {
            HttpConfiguration config = new HttpConfiguration();

            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.MapHttpAttributeRoutes();

            //https://damienbod.com/2014/01/11/using-protobuf-net-media-formatter-with-web-api-2/
            config.Formatters.Add(new ProtoBufFormatter());

            FormatterConfig.ConfigureFormatters(config.Formatters);
            UnityConfig.RegisterComponents(config, this.objectManager, this.updatedCounties, this.statsDictionary);

            appBuilder.UseWebApi(config);

            config.EnsureInitialized();
        }
    }
}