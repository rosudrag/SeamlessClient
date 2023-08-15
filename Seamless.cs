using HarmonyLib;
using NLog.Fluent;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SeamlessClient.Messages;
using SeamlessClient.ServerSwitching;
using SeamlessClient.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Plugins;
using VRage.Sync;
using VRage.Utils;

namespace SeamlessClient
{
    public class Seamless : IPlugin
    {
        public static Version SeamlessVersion;
        private static Harmony SeamlessPatcher;
        public const ushort SeamlessClientNetId = 2936;

        private List<ComponentBase> allComps = new List<ComponentBase>();
        private Assembly thisAssembly;
        private bool Initilized = false;

#if DEBUG
        public static bool isDebug = true;
#else
        public static bool isDebug = false;
#endif


        public void Init(object gameInstance)
        {
            thisAssembly = typeof(Seamless).Assembly;
            SeamlessVersion = thisAssembly.GetName().Version;
            TryShow($"Running Seamless Client Plugin v[{SeamlessVersion}]");
            SeamlessPatcher = new Harmony("SeamlessClientPatcher");
            GetComponents();
            

            PatchComponents(SeamlessPatcher);
        }


        private void GetComponents()
        {
            int failedCount = 0;
            foreach (Type type in thisAssembly.GetTypes())
            {

                if (type.BaseType != typeof(ComponentBase))
                    continue;

                try
                {
                    ComponentBase s = (ComponentBase)Activator.CreateInstance(type);
                    allComps.Add(s);

                }
                catch (Exception ex)
                {
                    failedCount++;

                    TryShow(ex, $"{type.FullName} failed to load!");
                }
            }
        }
        private void PatchComponents(Harmony patcher)
        {
            foreach (ComponentBase component in allComps)
            {
                try
                {
                    patcher.CreateClassProcessor(component.GetType()).Patch();
                    component.Patch(patcher);
                    TryShow($"Patched {component.GetType()}");

                }
                catch (Exception ex)
                {
                    TryShow(ex, $"Failed to Patch {component.GetType()}");
                }
            }
        }
        private void InitilizeComponents()
        {
            foreach(ComponentBase component in allComps)
            {
                try
                {
                    component.Initilized();
                    TryShow($"Initilized {component.GetType()}");

                }catch(Exception ex)
                {
                    TryShow(ex, $"Failed to initialize {component.GetType()}");
                }
            }
        }


        private static void MessageHandler(ushort packetID, byte[] data, ulong sender, bool fromServer)
        {
            //Ignore anything except dedicated server
            if (!fromServer || sender != 0)
                return;

            ClientMessage msg = MessageUtils.Deserialize<ClientMessage>(data);
            if (msg == null)
                return;


            switch (msg.MessageType)
            {
                case ClientMessageType.FirstJoin:
                    MyAPIGateway.Multiplayer?.SendMessageToServer(SeamlessClientNetId, MessageUtils.Serialize(new ClientMessage(ClientMessageType.FirstJoin)));
                    break;

                case ClientMessageType.TransferServer:
                    ServerSwitcher.StartSwitching(msg.data);
                    break;

                case ClientMessageType.OnlinePlayers: 
                    //Not implemented yet
                    break;
            }


        }



        public void Dispose()
        {
            
        }



        public void Update()
        {
            if (MyAPIGateway.Multiplayer == null) 
                return;

            if (!Initilized)
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(SeamlessClientNetId, MessageHandler);
                InitilizeComponents();

                Initilized = true;
            }
        }






        public static void TryShow(string message)
        {
            if (MySession.Static?.LocalHumanPlayer != null && isDebug)
                MyAPIGateway.Utilities?.ShowMessage("Seamless", message);

            MyLog.Default?.WriteLineAndConsole($"SeamlessClient: {message}");
        }

        public static void TryShow(Exception ex, string message)
        {
            if (MySession.Static?.LocalHumanPlayer != null && isDebug)
                MyAPIGateway.Utilities?.ShowMessage("Seamless", message + $"\n {ex.ToString()}");

            MyLog.Default?.WriteLineAndConsole($"SeamlessClient: {message} \n {ex.ToString()}");
        }
    }
}
