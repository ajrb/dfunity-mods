// Project:         Archaeologists Guild for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop;

namespace Archaeologists
{
    public class ArchaeologistsMod : MonoBehaviour
    {
        static Mod mod;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<ArchaeologistsMod>();
        }

        void Awake()
        {
            InitMod();
            mod.IsReady = true;
        }

        public static void InitMod()
        {
            Debug.Log("Begin mod init: Archaeologists");

            // Register the new faction id's
            if (RegisterFactionIds())
            {
                // Register the Guild class
                if (!GuildManager.RegisterCustomGuild(FactionFile.GuildGroups.GGroup0, typeof(ArchaeologistsGuild)))
                    throw new Exception("GuildGroup GGroup0 is already in use, unable to register Archaeologists Guild.");

                // Register the quest list
                if (!QuestListsManager.RegisterQuestList("Archaeologists"))
                    throw new Exception("Quest list name is already in use, unable to register Archaeologists quest list.");

                // Register the quest service id
                Services.RegisterGuildService(1000, GuildServices.Quests);
                // Register the custom locator service
                Services.RegisterGuildService(1001, ArchaeologistsGuild.LocatorService, "Locator Devices");
                // Register the custom locator item
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(LocatorItem.templateIndex, ItemGroups.MiscItems, typeof(LocatorItem));
                // Register the daedra summoning service
                Services.RegisterGuildService(1002, GuildServices.DaedraSummoning);
                // Register the custom repair service for teleport mark
                Services.RegisterGuildService(1003, ArchaeologistsGuild.RepairMarkService, "Repair Recall Mark");
                // Register the training service id
                Services.RegisterGuildService(1004, GuildServices.Training);
                // Register the indentification service id
                Services.RegisterGuildService(1005, GuildServices.Identify);
                // Register the buy potions service id
                Services.RegisterGuildService(1006, GuildServices.BuyPotions);
                // Register the make potions service id
                Services.RegisterGuildService(1007, GuildServices.MakePotions);
                // Register the make potions service id
                Services.RegisterGuildService(1008, GuildServices.MakeMagicItems);

                // Register the Teleport potion
                GameManager.Instance.EntityEffectBroker.RegisterEffectTemplate(new TeleportPotion(), true);
            }
            else
                throw new Exception("Faction id's are already in use, unable to register factions for Archaeologists Guild.");

            // Override default formula
            FormulaHelper.RegisterOverride(mod, "CalculateEnemyPacification", (Func<PlayerEntity, DFCareer.Skills, bool>)CalculateEnemyPacification);

            // Add locator device object to scene and attach script
            GameObject go = new GameObject("LocatorDevice");
            go.AddComponent<LocatorDevice>();

            Debug.Log("Finished mod init: Archaeologists");
        }

