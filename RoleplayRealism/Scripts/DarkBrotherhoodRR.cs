// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Player;
using DaggerfallWorkshop.Utility;

namespace DaggerfallWorkshop.Game.Guilds
{
    public class DarkBrotherhoodRR : DarkBrotherhood
    {
        protected static TextFile.Token newLine = TextFile.CreateFormatToken(TextFile.Formatting.JustifyCenter);
        protected static TextFile.Token[] expulsionTokens =
        {
            TextFile.CreateTextToken("%pcn, you have disappointed us yet again, and you've not"), newLine,
            TextFile.CreateTextToken("made powerful enough connections to save your reputation."), newLine, newLine,
            TextFile.CreateTextToken("Our patience is limited and it's worn thin. Now your"), newLine,
            TextFile.CreateTextToken("membership, and by extension your life, is forfeit!"), newLine,
        };

        public override TextFile.Token[] TokensExpulsion()
        {
            return expulsionTokens;
        }

        protected override int AllowGuildExpulsion(PlayerEntity playerEntity, int newRank)
        {
            // Allow Dark Brotherhood to expel members.
            return newRank;
        }

        override public void Join()
        {
            base.Join();

            // Ensure DB reputation starts at at least 2 to give a 1 quest failure buffer.
            PersistentFactionData factionData = GameManager.Instance.PlayerEntity.FactionData;
            if (factionData.GetReputation(FactionId) < 2)
                factionData.SetReputation(FactionId, 2);
        }

        public override void Leave()
        {
            base.Leave();

            // When leaving they will try to forcibly 'retire' you!
            int deathSquad = 4 + (GameManager.Instance.PlayerEntity.Level / 2);
            GameObjectHelper.CreateFoeSpawner(false, MobileTypes.Assassin, deathSquad, 1, 5);
            GameObjectHelper.CreateFoeSpawner(false, MobileTypes.Nightblade, deathSquad, 4, 16);
        }
    }
}
