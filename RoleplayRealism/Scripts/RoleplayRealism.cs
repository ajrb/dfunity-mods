// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2019 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallConnect;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Banking;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Questing;
using System;
using System.Collections;
using System.Collections.Generic;
using DaggerfallConnect.FallExe;
using DaggerfallWorkshop.Game.Utility;

namespace RoleplayRealism
{
    public class RoleplayRealism : MonoBehaviour
    {
        /* TODO: move this into a text table file!
         * 
         * Keys are in the following format, and if a key is not present no msg is displayed.
         *  Inside dungeon: <dungeonType><0-9>
         *  Outside:        <locType><climate><weather><dayNight><0-9>
         * Where:
         *  dungeonType = Crypt,OrcStronghold,HumanStronghold,Prison,DesecratedTemple,Mine,NaturalCave,Coven,VampireHaunt,Laboratory,HarpyNest,RuinedCastle,SpiderNest,GiantStronghold,DragonsDen,BarbarianStronghold,VolcanicCaves,ScorpionNest,Cemetery
         *  locType = None,TownCity,TownHamlet,TownVillage,HomeFarms,DungeonLabyrinth,ReligionTemple,Tavern,DungeonKeep,HomeWealthy,ReligionCult,DungeonRuin,HomePoor,Graveyard,Coven,HomeYourShips
         *  climate = Desert, Swamp, Woods, Mountains, Ocean
         *  weather = Clear, Cloudy, Rainy, Snowy
         *  dayNight = Day, Night
         */
        static IDictionary AmbientTexts = new Hashtable()
        {
            // General texts:

            { "NoneDesert0", "The sand here grows shallow." },
            { "NoneDesert1", "There are some dried bones under the sand here." },
            { "NoneSwamp0", "Someone recently burned oil here." },
            { "NoneSwamp1", "A horse neighs in the distance." },
            { "NoneSwamp2", "You see tattered clothing." },
            { "NoneSwamp3", "Tracks here suggest a recent ambush." },
            { "NoneWoods0", "You smell woodsmoke." },
            { "NoneWoods1", "A horse neighs in the distance." },
            { "NoneWoods2", "The pleasant arboreal smell relaxes you." },
            { "NoneMountains0", "You see old wagon trails." },
            { "NoneMountains1", "A horse neighs in the distance." },
            { "NoneOcean0", "Calm waves roll you." },
            { "Day0", "Small rodents scamper about nearby." },
            { "Day1", "You swat an insect buzzing in your ear." },
            { "Night0", "The darkness tries to lull you asleep." }, 
            { "Night1", "The dark night fills you with unease." }, 
            { "Night2", "Bats flutter about overhead." }, 

            { "TownCityDesert0", "You smell roasting scorpion." },
            { "TownCityDesert1", "You hear someone fiddling with a lock." },
            { "TownCitySwamp0", "Someone is cooking a vegetable stew." },
            { "TownCitySwamp1", "Footprints weigh heavily in this damp soil." },
            { "TownCityWoods0", "The smell of roast pig fills the air." },
            { "TownCityMountains0", "This area smells heavily of roast chicken." },
            { "TownCityOcean0", "The smell of the sea fills your lungs." },

            // Specific texts:

            // None-Desert-Clear
            { "NoneDesertClearDay0", "There are scorpion tracks here." },
            { "NoneDesertClearDay1", "Is that werewolf fur?" },
            { "NoneDesertClearDay2", "You see some footprints in the sand." },
            { "NoneDesertClearNight0", "A baby scorpion crosses your path." },

            // None-Desert-Cloudy
            { "NoneDesertCloudyDay0", "?" },
            { "NoneDesertCloudyNight0", "?" },

            // None-Desert-Rainy
            { "NoneDesertRainyDay0", "The wet sand is getting uncomfortable." },
            { "NoneDesertRainyNight0", "The wet sand grows deeper." },

            // None-Desert-Snowy
            { "NoneDesertSnowyDay0", "?" },
            { "NoneDesertSnowyNight0", "?" },

            // City-Desert-Clear
            { "TownCityDesertClearDay0", "You hear a child crying somewhere." },
            { "TownCityDesertClearDay1", "The heat of the day bears down on you." },
            { "TownCityDesertClearNight0", "The smell of burning fireplaces fills the air." },

            // City-Desert-Cloudy
            { "TownCityDesertCloudyDay0", "You smell delicious food being cooked around here." },
            { "TownCityDesertCloudyNight0", "?" },

            // City-Desert-Rainy
            { "TownCityDesertRainyDay0", "Puddles are starting to form nearby." },
            { "TownCityDesertRainyDay1", "You overhear, “Please, just some gold to buy bread?”" },
            { "TownCityDesertRainyDay2", "Someone shouts, “Give that back! Guards, help!”" },
            { "TownCityDesertRainyDay3", "You hear, “I already paid you! Cease these lies!”" },
            { "TownCityDesertRainyDay4", "A cry rings out, “Diseased! Keep away!”" },
            { "TownCityDesertRainyDay5", "Someone calls out, “Fresh brewed ale!”" },
            { "TownCityDesertRainyDay6", "Someone groans, “This shipment is all wrong! Why?”" },
            { "TownCityDesertRainyDay7", "You overhear, “Wanna try? I can take you!”" },
            { "TownCityDesertRainyDay8", "You hear, “That tavern's a mess!“" },
            { "TownCityDesertRainyDay9", "Someone sighs, “More miserable rain.“" },
            { "TownCityDesertRainyDay4", "A cry rings out, “Diseased! Keep away!”" },
            { "TownCityDesertRainyNight0", "Someone mutters, “Our house must be haunted.”" },

            // City-Desert-Snowy
            { "TownCityDesertSnowyDay0", "?" },
            { "TownCityDesertSnowyNight0", "?" },

            // City-Swamp-Clear
            { "TownCitySwampClearDay0", "?" },
            { "TownCitySwampClearNight0", "?" },

            // City-Swamp-Cloudy
            { "TownCitySwampCloudyDay0", "?" },
            { "TownCitySwampCloudyNight0", "?" },

            // City-Swamp-Rainy
            { "TownCitySwampRainyDay0", "?" },
            { "TownCitySwampRainyNight0", "?" },

            // City-Swamp-Snowy
            { "TownCitySwampSnowyDay0", "?" },
            { "TownCitySwampSnowyNight0", "?" },

            // Dungeon texts:

            // Dungeon: Crypt

            { "Crypt0", "You sense something creeping behind you."}            
            { "Crypt1", "A gust of cold air blows past you."}
            { "Crypt2", "Was that a werewolf howl?"}
            { "Crypt3", "A beetle crosses your path."}
            { "Crypt4", "The air here is very stiff."}
            { "Crypt5", "This place smells strongly of death."}
            { "Crypt6", "You hear very faint whispers."}
            { "Crypt7", "Someone made a very shallow grave here."}
            { "Crypt8", "Tiny rodents scamper away from you."}
            { "Crypt9", "A raven caws in the distance."}

            // Dungeon: OrcStronghold
            { "OrcStronghold0", "You notice broken fragments of a weapon."}
            { "OrcStronghold1", "This place smells terribly of orc."}
            { "OrcStronghold2", "The ground is smoothed here by dragged bodies."}
            { "OrcStronghold3", "Orcs seem to have slept here often."}
            { "OrcStronghold4", "You see writing in some primitive Orcish script."}
            { "OrcStronghold5", "Someone cries for help, far away."}
            { "OrcStronghold6", "Bits of fur and bone litter this area."}

            // Dungeon: HumanStronghold
            { "HumanStronghold0", "You hear the distant shot of an arrow."}
            { "HumanStronghold1", "Something rustles about."}
            { "HumanStronghold2", "You see unfamiliar markings scratched here."}
            { "HumanStronghold3", "Something flees from you."}
            { "HumanStronghold4", "The smell of death fills the air."}
            { "HumanStronghold5", "You smell burnt wood."}
            { "HumanStronghold6", "You vaguely smell ale."}
            { "HumanStronghold7", "Things are deathly quiet here."}
            { "HumanStronghold8", "A sigil is drawn here."}
            { "HumanStronghold9", "You could swear you hear whispers."}

            // Dungeon: Prison
            { "Prison0", "Scrawlings nearby plead for release." },
            { "Prison1", "You see tormented scribblings nearby." },
            { "Prison2", "The scent of a rotting corpse gathers here." },
            { "Prison3", "A broken key lies on the ground." },
            { "Prison4", "You notice tally marks scratched nearby." },
            { "Prison5", "Was that someone calling for help?" },
            { "Prison6", "You feel as if someone is watching you." },
            { "Prison7", "You could swear you just heard movement from afar." },
            { "Prison8", "Someone tried digging here." },
            { "Prison9", "Someone scratched an inaccurate map here." },

            // Dungeon: Desecrated Temple

            { "DesecratedTemple0", "You sense strong magic in the air." },
            { "DesecratedTemple1", "You see a prayer to Kynareth scrawled here." },
            { "DesecratedTemple2", "Someone scratched a prayer to Stendarr here." },
            { "DesecratedTemple3", "The feeling of death is overwhelming here." },
            { "DesecratedTemple4", "An upwelling of magic gathers near you." },
            { "DesecratedTemple5", "A hateful presence is nearby." },
            { "DesecratedTemple6", "Unholy energies permeate this area." },
            { "DesecratedTemple7", "Your thoughts are filled with daedra." },

            // Dungeon: Mine

            { "Mine0", "This ground is well worn." },
            { "Mine1", "Someone scratched several lines here." },
            { "Mine2", "Many footsteps have worn this ground smooth." },
            { "Mine3", "You notice writing in some kind of code." },
            { "Mine4", "You hear footsteps move away." },
            { "Mine5", "The ground is unusually warm here." },
            { "Mine6", "Tiny rodents flee from you." },
            { "Mine7", "A fly buzzes around your ear." },
            { "Mine0", "Someone tried to dig here." },

            // Dungeon: Natural Cave

            { "NaturalCave0", "You hear a cloud of bats flutter away." },
            { "NaturalCave1", "Old bear claws lie here." },
            { "NaturalCave2", "A snake crosses your path." },
            { "NaturalCave3", "Something tiny flees from you." },
            { "NaturalCave4", "You hear something rustling about." },
            { "NaturalCave5", "A moth flutters by." },
            { "NaturalCave6", "You see some wolf fur here." },
            { "NaturalCave7", "You encounter guano." },
            { "NaturalCave8", "Some whiskers litter the ground." },
            { "NaturalCave9", "You notice a snapped tree branch under foot." },

            // Dungeon: Coven

            { "Coven0", "Someone is watching you from afar." },
            { "Coven1", "You hear whispering." },
            { "Coven2", "Chanting echoes from far off." },
            { "Coven3", "You feel very dark energies here." },
            { "Coven4", "You wipe some oddly-colored dust from your face." },
            { "Coven5", "A small spider skitters in front of you." },
            { "Coven6", "You notice some gargoyle chips under your feet." },
            { "Coven7", "This area smells of something burning." },
            { "Coven8", "You feel the odd sensation of being laughed at." },
            { "Coven9", "You can’t shake the feeling of being watched." },

            // Dungeon: VampireHaunt

            { "VampireHaunt0", "You hear faint rustling." },
            { "VampireHaunt1", "Indistinct hissing comes from afar." },
            { "VampireHaunt2", "You smell blood." },
            { "VampireHaunt3", "A body was recently dragged through here." },
            { "VampireHaunt4", "The air here is disturbed." },
            { "VampireHaunt5", "Unintelligible markings cover the ground." },
            { "VampireHaunt6", "You notice a plea for help written here." },
            { "VampireHaunt7", "You see shards of a glass bottle mixed with blood." },
            { "VampireHaunt8", "A cold gust of air catches you off guard." },
            { "VampireHaunt9", "You smell death. Lots of it." },

            // Dungeon: Laboratory

            { "Laboratory0", "You see mineral deposits from a ruined atronach." },
            { "Laboratory1", "You notice rudimentary notes." },
            { "Laboratory2", "Unintelligible markings dot the ground." },
            { "Laboratory3", "You see some notes, thoroughly scratched out." },
            { "Laboratory4", "You feel concentrated magical forces." },
            { "Laboratory5", "The air grows very warm here." },
            { "Laboratory6", "The air here suddenly turns cold." },
            { "Laboratory7", "You sense volatile magicks in this area." },
            { "Laboratory8", "The ground shakes as something lumbers about." },
            { "Laboratory9", "Something pounds a surface far away." },

            // Dungeon: HarpyNest

            { "HarpyNest0", "This place is entirely devoid of smaller rodents." },
            { "HarpyNest1", "The stench of harpy is unbearable here." },
            { "HarpyNest2", "You avoid stepping in harpy droppings." },
            { "HarpyNest3", "Faint echoes of cawing fill the air." },
            { "HarpyNest4", "Caustic magicks surround you momentarily." },
            { "HarpyNest5", "A worm crawls across your foot." },
            { "HarpyNest6", "The ground and walls bear faint scratch marks." },
            { "HarpyNest7", "Is something watching you..?" },
            { "HarpyNest8", "The stench here is truly awful." },

            // Dungeon: RuinedCastle

            { "RuinedCastle0", "You hear the distant shot of an arrow."}
            { "RuinedCastle1", "Something rustles about."}
            { "RuinedCastle2", "You see unfamiliar markings scratched here."}
            { "RuinedCastle3", "Something flees from you."}
            { "RuinedCastle4", "The smell of death fills the air."}
            { "RuinedCastle5", "You smell burnt wood."}
            { "RuinedCastle6", "You vaguely smell ale."}
            { "RuinedCastle7", "Things are deathly quiet here."}
            { "RuinedCastle8", "A sigil is drawn here."}
            { "RuinedCastle9", "You could swear you hear whispers."}

            // Dungeon: SpiderNest

            { "SpiderNest0", "This area is devoid of insects."}
            { "SpiderNest1", "A tiny spider skitters across the ground."}
            { "SpiderNest2", "You feel as though something is crawling above you."}
            { "SpiderNest3", "Something is scratching a surface nearby."}
            { "SpiderNest4", "You clean some webs that have gathered on you."}
            { "SpiderNest5", "A lone insect buzzes past you."}
            { "SpiderNest6", "You brush off a small spider."}
            { "SpiderNest7", "Eight tiny legs move across your feet."}
            { "SpiderNest8", "Something hisses in the distance."}
            { "SpiderNest9", "A small spider crawls out from among your possessions."}

            // Dungeon: GiantStronghold

            { "GiantStronghold0", "Many feet have smoothed the ground here."}
            { "GiantStronghold1", "The stench of unbathed skin fills the air."}
            { "GiantStronghold2", "Tiger whiskers litter this area."}
            { "GiantStronghold3", "This area smells of fur."}
            { "GiantStronghold4", "You think you hear lumbering steps."}
            { "GiantStronghold5", "Some giant used this area as a latrine."}
            { "GiantStronghold6", "Your movement stirs up some giants’ hair."}
            { "GiantStronghold7", "The ground shakes momentarily."}
            { "GiantStronghold8", "Thudding footsteps sound above you."}

            // Dungeon: DragonsDen

            { "DragonsDen0", "You see wilted dragon scales."}
            { "DragonsDen1", "The floor here has been hardened by heat."}
            { "DragonsDen2", "Human tracks. Someone else is hunting dragons."}
            { "DragonsDen3", "You notice some lycanthrope tracks here."}
            { "DragonsDen4", "Someone whispers afar off."}
            { "DragonsDen5", "Someone scratched a message in phonetic Orcish here."}
            { "DragonsDen6", "Some footprints here are fairly recent."}
            { "DragonsDen7", "You smell burning wood here."}
            { "DragonsDen8", "A beetle scampers over your foot."}
            { "DragonsDen9", "You wipe some accumulating coal from your feet."}

            // Dungeon: BarbarianStronghold

            { "BarbarianStronghold0", "You hear the distant shot of an arrow."}
            { "BarbarianStronghold1", "Something rustles about."}
            { "BarbarianStronghold2", "You see unfamiliar markings scratched here."}
            { "BarbarianStronghold3", "Something flees from you."}
            { "BarbarianStronghold4", "The smell of death fills the air."}
            { "BarbarianStronghold5", "You smell burnt wood."}
            { "BarbarianStronghold6", "You vaguely smell ale."}
            { "BarbarianStronghold7", "Things are deathly quiet here."}
            { "BarbarianStronghold8", "A sigil is drawn here."}
            { "BarbarianStronghold9", "You could swear you hear whispers."}

            // Dungeon: VolcanicCaves

            { "VolcanicCaves0", "You feel some vents of steam under the ground."}
            { "VolcanicCaves1", "The air is moving quickly here."}
            { "VolcanicCaves2", "A warm draft blows by."}
            { "VolcanicCaves3", "The earth rumbles softly."}
            { "VolcanicCaves4", "The air here is very comfortable."}
            { "VolcanicCaves5", "You smell a whiff of sulfur."}
            { "VolcanicCaves6", "This area smells of charcoal."}
            { "VolcanicCaves7", "A summoning sigil was drawn and erased here."}
            { "VolcanicCaves8", "The air suddenly grows still."}
            { "VolcanicCaves9", "You smell something burning."}

            // Dungeon: ScorpionNest

            { "ScorpionNest0", "Tiny bits of animal corpses litter this area."}
            { "ScorpionNest1", "You see the remains of molted skin."}
            { "ScorpionNest2", "Flies buzz near you."}
            { "ScorpionNest3", "Your familiarity with the smell of scorpion grows."}
            { "ScorpionNest4", "You see some footprints nearby."}
            { "ScorpionNest5", "A small rat scampers away."}
            { "ScorpionNest6", "A tiny scorpion crawls out from your possessions."}
            { "ScorpionNest7", "The air is still here."}
            { "ScorpionNest8", "You feel like something is watching you."}
            { "ScorpionNest9", "The insects near you dissipate."}

            // Dungeon: Cemetery

            { "Cemetery0", "A beetle creeps over your foot."}
            { "Cemetery1", "Did the ground just move?"}
            { "Cemetery2", "You smell decomposing bodies."}
            { "Cemetery3", "The air is tense here."}
            { "Cemetery4", "You sense something staring at you."}
            { "Cemetery5", "Faint whispers fill the air."}
            { "Cemetery6", "You feel unwelcome here."}
            { "Cemetery7", "You feel the gaze of the dead upon you."}
            { "Cemetery8", "Someone attempted to dig the ground here."}
            { "Cemetery9", "Bones clatter about faintly."}



        };