        private static bool RegisterFactionIds()
        {
            bool success = FactionFile.RegisterCustomFaction(1000, new FactionFile.FactionData()
            {
                id = 1000,
                parent = 0,
                type = 2,
                name = "The Archaeologists Guild",
                summon = -1,
                region = -1,
                power = 40,
                enemy1 = (int)FactionFile.FactionIDs.The_Mages_Guild,
                face = -1,
                race = -1,
                sgroup = 2,
                ggroup = 0,
                children = new List<int>() { 1001, 1002, 1003 }
            });
            success = FactionFile.RegisterCustomFaction(1001, new FactionFile.FactionData()
            {
                id = 1001,
                parent = 1000,
                type = 2,
                name = "Archaeologist Locators",
                summon = -1,
                region = -1,
                power = 30,
                face = -1,
                race = -1,
                sgroup = 2,
                ggroup = 0,
                children = null
            }) && success;
            success = FactionFile.RegisterCustomFaction(1002, new FactionFile.FactionData()
            {
                id = 1002,
                parent = 1000,
                type = 2,
                name = "Archaeologist Summoners",
                summon = -1,
                region = -1,
                power = 25,
                face = -1,
                race = -1,
                sgroup = 2,
                ggroup = 0,
                children = null
            }) && success;
            success = FactionFile.RegisterCustomFaction(1003, new FactionFile.FactionData()
            {
                id = 1003,
                parent = 1000,
                type = 2,
                name = "Archaeologist Repairers",
                summon = -1,
                region = -1,
                power = 25,
                face = -1,
                race = -1,
                sgroup = 2,
                ggroup = 0,
                children = null
            }) && success;
            success = FactionFile.RegisterCustomFaction(1004, new FactionFile.FactionData()
            {
                id = 1004,
                parent = 1000,
                type = 2,
                name = "Archaeologist Trainers",
                summon = -1,
                region = -1,
                power = 25,
                face = -1,
                race = -1,
                sgroup = 2,
                ggroup = 0,
                children = null
            }) && success;
            success = FactionFile.RegisterCustomFaction(1005, new FactionFile.FactionData()
            {
                id = 1005,
                parent = 1000,
                type = 2,
                name = "Archaeologist Indentifiers",
                summon = -1,
                region = -1,
                power = 25,
                face = -1,
                race = -1,
                sgroup = 2,
                ggroup = 0,
                children = null
            }) && success;
            success = FactionFile.RegisterCustomFaction(1006, new FactionFile.FactionData()
            {
                id = 1006,
                parent = 1000,
                type = 2,
                name = "Archaeologist Apothecaries",
                summon = -1,
                region = -1,
                power = 25,
                face = -1,
                race = -1,
                sgroup = 2,
                ggroup = 0,
                children = null
            }) && success;
            success = FactionFile.RegisterCustomFaction(1007, new FactionFile.FactionData()
            {
                id = 1007,
                parent = 1000,
                type = 2,
                name = "Archaeologist Mixers",
                summon = -1,
                region = -1,
                power = 25,
                face = -1,
                race = -1,
                sgroup = 2,
                ggroup = 0,
                children = null
            }) && success;
            success = FactionFile.RegisterCustomFaction(1008, new FactionFile.FactionData()
            {
                id = 1008,
                parent = 1000,
                type = 2,
                name = "Archaeologist Enchanters",
                summon = -1,
                region = -1,
                power = 25,
                face = -1,
                race = -1,
                sgroup = 2,
                ggroup = 0,
                children = null
            }) && success;
            return success;
        }

        private static bool CalculateEnemyPacification(PlayerEntity player, DFCareer.Skills languageSkill)
        {
            double chance = 0;
            if (languageSkill == DFCareer.Skills.Etiquette ||
                languageSkill == DFCareer.Skills.Streetwise)
            {
                chance += player.Skills.GetLiveSkillValue(languageSkill) - 20;
                chance += (player.Stats.LivePersonality - 55) / 3;
                chance += (player.Stats.LiveIntelligence - 55) / 3;
            }
            else
            {
                chance += player.Skills.GetLiveSkillValue(languageSkill);
                chance += (player.Stats.LivePersonality - 50) / 5;
            }
            chance += GameManager.Instance.WeaponManager.Sheathed ? 10 : -25;
            chance += (player.Stats.LiveLuck - 50) / 5;

            // Add chance from Comprehend Languages effect if present
            ComprehendLanguages languagesEffect = (ComprehendLanguages)GameManager.Instance.PlayerEffectManager.FindIncumbentEffect<ComprehendLanguages>();
            if (languagesEffect != null)
                chance += languagesEffect.ChanceValue();
           
            int roll = UnityEngine.Random.Range(0, 130);
            bool success = (roll < chance);

#if UNITY_EDITOR
            Debug.LogFormat("Archaeologists Pacification {3} using {0} skill: chance= {1}  roll= {2}", languageSkill, chance, roll, success ? "success" : "failure");
#endif            
            return success;
        }

    }
}