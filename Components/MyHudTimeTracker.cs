using EmptyKeys.UserInterface.Generated.StoreBlockView_Bindings;
using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GUI.HudViewers;
using Sandbox.Game.World;
using SeamlessClient.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace SeamlessClient.Components
{
    public class MyHudTimeTracker : ComponentBase
    {

        public override void Patch(Harmony patcher)
        {
            var AppendDistance = PatchUtils.GetMethod(typeof(MyHudMarkerRender), "AppendDistance");

            patcher.Patch(AppendDistance, postfix: new HarmonyMethod(Get(typeof(MyHudTimeTracker), nameof(ApplyTimeToTarget))));
            base.Patch(patcher);
        }

        private static void ApplyTimeToTarget(MyHudMarkerRender __instance, StringBuilder stringBuilder, double distance)
        {
            if (distance < 500 || MySession.Static.LocalHumanPlayer == null || MySession.Static.LocalHumanPlayer.Character == null)
                return;


            Vector3 velocity = new Vector3(0, 0, 0);
            if (MySession.Static.LocalHumanPlayer.Character.Parent is MyCockpit cockpit)
            {
                velocity = cockpit.CubeGrid.LinearVelocity;
            }
            else
            {
                velocity = MySession.Static.LocalHumanPlayer.Character.Physics.LinearVelocity;
            }

            double v0 = velocity.Length();
            if (v0 <= 2)
                return;


            double t = Math.Round(CalculateTimeToTarget(v0, distance), 0);

            



            if(t <= 0)
                return;

            stringBuilder.AppendLine($" [T-{FormatDuration(t)}]");
        }




        static string FormatDuration(double durationInSeconds)
        {
            if (durationInSeconds < 60)
            {
                return $"{durationInSeconds}s";
            }
            else
            {
                double minutes = Math.Round(durationInSeconds / 60, 0);
                double seconds = Math.Round(durationInSeconds % 60, 1);

                if (seconds > 0)
                {
                    return $"{minutes}m {seconds}s";
                }
                else
                {
                    return $"{minutes}m";
                }
            }
        }


        static double CalculateTimeToTarget(double velocity, double distance)
        {
            // Check for zero velocity to avoid division by zero
            if (Math.Abs(velocity) < double.Epsilon)
            {
                throw new ArgumentException("Velocity must be non-zero.");
            }

            return distance / velocity;
        }




    }
}