        public static float EncEffectScaleFactor = 2f;

        protected static string[] placesTable =
        {
            "Aldleigh,              0x3181, 1, -1",
            "Northrock_Fort_Ext,    0x73A0, 1, -1",
            "Northrock_Fort,        0x73A1, 1, -1"
        };
        protected static string[] factionsTable =
        {
            "Lord_Verathon,         0, -1, 1020",
            "Captain_Ulthega,       0, -1, 1021",
            "Orthus_Dharjen,        0, -1, 1022"
        };

        static Mod mod;

        DaggerfallUnity dfUnity;
        PlayerEnterExit playerEnterExit;

        bool ambientText = false;
        float lastTickTime;
        float tickTimeInterval;
        int textChance = 95;
        int textSpecificChance = 50;
        float stdInterval = 2f;
        float postTextInterval = 4f;
        int textDisplayTime = 3;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<RoleplayRealism>();
        }

        void Awake()
        {
            ModSettings settings = mod.GetSettings();
            bool bedSleeping    = settings.GetBool("Modules", "bedSleeping");
            bool archery        = settings.GetBool("Modules", "advancedArchery");
            bool riding         = settings.GetBool("Modules", "enhancedRiding");
            bool encumbrance    = settings.GetBool("Modules", "encumbranceEffects");
            bool bandaging      = settings.GetBool("Modules", "bandaging");
            bool shipPorts      = settings.GetBool("Modules", "shipPorts");
            bool expulsion      = settings.GetBool("Modules", "underworldExpulsion");
            bool climbing       = settings.GetBool("Modules", "climbingRestriction");
            bool weaponSpeed    = settings.GetBool("Modules", "weaponSpeed");
            bool equipDamage    = settings.GetBool("Modules", "equipDamage");

            InitMod(bedSleeping, archery, riding, encumbrance, bandaging, shipPorts, expulsion, climbing, weaponSpeed, equipDamage);

            // Modules using update method.
            ambientText = settings.GetBool("Modules", "ambientText");
/*            if (ambientText)
            {
                textChance          = mod.GetSettings().GetInt("AmbientText", "textChance");
                textSpecificChance  = mod.GetSettings().GetInt("AmbientText", "textSpecificChance");
                stdInterval         = mod.GetSettings().GetInt("AmbientText", "interval");
                postTextInterval    = mod.GetSettings().GetInt("AmbientText", "postTextInterval");
                textDisplayTime     = mod.GetSettings().GetInt("AmbientText", "textDisplayTime");
            }*/

            mod.IsReady = true;
        }

