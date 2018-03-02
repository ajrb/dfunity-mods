// Project:         Archaeologists Guild for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2018 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using System.Collections.Generic;
using UnityEngine;

namespace Archaeologists
{
    public class _startupMod : MonoBehaviour
    {
        [Invoke(StateManager.StateTypes.Start)]
        public static void InitStart(InitParams initParams)
        {
            initMod();
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
            initMod();
        }

        public static void initMod()
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
                GuildManager.RegisterCustomGuild(FactionFile.GuildGroups.GGroup0, typeof(ArchaeologistsGuild));
                // Register the quest service id
                Services.RegisterGuildService(1000, GuildServices.Quests);
                // Register the custom locator service
                Services.RegisterGuildService(1001, ArchaeologistsGuild.LocatorGuildService, "Locator Charging");
            }
            else
                throw new System.Exception("Faction id's are already in use, unable to register factions for Archaeologists Guild.");
            //Debug.Log("Faction id's are already in use, unable to register factions for Archaeologists Guild. If this is the first time loading/starting with the mod, there's a problem!");

            Debug.Log("Finished mod init: Archaeologists");
        }
    }
}