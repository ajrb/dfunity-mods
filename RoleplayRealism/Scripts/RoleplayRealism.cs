// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2019 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallConnect;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Banking;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Questing;
using System.Collections.Generic;
using System;
using DaggerfallConnect.FallExe;
using DaggerfallWorkshop.Game.Utility;

namespace RoleplayRealism
{
    public class RoleplayRealism : MonoBehaviour
    {
        public static float EncEffectScaleFactor = 2f;

        protected static string[] placesTable =
        {
            "Aldleigh,              0x3181, 1, -1",
            "Northrock_Fort_Ext,    0x73A0, 1, -1",
            "Northrock_Fort,        0x73A1, 1, -1"
        };
        protected static string[] factionsTable =
        {
            "Lord_Verathon,         0, -1, 1020",
            "Captain_Ulthega,       0, -1, 1021",
            "Orthus_Dharjen,        0, -1, 1022"
        };


        static Mod mod;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<RoleplayRealism>();
        }

        void Awake()
        {
            ModSettings settings = mod.GetSettings();
            bool bedSleeping = settings.GetBool("Modules", "bedSleeping");
            bool archery = settings.GetBool("Modules", "advancedArchery");
            bool riding = settings.GetBool("Modules", "enhancedRiding");
            bool encumbrance = settings.GetBool("Modules", "encumbranceEffects");
            bool bandaging = settings.GetBool("Modules", "bandaging");
            bool shipPorts = settings.GetBool("Modules", "shipPorts");
            bool expulsion = settings.GetBool("Modules", "underworldExpulsion");
            bool climbing = settings.GetBool("Modules", "climbingRestriction");
            bool weaponSpeed = settings.GetBool("Modules", "weaponSpeed");
            bool equipDamage = settings.GetBool("Modules", "equipDamage");

            InitMod(bedSleeping, archery, riding, encumbrance, bandaging, shipPorts, expulsion, climbing, weaponSpeed, equipDamage);

            mod.IsReady = true;
        }

        public static void InitMod(bool bedSleeping, bool archery, bool riding, bool encumbrance, bool bandaging, bool shipPorts, bool expulsion, bool climbing, bool weaponSpeed, bool equipDamage)
        {
            Debug.Log("Begin mod init: RoleplayRealism");

            if (bedSleeping)
            {
                PlayerActivate.RegisterModelActivation(41000, BedActivation);
                PlayerActivate.RegisterModelActivation(41001, BedActivation);
                PlayerActivate.RegisterModelActivation(41002, BedActivation);
            }

            if (archery)
            {
                // Override adjust to hit and damage formulas
                FormulaHelper.RegisterOverride(mod, "AdjustWeaponHitChanceMod", (Func<DaggerfallEntity, DaggerfallEntity, int, int, DaggerfallUnityItem, int>)AdjustWeaponHitChanceMod);
                FormulaHelper.RegisterOverride(mod, "AdjustWeaponAttackDamage", (Func<DaggerfallEntity, DaggerfallEntity, int, int, DaggerfallUnityItem, int>)AdjustWeaponAttackDamage);
            }

            if (riding)
            {
                GameObject playerAdvGO = GameObject.Find("PlayerAdvanced");
                if (playerAdvGO)
                {
                    EnhancedRiding enhancedRiding = playerAdvGO.AddComponent<EnhancedRiding>();
                    if (enhancedRiding != null)
                    {
                        enhancedRiding.TerrainFollowing = mod.GetSettings().GetBool("EnhancedRiding", "followTerrainEnabled");
                        enhancedRiding.SetFollowTerrainSoftenFactor(mod.GetSettings().GetInt("EnhancedRiding", "followTerrainSoftenFactor"));
                    }
                }
            }

            if (encumbrance)
            {
                EntityEffectBroker.OnNewMagicRound += EncumbranceEffects_OnNewMagicRound;
            }

            Mod lootRealism = ModManager.Instance.GetMod("LootRealism");
            if (lootRealism == null && bandaging)
            {
                if (!DaggerfallUnity.Instance.ItemHelper.RegisterItemUseHander((int)UselessItems2.Bandage, UseBandage))
                    Debug.LogWarning("RoleplayRealism: Unable to register bandage use handler.");
            }

            if (shipPorts)
            {
                GameManager.Instance.TransportManager.ShipAvailiable = IsShipAvailiable;
            }

            if (expulsion)
            {
                // Register the TG/DB Guild classes
                if (!GuildManager.RegisterCustomGuild(FactionFile.GuildGroups.GeneralPopulace, typeof(ThievesGuildRR)))
                    throw new Exception("GuildGroup GeneralPopulace is already overridden, unable to register ThievesGuildRR guild class.");

                if (!GuildManager.RegisterCustomGuild(FactionFile.GuildGroups.DarkBrotherHood, typeof(DarkBrotherhoodRR)))
                    throw new Exception("GuildGroup DarkBrotherHood is already overridden, unable to register DarkBrotherhoodRR guild class.");
            }

            if (climbing)
            {
                FormulaHelper.RegisterOverride(mod, "CalculateClimbingChance", (Func<PlayerEntity, int, int>)CalculateClimbingChance);
            }

            if (weaponSpeed)
            {
                FormulaHelper.RegisterOverride(mod, "GetMeleeWeaponAnimTime", (Func<PlayerEntity, WeaponTypes, ItemHands, float>)GetMeleeWeaponAnimTime);
            }

            if (equipDamage)
            {
                FormulaHelper.RegisterOverride(mod, "ApplyConditionDamageThroughPhysicalHit", (Func<DaggerfallUnityItem, DaggerfallEntity, int, bool>)ApplyConditionDamageThroughPhysicalHit);
            }

            // Initialise the FG master quest.
            if (!QuestListsManager.RegisterQuestList("RoleplayRealism"))
                throw new Exception("Quest list name is already in use, unable to register RoleplayRealism quest list.");

            RegisterFactionIds();

            // Add additional data into the quest machine for the quests
            QuestMachine questMachine = GameManager.Instance.QuestMachine;
            questMachine.PlacesTable.AddIntoTable(placesTable);
            questMachine.FactionsTable.AddIntoTable(factionsTable);

            // Register the custom armor service
            Services.RegisterMerchantService(1022, CustomArmorService, "Custom Armor");

            Debug.Log("Finished mod init: RoleplayRealism");
        }

