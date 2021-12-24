// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2021 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;

namespace TravelOptions
{
    /// <summary>
    /// Cast spell when item held (equipped).
    /// Override for TO to prevent durability loss during time accelerated travel.
    /// </summary>
    public class CastWhenHeldTO : CastWhenHeld
    {
        protected override void ApplyDurabilityLoss(DaggerfallUnityItem item, DaggerfallEntityBehaviour entity)
        {
            if (!GameManager.Instance.EntityEffectBroker.SyntheticTimeIncrease && !TravelOptionsMod.Instance.GetTravelControlUI().isShowing)
            {
                int degradeRate = GameManager.Instance.PlayerEntity.IsResting ? restingMagicItemDegradeRate : normalMagicItemDegradeRate;
                if (GameManager.Instance.EntityEffectBroker.MagicRoundsSinceStartup % degradeRate == 0)
                {
                    item.LowerCondition(1, entity.Entity, entity.Entity.Items);
                    //UnityEngine.Debug.LogFormat("CastWhenHeld degraded '{0}' by 1 durability point. {1}/{2} remaining.", item.LongName, item.currentCondition, item.maxCondition);
                }
            }
        }
    }
}