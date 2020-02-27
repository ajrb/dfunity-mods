// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;

namespace RoleplayRealism
{
    /// <summary>
    /// Cure Disease effect with change of purification potion effect invisibility changed to cure poison
    /// </summary>
    public class CureDiseaseRR : CureDisease
    {
        public override void SetProperties()
        {
            base.SetProperties();
            properties.DisableReflectiveEnumeration = true;
        }

        public override void SetPotionProperties()
        {
            EffectSettings cureSettings = SetEffectChance(DefaultEffectSettings(), 1, 10, 1);
            PotionRecipe cureDisease = new PotionRecipe(
                TextManager.Instance.GetText(textDatabase, "cureDisease"),
                100,
                cureSettings,
                (int)MiscellaneousIngredients1.Elixir_vitae,
                (int)PlantIngredients2.Fig,
                (int)MiscellaneousIngredients1.Big_tooth);

            EffectSettings purificationSettings = SetEffectChance(DefaultEffectSettings(), 1, 10, 1);
            purificationSettings = SetEffectMagnitude(purificationSettings, 5, 5, 19, 19, 1);
            PotionRecipe purification = new PotionRecipe(
                TextManager.Instance.GetText(textDatabase, "purification"),
                500,
                purificationSettings,
                (int)MiscellaneousIngredients1.Elixir_vitae,
                (int)MiscellaneousIngredients1.Nectar,
                (int)MiscellaneousIngredients1.Rain_water,
                (int)PlantIngredients2.Fig,
                (int)MiscellaneousIngredients1.Big_tooth,
                (int)CreatureIngredients1.Ectoplasm,
                (int)Gems.Diamond,
                (int)CreatureIngredients2.Mummy_wrappings);
            purification.AddSecondaryEffect(HealHealth.EffectKey);
            purification.AddSecondaryEffect(CurePoison.EffectKey);

            cureDisease.TextureRecord = 35;
            purification.TextureRecord = 35;
            AssignPotionRecipes(cureDisease, purification);
        }
    }
}
