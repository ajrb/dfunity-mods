// Project:         Archaeologists Guild for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2019 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;

namespace DaggerfallWorkshop.Game.Guilds
{
    public class DarkBrotherhoodRR : DarkBrotherhood
    {
        protected static TextFile.Token newLine = TextFile.CreateFormatToken(TextFile.Formatting.JustifyCenter);
        protected static TextFile.Token[] expulsionTokens =
        {
            TextFile.CreateTextToken("%pcn, you've not made nearly enough connections to save"), newLine,
            TextFile.CreateTextToken("your reputation, and now you've disappointed us again"), newLine,
            TextFile.CreateTextToken("with more dire performance. Our patience has worn thin."), newLine, newLine,
            TextFile.CreateTextToken("Your membership is over and your life forfeit, get 'em lads!"), newLine,
        };

        public override TextFile.Token[] TokensExpulsion()
        {
            return expulsionTokens;
        }

        protected override int AllowGuildExpulsion(int newRank)
        {
            // Allow Dark Brotherhood to expel members.
            return newRank;
        }

        public override void Leave()
        {
            // When leaving they will try to forcibly 'retire' you!
            base.Leave();
            GameObjectHelper.CreateFoeSpawner(false, MobileTypes.Assassin, UnityEngine.Random.Range(6, 8), 1, 64);
            GameObjectHelper.CreateFoeSpawner(false, MobileTypes.Nightblade, UnityEngine.Random.Range(6, 8), 1, 64);
        }
    }
}
