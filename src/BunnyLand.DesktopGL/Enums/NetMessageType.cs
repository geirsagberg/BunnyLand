﻿namespace BunnyLand.DesktopGL.Enums
{
    public enum NetMessageType : byte
    {
        ListServersRequest,
        ListServersResponse,
        FullGameState,
        FullGameStateUpdate
    }
}