        private static int CalculateClimbingChance(PlayerEntity player, int basePercentSuccess)
        {
            // Fail to climb if weapon not sheathed.
            if (!GameManager.Instance.WeaponManager.Sheathed)
            {
                DaggerfallUI.SetMidScreenText("You can't climb whilst holding your weapon.", 1f);
                return 0;
            }

            int climbing = player.Skills.GetLiveSkillValue(DFCareer.Skills.Climbing);
            int luck = player.Stats.GetLiveStatValue(DFCareer.Stats.Luck);
            int skill = climbing;
            if (player.Race == Races.Khajiit)
                skill += 30;

            // Climbing effect states "target can climb twice as well" - doubling effective skill after racial applied
            if (player.IsEnhancedClimbing)
                skill *= 2;

            // Clamp skill range
            skill = Mathf.Clamp(skill, 5, 95);
            float luckFactor = Mathf.Lerp(0, 10, luck * 0.01f);

            // Skill Check
            int chance = (int)(Mathf.Lerp(basePercentSuccess, 100, skill * .01f) + luckFactor);

            Debug.LogFormat("RoleplayRealism CalculateClimbingChance = {0} with basePcSuccess={1}, climbing skill={2}, luck={3}", chance, basePercentSuccess, skill, luck);

            return chance;
        }

        private static float GetMeleeWeaponAnimTime(PlayerEntity player, WeaponTypes weaponType, ItemHands weaponHands)
        {
            float spdRatio = 0.8f;
            float strRatio = 0.2f;
            float capRatio = 0.08f;
            if (weaponHands == ItemHands.Both)
            {
                spdRatio = 0.5f;
                strRatio = 0.5f;
                capRatio = 0.15f;
            }
            else if (weaponType == WeaponTypes.Dagger || weaponType == WeaponTypes.Dagger_Magic)
            {
                spdRatio = 0.9f;
                strRatio = 0.1f;
                capRatio = 0.03f;
            }
            else if (weaponType == WeaponTypes.Melee)
            {
                spdRatio = 1f;
                strRatio = 0f;
                capRatio = 0f;
            }
            float speed = player.Stats.LiveSpeed;
            float strength = player.Stats.LiveStrength;
            if (speed > 70)
                spdRatio -= capRatio;
            if (strength > 70)
                strRatio -= capRatio;

            float frameSpeed = 3 * (115 - ((speed * spdRatio) + (strength * strRatio)));
            //Debug.LogFormat("anim= {0}ms/frame, speed={1} strength={2}", frameSpeed / FormulaHelper.classicFrameUpdate, speed * spdRatio, strength * strRatio);
            return frameSpeed / FormulaHelper.classicFrameUpdate;
        }

