// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using System.Collections;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;

namespace RoleplayRealism
{
    public class AmbientText
    {
        /* Keys are in the following format, and if a key is not present no msg is displayed.
         *  Inside dungeon: <dungeonType><0-9>
         *  Outside:        <locType><climate><0-9>
         *                  <locType><dayNight><0-9>
         *                  <locType><dayNight><weather><0-9>
         * Where:
         *  locType  = None,TownCity,TownHamlet,TownVillage,HomeFarms,DungeonLabyrinth,ReligionTemple,Tavern,DungeonKeep,HomeWealthy,ReligionCult,DungeonRuin,HomePoor,Graveyard,Coven,HomeYourShips
         *  climate  = Desert, Swamp, Woods, Mountains, Ocean
         *  dayNight = Day, Night
         *  weather  = Clear, Cloudy, Rainy, Snowy
         *  dungeonType = Crypt,OrcStronghold,HumanStronghold,Prison,DesecratedTemple,Mine,NaturalCave,Coven,VampireHaunt,Laboratory,HarpyNest,RuinedCastle,SpiderNest,GiantStronghold,DragonsDen,BarbarianStronghold,VolcanicCaves,ScorpionNest,Cemetery
         */
        static IDictionary AmbientTexts = new Hashtable()
        {
            #region Climate texts:

            { "NoneDesert0", "The sand here grows shallow." },
            { "NoneDesert1", "There are some dried bones under the sand here." },
            { "NoneDesert2", "There's evidence of a recent camp here." },
            { "NoneDesert3", "A tiny lizard scampers about." },
            { "NoneDesert4", "A snake submerges in the sand." },
            { "NoneDesert5", "There are scorpion tracks here." },
            { "NoneDesert6", "Is that werewolf fur?" },
            { "NoneDesert7", "You see some footprints in the sand." },
            { "NoneDesert8", "A baby scorpion crosses your path." },

            { "NoneSwamp0", "Someone recently burned oil here." },
            { "NoneSwamp1", "A horse neighs in the distance." },
            { "NoneSwamp2", "You see tattered clothing." },
            { "NoneSwamp3", "Tracks here suggest a recent ambush." },
            { "NoneSwamp4", "This ground emits gases." },

            { "NoneWoods0", "You smell woodsmoke." },
            { "NoneWoods1", "A horse neighs in the distance." },
            { "NoneWoods2", "This arboreal smell is very relaxing." },
            { "NoneWoods3", "Twigs snap underneath you." },
            { "NoneWoods4", "You notice an ant trail beside you." },
            { "NoneWoods5", "Broken bottles signal an old campsite." },

            { "NoneMountains0", "You see old wagon trails." },
            { "NoneMountains1", "A horse neighs in the distance." },
            { "NoneMountains2", "You cross over some old bones." },
            { "NoneMountains3", "There are some snake holes here." },

            { "NoneOcean0", "Calm waves roll around you." },
            { "NoneOcean1", "The smell of the sea fills your lungs." },

            { "TownCityDesert0", "You smell roasting scorpion." },
            { "TownCityDesert1", "You hear someone fiddling with a lock." },
            { "TownCityDesert2", "The heat of the day bears down on you." },
            { "TownCityDesert3", "The smell of burning fireplaces fills the air." },
            { "TownCityDesert4", "You smell delicious food being cooked." },

            { "TownCitySwamp0", "Someone is cooking a vegetable stew." },
            { "TownCitySwamp1", "Footprints weigh heavily in this damp soil." },

            { "TownCityWoods0", "The smell of roast pig fills the air." },

            { "TownCityMountains0", "This area smells heavily of roast chicken." },

            #endregion

            #region Weather texts:

            { "NoneDayClear0", "The day is so clear, you can see for miles." },
            { "NoneDayClear1", "The sky is so clear and blue." },

            { "NoneNightClear0", "Tonight it's so clear that you stop for a moment to marvel at the sheer number of stars overhead." },

            { "NoneDayRainy0", "Your clothes are becoming soaked through." },
            { "NoneDayRainy1", "The rain is making the ground muddy." },

            { "NoneNightRainy0", "The rain is making the night even more inpenetrable." },

            { "NoneDaySnowy0", "You can see your breath." },
            { "NoneDaySnowy1", "The snow is starting to pile up around you." },

            { "TownCityDayRainy0", "Puddles are starting to form nearby." },
            { "TownCityDayRainy1", "Someone sighs, “More miserable rain.“" },

            { "TownCityNightRainy0", "Someone mutters, “Our house must be haunted.”" },

            { "TownCityDaySnowy0", "The falling snow relaxes you." },
            { "TownCityDaySnowy1", "You breathe in some crisp, cold air." },
            { "TownCityDaySnowy2", "You hear, “Fresh baked bread, children! Come inside!”" },
            { "TownCityDaySnowy3", "You overhear, “I can escort your caravan, for the right price.”" },

            { "TownCityNightSnowy0", "Someone says, “Get inside, before you catch cold!”" },

            { "TownCityNightCloudy0", "The streets are quiet and gloomy, giving the night an eery feeling." },

            #endregion

            #region Day / Night texts:

            { "NoneDay0", "Small rodents scamper about nearby." },
            { "NoneDay1", "You swat an insect buzzing in your ear." },

            { "NoneNight0", "The darkness is trying to lull you to sleep." },
            { "NoneNight1", "The dark night fills you with unease." },
            { "NoneNight2", "Bats flutter about overhead." },

            { "TownCityDay0", "You hear, “That tavern's a mess!“" },
            { "TownCityDay1", "You overhear, “Please, just some gold to buy bread?”" },
            { "TownCityDay2", "Someone shouts, “Give that back! Guards, help!”" },
            { "TownCityDay3", "You hear, “I already paid you! Cease these lies!”" },
            { "TownCityDay4", "A cry rings out, “Diseased! Keep away!”" },
            { "TownCityDay5", "Someone calls out, “Fresh brewed ale!”" },
            { "TownCityDay6", "Someone groans, “This shipment is all wrong! Why?”" },
            { "TownCityDay7", "You overhear, “Wanna try? I can take you!”" },

            { "TownCityNight0", "A black cat hisses and flees." },
            { "TownCityNight1", "You hear someone fiddling with a lock." },
            { "TownCityNight2", "You hear a child crying somewhere in the distance." },

            { "TownVillageDay0", "A suspicious child watches you from afar." },
            { "TownVillageDay1", "A curious villager gawks at you." },

            { "HomeFarmsDay0", "The smell of livestock is penetrating." },
            { "HomeFarmsDay1", "The farm workers stare at you." },

            #endregion

            #region Dungeon texts:

            // Dungeon: Crypt
            { "Crypt0", "You sense something creeping behind you."}   ,         
            { "Crypt1", "A gust of cold air blows past you."},
            { "Crypt2", "Was that a werewolf howl?"},
            { "Crypt3", "A beetle crosses your path."},
            { "Crypt4", "The air here is very stiff."},
            { "Crypt5", "This place smells strongly of death."},
            { "Crypt6", "You hear very faint whispers."},
            { "Crypt7", "Someone made a very shallow grave here."},
            { "Crypt8", "Tiny rodents scamper away from you."},
            { "Crypt9", "A raven caws in the distance."},

            // Dungeon: OrcStronghold
            { "OrcStronghold0", "You notice broken fragments of a weapon."},
            { "OrcStronghold1", "This place smells terribly of orc."},
            { "OrcStronghold2", "The ground is smoothed here by dragged bodies."},
            { "OrcStronghold3", "Orcs seem to have slept here often."},
            { "OrcStronghold4", "You see writing in some primitive Orcish script."},
            { "OrcStronghold5", "Someone cries for help, far away."},
            { "OrcStronghold6", "Bits of fur and bone litter this area."},

            // Dungeon: HumanStronghold
            { "HumanStronghold0", "You hear the distant shot of an arrow."},
            { "HumanStronghold1", "Something rustles about."},
            { "HumanStronghold2", "You see unfamiliar markings scratched here."},
            { "HumanStronghold3", "Something flees from you."},
            { "HumanStronghold4", "The smell of death fills the air."},
            { "HumanStronghold5", "You smell burnt wood."},
            { "HumanStronghold6", "You vaguely smell ale."},
            { "HumanStronghold7", "Things are deathly quiet here."},
            { "HumanStronghold8", "A sigil is drawn here."},
            { "HumanStronghold9", "You could swear you hear whispers."},

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
            { "Mine8", "Someone tried to dig here." },

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
            { "RuinedCastle0", "You hear the distant shot of an arrow."},
            { "RuinedCastle1", "Something rustles about."},
            { "RuinedCastle2", "You see unfamiliar markings scratched here."},
            { "RuinedCastle3", "Something flees from you."},
            { "RuinedCastle4", "The smell of death fills the air."},
            { "RuinedCastle5", "You smell burnt wood."},
            { "RuinedCastle6", "You vaguely smell ale."},
            { "RuinedCastle7", "Things are deathly quiet here."},
            { "RuinedCastle8", "A sigil is drawn here."},
            { "RuinedCastle9", "You could swear you hear whispers."},

            // Dungeon: SpiderNest
            { "SpiderNest0", "This area is devoid of insects."},
            { "SpiderNest1", "A tiny spider skitters across the ground."},
            { "SpiderNest2", "You feel as though something is crawling above you."},
            { "SpiderNest3", "Something is scratching a surface nearby."},
            { "SpiderNest4", "You clean some webs that have gathered on you."},
            { "SpiderNest5", "A lone insect buzzes past you."},
            { "SpiderNest6", "You brush off a small spider."},
            { "SpiderNest7", "Eight tiny legs move across your feet."},
            { "SpiderNest8", "Something hisses in the distance."},
            { "SpiderNest9", "A small spider crawls out from among your possessions."},

            // Dungeon: GiantStronghold
            { "GiantStronghold0", "Many feet have smoothed the ground here."},
            { "GiantStronghold1", "The stench of unbathed skin fills the air."},
            { "GiantStronghold2", "Tiger whiskers litter this area."},
            { "GiantStronghold3", "This area smells of fur."},
            { "GiantStronghold4", "You think you hear lumbering steps."},
            { "GiantStronghold5", "Some giant used this area as a latrine."},
            { "GiantStronghold6", "Your movement has stirred up some fallen giant hair."},
            { "GiantStronghold7", "The ground shakes momentarily."},
            { "GiantStronghold8", "Thudding footsteps sound above you."},

            // Dungeon: DragonsDen
            { "DragonsDen0", "You see wilted dragon scales."},
            { "DragonsDen1", "The floor here has been hardened by heat."},
            { "DragonsDen2", "Human tracks. Someone else is hunting dragons."},
            { "DragonsDen3", "You notice some lycanthrope tracks here."},
            { "DragonsDen4", "Someone whispers afar off."},
            { "DragonsDen5", "Someone scratched a message in phonetic Orcish here."},
            { "DragonsDen6", "Some footprints here are fairly recent."},
            { "DragonsDen7", "You smell burning wood here."},
            { "DragonsDen8", "A beetle scampers over your foot."},
            { "DragonsDen9", "You wipe some accumulating coal from your feet."},

            // Dungeon: BarbarianStronghold
            { "BarbarianStronghold0", "You hear the distant shot of an arrow."},
            { "BarbarianStronghold1", "Something rustles about."},
            { "BarbarianStronghold2", "You see unfamiliar markings scratched here."},
            { "BarbarianStronghold3", "Something flees from you."},
            { "BarbarianStronghold4", "The smell of death fills the air."},
            { "BarbarianStronghold5", "You smell burnt wood."},
            { "BarbarianStronghold6", "You vaguely smell ale."},
            { "BarbarianStronghold7", "Things are deathly quiet here."},
            { "BarbarianStronghold8", "A sigil is drawn here."},
            { "BarbarianStronghold9", "You could swear you hear whispers."},

            // Dungeon: VolcanicCaves
            { "VolcanicCaves0", "You feel some vents of steam under the ground."},
            { "VolcanicCaves1", "The air is moving quickly here."},
            { "VolcanicCaves2", "A warm draft blows by."},
            { "VolcanicCaves3", "The earth rumbles softly."},
            { "VolcanicCaves4", "The air here is very comfortable."},
            { "VolcanicCaves5", "You smell a whiff of sulfur."},
            { "VolcanicCaves6", "This area smells of charcoal."},
            { "VolcanicCaves7", "A summoning sigil was drawn and erased here."},
            { "VolcanicCaves8", "The air suddenly grows still."},
            { "VolcanicCaves9", "You smell something burning."},

            // Dungeon: ScorpionNest
            { "ScorpionNest0", "Tiny bits of animal corpses litter this area."},
            { "ScorpionNest1", "You see the remains of molted skin."},
            { "ScorpionNest2", "Flies buzz near you."},
            { "ScorpionNest3", "Your familiarity with the smell of scorpion grows."},
            { "ScorpionNest4", "You see some footprints nearby."},
            { "ScorpionNest5", "A small rat scampers away."},
            { "ScorpionNest6", "A tiny scorpion crawls out from your possessions."},
            { "ScorpionNest7", "The air is still here."},
            { "ScorpionNest8", "You feel like something is watching you."},
            { "ScorpionNest9", "The insects near you dissipate."},

            // Dungeon: Cemetery
            { "Cemetery0", "A beetle creeps over your foot."},
            { "Cemetery1", "Did the ground just move?"},
            { "Cemetery2", "You smell decomposing bodies."},
            { "Cemetery3", "The air is tense here."},
            { "Cemetery4", "You sense something staring at you."},
            { "Cemetery5", "Faint whispers fill the air."},
            { "Cemetery6", "You feel unwelcome here."},
            { "Cemetery7", "You feel the gaze of the dead upon you."},
            { "Cemetery8", "Someone attempted to dig the ground here."},
            { "Cemetery9", "Bones clatter about faintly."}

            #endregion
        };

