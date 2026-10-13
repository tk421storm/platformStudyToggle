using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TKS_PlatformStudyToggle
{
    [StaticConstructorOnStartup]
    public static class InsertHarmony
    {
        static InsertHarmony()
        {
            Harmony harmony = new Harmony("TKS_PlatformStudyToggle");
            //Harmony.DEBUG = true;
            harmony.PatchAll();
            //Harmony.DEBUG = false;
            Log.Message($"TKS_PlatformStudyToggle: Patching finished");
        }
    }

    [StaticConstructorOnStartup]
    public static class Symbols
    {
        public static Texture2D symbolStudy;

        static Symbols()
        {
            symbolStudy = ContentFinder<Texture2D>.Get("UI/Icons/Study");
        }
    }

    public static class Utilities
    {
        public static void ToggleStudy(this Building_HoldingPlatform building, bool study)
        {
            MapComponent_PlatformStudyToggle component = building.Map.GetComponent<MapComponent_PlatformStudyToggle>();

            component.AddToCache(building.thingIDNumber, study);
        }

        public static bool GetToggleStudy(this Building_HoldingPlatform building)
        {
            MapComponent_PlatformStudyToggle component = building.Map.GetComponent<MapComponent_PlatformStudyToggle>();

            return component.GetFromCache(building.thingIDNumber);
        }
    }

    public class MapComponent_PlatformStudyToggle : MapComponent
    {

        private Dictionary<int, bool> studyCache;


        public MapComponent_PlatformStudyToggle(Map map) : base(map)
        {

        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
        }

        public override void FinalizeInit()
        {

            base.FinalizeInit();


            if (this.studyCache == null)
            {
                this.studyCache = new Dictionary<int, bool>();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.studyCache, "studyCache", LookMode.Value);
        }

        public void AddToCache(int ID, bool study)
        {
            TKS_PlatformStudyToggle.DebugMessage("caching " + study + " for " + ID);

            this.studyCache[ID] = study;

        }

        public bool GetFromCache(int ID)
        {
            if (this.studyCache.ContainsKey(ID))
            {
               return this.studyCache[ID];
            } else
            {
                TKS_PlatformStudyToggle.DebugMessage(" no cached value for " + ID+", return true");
                return true;
            }
        }

    }

    public class TKS_PlatformStudyToggleSettings : ModSettings
    {
        public bool debugPrint = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref debugPrint, "debugPrint");

            base.ExposeData();
        }
    }

    public class TKS_PlatformStudyToggle : Mod
    {
        TKS_PlatformStudyToggleSettings settings;

        public static void DebugMessage(string message)
        {
            if (LoadedModManager.GetMod<TKS_PlatformStudyToggle>().GetSettings<TKS_PlatformStudyToggleSettings>().debugPrint)
            {
                Log.Message(message);
            }
        }


        public TKS_PlatformStudyToggle(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<TKS_PlatformStudyToggleSettings>();
        }

        private string editBufferFloat;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("TKSDebugPrint".Translate(), ref settings.debugPrint);
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "TKSPlatformStudyToggletName".Translate();
        }
    }

    [HarmonyPatch(typeof(Building_HoldingPlatform))]
    static class Building_HoldingPlatform_Patches
    {


        [HarmonyPatch(typeof(Building_HoldingPlatform), "GetGizmos")]
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Building_HoldingPlatform_postfix(IEnumerable<Gizmo> __results, Building_HoldingPlatform __instance)
        {
            foreach (var value in __results)
            {
                yield return value;
            }

            Command_Toggle command_Toggle = new Command_Toggle
            {
                defaultLabel = "CommandToggleStudy".Translate(),
                defaultDesc = "CommandToggleStudyDesc".Translate(),
                icon = Symbols.symbolStudy,
                isActive = __instance.GetToggleStudy,
                toggleAction = delegate ()
                {
                    __instance.ToggleStudy(!__instance.GetToggleStudy());
                },
                hideIconIfDisabled = true
            };
            command_Toggle.tutorTag = "ToggleStudy";

            yield return command_Toggle;

            yield break;
            yield break;

        }
    }

    [HarmonyPatch(typeof(WorkGiver_DarkStudyInteract))]
    static class WorkGiver_DarkStudyInteract_patches
    {
        [HarmonyPatch(typeof(WorkGiver_DarkStudyInteract), "HasJobOnThing")]
        [HarmonyPostfix]
        public static void HasJobOnThing_Postfix(ref bool __result, Pawn pawn, Thing t, bool forced)
        {
            if (__result == true)
            {
                Building_HoldingPlatform building_HoldingPlatform;
                if (ModsConfig.AnomalyActive && (building_HoldingPlatform = (t as Building_HoldingPlatform)) != null)
                {
                    Building_HoldingPlatform platform = t as Building_HoldingPlatform;
                    if (!platform.GetToggleStudy())
                    {
                        TKS_PlatformStudyToggle.DebugMessage("not studying platform " + platform.thingIDNumber + " as study is disabled for it");
                        __result = false;

                        JobFailReason.Is("PlatformStudyDisabled".Translate());
                    }
                }
            }
        }
    }
    
}
