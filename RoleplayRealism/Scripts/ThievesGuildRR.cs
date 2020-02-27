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
    public class ThievesGuildRR : ThievesGuild
    {
        protected static TextFile.Token newLine = TextFile.CreateFormatToken(TextFile.Formatting.JustifyCenter);
        protected static TextFile.Token[] expulsionTokens =
        {
            TextFile.CreateTextToken("%pcn, you've disappointed us yet again with your bleedin'"), newLine,
            TextFile.CreateTextToken("shoddy work and our patience has run as dry as a desert."), newLine,
            TextFile.CreateTextToken("An' without bringing in nearly enough dough or gaining"), newLine,
            TextFile.CreateTextToken("powerful connections, is not sufficient to save your skin."), newLine, newLine,
            TextFile.CreateTextToken("You're bloody done as a thief, get 'em lads!"), newLine,
        };

        public override TextFile.Token[] TokensExpulsion()
        {
            return expulsionTokens;
        }

        protected override int AllowGuildExpulsion(PlayerEntity playerEntity, int newRank)
        {
            // Allow Thieves Guild to expel members.
            return newRank;
        }

        override public void Join()
        {
            base.Join();

            // Ensure TG reputation starts at at least 2 to give a 1 quest failure buffer.
            PersistentFactionData factionData = GameManager.Instance.PlayerEntity.FactionData;
            if (factionData.GetReputation(FactionId) < 2)
                factionData.SetReputation(FactionId, 2);
        }

        public override void Leave()
        {
            base.Leave();

            // When leaving they will try to forcibly 'retire' you!
            int deathSquad = 4 + (int)(GameManager.Instance.PlayerEntity.Level / 1.5);
            GameObjectHelper.CreateFoeSpawner(false, MobileTypes.Rogue, deathSquad, 1, 8);
            GameObjectHelper.CreateFoeSpawner(false, MobileTypes.Thief, deathSquad, 1, 4);
        }
    }
}
