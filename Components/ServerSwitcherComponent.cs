using HarmonyLib;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using Sandbox.ModAPI;
using SeamlessClient.Components;
using SeamlessClient.OnlinePlayersWindow;
using SeamlessClient.Utilities;
using SpaceEngineers.Game.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.GameServices;
using VRage.Network;
using VRage.Utils;
using VRageRender;
using VRageRender.Messages;

namespace SeamlessClient.ServerSwitching
{
    public class ServerSwitcherComponent : ComponentBase
    {
        public static ConstructorInfo ClientConstructor;
        public static ConstructorInfo SyncLayerConstructor;
        public static ConstructorInfo TransportLayerConstructor;

        public static PropertyInfo MySessionLayer;

        private static FieldInfo RemoteAdminSettings;
        private static FieldInfo AdminSettings;
        private static MethodInfo UnloadProceduralWorldGenerator;
        private static MethodInfo GpsRegisterChat;
        private static MethodInfo LoadMembersFromWorld;
        private static MethodInfo InitVirtualClients;

        private static FieldInfo VirtualClients;


        public static ServerSwitcherComponent Instance { get; private set; }

        private MyGameServerItem TargetServer { get; set; }
        private MyObjectBuilder_World TargetWorld { get; set; }
        private string OldArmorSkin { get; set; } = string.Empty;

        private bool needsEntityUnload = true;

        public static event EventHandler<JoinResultMsg> OnJoinEvent;



        public ServerSwitcherComponent()
        {
            Instance = this;
        }


        public override void Patch(Harmony patcher)
        {
            MySessionLayer = PatchUtils.GetProperty(typeof(MySession), "SyncLayer");

            ClientConstructor = PatchUtils.GetConstructor(PatchUtils.ClientType, new[] { typeof(MyGameServerItem), PatchUtils.SyncLayerType });
            SyncLayerConstructor = PatchUtils.GetConstructor(PatchUtils.SyncLayerType, new[] { PatchUtils.MyTransportLayerType });
            TransportLayerConstructor = PatchUtils.GetConstructor(PatchUtils.MyTransportLayerType, new[] { typeof(int) });


            RemoteAdminSettings = PatchUtils.GetField(typeof(MySession), "m_remoteAdminSettings");
            AdminSettings = PatchUtils.GetField(typeof(MySession), "m_adminSettings");
            VirtualClients = PatchUtils.GetField(typeof(MySession), "VirtualClients");

            UnloadProceduralWorldGenerator = PatchUtils.GetMethod(typeof(MyProceduralWorldGenerator), "UnloadData");
            GpsRegisterChat = PatchUtils.GetMethod(typeof(MyGpsCollection), "RegisterChat");
            LoadMembersFromWorld = PatchUtils.GetMethod(typeof(MySession), "LoadMembersFromWorld");
            InitVirtualClients = PatchUtils.GetMethod(PatchUtils.VirtualClientsType, "Init");



            var onJoin = PatchUtils.GetMethod(PatchUtils.ClientType, "OnUserJoined");
            patcher.Patch(onJoin, postfix: new HarmonyMethod(Get(typeof(ServerSwitcherComponent), nameof(OnUserJoined))));

            base.Patch(patcher);
        }



        private static void OnUserJoined(ref JoinResultMsg msg)
        {
            if (msg.JoinResult == JoinResult.OK)
            {
                Seamless.TryShow("User Joined! Result: " + msg.JoinResult.ToString());

                //Invoke the switch event
                OnJoinEvent?.Invoke(null, msg);
            }
        }





        public override void Initilized()
        {
            base.Initilized();
        }





        public void StartBackendSwitch(MyGameServerItem TargetServer, MyObjectBuilder_World TargetWorld)
        {
            this.TargetServer = TargetServer;
            this.TargetWorld = TargetWorld;
            OldArmorSkin = MySession.Static.LocalHumanPlayer.BuildArmorSkin;

            if (Seamless.NexusVersion.Major >= 2)
                needsEntityUnload = false;


            AsyncInvoke.InvokeAsync(() => 
            { 
                if(needsEntityUnload)
                    MySession.Static.SetCameraController(MyCameraControllerEnum.SpectatorFixed);


                UnloadServer();
                SetNewMultiplayerClient();
                

            });
        }

