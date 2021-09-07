using DaggerfallConnect.Save;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;

namespace TravelOptions
{
    class TravelOptionsCastWhenHeld : CastWhenHeld
    {
        public override void SetProperties()
        {
            base.SetProperties();
            properties.DisableReflectiveEnumeration = true;
        }

        const int normalMagicItemDegradeRate = 4;
        const int restingMagicItemDegradeRate = 60;

        public override PayloadCallbackResults? EnchantmentPayloadCallback(EnchantmentPayloadFlags context, EnchantmentParam? param = null, DaggerfallEntityBehaviour sourceEntity = null, DaggerfallEntityBehaviour targetEntity = null, DaggerfallUnityItem sourceItem = null, int sourceDamage = 0)
        {
            base.EnchantmentPayloadCallback(context, param, sourceEntity, targetEntity, sourceItem, sourceDamage);

            // Validate
            if ((context != EnchantmentPayloadFlags.Equipped &&
                 context != EnchantmentPayloadFlags.MagicRound &&
                 context != EnchantmentPayloadFlags.RerollEffect) ||
                param == null || sourceEntity == null || sourceItem == null)
                return null;

            // Get caster effect manager
            EntityEffectManager casterManager = sourceEntity.GetComponent<EntityEffectManager>();
            if (!casterManager)
                return null;

            if (context == EnchantmentPayloadFlags.Equipped)
            {
                // Cast when held enchantment invokes a spell bundle that is permanent until item is removed
                InstantiateSpellBundle(param.Value, sourceEntity, sourceItem, casterManager);
            }
            else if (context == EnchantmentPayloadFlags.MagicRound)
            {
                // Apply CastWhenHeld durability loss
                ApplyDurabilityLoss(sourceItem, sourceEntity);
            }
            else if (context == EnchantmentPayloadFlags.RerollEffect)
            {
                // Recast spell bundle - previous instance has already been removed by EntityEffectManager prior to callback
                InstantiateSpellBundle(param.Value, sourceEntity, sourceItem, casterManager, true);
            }

            return null;
        }

        void ApplyDurabilityLoss(DaggerfallUnityItem item, DaggerfallEntityBehaviour entity)
        {
            // TravelOptions change: don't decrease charge while tedious traveling, in order to replicate the behavior of fast travel
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

        void InstantiateSpellBundle(EnchantmentParam param, DaggerfallEntityBehaviour sourceEntity, DaggerfallUnityItem sourceItem, EntityEffectManager casterManager, bool recast = false)
        {
            if (!string.IsNullOrEmpty(param.CustomParam))
            {
                // TODO: Instantiate a custom spell bundle
            }
            else
            {
                // Instantiate a classic spell bundle
                SpellRecord.SpellRecordData spell;
                if (GameManager.Instance.EntityEffectBroker.GetClassicSpellRecord(param.ClassicParam, out spell))
                {
                    UnityEngine.Debug.LogFormat("CastWhenHeld callback found enchantment '{0}'", spell.spellName);

                    // Create effect bundle settings from classic spell
                    EffectBundleSettings bundleSettings;
                    if (GameManager.Instance.EntityEffectBroker.ClassicSpellRecordDataToEffectBundleSettings(spell, BundleTypes.HeldMagicItem, out bundleSettings))
                    {
                        // Assign bundle
                        EntityEffectBundle bundle = new EntityEffectBundle(bundleSettings, sourceEntity);
                        bundle.FromEquippedItem = sourceItem;
                        bundle.AddRuntimeFlags(BundleRuntimeFlags.ItemRecastEnabled);
                        casterManager.AssignBundle(bundle, AssignBundleFlags.BypassSavingThrows);

                        // Play cast sound on equip for player only
                        if (casterManager.IsPlayerEntity)
                            casterManager.PlayCastSound(sourceEntity, casterManager.GetCastSoundID(bundle.Settings.ElementType), true);

                        // Classic uses an item last "cast when held" effect spell cost to determine its durability loss on equip
                        // Here, all effects are considered, as it seems more coherent to do so
                        if (!recast)
                        {
                            int amount = FormulaHelper.CalculateCastingCost(spell, false);
                            sourceItem.LowerCondition(amount, sourceEntity.Entity, sourceEntity.Entity.Items);
                            //UnityEngine.Debug.LogFormat("CastWhenHeld degraded '{0}' by {1} durability points on equip. {2}/{3} remaining.", sourceItem.LongName, amount, sourceItem.currentCondition, sourceItem.maxCondition);
                        }
                    }

                    // Store equip time as last reroll time
                    sourceItem.timeEffectsLastRerolled = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
                }
            }
        }
    }
}