        void Start()
        {
            dfUnity = DaggerfallUnity.Instance;
            playerEnterExit = GameManager.Instance.PlayerEnterExit;
            lastTickTime = Time.unscaledTime;
            tickTimeInterval = stdInterval;
        }

        void Update()
        {
            if (!dfUnity.IsReady || !playerEnterExit || GameManager.IsGamePaused)
                return;

            // Ambient text module.
            if (ambientText && !playerEnterExit.IsPlayerInsideBuilding &&
                Time.unscaledTime > lastTickTime + tickTimeInterval)
            {
                lastTickTime = Time.unscaledTime;
                tickTimeInterval = stdInterval;
                Debug.Log("tick");

                if (Dice100.SuccessRoll(textChance))
                {
                    string textMsg = SelectAmbientText();
                    if (textMsg != null)
                    {
                        Debug.Log(textMsg);
                        DaggerfallUI.AddHUDText(textMsg, textDisplayTime);
                        tickTimeInterval = postTextInterval;
                    }
                }
            }
        }

        string SelectAmbientText()
        {
            // Index (0-9)
            int index = UnityEngine.Random.Range(0, 10);
            string textKey;

            if (playerEnterExit.IsPlayerInsideDungeon)
            {
                // Handle dungeon interiors
                DFRegion.DungeonTypes dungeonType = playerEnterExit.Dungeon.Summary.DungeonType;

                textKey = string.Format("{0}{1}", dungeonType.ToString(), index);
            }
            else
            {
                // Handle exteriors - wilderness and locations based on climate, locationtype, weather, day/night
                PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
                DFRegion.LocationTypes locationType = playerGPS.IsPlayerInLocationRect ? playerGPS.CurrentLocationType : DFRegion.LocationTypes.None;

                if (Dice100.SuccessRoll(textSpecificChance))
                {
                    // Weather
                    string weather = "Clear";
                    WeatherManager weatherManager = GameManager.Instance.WeatherManager;
                    if (weatherManager.IsRaining || weatherManager.IsStorming)
                        weather = "Rainy";
                    else if (weatherManager.IsOvercast)
                        weather = "Cloudy";
                    else if (weatherManager.IsSnowing)
                        weather = "Snowy";

                    // Day / Night
                    string dayNight = DaggerfallUnity.Instance.WorldTime.Now.IsDay ? "Day" : "Night";

                    // Specific message key: LocationType & Climate & Weather & DayNight
                    textKey = string.Format("{0}{1}{2}{3}{4}", locationType.ToString(), ClimateKey(), weather, dayNight, index);
                }
                else
                {
                    // General message key: LocationType & Climate
                    textKey = string.Format("{0}{1}{2}", locationType.ToString(), ClimateKey(), index);
                }
            }

            if (AmbientTexts.Contains(textKey))
                return (string) AmbientTexts[textKey];
            else
                // TODO: return null;
                return textKey;
        }