        private void UnloadServer()
        {
            if (MyMultiplayer.Static == null)
                throw new Exception("MyMultiplayer.Static is null on unloading? dafuq?");

            UnloadOldEntities();

            //Close and Cancel any screens (Medical or QuestLog)
            MySessionComponentIngameHelp component = MySession.Static.GetComponent<MySessionComponentIngameHelp>();
            component?.TryCancelObjective();
            MyGuiScreenMedicals.Close();


            //Clear all old players and clients.
            Sync.Clients.Clear();
            Sync.Players.ClearPlayers();

            //Unregister Chat
            MyHud.Chat.UnregisterChat(MyMultiplayer.Static);

            //Clear all local GPS Points
            MySession.Static.Gpss.RemovePlayerGpss(MySession.Static.LocalPlayerId);
            MyHud.GpsMarkers.Clear();

            //DisconnectReplication
            MyMultiplayer.Static.ReplicationLayer.Disconnect();
            MyMultiplayer.Static.ReplicationLayer.Dispose();
            MyMultiplayer.Static.Dispose();
            MyMultiplayer.Static = null;
        }

        private void UnloadOldEntities()
        {
            foreach (var ent in MyEntities.GetEntities())
            {
                if (ent is MyPlanet)
                    continue;

                //ent.Close();
            }
        }

        private void SetNewMultiplayerClient()
        {
            OnJoinEvent += ServerSwitcherComponent_OnJoinEvent;

            MySandboxGame.Static.SessionCompatHelper.FixSessionComponentObjectBuilders(TargetWorld.Checkpoint, TargetWorld.Sector);


            // Create constructors
            var LayerInstance = TransportLayerConstructor.Invoke(new object[] { 2 });
            var SyncInstance = SyncLayerConstructor.Invoke(new object[] { LayerInstance });
            var instance = ClientConstructor.Invoke(new object[] { TargetServer, SyncInstance });


            MyMultiplayer.Static = UtilExtensions.CastToReflected(instance, PatchUtils.ClientType);
            MyMultiplayer.Static.ExperimentalMode = true;

            // Set the new SyncLayer to the MySession.Static.SyncLayer
            MySessionLayer.SetValue(MySession.Static, MyMultiplayer.Static.SyncLayer);

            Seamless.TryShow("Successfully set MyMultiplayer.Static");


            Sync.Clients.SetLocalSteamId(Sync.MyId, false, MyGameService.UserName);
            Sync.Players.RegisterEvents();
        }

        private void ServerSwitcherComponent_OnJoinEvent(object sender, JoinResultMsg e)
        {
            OnJoinEvent -= ServerSwitcherComponent_OnJoinEvent;


            if (e.JoinResult != JoinResult.OK) 
            {
                Seamless.TryShow("Failed to join the target server!");
                return;
            }

            Seamless.TryShow("Starting new MP Client!");

            /* On Server Successfull Join
             * 
             * 
             */


            SetWorldSettings();
            InitComponents();
            LoadConnectedClients();
            StartEntitySync();



            MyMultiplayer.Static.OnSessionReady();

            MyHud.Chat.RegisterChat(MyMultiplayer.Static);
            GpsRegisterChat.Invoke(MySession.Static.Gpss, new object[] { MyMultiplayer.Static });


            // Allow the game to start proccessing incoming messages in the buffer
            MyMultiplayer.Static.StartProcessingClientMessages();

            //Recreate all controls... Will fix weird gui/paint/crap
            MyGuiScreenHudSpace.Static.RecreateControls(true);
        }




