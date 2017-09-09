using System;
using System.Fabric;
using System.Fabric.Description;
using System.Globalization;

namespace BladeRuiner.Common.Listeners
{
    public static class ListenerUtilities
    {
        public static void generateAddress(ServiceContext serviceContext, string endpointResourceName, string appRoot, AddressScheme listenerScheme, AddressScheme publishScheme, out string listeningAddress, out string publishAddress, bool statelessRandom = false)
        {
            EndpointResourceDescription endpoint = serviceContext.CodePackageActivationContext.GetEndpoint(endpointResourceName);

            int port = endpoint.Port;

            listeningAddress = string.Format(CultureInfo.InvariantCulture, "{0}://+:{1}/", listenerScheme, port);

            if(!string.IsNullOrWhiteSpace(appRoot))
            {
                appRoot = appRoot.TrimEnd('/');
                listeningAddress = string.Format(CultureInfo.InvariantCulture, "{0}{1}/", listeningAddress, appRoot);
            }

            if (serviceContext is StatefulServiceContext)
            {
                StatefulServiceContext sip = (StatefulServiceContext)serviceContext;
                listeningAddress = string.Format(CultureInfo.InvariantCulture, "{0}{1}/{2}/{3}", listeningAddress, sip.PartitionId, sip.ReplicaId, Guid.NewGuid());
            }
            else if(statelessRandom)
            {
                //if !string.IsNullOrWhiteSpace(appRoot) then this is weird but its your address so ok
                listeningAddress = string.Format(CultureInfo.InvariantCulture, "{0}{1}", listeningAddress, serviceContext.ReplicaOrInstanceId);
            }

            publishAddress = listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            publishAddress = publishAddress.Replace(String.Format("{0}://", listenerScheme), String.Format("{0}://", publishScheme));

        }
    }

    public sealed class AddressScheme
    {

        private readonly string name;
        private readonly int value;

        public static readonly AddressScheme HTTP = new AddressScheme(1, "http");
        public static readonly AddressScheme HTTPS = new AddressScheme(2, "https");
        public static readonly AddressScheme WS = new AddressScheme(3, "ws");
        public static readonly AddressScheme WSS = new AddressScheme(4, "wss"); 
        public static readonly AddressScheme TCP = new AddressScheme(5, "tcp"); 
        public static readonly AddressScheme UDP = new AddressScheme(6, "udp"); 

        private AddressScheme(int value, String name)
        {
            this.name = name;
            this.value = value;
        }

        public override String ToString()
        {
            return name;
        }

    }
}
