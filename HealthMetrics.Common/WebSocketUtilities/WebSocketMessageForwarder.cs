using BladeRuiner.Common.Serializers;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BladeRuiner.Common.WebSockets
{
    public class WebSocketMessageForwarder : IWebSocketMessageHandler
    {
        private readonly ICommunicationClientFactory<WsCommunicationClient> clientFactory
            = new WsCommunicationClientFactory();

        private readonly Uri DestinationService;
        private readonly string DestinationListenerName;

        public WebSocketMessageForwarder(Uri destinationService, string listenerName)
        {
            this.DestinationService = destinationService;
            this.DestinationListenerName = listenerName;
        }    

        public async Task<byte[]> HandleMessageAsync(byte[] wsrequest, CancellationToken cancellationToken)
        {
            IWsSerializer mserializer = new ProtobufWsSerializer();
            WsRequestMessage mrequest = await mserializer.DeserializeAsync<WsRequestMessage>(wsrequest);

            return await ForwardWebsocketMessage(wsrequest, cancellationToken, mrequest);
        }

        private async Task<byte[]> ForwardWebsocketMessage(byte[] wsrequest, CancellationToken cancellationToken, WsRequestMessage mrequest)
        {
            ServicePartitionClient<WsCommunicationClient> serviceClient =
                new ServicePartitionClient<WsCommunicationClient>(
                    this.clientFactory,
                    this.DestinationService,
                    new ServicePartitionKey(mrequest.PartitionKey),
                    TargetReplicaSelector.Default,
                    this.DestinationListenerName);

            return await serviceClient.InvokeWithRetryAsync(
                async client => await client.SendReceiveAsync(wsrequest),
                cancellationToken
                );
        }
    }
}
