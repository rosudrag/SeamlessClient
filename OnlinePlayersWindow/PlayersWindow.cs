using HarmonyLib;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using SeamlessClient.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamlessClient.OnlinePlayersWindow
{

    public class PlayersWindow : ComponentBase
    {

        public override void Patch(Harmony patcher)
        {
            patcher.CreateClassProcessor(typeof(OnlineNexusPlayersWindow)).Patch();
        }


        public override void Initilized()
        {
            MyPerGameSettings.GUI.PlayersScreen = typeof(OnlineNexusPlayersWindow); 
        }






    }
}