        private void SetWorldSettings()
        {
            //Clear old list
            MySession.Static.PromotedUsers.Clear();
            MySession.Static.CreativeTools.Clear();
            Dictionary<ulong, AdminSettingsEnum> AdminSettingsList = (Dictionary<ulong, AdminSettingsEnum>)RemoteAdminSettings.GetValue(MySession.Static);
            AdminSettingsList.Clear();



            // Set new world settings
            MySession.Static.Name = MyStatControlText.SubstituteTexts(TargetWorld.Checkpoint.SessionName);
            MySession.Static.Description = TargetWorld.Checkpoint.Description;

            MySession.Static.Mods = TargetWorld.Checkpoint.Mods;
            MySession.Static.Settings = TargetWorld.Checkpoint.Settings;
            MySession.Static.CurrentPath = MyLocalCache.GetSessionSavesPath(MyUtils.StripInvalidChars(TargetWorld.Checkpoint.SessionName), contentFolder: false, createIfNotExists: false);
            MySession.Static.WorldBoundaries = TargetWorld.Checkpoint.WorldBoundaries;
            MySession.Static.InGameTime = MyObjectBuilder_Checkpoint.DEFAULT_DATE;
            MySession.Static.ElapsedGameTime = new TimeSpan(TargetWorld.Checkpoint.ElapsedGameTime);
            MySession.Static.Settings.EnableSpectator = false;

            MySession.Static.Password = TargetWorld.Checkpoint.Password;
            MySession.Static.PreviousEnvironmentHostility = TargetWorld.Checkpoint.PreviousEnvironmentHostility;
            MySession.Static.RequiresDX = TargetWorld.Checkpoint.RequiresDX;
            MySession.Static.CustomLoadingScreenImage = TargetWorld.Checkpoint.CustomLoadingScreenImage;
            MySession.Static.CustomLoadingScreenText = TargetWorld.Checkpoint.CustomLoadingScreenText;
            MySession.Static.CustomSkybox = TargetWorld.Checkpoint.CustomSkybox;

            try
            {
                MySession.Static.Gpss = new MyGpsCollection();
                MySession.Static.Gpss.LoadGpss(TargetWorld.Checkpoint);

            }
            catch (Exception ex)
            {
                Seamless.TryShow($"An error occured while loading GPS points! You will have an empty gps list! \n {ex.ToString()}");
            }


            MyRenderProxy.RebuildCullingStructure();
            //MySession.Static.Toolbars.LoadToolbars(checkpoint);

            Sync.Players.RespawnComponent.InitFromCheckpoint(TargetWorld.Checkpoint);


            // Set new admin settings
            if (TargetWorld.Checkpoint.PromotedUsers != null)
            {
                MySession.Static.PromotedUsers = TargetWorld.Checkpoint.PromotedUsers.Dictionary;
            }
            else
            {
                MySession.Static.PromotedUsers = new Dictionary<ulong, MyPromoteLevel>();
            }




            foreach (KeyValuePair<MyObjectBuilder_Checkpoint.PlayerId, MyObjectBuilder_Player> item in TargetWorld.Checkpoint.AllPlayersData.Dictionary)
            {
                ulong clientId = item.Key.GetClientId();
                AdminSettingsEnum adminSettingsEnum = (AdminSettingsEnum)item.Value.RemoteAdminSettings;
                if (TargetWorld.Checkpoint.RemoteAdminSettings != null && TargetWorld.Checkpoint.RemoteAdminSettings.Dictionary.TryGetValue(clientId, out var value))
                {
                    adminSettingsEnum = (AdminSettingsEnum)value;
                }
                if (!MyPlatformGameSettings.IsIgnorePcuAllowed)
                {
                    adminSettingsEnum &= ~AdminSettingsEnum.IgnorePcu;
                    adminSettingsEnum &= ~AdminSettingsEnum.KeepOriginalOwnershipOnPaste;
                }


                AdminSettingsList[clientId] = adminSettingsEnum;
                if (!Sync.IsDedicated && clientId == Sync.MyId)
                {
                    AdminSettings.SetValue(MySession.Static, adminSettingsEnum);
                }



                if (!MySession.Static.PromotedUsers.TryGetValue(clientId, out var value2))
                {
                    value2 = MyPromoteLevel.None;
                }
                if (item.Value.PromoteLevel > value2)
                {
                    MySession.Static.PromotedUsers[clientId] = item.Value.PromoteLevel;
                }
                if (!MySession.Static.CreativeTools.Contains(clientId) && item.Value.CreativeToolsEnabled)
                {
                    MySession.Static.CreativeTools.Add(clientId);
                }
            }


            MySector.InitEnvironmentSettings(TargetWorld.Sector.Environment);

            string text = ((!string.IsNullOrEmpty(TargetWorld.Checkpoint.CustomSkybox)) ? TargetWorld.Checkpoint.CustomSkybox : MySector.EnvironmentDefinition.EnvironmentTexture);
            MyRenderProxy.PreloadTextures(new string[1] { text }, TextureType.CubeMap);




        }

        private void InitComponents()
        {
            MyModAPIHelper.Initialize();
            MySession.Static.LoadDataComponents();

            //MySession.Static.LoadObjectBuildersComponents(TargetWorld.Checkpoint.SessionComponents);
            MyModAPIHelper.Initialize();
            // MySession.Static.LoadObjectBuildersComponents(TargetWorld.Checkpoint.SessionComponents);

            UpdateWorldGenerator();
            //MethodInfo A = typeof(MySession).GetMethod("LoadGameDefinition", BindingFlags.Instance | BindingFlags.NonPublic);
            // A.Invoke(MySession.Static, new object[] { TargetWorld.Checkpoint });
        }


