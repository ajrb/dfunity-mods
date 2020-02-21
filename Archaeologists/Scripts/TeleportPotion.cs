// Project:         Archaeologists Guild for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;

namespace Archaeologists
{
    /// <summary>
    /// Teleport
    /// </summary>
    public class TeleportPotion : Teleport
    {
        public static readonly new string EffectKey = "Teleport-Potion";

        public override void SetProperties()
        {
            properties.Key = EffectKey;
            properties.AllowedTargets = EntityEffectBroker.TargetFlags_Self;
            properties.AllowedElements = EntityEffectBroker.ElementFlags_MagicOnly;
            properties.AllowedCraftingStations = MagicCraftingStations.PotionMaker;
            properties.ShowSpellIcon = false;
            properties.DisableReflectiveEnumeration = true;
        }

        public override void SetPotionProperties()
        {
            PotionRecipe teleport = new PotionRecipe(
                "Recall",
                150,
                DefaultEffectSettings(),
                (int)MiscellaneousIngredients1.Medium_tooth,
                (int)PlantIngredients1.Clover,
                (int)MetalIngredients.Tin,
                (int)CreatureIngredients1.Wraith_essence);

            // Assign recipes
            AssignPotionRecipes(teleport);

            teleport.TextureRecord = 2;

        }

    }
}