        string ClimateKey()
        {
            switch (GameManager.Instance.PlayerGPS.CurrentClimateIndex)
            {
                case (int)MapsFile.Climates.Desert2:
                case (int)MapsFile.Climates.Desert:
                case (int)MapsFile.Climates.Subtropical:
                    return "Desert";
                case (int)MapsFile.Climates.Rainforest:
                case (int)MapsFile.Climates.Swamp:
                    return "Swamp";
                case (int)MapsFile.Climates.Woodlands:
                case (int)MapsFile.Climates.HauntedWoodlands:
                case (int)MapsFile.Climates.MountainWoods:
                    return "Woods";
                case (int)MapsFile.Climates.Mountain:
                    return "Mountains";
                default:
                    return "Ocean";
            }
        }


        public static void InitMod(bool bedSleeping, bool archery, bool riding, bool encumbrance, bool bandaging, bool shipPorts, bool expulsion, bool climbing, bool weaponSpeed, bool equipDamage)
        {
            Debug.Log("Begin mod init: RoleplayRealism");

            if (bedSleeping)
            {
                PlayerActivate.RegisterModelActivation(41000, BedActivation);
                PlayerActivate.RegisterModelActivation(41001, BedActivation);
                PlayerActivate.RegisterModelActivation(41002, BedActivation);
            }

            if (archery)
            {
                // Override adjust to hit and damage formulas
                FormulaHelper.RegisterOverride(mod, "AdjustWeaponHitChanceMod", (Func<DaggerfallEntity, DaggerfallEntity, int, int, DaggerfallUnityItem, int>)AdjustWeaponHitChanceMod);
                FormulaHelper.RegisterOverride(mod, "AdjustWeaponAttackDamage", (Func<DaggerfallEntity, DaggerfallEntity, int, int, DaggerfallUnityItem, int>)AdjustWeaponAttackDamage);
            }

            if (riding)
            {
                GameObject playerAdvGO = GameObject.Find("PlayerAdvanced");
                if (playerAdvGO)
                {
                    EnhancedRiding enhancedRiding = playerAdvGO.AddComponent<EnhancedRiding>();
                    if (enhancedRiding != null)
                    {
                        enhancedRiding.TerrainFollowing = mod.GetSettings().GetBool("EnhancedRiding", "followTerrainEnabled");
                        enhancedRiding.SetFollowTerrainSoftenFactor(mod.GetSettings().GetInt("EnhancedRiding", "followTerrainSoftenFactor"));
                    }
                }
            }

            if (encumbrance)
            {
                EntityEffectBroker.OnNewMagicRound += EncumbranceEffects_OnNewMagicRound;
            }

            Mod lootRealism = ModManager.Instance.GetMod("LootRealism");
            if (lootRealism == null && bandaging)
            {
                if (!DaggerfallUnity.Instance.ItemHelper.RegisterItemUseHander((int)UselessItems2.Bandage, UseBandage))
                    Debug.LogWarning("RoleplayRealism: Unable to register bandage use handler.");
            }

            if (shipPorts)
            {
                GameManager.Instance.TransportManager.ShipAvailiable = IsShipAvailiable;
            }

            if (expulsion)
            {
                // Register the TG/DB Guild classes
                if (!GuildManager.RegisterCustomGuild(FactionFile.GuildGroups.GeneralPopulace, typeof(ThievesGuildRR)))
                    throw new Exception("GuildGroup GeneralPopulace is already overridden, unable to register ThievesGuildRR guild class.");

                if (!GuildManager.RegisterCustomGuild(FactionFile.GuildGroups.DarkBrotherHood, typeof(DarkBrotherhoodRR)))
                    throw new Exception("GuildGroup DarkBrotherHood is already overridden, unable to register DarkBrotherhoodRR guild class.");
            }

            if (climbing)
            {
                FormulaHelper.RegisterOverride(mod, "CalculateClimbingChance", (Func<PlayerEntity, int, int>)CalculateClimbingChance);
            }

            if (weaponSpeed)
            {
                FormulaHelper.RegisterOverride(mod, "GetMeleeWeaponAnimTime", (Func<PlayerEntity, WeaponTypes, ItemHands, float>)GetMeleeWeaponAnimTime);
            }

            if (equipDamage)
            {
                FormulaHelper.RegisterOverride(mod, "ApplyConditionDamageThroughPhysicalHit", (Func<DaggerfallUnityItem, DaggerfallEntity, int, bool>)ApplyConditionDamageThroughPhysicalHit);
            }

            // Initialise the FG master quest.
            if (!QuestListsManager.RegisterQuestList("RoleplayRealism"))
                throw new Exception("Quest list name is already in use, unable to register RoleplayRealism quest list.");

            RegisterFactionIds();

            // Add additional data into the quest machine for the quests
            QuestMachine questMachine = GameManager.Instance.QuestMachine;
            questMachine.PlacesTable.AddIntoTable(placesTable);
            questMachine.FactionsTable.AddIntoTable(factionsTable);

            // Register the custom armor service
            Services.RegisterMerchantService(1022, CustomArmorService, "Custom Armor");

            Debug.Log("Finished mod init: RoleplayRealism");
        }