        private static bool ApplyConditionDamageThroughPhysicalHit(DaggerfallUnityItem item, DaggerfallEntity owner, int damage)
        {
            if (item.ItemGroup == ItemGroups.Armor)
            {
                int amount = item.IsShield ? damage / 2 : damage;
                item.LowerCondition(amount, owner);
                if (owner == GameManager.Instance.PlayerEntity)
                    Debug.LogFormat("Damaged {0} by {1} from dmg {3}, cond={2}", item.ItemName, amount, item.currentCondition, damage);
                return true;
            }
            return false;
        }

        public static ArmorMaterialTypes[] customArmorMaterials = {
            ArmorMaterialTypes.Mithril, ArmorMaterialTypes.Adamantium, ArmorMaterialTypes.Ebony, ArmorMaterialTypes.Orcish, ArmorMaterialTypes.Daedric
        };

        public static void CustomArmorService(IUserInterfaceWindow window)
        {
            Debug.Log("Custom Armor service.");

            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            ItemCollection armorItems = new ItemCollection();
            Array armorTypes = DaggerfallUnity.Instance.ItemHelper.GetEnumArray(ItemGroups.Armor);
            foreach (ArmorMaterialTypes material in customArmorMaterials)
            {
                if (playerEntity.Level < 10 ||
                    (playerEntity.Level < 12 && material >= ArmorMaterialTypes.Adamantium) ||
                    (playerEntity.Level < 15 && material >= ArmorMaterialTypes.Orcish) ||
                    (playerEntity.Level < 18 && material >= ArmorMaterialTypes.Daedric))
                    break;

                for (int i = 0; i < armorTypes.Length; i++)
                {
                    Armor armorType = (Armor)armorTypes.GetValue(i);
                    ItemTemplate itemTemplate = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate(ItemGroups.Armor, i);
                    int vs = 0;
                    int vf = 0;
                    switch (armorType)
                    {
                        case Armor.Cuirass:
                        case Armor.Left_Pauldron:
                        case Armor.Right_Pauldron:
                            vs = 1;
                            vf = 3;
                            break;
                        case Armor.Greaves:
                            vs = 2;
                            vf = 5;
                            break;
                        case Armor.Gauntlets:
                        case Armor.Boots:
                            vs = 1;
                            vf = 1;
                            break;
                        case Armor.Helm:
                            vs = 1;
                            vf = itemTemplate.variants-1;
                            break;
                        default:
                            continue;
                    }
                    for (int v = vs; v <= vf; v++)
                        armorItems.AddItem(ItemBuilder.CreateArmor(playerEntity.Gender, playerEntity.Race, armorType, material, v));
                }
            }

            DaggerfallTradeWindow tradeWindow = (DaggerfallTradeWindow)
                UIWindowFactory.GetInstanceWithArgs(UIWindowType.Trade, new object[] { DaggerfallUI.UIManager, null, DaggerfallTradeWindow.WindowModes.Buy, null });
            tradeWindow.MerchantItems = armorItems;
            DaggerfallUI.UIManager.PushWindow(tradeWindow);
        }

        private static void BedActivation(Transform transform)
        {
            //Debug.Log("zzzzzzzzzz!");
            IUserInterfaceManager uiManager = DaggerfallUI.UIManager;
            uiManager.PushWindow(new DaggerfallRestWindow(uiManager, true));
        }

        private static int AdjustWeaponHitChanceMod(DaggerfallEntity attacker, DaggerfallEntity target, int hitChanceMod, int weaponAnimTime, DaggerfallUnityItem weapon)
        {
            if (weaponAnimTime > 0 && (weapon.TemplateIndex == (int)Weapons.Short_Bow || weapon.TemplateIndex == (int)Weapons.Long_Bow))
            {
                int adjustedHitChanceMod = hitChanceMod;
                if (weaponAnimTime < 200)
                    adjustedHitChanceMod -= 40;
                else if (weaponAnimTime < 500)
                    adjustedHitChanceMod -= 10;
                else if (weaponAnimTime < 1000)
                    adjustedHitChanceMod = hitChanceMod;
                else if (weaponAnimTime < 2000)
                    adjustedHitChanceMod += 10;
                else if (weaponAnimTime > 5000)
                    adjustedHitChanceMod -= 10;
                else if (weaponAnimTime > 8000)
                    adjustedHitChanceMod -= 20;

                Debug.LogFormat("Adjusted Weapon HitChanceMod for bow drawing from {0} to {1} (t={2}ms)", hitChanceMod, adjustedHitChanceMod, weaponAnimTime);
                return adjustedHitChanceMod;
            }

            return hitChanceMod;
        }

