using Microsoft.Owin.Hosting;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Owin;
using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;

namespace BladeRuiner.Common.Listeners
{
    public class OwinCommunicationListener : ICommunicationListener
    {
        private readonly Action<IAppBuilder> startup;
        private readonly ServiceContext serviceContext;
        private readonly string endpointResourceName;
        private readonly string appRoot;

        private IDisposable webApp;

        public OwinCommunicationListener(Action<IAppBuilder> startup, ServiceContext serviceContext, string endpointResourceName)
            : this(startup, serviceContext, endpointResourceName, null)
        {
        }

        public OwinCommunicationListener(Action<IAppBuilder> startup, ServiceContext serviceContext, string endpointResourceName, string appRoot)
        {
            if (startup == null)
            {
                throw new ArgumentNullException(nameof(startup));
            }

            if (serviceContext == null)
            {
                throw new ArgumentNullException(nameof(serviceContext));
            }

            if (endpointResourceName == null)
            {
                throw new ArgumentNullException(nameof(endpointResourceName));
            }

            this.startup = startup;
            this.serviceContext = serviceContext;
            this.endpointResourceName = endpointResourceName;
            this.appRoot = appRoot;
        }

        public bool ListenOnSecondary { get; set; }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {

            try
            {
                string listeningAddress;
                string publishAddress;

                ListenerUtilities.generateAddress(
                    this.serviceContext,
                    this.endpointResourceName,
                    this.appRoot,
                    AddressScheme.HTTP,
                    AddressScheme.HTTP,
                    out listeningAddress,
                    out publishAddress);

                //this.eventSource.ServiceMessage(this.serviceContext, "Starting web server on " + this.listeningAddress);

                this.webApp = WebApp.Start(listeningAddress, appBuilder => this.startup.Invoke(appBuilder));

                //this.eventSource.ServiceMessage(this.serviceContext, "Listening on " + this.publishAddress);

                return Task.FromResult(publishAddress);
            }
            catch (Exception ex)
            {
                //this.eventSource.ServiceMessage(this.serviceContext, "Web server failed to open. " + ex.ToString());

                this.StopWebServer();

                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            //this.eventSource.ServiceMessage(this.serviceContext, "Closing web server");

            this.StopWebServer();

            return Task.FromResult(true);
        }

        public void Abort()
        {
            //this.eventSource.ServiceMessage(this.serviceContext, "Aborting web server");

            this.StopWebServer();
        }

        private void StopWebServer()
        {
            if (this.webApp != null)
            {
                try
                {
                    this.webApp.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // no-op
                }
            }
        }
    }
}
