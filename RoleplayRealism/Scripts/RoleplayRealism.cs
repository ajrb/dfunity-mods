// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.FallExe;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Banking;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Utility.AssetInjection;

namespace RoleplayRealism
{
    public class RoleplayRealism : MonoBehaviour
    {
        public static float EncEffectScaleFactor = 2f;
        const int G = 85;   // Mob Array Gap from 42 .. 128 = 85

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
            bool weaponMaterials = settings.GetBool("Modules", "weaponMaterials");
            bool equipDamage = settings.GetBool("Modules", "equipDamage");
            bool enemyAppearance = settings.GetBool("Modules", "enemyAppearance");
            bool purifyPot = settings.GetBool("Modules", "purificationPotion");
            bool autoExtinguishLight = settings.GetBool("Modules", "autoExtinguishLight");
            bool classicStrDmgBonus = settings.GetBool("Modules", "classicStrengthDamageBonus");
            bool variantNpcs = settings.GetBool("Modules", "variantNpcs");

            InitMod(bedSleeping, archery, riding, encumbrance, bandaging, shipPorts, expulsion, climbing, weaponSpeed, weaponMaterials, equipDamage, enemyAppearance, purifyPot, autoExtinguishLight, classicStrDmgBonus, variantNpcs);

            mod.IsReady = true;
        }

