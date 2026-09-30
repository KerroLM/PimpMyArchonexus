using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace PimpMyArchonexus
{
    public class PimpMyArchonexusMod : Mod
    {
        public static PimpMyArchonexusSettings settings;

        public PimpMyArchonexusMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<PimpMyArchonexusSettings>();
            var harmony = new Harmony("com.pimpmyarchonexus.patch16");
            harmony.PatchAll();
            LongEventHandler.ExecuteWhenFinished(ApplyLiveWealthToStoryteller);
        }

        public static void ApplyLiveWealthToStoryteller()
        {
            if (settings == null) return;

            var storytellerDefs = DefDatabase<StorytellerDef>.AllDefsListForReading;
            if (storytellerDefs == null) return;

            for (int i = 0; i < storytellerDefs.Count; i++)
            {
                StorytellerDef def = storytellerDefs[i];
                if (def.comps == null) continue;

                for (int j = 0; j < def.comps.Count; j++)
                {
                    StorytellerCompProperties prop = def.comps[j];

                    if (prop is StorytellerCompProperties_RefiringUniqueQuest uniqueQuestProp)
                    {
                        if (uniqueQuestProp.incident != null && uniqueQuestProp.incident.defName == "GiveQuest_EndGame_ArchonexusVictory")
                        {
                            uniqueQuestProp.minColonyWealth = Mathf.RoundToInt(settings.minWealthTrigger);
                        }
                    }
                }
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.Label("Max colonists number :");
            string maxColonistsBuffer = settings.maxColonists.ToString();
            maxColonistsBuffer = listingStandard.TextEntry(maxColonistsBuffer);
            if (int.TryParse(maxColonistsBuffer, out int newColonists))
            {
                settings.maxColonists = Mathf.Max(newColonists, 1);
            }
            listingStandard.Gap();

            listingStandard.Label("Max animals number :");
            string maxAnimalsBuffer = settings.maxAnimals.ToString();
            maxAnimalsBuffer = listingStandard.TextEntry(maxAnimalsBuffer);
            if (int.TryParse(maxAnimalsBuffer, out int newAnimals))
            {
                settings.maxAnimals = Mathf.Max(newAnimals, 0);
            }
            listingStandard.Gap();

            listingStandard.Label("Max relics number :");
            string maxRelicsBuffer = settings.maxRelics.ToString();
            maxRelicsBuffer = listingStandard.TextEntry(maxRelicsBuffer);
            if (int.TryParse(maxRelicsBuffer, out int newRelics))
            {
                settings.maxRelics = Mathf.Max(newRelics, 0);
            }
            listingStandard.Gap();

            listingStandard.Label("Max items number :");
            string maxItemsBuffer = settings.maxItems.ToString();
            maxItemsBuffer = listingStandard.TextEntry(maxItemsBuffer);
            if (int.TryParse(maxItemsBuffer, out int newItems))
            {
                settings.maxItems = Mathf.Max(newItems, 0);
            }
            listingStandard.Gap();

            listingStandard.Label($"Trigger wealth (Quest appearance, this requires a game restart to take effect) : {Mathf.RoundToInt(settings.minWealthTrigger)}");
            string triggerWealthBuffer = Mathf.RoundToInt(settings.minWealthTrigger).ToString();
            triggerWealthBuffer = listingStandard.TextEntry(triggerWealthBuffer);
            if (float.TryParse(triggerWealthBuffer, out float newTriggerWealth))
            {
                settings.minWealthTrigger = Mathf.Max(newTriggerWealth, 0f);
            }
            listingStandard.Gap();

            listingStandard.Label($"Required wealth to accept sale : {Mathf.RoundToInt(settings.minWealth)}");
            string minWealthBuffer = Mathf.RoundToInt(settings.minWealth).ToString();
            minWealthBuffer = listingStandard.TextEntry(minWealthBuffer);
            if (float.TryParse(minWealthBuffer, out float newMinWealth))
            {
                settings.minWealth = Mathf.Max(newMinWealth, 0f);
            }

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Pimp My Archonexus";
        }
    }

    public class PimpMyArchonexusSettings : ModSettings
    {
        public int maxColonists = 5;
        public int maxAnimals = 5;
        public int maxRelics = 1;
        public int maxItems = 7;
        public float minWealth = 350000f;
        public float minWealthTrigger = 150000f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref maxColonists, "maxColonists", 5);
            Scribe_Values.Look(ref maxAnimals, "maxAnimals", 5);
            Scribe_Values.Look(ref maxRelics, "maxRelics", 1);
            Scribe_Values.Look(ref maxItems, "maxItems", 7);
            Scribe_Values.Look(ref minWealth, "minWealth", 350000f);
            Scribe_Values.Look(ref minWealthTrigger, "minWealthTrigger", 150000f);
        }
    }

    [HarmonyPatch(typeof(Dialog_ChooseThingsForNewColony))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(Action<List<Thing>>), typeof(int), typeof(int), typeof(int), typeof(int), typeof(Action) })]
    public static class Patch_DialogChooseThings_Constructor
    {
        public static void Prefix(
            Action<List<Thing>> postAccepted,
            ref int maxColonists,
            ref int maxAnimals,
            ref int maxRelics,
            ref int maxItems,
            Action cancel)
        {
            if (PimpMyArchonexusMod.settings != null)
            {
                maxColonists = PimpMyArchonexusMod.settings.maxColonists;
                maxAnimals = PimpMyArchonexusMod.settings.maxAnimals;
                maxRelics = PimpMyArchonexusMod.settings.maxRelics;
                maxItems = PimpMyArchonexusMod.settings.maxItems;
            }
        }
    }

    [HarmonyPatch(typeof(QuestNode_Root_ArchonexusVictory_Cycle), "RunInt")]
    public static class Patch_QuestNode_ArchonexusVictory_Cycle_RunInt
    {
        [HarmonyPrefix]
        public static bool Prefix(QuestNode_Root_ArchonexusVictory_Cycle __instance)
        {
            if (PimpMyArchonexusMod.settings == null) return true;

            float customWealth = PimpMyArchonexusMod.settings.minWealth;
            int customColonists = PimpMyArchonexusMod.settings.maxColonists;
            int customAnimals = PimpMyArchonexusMod.settings.maxAnimals;

            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;

            var mapField = typeof(QuestNode_Root_ArchonexusVictory_Cycle).GetField("map", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Map currentMap = QuestGen_Get.GetMap(mustBeInfestable: false, null, canBeSpace: true);
            mapField?.SetValue(__instance, currentMap);

            string text = QuestGen.GenerateNewSignal("PlayerWealthSatisfied");
            string text2 = QuestGen.GenerateNewSignal("SendLetterReminder");
            QuestGen.GenerateNewSignal("ActivateLetterReminderSignal");

            QuestPart_RequirementsToAcceptPlayerWealth reqWealth = new QuestPart_RequirementsToAcceptPlayerWealth();
            reqWealth.requiredPlayerWealth = customWealth;
            quest.AddPart(reqWealth);

            QuestPart_PlayerWealth pWealth = new QuestPart_PlayerWealth();
            pWealth.inSignalEnable = quest.AddedSignal;
            pWealth.playerWealth = customWealth;
            pWealth.outSignalsCompleted.Add(text);
            pWealth.signalListenMode = QuestPart.SignalListenMode.NotYetAcceptedOnly;
            quest.AddPart(pWealth);

            QuestPart_PassOutInterval passOut = new QuestPart_PassOutInterval();
            passOut.signalListenMode = QuestPart.SignalListenMode.NotYetAcceptedOnly;
            passOut.inSignalEnable = text;
            passOut.ticksInterval = new IntRange(3600000, 3600000);
            passOut.outSignals.Add(text2);
            quest.AddPart(passOut);

            QuestPart_Filter_PlayerWealth filterWealth = new QuestPart_Filter_PlayerWealth();
            filterWealth.minPlayerWealth = customWealth;
            filterWealth.inSignal = text2;
            filterWealth.outSignal = QuestGen.GenerateNewSignal("OuterNodeCompleted");
            filterWealth.signalListenMode = QuestPart.SignalListenMode.NotYetAcceptedOnly;
            quest.AddPart(filterWealth);

            quest.CanAcceptQuest(delegate
            {
                QuestNode_ResolveQuestName.Resolve();
                string text3 = slate.Get<string>("resolvedQuestName");
                quest.Letter(LetterDefOf.PositiveEvent, null, null, null, null, useColonistsFromCaravanArg: false, QuestPart.SignalListenMode.NotYetAcceptedOnly, null, filterDeadPawnsFromLookTargets: false, label: "LetterLabelArchonexusWealthReached".Translate(text3), text: "LetterTextArchonexusWealthReached".Translate(text3));
            }, null, filterWealth.outSignal, null, null, QuestPart.SignalListenMode.NotYetAcceptedOnly);

            var cycleProperty = typeof(QuestNode_Root_ArchonexusVictory_Cycle).GetProperty("ArchonexusCycle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            int currentCycle = (int)(cycleProperty?.GetValue(__instance) ?? 1);

            Reward_ArchonexusMap rewardMap = new Reward_ArchonexusMap();
            rewardMap.currentPart = currentCycle;
            QuestPart_Choice choice = quest.RewardChoice();
            QuestPart_Choice.Choice item = new QuestPart_Choice.Choice
            {
                rewards = { (Reward)rewardMap }
            };
            choice.choices.Add(item);

            List<MapParent> list = new List<MapParent>();
            List<Map> maps = Find.Maps;
            for (int num = 0; num < maps.Count; num++)
            {
                Map m = maps[num];
                if (m.IsPlayerHome)
                {
                    list.Add(m.Parent);
                }
            }

            slate.Set("playerSettlements", list);
            slate.Set("playerSettlementsCount", list.Count);
            slate.Set("colonistsAllowed", customColonists);
            slate.Set("animalsAllowed", customAnimals);
            slate.Set("requiredWealth", customWealth);
            slate.Set("maxRelics", PimpMyArchonexusMod.settings.maxRelics);
            slate.Set("map", currentMap);
            slate.Set("mapParent", currentMap.Parent);

            return false;
        }
    }
}
