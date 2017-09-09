// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace BladeRuiner.Common.WebSockets
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IWebSocketMessageHandler
    {
        Task<byte[]> HandleMessageAsync(byte[] wsrequest, CancellationToken cancellationToken);
    }
}