        private static int CalculateClimbingChance(PlayerEntity player, int basePercentSuccess)
        {
            // Fail to climb if weapon not sheathed.
            if (!GameManager.Instance.WeaponManager.Sheathed)
            {
                DaggerfallUI.SetMidScreenText("You can't climb whilst holding your weapon.", 1f);
                return 0;
            }

            int climbing = player.Skills.GetLiveSkillValue(DFCareer.Skills.Climbing);
            int luck = player.Stats.GetLiveStatValue(DFCareer.Stats.Luck);
            int skill = climbing;
            if (player.Race == Races.Khajiit)
                skill += 30;

            // Climbing effect states "target can climb twice as well" - doubling effective skill after racial applied
            if (player.IsEnhancedClimbing)
                skill *= 2;

            // Clamp skill range
            skill = Mathf.Clamp(skill, 5, 95);
            float luckFactor = Mathf.Lerp(0, 10, luck * 0.01f);

            // Skill Check
            int chance = (int)(Mathf.Lerp(basePercentSuccess, 100, skill * .01f) + luckFactor);

            Debug.LogFormat("RoleplayRealism CalculateClimbingChance = {0} with basePcSuccess={1}, climbing skill={2}, luck={3}", chance, basePercentSuccess, skill, luck);

            return chance;
        }

