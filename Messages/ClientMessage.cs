using ProtoBuf;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SeamlessClient.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamlessClient.Messages
{
    public enum ClientMessageType
    {
        FirstJoin,
        TransferServer,
        OnlinePlayers,
    }

    [ProtoContract]
    public class ClientMessage
    {
        [ProtoMember(1)] public ClientMessageType MessageType;
        [ProtoMember(2)] public TransferData data;
        [ProtoMember(3)] public long IdentityID;
        [ProtoMember(4)] public ulong SteamID;
        [ProtoMember(5)] public string PluginVersion = "0";

        public ClientMessage(ClientMessageType Type)
        {
            MessageType = Type;

            if (MyAPIGateway.Multiplayer == null || MyAPIGateway.Multiplayer.IsServer) return;
            if (MyAPIGateway.Session.LocalHumanPlayer == null) return;

            IdentityID = MySession.Static?.LocalHumanPlayer?.Identity?.IdentityId ?? 0;
            SteamID = MySession.Static?.LocalHumanPlayer?.Id.SteamId ?? 0;
            //PluginVersion = SeamlessClient.Version;
        }


        public ClientMessage()
        {
        }



    }
}