        private static int AdjustWeaponAttackDamage(DaggerfallEntity attacker, DaggerfallEntity target, int damage, int weaponAnimTime, DaggerfallUnityItem weapon)
        {
            if (weaponAnimTime > 0 && (weapon.TemplateIndex == (int)Weapons.Short_Bow || weapon.TemplateIndex == (int)Weapons.Long_Bow))
            {
                double adjustedDamage = damage;
                if (weaponAnimTime < 800)
                    adjustedDamage *= (double)weaponAnimTime / 800;
                else if (weaponAnimTime < 5000)
                    adjustedDamage = damage;
                else if (weaponAnimTime < 6000)
                    adjustedDamage *= 0.85;
                else if (weaponAnimTime < 8000)
                    adjustedDamage *= 0.75;
                else if (weaponAnimTime < 9000)
                    adjustedDamage *= 0.5;
                else if (weaponAnimTime >= 9000)
                    adjustedDamage *= 0.25;

                Debug.LogFormat("Adjusted Weapon Damage for bow drawing from {0} to {1} (t={2}ms)", damage, (int)adjustedDamage, weaponAnimTime);
                return (int)adjustedDamage;
            }

            return damage;
        }

        private static void EncumbranceEffects_OnNewMagicRound()
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            if (!GameManager.IsGamePaused &&
                !playerEntity.IsResting &&
                playerEntity.CurrentHealth > 0 &&
                !GameManager.Instance.EntityEffectBroker.SyntheticTimeIncrease &&
                !DaggerfallUI.Instance.FadeBehaviour.FadeInProgress)
            {
                float encPc = playerEntity.CarriedWeight / playerEntity.MaxEncumbrance;
                float encOver = Mathf.Max(encPc - 0.75f, 0f) * EncEffectScaleFactor;
                if (encOver > 0 && encOver < 0.8)
                {
                    int speedEffect = (int)(playerEntity.Stats.PermanentSpeed * encOver);
                    int fatigueEffect = (int)(encOver * 100);
                    //Debug.LogFormat("Encumbrance {0}, over {1} = effects: {2} speed, {3} fatigue", encPc, encOver, speedEffect, fatigueEffect);

                    playerEntity.DecreaseFatigue(fatigueEffect, false);

                    EntityEffectManager playerEffectManager = playerEntity.EntityBehaviour.GetComponent<EntityEffectManager>();
                    int[] statMods = new int[DaggerfallStats.Count];
                    statMods[(int)DFCareer.Stats.Speed] = -speedEffect;
                    playerEffectManager.MergeDirectStatMods(statMods);
                }
            }
        }

        private static bool UseBandage(DaggerfallUnityItem item, ItemCollection collection)
        {
            if (collection != null)
            {
                PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
                int medical = playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Medical);
                int heal = (int) Mathf.Min(medical / 2, playerEntity.MaxHealth * 0.4f);
                Debug.LogFormat("Applying a Bandage to heal {0} health.", heal);
                collection.RemoveItem(item);

                playerEntity.IncreaseHealth(heal);
            }
            return true;
        }

        private static bool IsShipAvailiable()
        {
            if (GameManager.Instance.TransportManager.IsOnShip())
                return true;

            DFLocation location = GameManager.Instance.PlayerGPS.CurrentLocation;
            if (location.Loaded == true)
            {
                return location.Exterior.ExteriorData.PortTownAndUnknown != 0 && DaggerfallBankManager.OwnsShip;
            }

            return false;
        }

        private static bool RegisterFactionIds()
        {
            bool success = FactionFile.RegisterCustomFaction(1020, new FactionFile.FactionData()
            {
                id = 1020,
                parent = 0,
                type = 4,
                name = "Lord Verathon",
                summon = -1,
                region = 16,
                power = 10,
                face = 12,
                race = 2,
                flat1 = (183 << 7) + 20,
                sgroup = 3,
                ggroup = 0,
                children = new List<int>() { 1021 }
            });
            success = FactionFile.RegisterCustomFaction(1021, new FactionFile.FactionData()
            {
                id = 1021,
                parent = 1020,
                type = 4,
                name = "Captain Ulthega",
                summon = -1,
                region = 16,
                power = 2,
                face = 57,
                race = 2,
                flat1 = (180 << 7) + 2,
                sgroup = 4,
                ggroup = 0,
                children = null
            }) && success;
            success = FactionFile.RegisterCustomFaction(1022, new FactionFile.FactionData()
            {
                id = 1022,
                parent = 0,
                type = 4,
                name = "Orthus Dharjen",
                summon = -1,
                region = 17,
                power = 2,
                face = 380,
                race = 2,
                flat1 = (334 << 7) + 14,
                sgroup = 1,
                ggroup = 0,
                children = null
            }) && success;
            return success;
        }

    }
}