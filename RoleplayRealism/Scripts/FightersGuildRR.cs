// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2024 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using System.Collections.Generic;
using DaggerfallConnect;

namespace DaggerfallWorkshop.Game.Guilds
{
    public class FightersGuildRR : FightersGuild
    {
        static List<DFCareer.Skills> guildSkills = new List<DFCareer.Skills>() {
                DFCareer.Skills.Archery,
                DFCareer.Skills.Axe,
                DFCareer.Skills.BluntWeapon,
                DFCareer.Skills.HandToHand,
                DFCareer.Skills.LongBlade,
                DFCareer.Skills.Orcish,
                DFCareer.Skills.ShortBlade,
            };

        static List<DFCareer.Skills> trainingSkills = new List<DFCareer.Skills>() {
                DFCareer.Skills.Archery,
                DFCareer.Skills.Axe,
                DFCareer.Skills.BluntWeapon,
                DFCareer.Skills.CriticalStrike,
                DFCareer.Skills.HandToHand,
                DFCareer.Skills.Jumping,
                DFCareer.Skills.LongBlade,
                DFCareer.Skills.Orcish,
                DFCareer.Skills.Running,
                DFCareer.Skills.ShortBlade,
                DFCareer.Skills.Swimming
            };

        public override List<DFCareer.Skills> GuildSkills { get { return guildSkills; } }

        public override List<DFCareer.Skills> TrainingSkills { get { return trainingSkills; } }

    }
}