        public static void InitMod(bool bedSleeping, bool archery, bool riding, bool encumbrance, bool bandaging, bool shipPorts, bool expulsion, bool climbing, bool weaponSpeed, bool weaponMaterials, bool equipDamage, bool enemyAppearance,
            bool purifyPot, bool autoExtinguishLight, bool classicStrDmgBonus, bool variantNpcs)
        {
            Debug.Log("Begin mod init: RoleplayRealism");

            Mod rrItemsMod = ModManager.Instance.GetMod("RoleplayRealismItems");
            ModSettings rrItemsSettings = rrItemsMod != null ? rrItemsMod.GetSettings() : null;

            if (bedSleeping)
            {
                PlayerActivate.RegisterCustomActivation(mod, 41000, BedActivation);
                PlayerActivate.RegisterCustomActivation(mod, 41001, BedActivation);
                PlayerActivate.RegisterCustomActivation(mod, 41002, BedActivation);
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
                        enhancedRiding.RealisticMovement = mod.GetSettings().GetBool("EnhancedRiding", "RealisticMovement");
                        enhancedRiding.TerrainFollowing = mod.GetSettings().GetBool("EnhancedRiding", "followTerrainEnabled");
                        enhancedRiding.SetFollowTerrainSoftenFactor(mod.GetSettings().GetInt("EnhancedRiding", "followTerrainSoftenFactor"));
                    }
                }
            }

            if (encumbrance)
            {
                EntityEffectBroker.OnNewMagicRound += EncumbranceEffects_OnNewMagicRound;
            }

            if (rrItemsMod == null && bandaging)
            {
                DaggerfallUnity.Instance.ItemHelper.RegisterItemUseHandler((int)UselessItems2.Bandage, UseBandage);
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

            if (weaponSpeed && (rrItemsSettings == null || !rrItemsSettings.GetBool("Modules", "weaponBalance")))
            {
                FormulaHelper.RegisterOverride(mod, "GetMeleeWeaponAnimTime", (Func<PlayerEntity, WeaponTypes, ItemHands, float>)GetMeleeWeaponAnimTime);
            }

            if (weaponMaterials)
            {
                FormulaHelper.RegisterOverride(mod, "CalculateWeaponToHit", (Func<DaggerfallUnityItem, int>)CalculateWeaponToHit);
            }

            if (equipDamage)
            {
                FormulaHelper.RegisterOverride(mod, "ApplyConditionDamageThroughPhysicalHit", (Func<DaggerfallUnityItem, DaggerfallEntity, int, bool>)ApplyConditionDamageThroughPhysicalHit);
            }

            if (enemyAppearance)
            {
                UpdateEnemyClassAppearances();
            }

            if (purifyPot)
            {
                GameManager.Instance.EntityEffectBroker.RegisterEffectTemplate(new CureDiseaseRR(), true);
            }

            if (autoExtinguishLight)
            {
                PlayerEnterExit.OnPreTransition += OnTransitionToDungeonExterior_ExtinguishLight;
            }

            if (classicStrDmgBonus)
            {
                FormulaHelper.RegisterOverride(mod, "DamageModifier", (Func<int, int>)DamageModifier_classicDisplay);
            }

            if (variantNpcs)
            {
                PlayerEnterExit.OnTransitionInterior += OnTransitionToInterior_VariantNPCsprites;
            }

            // Initialise the FG master quest.
            if (!QuestListsManager.RegisterQuestList("RoleplayRealism"))
                throw new Exception("Quest list name is already in use, unable to register RoleplayRealism quest list.");

            RegisterFactionIds();

            // Add additional data into the quest machine for the quests
            QuestMachine questMachine = GameManager.Instance.QuestMachine;
            questMachine.PlacesTable.AddIntoTable(placesTable);
            questMachine.FactionsTable.AddIntoTable(factionsTable);

            // Register the custom armor service and location detection
            Services.RegisterMerchantService(1022, CustomArmorService, "Custom Armor");
            PlayerGPS.OnEnterLocationRect += PlayerGPS_OnEnterLocationRect;

            Debug.Log("Finished mod init: RoleplayRealism");
        }

        private static void PlayerGPS_OnEnterLocationRect(DFLocation location)
        {
            if (WorldDataVariants.GetBuildingVariant(location.RegionIndex, location.LocationIndex, "ARMRAM03.RMB", 14) != null)
            {
                // Entered the location of the master armorer, so discover his shop with a custom name
                GameManager.Instance.PlayerGPS.DiscoverBuilding(GetMasterArmBuildingKey(location.RegionIndex), "Dharjen Custom Armor");
            }
        }

        static int GetMasterArmBuildingKey(int index)
        {
            switch (index)
            {
                case 52:    // Pjiga
                    return 131342;
                case 18:    // Penmore
                    return 197134;
                case 48:    // Paponirea
                    return 131598;
            }
            return 0;
        }

        public static int DamageModifier_classicDisplay(int strength)
        {
            return (int)Mathf.Floor((strength - 50) / 10f);
        }

        private static int CalculateClimbingChance(PlayerEntity player, int basePercentSuccess)
        {
            // Fail to climb if weapon not sheathed.
            if (!GameManager.Instance.WeaponManager.Sheathed && GameManager.Instance.WeaponManager.ScreenWeapon.WeaponType != WeaponTypes.Melee)
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

#if UNITY_EDITOR
            Debug.LogFormat("anim= {0}ms/frame, speed={1} strength={2}", frameSpeed / FormulaHelper.classicFrameUpdate, speed * spdRatio, strength * strRatio);
#endif
            return frameSpeed / FormulaHelper.classicFrameUpdate;
        }

        private static int CalculateWeaponToHit(DaggerfallUnityItem weapon)
        {
            return weapon.GetWeaponMaterialModifier() * 3;
        }

        private static bool ApplyConditionDamageThroughPhysicalHit(DaggerfallUnityItem item, DaggerfallEntity owner, int damage)
        {
            if (item.ItemGroup == ItemGroups.Armor)
            {
                int amount = damage * 5;
                item.LowerCondition(amount, owner);
#if UNITY_EDITOR
                if (owner == GameManager.Instance.PlayerEntity)
                    Debug.LogFormat("Damaged {0} by {1} from dmg {3}, cond={2}", item.ItemName, amount, item.currentCondition, damage);
#endif
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
            ItemHelper itemHelper = DaggerfallUnity.Instance.ItemHelper;
            if (playerEntity.Level < 9)
            {
                DaggerfallUI.MessageBox("Sorry I have not yet sourced enough rare materials to make you armor.");
                return;
            }
            ItemCollection armorItems = new ItemCollection();
            Array armorTypes = itemHelper.GetEnumArray(ItemGroups.Armor);
            foreach (ArmorMaterialTypes material in customArmorMaterials)
            {
                if (playerEntity.Level < 9 ||
                    (playerEntity.Level < 12 && material >= ArmorMaterialTypes.Adamantium) ||
                    (playerEntity.Level < 15 && material >= ArmorMaterialTypes.Orcish) ||
                    (playerEntity.Level < 18 && material >= ArmorMaterialTypes.Daedric))
                    break;

                for (int i = 0; i < armorTypes.Length; i++)
                {
                    Armor armorType = (Armor)armorTypes.GetValue(i);
                    ItemTemplate itemTemplate = itemHelper.GetItemTemplate(ItemGroups.Armor, i);
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
                            vs = 1;
                            vf = 1;
                            break;
                        case Armor.Boots:
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
                int[] customItemTemplates = itemHelper.GetCustomItemsForGroup(ItemGroups.Armor);
                for (int i = 0; i < customItemTemplates.Length; i++)
                {
                    DaggerfallUnityItem item = ItemBuilder.CreateItem(ItemGroups.Armor, customItemTemplates[i]);
                    ItemBuilder.ApplyArmorSettings(item, playerEntity.Gender, playerEntity.Race, material);
                    armorItems.AddItem(item);
                }
            }

            DaggerfallTradeWindow tradeWindow = (DaggerfallTradeWindow)
                UIWindowFactory.GetInstanceWithArgs(UIWindowType.Trade, new object[] { DaggerfallUI.UIManager, null, DaggerfallTradeWindow.WindowModes.Buy, null });
            tradeWindow.MerchantItems = armorItems;
            DaggerfallUI.UIManager.PushWindow(tradeWindow);
        }

        private static void BedActivation(RaycastHit hit)
        {
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

#if UNITY_EDITOR
                Debug.LogFormat("Adjusted Weapon HitChanceMod for bow drawing from {0} to {1} (t={2}ms)", hitChanceMod, adjustedHitChanceMod, weaponAnimTime);
#endif
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

#if UNITY_EDITOR
                Debug.LogFormat("Adjusted Weapon Damage for bow drawing from {0} to {1} (t={2}ms)", damage, (int)adjustedDamage, weaponAnimTime);
#endif
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
                if (encOver > 0)
                {
                    int speedEffect = Mathf.Min(playerEntity.Stats.PermanentSpeed - 2, (int)(playerEntity.Stats.PermanentSpeed * encOver));
                    int fatigueEffect = Mathf.Min(playerEntity.CurrentFatigue - 100, (int)(encOver * 100));

#if UNITY_EDITOR
                    Debug.LogFormat("Encumbrance {0}, over {1} = effects: {2} speed, {3} fatigue", encPc, encOver, speedEffect, fatigueEffect);
#endif
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
                collection.RemoveItem(item);
                playerEntity.IncreaseHealth(heal);
                playerEntity.TallySkill(DFCareer.Skills.Medical, 1);
#if UNITY_EDITOR
                Debug.LogFormat("Applied a Bandage to heal {0} health.", heal);
#endif
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

        private static void OnTransitionToDungeonExterior_ExtinguishLight(PlayerEnterExit.TransitionEventArgs args)
        {
            if (args.TransitionType == PlayerEnterExit.TransitionType.ToDungeonExterior && GameManager.Instance.PlayerEntity.LightSource != null && DaggerfallUnity.Instance.WorldTime.Now.IsDay)
            {
                DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("lightDouse"), false, GameManager.Instance.PlayerEntity.LightSource);
                GameManager.Instance.PlayerEntity.LightSource = null;
            }
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

        static void UpdateEnemyClassAppearances()
        {
            EnemyBasics.Enemies[(int)MobileTypes.Sorcerer - G].MaleTexture = 476;
            EnemyBasics.Enemies[(int)MobileTypes.Sorcerer - G].FemaleTexture = 475;
            EnemyBasics.Enemies[(int)MobileTypes.Sorcerer - G].HasRangedAttack1 = false;
            EnemyBasics.Enemies[(int)MobileTypes.Sorcerer - G].CastsMagic = true;
            EnemyBasics.Enemies[(int)MobileTypes.Sorcerer - G].PrimaryAttackAnimFrames = new int[] { 0, 1, -1, 2, 3, 4, 5 };
            EnemyBasics.Enemies[(int)MobileTypes.Sorcerer - G].ChanceForAttack2 = 33;
            EnemyBasics.Enemies[(int)MobileTypes.Sorcerer - G].PrimaryAttackAnimFrames2 = new int[] { 5, 4, 3, -1, 2, 1, 0 };
            EnemyBasics.Enemies[(int)MobileTypes.Sorcerer - G].ChanceForAttack3 = 33;
            EnemyBasics.Enemies[(int)MobileTypes.Sorcerer - G].PrimaryAttackAnimFrames3 = new int[] { 0, 1, -1, 2, 2, 1, 0 };
            EnemyBasics.Enemies[(int)MobileTypes.Sorcerer - G].HasSpellAnimation = true;
            EnemyBasics.Enemies[(int)MobileTypes.Sorcerer - G].SpellAnimFrames = new int[] { 0, 1, 2, 3, 3 };

            EnemyBasics.Enemies[(int)MobileTypes.Bard - G].MaleTexture = 482;
            EnemyBasics.Enemies[(int)MobileTypes.Bard - G].FemaleTexture = 481;
            EnemyBasics.Enemies[(int)MobileTypes.Bard - G].PrimaryAttackAnimFrames = new int[] { 0, 1, -1, 2, 3, 4, -1, 5 };
            EnemyBasics.Enemies[(int)MobileTypes.Bard - G].ChanceForAttack2 = 50;
            EnemyBasics.Enemies[(int)MobileTypes.Bard - G].PrimaryAttackAnimFrames2 = new int[] { 3, 4, -1, 5, 0 };
            EnemyBasics.Enemies[(int)MobileTypes.Bard - G].ChanceForAttack3 = 0;

            EnemyBasics.Enemies[(int)MobileTypes.Rogue - G].MaleTexture = 488;
            EnemyBasics.Enemies[(int)MobileTypes.Rogue - G].FemaleTexture = 487;
            EnemyBasics.Enemies[(int)MobileTypes.Rogue - G].PrimaryAttackAnimFrames = new int[] { 0, 0, 1, -1, 2, 2, 1, 0 };
            EnemyBasics.Enemies[(int)MobileTypes.Rogue - G].PrimaryAttackAnimFrames2 = new int[] { 0, 1, -1, 2, 3, 4, 5 };
            EnemyBasics.Enemies[(int)MobileTypes.Rogue - G].PrimaryAttackAnimFrames3 = new int[] { 5, 5, 3, -1, 2, 1, 0 };

            EnemyBasics.Enemies[(int)MobileTypes.Archer - G].MaleTexture = 480;
            EnemyBasics.Enemies[(int)MobileTypes.Archer - G].FemaleTexture = 479;
            EnemyBasics.Enemies[(int)MobileTypes.Archer - G].PrimaryAttackAnimFrames = new int[] { 0, 1, -1, 2, 3, 4, -1, 5, 0 };
            EnemyBasics.Enemies[(int)MobileTypes.Archer - G].ChanceForAttack2 = 33;
            EnemyBasics.Enemies[(int)MobileTypes.Archer - G].PrimaryAttackAnimFrames2 = new int[] { 4, 4, -1, 5, 0, 0 };
            EnemyBasics.Enemies[(int)MobileTypes.Archer - G].ChanceForAttack3 = 33;
            EnemyBasics.Enemies[(int)MobileTypes.Archer - G].PrimaryAttackAnimFrames3 = new int[] { 4, -1, 5, 0, 0, 1, -1, 2, 3, 4, -1, 5, 0 };

            EnemyBasics.Enemies[(int)MobileTypes.Ranger - G].MaleTexture = 480;
            EnemyBasics.Enemies[(int)MobileTypes.Ranger - G].FemaleTexture = 479;
            EnemyBasics.Enemies[(int)MobileTypes.Ranger - G].PrimaryAttackAnimFrames = new int[] { 0, 1, -1, 2, 3, 4, -1, 5, 0 };
            EnemyBasics.Enemies[(int)MobileTypes.Ranger - G].ChanceForAttack2 = 33;
            EnemyBasics.Enemies[(int)MobileTypes.Ranger - G].PrimaryAttackAnimFrames2 = new int[] { 4, 4, -1, 5, 0, 0 };
            EnemyBasics.Enemies[(int)MobileTypes.Ranger - G].ChanceForAttack3 = 33;
            EnemyBasics.Enemies[(int)MobileTypes.Ranger - G].PrimaryAttackAnimFrames3 = new int[] { 4, -1, 5, 0, 0, 1, -1, 2, 3, 4, -1, 5, 0 };

            EnemyBasics.Enemies[(int)MobileTypes.Barbarian - G].MaleTexture = 482;
            EnemyBasics.Enemies[(int)MobileTypes.Barbarian - G].FemaleTexture = 481;
            EnemyBasics.Enemies[(int)MobileTypes.Barbarian - G].PrimaryAttackAnimFrames = new int[] { 0, 1, -1, 2, 3, 4, -1, 5 };
            EnemyBasics.Enemies[(int)MobileTypes.Barbarian - G].ChanceForAttack2 = 50;
            EnemyBasics.Enemies[(int)MobileTypes.Barbarian - G].PrimaryAttackAnimFrames2 = new int[] { 3, 4, -1, 5, 0 };
            EnemyBasics.Enemies[(int)MobileTypes.Barbarian - G].ChanceForAttack3 = 0;

            EnemyBasics.Enemies[(int)MobileTypes.Warrior - G].MaleTexture = 478;
            EnemyBasics.Enemies[(int)MobileTypes.Warrior - G].FemaleTexture = 477;
            EnemyBasics.Enemies[(int)MobileTypes.Warrior - G].PrimaryAttackAnimFrames = new int[] { 0, 1, 2, -1, 3, 4, 5 };
            EnemyBasics.Enemies[(int)MobileTypes.Warrior - G].ChanceForAttack2 = 50;
            EnemyBasics.Enemies[(int)MobileTypes.Warrior - G].PrimaryAttackAnimFrames2 = new int[] { 4, 5, -1, 3, 2, 1, 0 };
            EnemyBasics.Enemies[(int)MobileTypes.Warrior - G].ChanceForAttack3 = 0;

            EnemyBasics.Enemies[(int)MobileTypes.Knight - G].MaleTexture = 478;
            EnemyBasics.Enemies[(int)MobileTypes.Knight - G].FemaleTexture = 477;
            EnemyBasics.Enemies[(int)MobileTypes.Knight - G].PrimaryAttackAnimFrames = new int[] { 0, 1, 2, -1, 3, 4, 5 };
            EnemyBasics.Enemies[(int)MobileTypes.Knight - G].ChanceForAttack2 = 50;
            EnemyBasics.Enemies[(int)MobileTypes.Knight - G].PrimaryAttackAnimFrames2 = new int[] { 4, 5, -1, 3, 2, 1, 0 };
            EnemyBasics.Enemies[(int)MobileTypes.Knight - G].ChanceForAttack3 = 0;
        }

        private static void OnTransitionToInterior_VariantNPCsprites(PlayerEnterExit.TransitionEventArgs args)
        {
            PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
            DFLocation.BuildingData buildingData = playerEnterExit.Interior.BuildingData;
            if (buildingData.BuildingType == DFLocation.BuildingTypes.Tavern || RMBLayout.IsShop(buildingData.BuildingType))
            {
                DaggerfallBillboard[] dfBillboards = playerEnterExit.Interior.GetComponentsInChildren<DaggerfallBillboard>();
                foreach (DaggerfallBillboard billboard in dfBillboards)
                {
                    int record = -1;
                    if (billboard.Summary.Archive == 182 && billboard.Summary.Record == 0)
                    {
                        record = GetRecord_182_0(buildingData.Quality);     // (buildingData.Quality - 1) / 4;
                        Debug.LogFormat("Shop quality {0} using record {1} to replace 182_0", buildingData.Quality, record);
                    }
                    else if (billboard.Summary.Archive == 182 && billboard.Summary.Record == 1)
                    {
                        if (buildingData.Quality < 12)
                        {   // Using big test flats version
                            record = 4;
                        }
                        else if (buildingData.Quality > 14)
                        {
                            record = 5;
                        }
                        Debug.LogFormat("Tavern quality {0} using record {1} to replace 182_1", buildingData.Quality, record);
                    }
                    else if (billboard.Summary.Archive == 182 && billboard.Summary.Record == 2)
                    {
                        if (buildingData.Quality > 12)
                        {
                            record = 6;
                        }
                        Debug.LogFormat("Tavern quality {0} using record {1} to replace 182_2", buildingData.Quality, record);
                    }

                    if (record > -1)
                    {
                        billboard.SetMaterial(197, record);
                        GameObjectHelper.AlignBillboardToGround(billboard.gameObject, billboard.Summary.Size);
                    }
                }
            }
        }

        private static int GetRecord_182_0(byte quality)
        {
            switch (quality)
            {
                case 6:
                case 7:
                case 8:
                case 9:
                    return 0;
                case 10:
                case 11:
                case 12:
                case 13:
                    return 1;
                case 14:
                case 15:
                case 16:
                case 17:
                    return 2;
                case 18:
                case 19:
                case 20:
                    return 3;
                default:
                    return -1;
            }
        }

        private static int GetRecord_182_1(byte quality)
        {
            switch (quality)
            {
                case 6:
                case 7:
                case 8:
                case 9:
                    return 0;
                case 10:
                case 11:
                case 12:
                case 13:
                    return 1;
                case 14:
                case 15:
                case 16:
                case 17:
                    return 2;
                case 18:
                case 19:
                case 20:
                    return 3;
                default:
                    return -1;
            }
        }

    }
}