using HarmonyLib;
using Sandbox.Game.Entities;
using SeamlessClient.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;

namespace SeamlessClient.Components
{
    public class EntityPerformanceImprovements : ComponentBase
    {
        private static bool isEnabled = false;
        private static ConcurrentDictionary<long, string> EntityNameReverseLookup = new ConcurrentDictionary<long, string>();


        public override void Patch(Harmony patcher)
        {
            if (!isEnabled)
                return;



            var setEntityName = PatchUtils.GetMethod(PatchUtils.MyEntitiesType, "SetEntityName");
            var removeName = PatchUtils.GetMethod(PatchUtils.MyEntitiesType, "RemoveName");
            var isNameExists = PatchUtils.GetMethod(PatchUtils.MyEntitiesType, "IsNameExists");
            var unloadData = PatchUtils.GetMethod(PatchUtils.MyEntitiesType, "UnloadData");

            patcher.Patch(setEntityName, prefix: new HarmonyMethod(Get(typeof(EntityPerformanceImprovements), nameof(SetEntityName))));
            patcher.Patch(removeName, prefix: new HarmonyMethod(Get(typeof(EntityPerformanceImprovements), nameof(RemoveName))));
            patcher.Patch(isNameExists, prefix: new HarmonyMethod(Get(typeof(EntityPerformanceImprovements), nameof(IsNameExists))));
            patcher.Patch(unloadData, postfix: new HarmonyMethod(Get(typeof(EntityPerformanceImprovements), nameof(UnloadData))));

            base.Patch(patcher);
        }



        // reverse dictionary

        private static bool SetEntityName(MyEntity myEntity, bool possibleRename)
        {
            if (string.IsNullOrEmpty(myEntity.Name))
                return false;


            if (possibleRename && EntityNameReverseLookup.ContainsKey(myEntity.EntityId))
            {
                var previousName = EntityNameReverseLookup[myEntity.EntityId];
                if (previousName != myEntity.Name) MyEntities.m_entityNameDictionary.Remove(previousName);
            }


            if (MyEntities.m_entityNameDictionary.TryGetValue(myEntity.Name, out var myEntity1))
            {
                if (myEntity1 == myEntity)
                    return false;
            }
            else
            {
                MyEntities.m_entityNameDictionary[myEntity.Name] = myEntity;
                EntityNameReverseLookup[myEntity.EntityId] = myEntity.Name;
            }

            return false;
        }

        private static bool RemoveName(MyEntity entity)
        {
            if (string.IsNullOrEmpty(entity.Name))
                return false;
            MyEntities.m_entityNameDictionary.Remove(entity.Name);
            EntityNameReverseLookup.Remove(entity.EntityId);
            return false;
        }

        private static bool IsNameExists(ref bool __result, MyEntity entity, string name)
        {
            if (string.IsNullOrEmpty(entity.Name))
            {
                __result = false;
                return false;
            }

            if (MyEntities.m_entityNameDictionary.ContainsKey(name))
            {
                var ent = MyEntities.m_entityNameDictionary[entity.Name];
                __result = ent != entity;
                return false;
            }

            __result = false;
            return false;
        }

        private static void UnloadData()
        {
            EntityNameReverseLookup.Clear();
        }


    }
}