        private static float GetMeleeWeaponAnimTime(PlayerEntity player, WeaponTypes weaponType, ItemHands weaponHands)
        {
            float spdRatio = 0.8f;
            float strRatio = 0.2f;
            float capRatio = 0.08f;
            if (weaponHands == ItemHands.Both)
            {
                spdRatio = 0.5f;
                strRatio = 0.5f;
                capRatio = 0.15f;
            }
            else if (weaponType == WeaponTypes.Dagger || weaponType == WeaponTypes.Dagger_Magic)
            {
                spdRatio = 0.9f;
                strRatio = 0.1f;
                capRatio = 0.03f;
            }
            else if (weaponType == WeaponTypes.Melee)
            {
                spdRatio = 1f;
                strRatio = 0f;
                capRatio = 0f;
            }
            float speed = player.Stats.LiveSpeed;
            float strength = player.Stats.LiveStrength;
            if (speed > 70)
                spdRatio -= capRatio;
            if (strength > 70)
                strRatio -= capRatio;

            float frameSpeed = 3 * (115 - ((speed * spdRatio) + (strength * strRatio)));
            //Debug.LogFormat("anim= {0}ms/frame, speed={1} strength={2}", frameSpeed / FormulaHelper.classicFrameUpdate, speed * spdRatio, strength * strRatio);
            return frameSpeed / FormulaHelper.classicFrameUpdate;
        }

