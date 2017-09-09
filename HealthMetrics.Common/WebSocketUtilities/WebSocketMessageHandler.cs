using BladeRuiner.Common.Serializers;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BladeRuiner.Common.WebSockets
{
    public class WebSocketMessageHandler : IWebSocketMessageHandler
    {
        private readonly ICommunicationClientFactory<WsCommunicationClient> clientFactory
            = new WsCommunicationClientFactory();

        private readonly Func<WsRequestMessage, Task<WsResponseMessage>> callHandler;
 

        public WebSocketMessageHandler(Func<WsRequestMessage, Task<WsResponseMessage>> handler)
        {
            this.callHandler = handler;
        }    

        public async Task<byte[]> HandleMessageAsync(byte[] wsrequest, CancellationToken cancellationToken)
        {
            IWsSerializer mserializer = new ProtobufWsSerializer();
            WsRequestMessage mrequest = await mserializer.DeserializeAsync<WsRequestMessage>(wsrequest);
            return await mserializer.SerializeAsync<WsResponseMessage>(await this.callHandler(mrequest));
        }
        
    }
}
