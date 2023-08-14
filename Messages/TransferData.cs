using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace SeamlessClient.Messages
{
    [ProtoContract]
    public class TransferData
    {
        [ProtoMember(1)] public ulong TargetServerId;
        [ProtoMember(2)] public string IpAddress;
        [ProtoMember(6)] public WorldRequest WorldRequest;
        [ProtoMember(7)] public string PlayerName;
        [ProtoMember(9)] public MyObjectBuilder_Toolbar PlayerToolbar;
        [ProtoMember(10)] public string ServerName;

        public TransferData(ulong ServerID, string IPAdress)
        {
            IpAddress = IPAdress;
            TargetServerId = ServerID;
        }

        public TransferData()
        {
        }
    }
}