        private static bool ApplyConditionDamageThroughPhysicalHit(DaggerfallUnityItem item, DaggerfallEntity owner, int damage)
        {
            if (item.ItemGroup == ItemGroups.Armor)
            {
                int amount = item.IsShield ? damage / 2 : damage;
                item.LowerCondition(amount, owner);
                if (owner == GameManager.Instance.PlayerEntity)
                    Debug.LogFormat("Damaged {0} by {1} from dmg {3}, cond={2}", item.ItemName, amount, item.currentCondition, damage);
                return true;
            }
            return false;
        }

        public static ArmorMaterialTypes[] customArmorMaterials = {
            ArmorMaterialTypes.Mithril, ArmorMaterialTypes.Adamantium, ArmorMaterialTypes.Ebony, ArmorMaterialTypes.Orcish, ArmorMaterialTypes.Daedric
        };

        public static void CustomArmorService(IUserInterfaceWindow window)
        {
            Debug.Log("Custom Armor service.");

            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            if (playerEntity.Level < 9)
            {
                DaggerfallUI.MessageBox("Sorry I have not yet sourced enough rare materials to make you armor.");
                return;
            }
            ItemCollection armorItems = new ItemCollection();
            Array armorTypes = DaggerfallUnity.Instance.ItemHelper.GetEnumArray(ItemGroups.Armor);
            foreach (ArmorMaterialTypes material in customArmorMaterials)
            {
                if (playerEntity.Level < 9 ||
                    (playerEntity.Level < 12 && material >= ArmorMaterialTypes.Adamantium) ||
                    (playerEntity.Level < 15 && material >= ArmorMaterialTypes.Orcish) ||
                    (playerEntity.Level < 18 && material >= ArmorMaterialTypes.Daedric))
                    break;

                for (int i = 0; i < armorTypes.Length; i++)
                {
                    Armor armorType = (Armor)armorTypes.GetValue(i);
                    ItemTemplate itemTemplate = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate(ItemGroups.Armor, i);
                    int vs = 0;
                    int vf = 0;
                    switch (armorType)
                    {
                        case Armor.Cuirass:
                        case Armor.Left_Pauldron:
                        case Armor.Right_Pauldron:
                            vs = 1;
                            vf = 3;
                            break;
                        case Armor.Greaves:
                            vs = 2;
                            vf = 5;
                            break;
                        case Armor.Gauntlets:
                        case Armor.Boots:
                            vs = 1;
                            vf = 1;
                            break;
                        case Armor.Helm:
                            vs = 1;
                            vf = itemTemplate.variants-1;
                            break;
                        default:
                            continue;
                    }
                    for (int v = vs; v <= vf; v++)
                        armorItems.AddItem(ItemBuilder.CreateArmor(playerEntity.Gender, playerEntity.Race, armorType, material, v));
                }
            }

            DaggerfallTradeWindow tradeWindow = (DaggerfallTradeWindow)
                UIWindowFactory.GetInstanceWithArgs(UIWindowType.Trade, new object[] { DaggerfallUI.UIManager, null, DaggerfallTradeWindow.WindowModes.Buy, null });
            tradeWindow.MerchantItems = armorItems;
            DaggerfallUI.UIManager.PushWindow(tradeWindow);
        }

        private static void BedActivation(Transform transform)
        {
            //Debug.Log("zzzzzzzzzz!");
            IUserInterfaceManager uiManager = DaggerfallUI.UIManager;
            uiManager.PushWindow(new DaggerfallRestWindow(uiManager, true));
        }

        private static int AdjustWeaponHitChanceMod(DaggerfallEntity attacker, DaggerfallEntity target, int hitChanceMod, int weaponAnimTime, DaggerfallUnityItem weapon)
        {
            if (weaponAnimTime > 0 && (weapon.TemplateIndex == (int)Weapons.Short_Bow || weapon.TemplateIndex == (int)Weapons.Long_Bow))
            {
                int adjustedHitChanceMod = hitChanceMod;
                if (weaponAnimTime < 200)
                    adjustedHitChanceMod -= 40;
                else if (weaponAnimTime < 500)
                    adjustedHitChanceMod -= 10;
                else if (weaponAnimTime < 1000)
                    adjustedHitChanceMod = hitChanceMod;
                else if (weaponAnimTime < 2000)
                    adjustedHitChanceMod += 10;
                else if (weaponAnimTime > 5000)
                    adjustedHitChanceMod -= 10;
                else if (weaponAnimTime > 8000)
                    adjustedHitChanceMod -= 20;

                Debug.LogFormat("Adjusted Weapon HitChanceMod for bow drawing from {0} to {1} (t={2}ms)", hitChanceMod, adjustedHitChanceMod, weaponAnimTime);
                return adjustedHitChanceMod;
            }

            return hitChanceMod;
        }

