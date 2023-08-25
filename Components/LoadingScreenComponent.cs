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

namespace SeamlessClient.Components
{
    public class LoadingScreenComponent : ComponentBase
    {
        private static MethodInfo LoadMultiplayer;

        private static string _loadingScreenTexture = null;
        private static string _serverName;


        public override void Patch(Harmony patcher)
        {
            var loadingAction = PatchUtils.GetMethod(typeof(MySessionLoader), "LoadMultiplayerSession");
            var loadingScreenDraw = PatchUtils.GetMethod(typeof(MyGuiScreenLoading), "DrawInternal");
            LoadMultiplayer = PatchUtils.GetMethod(typeof(MySession), "LoadMultiplayer");


            patcher.Patch(loadingAction, prefix: new HarmonyMethod(Get(typeof(LoadingScreenComponent), nameof(LoadMultiplayerSession))));
            patcher.Patch(loadingScreenDraw, prefix: new HarmonyMethod(Get(typeof(LoadingScreenComponent), nameof(DrawInternal))));



            base.Patch(patcher);
        }



        private static bool LoadMultiplayerSession(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            MyLog.Default.WriteLine("LoadSession() - Start");
            if (!MyWorkshop.CheckLocalModsAllowed(world.Checkpoint.Mods, false))
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.OK, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), messageText: MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer)));
                MyLog.Default.WriteLine("LoadSession() - End");
                return false;
            }


            MyWorkshop.DownloadModsAsync(world.Checkpoint.Mods, delegate (MyGameServiceCallResult result)
            {
                switch (result)
                {
                    case MyGameServiceCallResult.NotEnoughSpace:
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.OK, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed_NotEnoughSpace), okButtonText: null, cancelButtonText: null, yesButtonText: null, noButtonText: null, callback: delegate
                        {
                            MySessionLoader.UnloadAndExitToMenu();
                        }));
                        break;
                    case MyGameServiceCallResult.OK:
                        MyScreenManager.CloseAllScreensNowExcept(null);
                        MyGuiSandbox.Update(16);

                        if (MySession.Static != null)
                        {
                            MySession.Static.Unload();
                            MySession.Static = null;
                        }

                        MySessionLoader.StartLoading(delegate
                        {
                            LoadMultiplayer.Invoke(null, new object[] { world, multiplayerSession });
                        });

                        break;

                    default:
                        multiplayerSession.Dispose();
                        MySessionLoader.UnloadAndExitToMenu();
                        if (MyGameService.IsOnline)
                        {
                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.OK, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed)));
                        }
                        else
                        {
                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.OK, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), messageText: new StringBuilder(string.Format(MyTexts.GetString(MyCommonTexts.DialogTextDownloadModsFailedSteamOffline), MySession.GameServiceDisplayName))));
                        }
                        break;
                }
                MyLog.Default.WriteLine("LoadSession() - End");
            }, delegate
            {
                multiplayerSession.Dispose();
                MySessionLoader.UnloadAndExitToMenu();
            });

            return false;
        }

        private void OnFinished(MyGameServiceCallResult result)
        {

        }


        private static bool DrawInternal(MyGuiScreenLoading __instance)
        {
            //If we dont have a custom loading screen texture, do not do the special crap below

            const string mFont = "LoadingScreen";
            var mTransitionAlpha = (float)typeof(MyGuiScreenBase).GetField("m_transitionAlpha", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            MyGuiManager.DrawString(mFont, "Nexus & SeamlessClient Made by: Casimir", new Vector2(0.95f, 0.95f),
                MyGuiSandbox.GetDefaultTextScaleWithLanguage() * 1.1f,
                new Color(MyGuiConstants.LOADING_PLEASE_WAIT_COLOR * mTransitionAlpha),
                MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM);



            if (string.IsNullOrEmpty(_loadingScreenTexture))
                return true;


   

            var color = new Color(255, 255, 255, 250);
            color.A = (byte)(color.A * mTransitionAlpha);
            var fullscreenRectangle = MyGuiManager.GetFullscreenRectangle();
            MyGuiManager.DrawSpriteBatch("Textures\\GUI\\Blank.dds", fullscreenRectangle, Color.Black, false, true);
            Rectangle outRect;
            MyGuiManager.GetSafeHeightFullScreenPictureSize(MyGuiConstants.LOADING_BACKGROUND_TEXTURE_REAL_SIZE,
                out outRect);
            MyGuiManager.DrawSpriteBatch(_loadingScreenTexture, outRect,
                new Color(new Vector4(1f, 1f, 1f, mTransitionAlpha)), true, true);
            MyGuiManager.DrawSpriteBatch("Textures\\Gui\\Screens\\screen_background_fade.dds", outRect,
                new Color(new Vector4(1f, 1f, 1f, mTransitionAlpha)), true, true);

            //MyGuiSandbox.DrawGameLogoHandler(m_transitionAlpha, MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, 44, 68));

            var loadScreen = $"Loading into {_serverName}! Please wait!";


            MyGuiManager.DrawString(mFont, loadScreen, new Vector2(0.5f, 0.95f),
                MyGuiSandbox.GetDefaultTextScaleWithLanguage() * 1.1f,
                new Color(MyGuiConstants.LOADING_PLEASE_WAIT_COLOR * mTransitionAlpha),
                MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM);



            /*
            if (string.IsNullOrEmpty(m_customTextFromConstructor))
            {
                string font = m_font;
                Vector2 positionAbsoluteBottomLeft = m_multiTextControl.GetPositionAbsoluteBottomLeft();
                Vector2 textSize = m_multiTextControl.TextSize;
                Vector2 normalizedCoord = positionAbsoluteBottomLeft + new Vector2((m_multiTextControl.Size.X - textSize.X) * 0.5f + 0.025f, 0.025f);
                MyGuiManager.DrawString(font, m_authorWithDash.ToString(), normalizedCoord, MyGuiSandbox.GetDefaultTextScaleWithLanguage());
            }
            */


            //m_multiTextControl.Draw(1f, 1f);

            return false;
        }


    }
}
