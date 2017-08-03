// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System.Collections.Generic;
using TrakHound.Api.v2.Data;

namespace TrakHound.TempServer.MTConnect
{
    internal delegate void AgentHandler(MTConnectConnection connection, AgentDefinition definition);

    internal delegate void AssetsHandler(MTConnectConnection connection, List<AssetDefinition> definitions);

    internal delegate void ComponentsHandler(MTConnectConnection connection, List<ComponentDefinition> definitions);

    internal delegate void DataItemsHandler(MTConnectConnection connection, List<DataItemDefinition> definitions);

    internal delegate void DeviceHandler(MTConnectConnection connection, DeviceDefinition definition);

    internal delegate void SampleHandler(MTConnectConnection connection, List<Sample> streamData);

    internal delegate void StatusHandler(MTConnectConnection connection, Status status);
}
