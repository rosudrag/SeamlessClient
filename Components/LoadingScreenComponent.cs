using HarmonyLib;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.Graphics;
using SeamlessClient.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.World;
using Sandbox.Engine.Multiplayer;
using VRage.Game;
using Sandbox.Engine.Networking;
using VRage;
using VRage.GameServices;
using VRageRender;
using Sandbox.Game.Multiplayer;
using Sandbox.Game;
using SeamlessClient.GUI.Screens;
using Sandbox.Engine.Analytics;
using System.IO;
using VRage.FileSystem;

namespace SeamlessClient.Components
{
    public class LoadingScreenComponent : ComponentBase
    {
        private static MethodInfo LoadMultiplayer;
        private static string _loadingScreenTexture = null;
        private bool ScannedMods = false;

        public static string JoiningServerName { get; private set; }
        public static List<string> CustomLoadingTextures { get; private set; } = new List<string>();    

        static Random random = new Random(Guid.NewGuid().GetHashCode());
        delegate void MyDelegate(string text);

        private static List<MyObjectBuilder_Checkpoint.ModItem> mods; 

        public override void Patch(Harmony patcher)
        {
            var startLoading = PatchUtils.GetMethod(typeof(MySessionLoader), "StartLoading");
            var loadingAction = PatchUtils.GetMethod(typeof(MySessionLoader), "LoadMultiplayerSession");

            patcher.Patch(startLoading, prefix: new HarmonyMethod(Get(typeof(LoadingScreenComponent), nameof(StartLoading))));
            patcher.Patch(loadingAction, postfix: new HarmonyMethod(Get(typeof(LoadingScreenComponent), nameof(LoadMultiplayerSession))));
        }

        public static string getRandomLoadingScreen()
        {
            int randomIndex = random.Next(CustomLoadingTextures.Count);
            return CustomLoadingTextures[randomIndex];
        }



        private static void LoadMultiplayerSession(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            //This is the main enty point for the start loading system...
            if (Sync.IsServer)
                return;


            JoiningServerName = multiplayerSession.HostName;
            mods = world.Checkpoint.Mods;
            //GetCustomLoadingScreenPath(world.Checkpoint.Mods);
            //Search for any custom loading screens

            return;
        }

        private static bool StartLoading(Action loadingAction, Action loadingActionXMLAllowed = null, string customLoadingBackground = null, string customLoadingtext = null)
        {
            /* Control what screen is being loaded */


            //If we are loading into a single player enviroment, skip override
            if (Sync.IsServer)
                return true;



            GetCustomLoadingScreenPath();
            if (MySpaceAnalytics.Instance != null)
            {
                MySpaceAnalytics.Instance.StoreWorldLoadingStartTime();
            }


            MyGuiScreenGamePlay newGameplayScreen = new MyGuiScreenGamePlay();
            MyGuiScreenGamePlay myGuiScreenGamePlay = newGameplayScreen;
            myGuiScreenGamePlay.OnLoadingAction = (Action)Delegate.Combine(myGuiScreenGamePlay.OnLoadingAction, loadingAction);

            //Use custom loading screen
            GUILoadingScreen myGuiScreenLoading = new GUILoadingScreen(newGameplayScreen, MyGuiScreenGamePlay.Static, customLoadingBackground, customLoadingtext);
           
            myGuiScreenLoading.OnScreenLoadingFinished += delegate
            {
                if (MySession.Static != null)
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.HUDScreen));
                    newGameplayScreen.LoadingDone = true;
                }
            };

            myGuiScreenLoading.OnLoadingXMLAllowed = loadingActionXMLAllowed;
            MyGuiSandbox.AddScreen(myGuiScreenLoading);

            return false;
        }




        public static void GetCustomLoadingScreenPath()
        {
            try
            {
                //var Mods = world.Checkpoint.Mods;
                var Mods = mods;

                Seamless.TryShow("Server Mods: " + Mods);
                foreach (var mod in Mods)
                {
                    var searchDir = mod.GetPath();

                    if (!Directory.Exists(searchDir))
                        continue;

                    var files = Directory.GetFiles(searchDir, "CustomLoadingBackground*.dds", SearchOption.TopDirectoryOnly);

                    foreach (var file in files)
                    {
                        // Adds all files containing CustomLoadingBackground to a list for later randomisation
                        Seamless.TryShow(mod.FriendlyName + " contains a custom loading background!");
                        CustomLoadingTextures.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Seamless.TryShow(ex.ToString());
            }

            Seamless.TryShow("");
            return;
        }

    }
}
