// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace BladeRuiner.Common.WebSockets
{
    using ProtoBuf;

    [ProtoContract]
    public class WsResponseMessage : WsMessage
    {
        [ProtoMember(1)] public long Result;
        [ProtoMember(2)] public byte[] Value;
        [ProtoMember(3)] public bool Success;
    }
}