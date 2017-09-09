// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace BladeRuiner.Common.WebSockets
{
    using ProtoBuf;

    [ProtoContract]
    public class WsRequestMessage : WsMessage
    {
        [ProtoMember(1)] public long PartitionKey;
        [ProtoMember(2)] public int Operation;
        [ProtoMember(3)] public byte[] Value;
    }
}