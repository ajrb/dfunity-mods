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
    public class DarkBrotherhoodRR : DarkBrotherhood
    {
        static Dictionary<string, string> textDataBase = null;

        public override TextFile.Token[] TokensExpulsion()
        {
            LoadTextData();

            return new TextFile.Token[] {
                TextFile.CreateTextToken(Localize("DarkBrotherhoodExpulsion1")),
                TextFile.CreateFormatToken(TextFile.Formatting.JustifyCenter),

                TextFile.CreateTextToken(Localize("DarkBrotherhoodExpulsion2")),
                TextFile.CreateFormatToken(TextFile.Formatting.JustifyCenter),

                TextFile.NewLineToken,

                TextFile.CreateTextToken(Localize("DarkBrotherhoodExpulsion3")),
                TextFile.CreateFormatToken(TextFile.Formatting.JustifyCenter),

                TextFile.CreateTextToken(Localize("DarkBrotherhoodExpulsion4")),
                TextFile.CreateFormatToken(TextFile.Formatting.JustifyCenter),
            };
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
