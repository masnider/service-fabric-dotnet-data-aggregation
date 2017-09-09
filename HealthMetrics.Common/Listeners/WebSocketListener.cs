// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace BladeRuiner.Common.Listeners
{
    using BladeRuiner.Common.Serializers;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using System;
    using System.Fabric;
    using System.IO;
    using System.Net;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using WebSockets;

    public class WebSocketListener : ICommunicationListener
    {
        private const int MaxBufferSize = 102400;
        //private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(WebSocketListener));
        private readonly string appRoot;
        private readonly ServiceContext serviceContext;
        private readonly Func<IWebSocketMessageHandler> createConnectionHandler;
        private readonly string endpointResourceName;
        private Task mainLoop;
        private WebSocketApp webSocketApp;


        public WebSocketListener(Func<IWebSocketMessageHandler> websocketConnectionHandlerCreationFunction, ServiceContext serviceContext, string endpointResourceName)
            : this(websocketConnectionHandlerCreationFunction, serviceContext, endpointResourceName, null)
        { }

        public WebSocketListener(Func<IWebSocketMessageHandler> websocketConnectionHandlerCreationFunction, ServiceContext serviceContext, string endpointResourceName, string appRoot)
        {
            if (websocketConnectionHandlerCreationFunction == null)
            {
                throw new ArgumentNullException(nameof(websocketConnectionHandlerCreationFunction));
            }

            if (serviceContext == null)
            {
                throw new ArgumentNullException(nameof(serviceContext));
            }

            if (endpointResourceName == null)
            {
                throw new ArgumentNullException(nameof(endpointResourceName));
            }


            this.createConnectionHandler = websocketConnectionHandlerCreationFunction;
            this.serviceContext = serviceContext;
            this.endpointResourceName = endpointResourceName;
            this.appRoot = appRoot;
        }    
            

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            //Logger.Debug(nameof(this.OpenAsync));

            try
            {
                string listeningAddress;
                string publishAddress;

                ListenerUtilities.generateAddress(
                    this.serviceContext,
                    this.endpointResourceName,
                    this.appRoot,
                    AddressScheme.HTTP,
                    AddressScheme.WS,
                    out listeningAddress,
                    out publishAddress);

                //Logger.Info("Starting websocket listener on {0}", this.listeningAddress);
                this.webSocketApp = new WebSocketApp(listeningAddress);
                this.webSocketApp.Init();

                this.mainLoop = this.webSocketApp.StartAsync(this.ProcessConnectionAsync);
                //Logger.Info("Started websocket listener on {0}", this.listeningAddress);

                return await Task.FromResult(publishAddress);
            }
            catch (Exception ex)
            {
                //Logger.Error(ex, nameof(this.OpenAsync));
                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            this.StopAll();
            return Task.FromResult(true);
        }

        public void Abort()
        {
            this.StopAll();
        }

        /// <summary>
        ///     Stops, cancels, and disposes everything.
        /// </summary>
        private void StopAll()
        {
            //Logger.Debug(nameof(this.StopAll));

            try
            {
                this.webSocketApp.Dispose();
                if (this.mainLoop != null)
                {
                    // allow a few seconds to complete the main loop
                    if (!this.mainLoop.Wait(TimeSpan.FromSeconds(3)))
                    {
                        //Logger.Warning("MainLoop did not complete within allotted time");
                    }

                    this.mainLoop.Dispose();
                    this.mainLoop = null;
                }

            }
            catch (ObjectDisposedException)
            {
            }
        }

        private async Task<bool> ProcessConnectionAsync(
            CancellationToken cancellationToken,
            HttpListenerContext httpContext)
        {
            //Logger.Debug("ProcessConnectionAsync");

            WebSocketContext webSocketContext = null;
            try
            {
                webSocketContext = await httpContext.AcceptWebSocketAsync(null);
            }
            catch (Exception ex)
            {
                //Logger.Error(ex, "AcceptWebSocketAsync");

                // The upgrade process failed somehow. For simplicity lets assume it was a failure on the part of the server and indicate this using 500.
                httpContext.Response.StatusCode = 500;
                httpContext.Response.Close();
                return false;
            }

            WebSocket webSocket = webSocketContext.WebSocket;
            MemoryStream ms = new MemoryStream();
            try
            {
                IWebSocketMessageHandler handler = this.createConnectionHandler();

                byte[] receiveBuffer = null;

                // While the WebSocket connection remains open run a simple loop that receives data and sends it back.
                while (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        if (receiveBuffer == null)
                        {
                            receiveBuffer = new byte[MaxBufferSize];
                        }

                        WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            //Logger.Debug("ProcessConnectionAsync: closing websocket");
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);
                            continue;
                        }

                        if (receiveResult.EndOfMessage)
                        {
                            await ms.WriteAsync(receiveBuffer, 0, receiveResult.Count, cancellationToken);
                            receiveBuffer = ms.ToArray();
                            ms.Dispose();
                            ms = new MemoryStream();
                        }
                        else
                        {
                            await ms.WriteAsync(receiveBuffer, 0, receiveResult.Count, cancellationToken);
                            continue;
                        }

                        byte[] wsresponse = null;
                        try
                        {
                            // dispatch to App provided function with requested payload
                            wsresponse = await handler.HandleMessageAsync(receiveBuffer, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            // catch any error in the appAction and notify the client
                            wsresponse = await new ProtobufWsSerializer().SerializeAsync(
                                new WsResponseMessage
                                {
                                    Result = WsResult.Error,
                                    Value = Encoding.UTF8.GetBytes(ex.Message),
                                    Success = false                                    
                                });
                        }

                        // Send Result back to client
                        await webSocket.SendAsync(
                            new ArraySegment<byte>(wsresponse),
                            WebSocketMessageType.Binary,
                            true,
                            cancellationToken);
                    }
                    catch (WebSocketException ex)
                    {
                        //Logger.Error(ex, "ProcessConnectionAsync: WebSocketException={0}", webSocket.State);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                //Logger.Error(ex, "ProcessConnectionAsync");
                throw;
            }
        }
    }
}