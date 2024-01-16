// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using System.Collections.Generic;

using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Player;
using DaggerfallWorkshop.Utility;

namespace DaggerfallWorkshop.Game.Guilds
{
    public class ThievesGuildRR : ThievesGuild
    {
        static Dictionary<string, string> textDataBase = null;

        protected static TextFile.Token newLine = TextFile.CreateFormatToken(TextFile.Formatting.JustifyCenter);

        public override TextFile.Token[] TokensExpulsion()
        {
            LoadTextData();

            return new TextFile.Token[] {
                TextFile.CreateTextToken(Localize("ThievesGuildExpulsion1")), newLine,
                TextFile.CreateTextToken(Localize("ThievesGuildExpulsion2")), newLine,
                TextFile.CreateTextToken(Localize("ThievesGuildExpulsion3")), newLine,
                TextFile.CreateTextToken(Localize("ThievesGuildExpulsion4")), newLine, newLine,
                TextFile.CreateTextToken(Localize("ThievesGuildExpulsion5")), newLine,
            };
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

        static void LoadTextData()
        {
            const string csvFilename = "RoleplayRealismModData.csv";

            if (textDataBase == null)
                textDataBase = StringTableCSVParser.LoadDictionary(csvFilename);

            return;
        }

        public static string Localize(string Key)
        {
            if (textDataBase.ContainsKey(Key))
                return textDataBase[Key];

            return string.Empty;
        }
    }
}