        private static int AdjustWeaponAttackDamage(DaggerfallEntity attacker, DaggerfallEntity target, int damage, int weaponAnimTime, DaggerfallUnityItem weapon)
        {
            if (weaponAnimTime > 0 && (weapon.TemplateIndex == (int)Weapons.Short_Bow || weapon.TemplateIndex == (int)Weapons.Long_Bow))
            {
                double adjustedDamage = damage;
                if (weaponAnimTime < 800)
                    adjustedDamage *= (double)weaponAnimTime / 800;
                else if (weaponAnimTime < 5000)
                    adjustedDamage = damage;
                else if (weaponAnimTime < 6000)
                    adjustedDamage *= 0.85;
                else if (weaponAnimTime < 8000)
                    adjustedDamage *= 0.75;
                else if (weaponAnimTime < 9000)
                    adjustedDamage *= 0.5;
                else if (weaponAnimTime >= 9000)
                    adjustedDamage *= 0.25;

                Debug.LogFormat("Adjusted Weapon Damage for bow drawing from {0} to {1} (t={2}ms)", damage, (int)adjustedDamage, weaponAnimTime);
                return (int)adjustedDamage;
            }

            return damage;
        }

        private static void EncumbranceEffects_OnNewMagicRound()
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            if (!GameManager.IsGamePaused &&
                !playerEntity.IsResting &&
                playerEntity.CurrentHealth > 0 &&
                !GameManager.Instance.EntityEffectBroker.SyntheticTimeIncrease)
            {
                float encPc = playerEntity.CarriedWeight / playerEntity.MaxEncumbrance;
                float encOver = Mathf.Max(encPc - 0.75f, 0f) * EncEffectScaleFactor;
                if (encOver > 0 && encOver < 0.8)
                {
                    int speedEffect = (int)(playerEntity.Stats.PermanentSpeed * encOver);
                    int fatigueEffect = (int)(encOver * 100);
                    //Debug.LogFormat("Encumbrance {0}, over {1} = effects: {2} speed, {3} fatigue", encPc, encOver, speedEffect, fatigueEffect);

                    playerEntity.DecreaseFatigue(fatigueEffect, false);

                    EntityEffectManager playerEffectManager = playerEntity.EntityBehaviour.GetComponent<EntityEffectManager>();
                    int[] statMods = new int[DaggerfallStats.Count];
                    statMods[(int)DFCareer.Stats.Speed] = -speedEffect;
                    playerEffectManager.MergeDirectStatMods(statMods);
                }
            }
        }

        private static bool UseBandage(DaggerfallUnityItem item, ItemCollection collection)
        {
            if (collection != null)
            {
                PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
                int medical = playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Medical);
                int heal = (int) Mathf.Min(medical / 2, playerEntity.MaxHealth * 0.4f);
                Debug.LogFormat("Applying a Bandage to heal {0} health.", heal);
                collection.RemoveItem(item);

                playerEntity.IncreaseHealth(heal);
            }
            return true;
        }

        private static bool IsShipAvailiable()
        {
            if (GameManager.Instance.TransportManager.IsOnShip())
                return true;

            DFLocation location = GameManager.Instance.PlayerGPS.CurrentLocation;
            if (location.Loaded == true)
            {
                return location.Exterior.ExteriorData.PortTownAndUnknown != 0 && DaggerfallBankManager.OwnsShip;
            }

            return false;
        }

        private static bool RegisterFactionIds()
        {
            bool success = FactionFile.RegisterCustomFaction(1020, new FactionFile.FactionData()
            {
                id = 1020,
                parent = 0,
                type = 4,
                name = "Lord Verathon",
                summon = -1,
                region = 16,
                power = 10,
                face = 12,
                race = 2,
                flat1 = (183 << 7) + 20,
                sgroup = 3,
                ggroup = 0,
                children = new List<int>() { 1021 }
            });
            success = FactionFile.RegisterCustomFaction(1021, new FactionFile.FactionData()
            {
                id = 1021,
                parent = 1020,
                type = 4,
                name = "Captain Ulthega",
                summon = -1,
                region = 16,
                power = 2,
                face = 57,
                race = 2,
                flat1 = (180 << 7) + 2,
                sgroup = 4,
                ggroup = 0,
                children = null
            }) && success;
            success = FactionFile.RegisterCustomFaction(1022, new FactionFile.FactionData()
            {
                id = 1022,
                parent = 0,
                type = 4,
                name = "Orthus Dharjen",
                summon = -1,
                region = 17,
                power = 2,
                face = 380,
                race = 2,
                flat1 = (334 << 7) + 14,
                sgroup = 1,
                ggroup = 0,
                children = null
            }) && success;
            return success;
        }

    }
}