        private void UpdateWorldGenerator()
        {
            //This will re-init the MyProceduralWorldGenerator. (Not doing this will result in asteroids not rendering in properly)


            //This shoud never be null
            var Generator = MySession.Static.GetComponent<MyProceduralWorldGenerator>();

            //Force component to unload
            UnloadProceduralWorldGenerator.Invoke(Generator, null);

            //Re-call the generator init
            MyObjectBuilder_WorldGenerator GeneratorSettings = (MyObjectBuilder_WorldGenerator)TargetWorld.Checkpoint.SessionComponents.FirstOrDefault(x => x.GetType() == typeof(MyObjectBuilder_WorldGenerator));
            if (GeneratorSettings != null)
            {
                //Re-initilized this component (forces to update asteroid areas like not in planets etc)
                Generator.Init(GeneratorSettings);
            }

            //Force component to reload, re-syncing settings and seeds to the destination server
            Generator.LoadData();

            //We need to go in and force planets to be empty areas in the generator. This is originially done on planet init.
            FieldInfo PlanetInitArgs = typeof(MyPlanet).GetField("m_planetInitValues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var Planet in MyEntities.GetEntities().OfType<MyPlanet>())
            {
                MyPlanetInitArguments args = (MyPlanetInitArguments)PlanetInitArgs.GetValue(Planet);

                float MaxRadius = args.MaxRadius;

                Generator.MarkEmptyArea(Planet.PositionComp.GetPosition(), MaxRadius);
            }
        }

        private void StartEntitySync()
        {
            Seamless.TryShow("Requesting Player From Server");
            Sync.Players.RequestNewPlayer(Sync.MyId, 0, MyGameService.UserName, null, realPlayer: true, initialPlayer: true);
            if (MySession.Static.ControlledEntity == null && Sync.IsServer && !Sandbox.Engine.Platform.Game.IsDedicated)
            {
                MyLog.Default.WriteLine("ControlledObject was null, respawning character");
                //m_cameraAwaitingEntity = true;
                MyPlayerCollection.RequestLocalRespawn();
            }

            //Request client state batch
            (MyMultiplayer.Static as MyMultiplayerClientBase).RequestBatchConfirmation();
            MyMultiplayer.Static.PendingReplicablesDone += Static_PendingReplicablesDone;
            //typeof(MyGuiScreenTerminal).GetMethod("CreateTabs")

            MySession.Static.LoadDataComponents();
            //MyGuiSandbox.LoadData(false);
            //MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.HUDScreen));
            MyRenderProxy.RebuildCullingStructure();
            MyRenderProxy.CollectGarbage();

            Seamless.TryShow("OnlinePlayers: " + MySession.Static.Players.GetOnlinePlayers().Count);
            Seamless.TryShow("Loading Complete!");
        }

        private void Static_PendingReplicablesDone()
        {
            if (MySession.Static.VoxelMaps.Instances.Count > 0)
            {
                MySandboxGame.AreClipmapsReady = false;
            }
            MyMultiplayer.Static.PendingReplicablesDone -= Static_PendingReplicablesDone;
        }

        private void LoadConnectedClients()
        {
            LoadMembersFromWorld.Invoke(MySession.Static, new object[] { TargetWorld, MyMultiplayer.Static });


            //Re-Initilize Virtual clients
            object VirtualClientsValue = VirtualClients.GetValue(MySession.Static);
            InitVirtualClients.Invoke(VirtualClientsValue, null);



            MyPlayer.PlayerId? savingPlayerId = new MyPlayer.PlayerId(Sync.MyId);
            if (!savingPlayerId.HasValue)
            {
                Seamless.TryShow("SavingPlayerID is null! Creating Default!");
                savingPlayerId = new MyPlayer.PlayerId(Sync.MyId);
            }
            Seamless.TryShow("Saving PlayerID: " + savingPlayerId.ToString());

            Sync.Players.LoadConnectedPlayers(TargetWorld.Checkpoint, savingPlayerId);
            Sync.Players.LoadControlledEntities(TargetWorld.Checkpoint.ControlledEntities, TargetWorld.Checkpoint.ControlledObject, savingPlayerId);
        }
    }
}