        public static string SelectAmbientText()
        {
            // Index (0-9)
            int index = Random.Range(0, 10);
            string textKey;

            PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
            if (playerEnterExit.IsPlayerInsideDungeon)
            {
                // Handle dungeon interiors
                DFRegion.DungeonTypes dungeonType = playerEnterExit.Dungeon.Summary.DungeonType;

                textKey = string.Format("{0}{1}", dungeonType.ToString(), index);
            }
            else
            {
                // Handle exteriors - wilderness and locations based on climate, locationtype, weather, day/night.

                // LocationType
                PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
                DFRegion.LocationTypes locationType = playerGPS.IsPlayerInLocationRect ? playerGPS.CurrentLocationType : DFRegion.LocationTypes.None;

                // Day / Night
                string dayNight = DaggerfallUnity.Instance.WorldTime.Now.IsDay ? "Day" : "Night";

                int outsideVariant = Random.Range(0, 3);
                if (outsideVariant == 0)
                {
                    // LocationType & Climate: <locType><climate><0-9>
                    textKey = string.Format("{0}{1}{2}", locationType.ToString(), ClimateKey(), index);
                }
                else if (outsideVariant == 1)
                {
                    // LocationType & DayNight: <locType><dayNight><0-9>
                    textKey = string.Format("{0}{1}{2}", locationType.ToString(), dayNight, index);
                }
                else// if (outsideVariant == 2)
                {
                    // LocationType & DayNight & Weather: <locType><dayNight><weather><0-9>
                    textKey = string.Format("{0}{1}{2}{3}", locationType.ToString(), dayNight, WeatherKey(), index);
                }
            }

            if (AmbientTexts.Contains(textKey))
                return (string) AmbientTexts[textKey];
            else
                // TODO: return null;
                return textKey;
        }

        private static string WeatherKey()
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
            return weather;
        }

        static string ClimateKey()
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
    }
}