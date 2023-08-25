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
        [ProtoMember(2)] public byte[] MessageData;
        [ProtoMember(3)] public long IdentityID;
        [ProtoMember(4)] public ulong SteamID;
        [ProtoMember(5)] public string PluginVersion = "0";
        [ProtoMember(6)] public string NexusVersion = "0";

        public ClientMessage(string PluginVersion)
        {
            MessageType = ClientMessageType.FirstJoin;

            IdentityID = MySession.Static?.LocalHumanPlayer?.Identity?.IdentityId ?? 0;
            SteamID = MySession.Static?.LocalHumanPlayer?.Id.SteamId ?? 0;
            this.PluginVersion = PluginVersion;

        }

        public TransferData GetTransferData()
        {
            return MessageData == null ? default : MessageUtils.Deserialize<TransferData>(MessageData);
        }

        public OnlinePlayerData GetOnlinePlayers()
        {
            if (MessageData == null)
                return default;

            var msg = MessageUtils.Deserialize<OnlinePlayerData>(MessageData);
            return msg;
        }

    }
}
