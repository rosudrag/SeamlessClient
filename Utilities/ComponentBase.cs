using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamlessClient.Utilities
{
    public class ComponentBase
    {

        public virtual void Initilized()
        {

        }

        public virtual void Update() { }

        public virtual void Patch(Harmony patcher) { }


        public virtual void Destroy() { }


    }
}
