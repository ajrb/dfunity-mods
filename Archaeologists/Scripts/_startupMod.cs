// Project:         Archaeologists Guild for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2018 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using System.Collections.Generic;
using UnityEngine;

namespace Archaeologists
{
    public class _startupMod : MonoBehaviour
    {
        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void InitStart(InitParams initParams)
        {
            InitMod();
        }

        /* 
        *   used for debugging
        *   howto debug:
        *       -) add a dummy GameObject to DaggerfallUnityGame scene
        *       -) attach this script (_startupMod) as component
        *       -) deactivate mod in mod list (since dummy gameobject will start up mod)
        *       -) attach debugger and set breakpoint to one of the mod's cs files and debug
        */
        void Awake()
        {
            InitMod();
        }

        public static void InitMod()
        {
            Debug.Log("Begin mod init: Archaeologists");

            // Register the new faction id's
            bool success = FactionFile.RegisterCustomFaction(1000, new FactionFile.FactionData()
            {
                id = 1000,
                parent = 0,
                type = 2,
                name = "The Archaeologists Guild",
                summon = -1,
                region = -1,
                power = 40,
                face = -1,
                race = -1,
                sgroup = 2,
                ggroup = 0,
                children = new List<int>() { 1001 }
            });
            success = FactionFile.RegisterCustomFaction(1001, new FactionFile.FactionData()
            {
                id = 1001,
                parent = 1000,
                type = 2,
                name = "The Archaeologist Locators",
                summon = -1,
                region = -1,
                power = 30,
                face = -1,
                race = -1,
                sgroup = 2,
                ggroup = 0,
                children = null
            }) && success;
            if (success)
            {
                // Register the Guild class
                if (!GuildManager.RegisterCustomGuild(FactionFile.GuildGroups.GGroup0, typeof(ArchaeologistsGuild)))
                    throw new System.Exception("GuildGroup GGroup0 is already in use, unable to register Archaeologists Guild.");
                // Register the quest service id
                Services.RegisterGuildService(1000, GuildServices.Quests);
                // Register the custom locator service
                Services.RegisterGuildService(1001, ArchaeologistsGuild.LocatorService, "Locator Charges");
                // Register the custom locator item
                ItemCollection.RegisterCustomItem(typeof(LocatorItem).ToString(), typeof(LocatorItem));
                // Register the quest pack
                QuestTablesManager.RegisterQuestPack("Archaeologists");
            }
            else
                throw new System.Exception("Faction id's are already in use, unable to register factions for Archaeologists Guild.");

            // Override default formula
            FormulaHelper.formula_1pe_1sk.Add("CalculateEnemyPacification", CalculateEnemyPacification);

            // Add locator device object to scene and attach script
            GameObject go = new GameObject("LocatorDevice");
            go.AddComponent<LocatorDevice>();

            Debug.Log("Finished mod init: Archaeologists");
        }

        private static bool CalculateEnemyPacification(PlayerEntity player, DFCareer.Skills languageSkill)
        {
            Debug.Log("Pacification override!");
            double chance = 0;
            if (languageSkill == DFCareer.Skills.Etiquette ||
                languageSkill == DFCareer.Skills.Streetwise)
            {
                chance += player.Skills.GetLiveSkillValue(languageSkill) / 2;
                chance += player.Stats.LivePersonality / 2;
            }
            else
            {
                chance += player.Skills.GetLiveSkillValue(languageSkill);
                chance += (player.Stats.LivePersonality - 50) / 5;
            }
            chance += GameManager.Instance.WeaponManager.Sheathed ? 10 : -25;
            chance += (player.Stats.LiveLuck - 50) / 5;

            int roll = Random.Range(0, 130);
            bool success = (roll < chance);

            if (success)
                player.TallySkill(languageSkill, 3);    // Increased skill uses from (assumed) 1 in classic on success to make raising language skills easier
            else if (languageSkill != DFCareer.Skills.Etiquette && languageSkill != DFCareer.Skills.Streetwise)
                player.TallySkill(languageSkill, 1);

            Debug.LogFormat("Pacification {3} using {0} skill: chance= {1}  roll= {2}", languageSkill, chance, roll, success ? "success" : "failure");
            return success;
        }
    }
}