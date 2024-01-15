// Project:         RoleplayRealism:Items mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Authors:         Hazelnut & Ralzar

using System;
using System.Collections;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Save;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Player;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using DaggerfallWorkshop.Game.Guilds;
using System.Collections.Generic;
using DaggerfallConnect.FallExe;

namespace RoleplayRealism
{
    public class RoleplayRealismItemsMod : MonoBehaviour
    {
        const int G = 85;   // Mob Array Gap from 42 .. 128 = 85

        const float SpeedReductionFactor = 3.4f;
        const float RepairCostFactor = 0.6f;
        const float InstantRepairCostFactor = 0.9f;

        static Mod mod;

        static bool newWeapons = false;
        static bool newArmor = false;

        static Dictionary<string, string> textDataBase = null;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<RoleplayRealismItemsMod>();
        }

        void Awake()
        {
            ModSettings settings = mod.GetSettings();
            bool lootRebalance = settings.GetBool("Modules", "lootRebalance");
            bool bandaging = settings.GetBool("Modules", "bandaging");
            bool conditionBasedPrices = settings.GetBool("Modules", "conditionBasedPrices");
            bool storeQualityItems = settings.GetBool("Modules", "storeQualityItemCondition");
            bool enemyEquipment = settings.GetBool("Modules", "realisticEnemyEquipment");
            bool skillStartEquip = settings.GetBool("Modules", "skillBasedStartingEquipment");
            bool skillStartSpells = settings.GetBool("Modules", "skillBasedStartingSpells");
            bool weaponBalance = settings.GetBool("Modules", "weaponBalance");
            newWeapons = settings.GetBool("Modules", "newWeapons");
            newArmor = settings.GetBool("Modules", "newArmor");
            bool alchemistPotions = settings.GetBool("Modules", "alchemistPotions");

            LoadTextData();

            InitMod(lootRebalance, bandaging, conditionBasedPrices, storeQualityItems, enemyEquipment, skillStartEquip, skillStartSpells, weaponBalance, newWeapons, newArmor, alchemistPotions);

            mod.IsReady = true;
        }

        private static void InitMod(bool lootRebalance, bool bandaging, bool conditionBasedPrices, bool storeQualityItems, bool enemyEquipment, bool skillStartEquip, bool skillStartSpells, bool weaponBalance, bool newWeapons, bool newArmor, bool alchemistPotions)
        {
            Debug.Log("Begin mod init: RoleplayRealismItems");

            if (lootRebalance)
            {
                // Iterate over the new mob enemy data array and load into DFU enemies data.
                foreach (int mobDataId in MobLootKeys.Keys)
                {
                    // Log a message indicating the enemy mob being updated and update the loot key.
                    Debug.LogFormat("Updating enemy loot key for {0} to {1}.", TextManager.Instance.GetLocalizedEnemyName(EnemyBasics.Enemies[mobDataId].ID), MobLootKeys[mobDataId]);
                    EnemyBasics.Enemies[mobDataId].LootTableKey = (string) MobLootKeys[mobDataId];
                }
                // Replace the default loot matrix table with custom data.
                LootTables.DefaultLootTables = LootRealismTables;
            }

            if (bandaging)
            {
                DaggerfallUnity.Instance.ItemHelper.RegisterItemUseHandler((int)UselessItems2.Bandage, UseBandage);
                FormulaHelper.RegisterOverride(mod, "IsItemStackable", (Func<DaggerfallUnityItem, bool>)IsItemStackable);
                PlayerActivate.OnLootSpawned += StackableBandages_OnLootSpawned;
            }

            if (conditionBasedPrices)
            {
                EnemyEntity.OnLootSpawned += RandomConditionEnemyItems;     // Enemy random loot (not allocated equipment)
                LootTables.OnLootSpawned += RandomConditionLootItems;       // Container random loot

                FormulaHelper.RegisterOverride(mod, "CalculateCost", (Func<int, int, int, int>)CalculateConditionCost);
                FormulaHelper.RegisterOverride(mod, "CalculateItemRepairCost", (Func<int, int, int, int, IGuild, int>)CalculateItemRepairCost);

                if (storeQualityItems)
                {
                    PlayerActivate.OnLootSpawned += StoreQualityItemCondition;     // Store items
                }
            }

            if (enemyEquipment)
            {
                EnemyEntity.AssignEnemyEquipment = AssignEnemyStartingEquipment;
            }

            StartGameBehaviour startGameBehaviour = GameManager.Instance.StartGameBehaviour;
            if (skillStartEquip)
            {
                startGameBehaviour.AssignStartingEquipment = AssignSkillEquipment;
            }
            if (skillStartSpells)
            {
                startGameBehaviour.AssignStartingSpells = AssignSkillSpellbook;
            }

            if (weaponBalance)
            {
                FormulaHelper.RegisterOverride(mod, "GetMeleeWeaponAnimTime", (Func<PlayerEntity, WeaponTypes, ItemHands, float>)GetMeleeWeaponAnimTime);
                FormulaHelper.RegisterOverride(mod, "CalculateWeaponMinDamage", (Func<Weapons, int>)CalculateWeaponMinDamage);
                FormulaHelper.RegisterOverride(mod, "CalculateWeaponMaxDamage", (Func<Weapons, int>)CalculateWeaponMaxDamage);
            }

            if (newWeapons)
            {
                // Add Archers Axe and Light Flail as custom weapon items.
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemArchersAxe.templateIndex, ItemGroups.Weapons, typeof(ItemArchersAxe));
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemLightFlail.templateIndex, ItemGroups.Weapons, typeof(ItemLightFlail));
            }

            if (newArmor)
            {
                // Add new medium 'chain' armor set as custom armor pieces.
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemHauberk.templateIndex, ItemGroups.Armor, typeof(ItemHauberk));
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemChausses.templateIndex, ItemGroups.Armor, typeof(ItemChausses));
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemLeftSpaulder.templateIndex, ItemGroups.Armor, typeof(ItemLeftSpaulder));
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemRightSpaulder.templateIndex, ItemGroups.Armor, typeof(ItemRightSpaulder));
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemSollerets.templateIndex, ItemGroups.Armor, typeof(ItemSollerets));

                // Add new light 'leather' armor set as custom armor pieces.
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemJerkin.templateIndex, ItemGroups.Armor, typeof(ItemJerkin));
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemCuisse.templateIndex, ItemGroups.Armor, typeof(ItemCuisse));
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemHelmet.templateIndex, ItemGroups.Armor, typeof(ItemHelmet));
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemBoots.templateIndex, ItemGroups.Armor, typeof(ItemBoots));
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemGloves.templateIndex, ItemGroups.Armor, typeof(ItemGloves));
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemLeftVambrace.templateIndex, ItemGroups.Armor, typeof(ItemLeftVambrace));
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemRightVambrace.templateIndex, ItemGroups.Armor, typeof(ItemRightVambrace));
            }

            if (alchemistPotions)
            {
                PlayerActivate.OnLootSpawned += AddPotions_OnLootSpawned;
            }

            Debug.Log("Finished mod init: RoleplayRealismItems");
        }

        public static bool IsItemStackable(DaggerfallUnityItem item)
        {
            return item.IsOfTemplate(ItemGroups.UselessItems2, (int)UselessItems2.Bandage);
        }

        public static void StackableBandages_OnLootSpawned(object sender, ContainerLootSpawnedEventArgs e)
        {
            DaggerfallInterior interior = GameManager.Instance.PlayerEnterExit.Interior;
            if (interior != null && e.ContainerType == LootContainerTypes.ShopShelves)
            {
                DaggerfallUnityItem item = e.Loot.GetItem(ItemGroups.UselessItems2, (int)UselessItems2.Bandage, false, false, false);
                if (item != null)
                {
                    item.stackCount = Mathf.Clamp(UnityEngine.Random.Range(1, interior.BuildingData.Quality / 2), 1, 8);
                }
            }
        }

        static bool UseBandage(DaggerfallUnityItem item, ItemCollection collection)
        {
            if (collection != null)
            {
                PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
                int medical = playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Medical);
                int heal = (int)Mathf.Min(medical / 3, playerEntity.MaxHealth * 0.4f);
                collection.RemoveOne(item);
                playerEntity.IncreaseHealth(heal);
                playerEntity.TallySkill(DFCareer.Skills.Medical, 1);
#if UNITY_EDITOR
                Debug.LogFormat("RRI Applied a Bandage and healed {0} health.", heal);
#endif
            }
            return true;
        }

        public static void AddPotions_OnLootSpawned(object sender, ContainerLootSpawnedEventArgs e)
        {
            DaggerfallInterior interior = GameManager.Instance.PlayerEnterExit.Interior;
            if (interior != null &&
                e.ContainerType == LootContainerTypes.ShopShelves &&
                interior.BuildingData.BuildingType == DFLocation.BuildingTypes.Alchemist)
            {
                int numPotions = Mathf.Clamp(UnityEngine.Random.Range(0, interior.BuildingData.Quality), 1, 12);

                while (numPotions > 0)
                {
                    DaggerfallUnityItem item = ItemBuilder.CreateRandomPotion();
                    item.value *= 2;
                    e.Loot.AddItem(item);
                    numPotions--;
                }
            }
        }

        private static void RandomConditionEnemyItems(object sender, EnemyLootSpawnedEventArgs lootArgs)
        {
            RandomConditionFoundLootItems(lootArgs.Items);
        }

        private static void RandomConditionLootItems(object sender, TabledLootSpawnedEventArgs lootArgs)
        {
            RandomConditionFoundLootItems(lootArgs.Items);
        }

        private static void RandomConditionFoundLootItems(ItemCollection lootItems)
        {
            for (int i = 0; i < lootItems.Count; i++)
            {
                DaggerfallUnityItem item = lootItems.GetItem(i);

                if ((item.ItemGroup == ItemGroups.Armor || item.ItemGroup == ItemGroups.Weapons || item.ItemGroup == ItemGroups.Books) && !item.IsArtifact)
                {
                    // Apply a random condition between 20% and 70%.
                    float conditionMod = UnityEngine.Random.Range(0.2f, 0.75f);
                    item.currentCondition = (int)(item.maxCondition * conditionMod);
                }
            }
        }

        public static int CalculateConditionCost(int baseValue, int shopQuality, int conditionPercentage = -1)
        {
            float conditionMod = (conditionPercentage == -1) ? 1f : Mathf.Max((float)conditionPercentage / 100, 0.2f);

            int cost = (int)(baseValue * conditionMod);

            if (cost < 1)
                cost = 1;

            cost = FormulaHelper.ApplyRegionalPriceAdjustment(cost);
            cost = 2 * (cost * (shopQuality - 10) / 100 + cost);

            return cost;
        }

        public static int CalculateItemRepairCost(int baseItemValue, int shopQuality, int condition, int max, IGuild guild)
        {
            // Don't cost already repaired item
            if (condition == max)
                return 0;

            float repairCostScaleFactor = DaggerfallUnity.Settings.InstantRepairs ? InstantRepairCostFactor : RepairCostFactor;
            float conditionFactor = repairCostScaleFactor * (max - condition) / max;
            int cost = Mathf.Max((int)(baseItemValue * conditionFactor), 1);

            cost = FormulaHelper.CalculateCost(cost, shopQuality);

            if (guild != null)
                cost = guild.ReducedRepairCost(cost);

            return cost;
        }

        public static void StoreQualityItemCondition(object sender, ContainerLootSpawnedEventArgs e)
        {
            DaggerfallInterior interior = GameManager.Instance.PlayerEnterExit.Interior;
            if (interior != null && e.ContainerType == LootContainerTypes.ShopShelves)
            {
                float low = 1f;
                if (interior.BuildingData.Quality <= 3)
                    low = 0.25f;        // 01 - 03, worn+
                else if (interior.BuildingData.Quality <= 7)
                    low = 0.40f;        // 04 - 07, used+
                else if (interior.BuildingData.Quality <= 13)
                    low = 0.60f;        // 08 - 13, slightly used+
                else if (interior.BuildingData.Quality <= 17)
                    low = 0.75f;        // 14 - 17, almost new+
                else
                    return;     // Quality 18+ only ever stock new items.

                Debug.LogFormat("Altering item conditions for store quality: {0}", interior.BuildingData.Quality);
                ItemCollection shelfItems = e.Loot;
                for (int i = 0; i < shelfItems.Count; i++)
                {
                    DaggerfallUnityItem item = shelfItems.GetItem(i);
                    if (item != null && (item.ItemGroup == ItemGroups.Armor || item.ItemGroup == ItemGroups.Weapons) && !item.IsArtifact)
                    {
                        // Apply a random condition between 25% and 100% based on store quality.
                        float conditionMod = UnityEngine.Random.Range(low, 1f);
                        item.currentCondition = (int)(item.maxCondition * conditionMod);
                    }
                }
            }
        }

        public static int CalculateWeaponMinDamage(Weapons weapon)
        {
            switch (weapon)
            {
                case Weapons.Dagger:
                case Weapons.Tanto:
                case Weapons.Wakazashi:
                case Weapons.Saber:
                case Weapons.Katana:
                case Weapons.Dai_Katana:
                    return 1;
                case Weapons.Shortsword:
                case Weapons.Broadsword:
                case Weapons.Longsword:
                case Weapons.Claymore:
                    return 2;
                case Weapons.Battle_Axe:
                case Weapons.War_Axe:
                case Weapons.Staff:
                    return 3;
                case Weapons.Mace:
                case Weapons.Flail:
                case Weapons.Warhammer:
                case Weapons.Short_Bow:
                case Weapons.Long_Bow:
                    return 4;
                default:
                    return 0;
            }
        }

        public static int CalculateWeaponMaxDamage(Weapons weapon)
        {
            switch (weapon)
            {
                case Weapons.Dagger:
                    return 6;
                case Weapons.Tanto:
                    return 7;
                case Weapons.Shortsword:
                case Weapons.Staff:
                    return 8;
                case Weapons.Wakazashi:
                    return 10;
                case Weapons.Mace:
                    return 12;
                case Weapons.Battle_Axe:
                    return 13;
                case Weapons.Broadsword:
                case Weapons.Saber:
                    return 14;
                case Weapons.Longsword:
                    return 15;
                case Weapons.Katana:
                case Weapons.Flail:
                case Weapons.Short_Bow:
                    return 16;
                case Weapons.Dai_Katana:
                case Weapons.War_Axe:
                case Weapons.Warhammer:
                case Weapons.Long_Bow:
                    return 18;
                case Weapons.Claymore:
                    return 19;
                default:
                    return 0;
            }
        }

        private static float GetMeleeWeaponAnimTime(PlayerEntity player, WeaponTypes weaponType, ItemHands weaponHands)
        {
            EquipSlots weaponSlot = GameManager.Instance.WeaponManager.UsingRightHand ? EquipSlots.RightHand : EquipSlots.LeftHand;
            DaggerfallUnityItem weapon = player.ItemEquipTable.GetItem(weaponSlot);
            int adjustedSpeed = 0;
            float weaponWeight = 0f;

            if (weaponType == WeaponTypes.Melee || weapon == null)
            {
                adjustedSpeed = player.Stats.LiveSpeed;
            }
            else
            {
                weaponWeight = weapon.ItemTemplate.baseWeight;
                int strWeightPerc = 150 - player.Stats.LiveStrength;
                float adjustedWeight = strWeightPerc * weaponWeight / 100;
                float speedReductionPerc = adjustedWeight * SpeedReductionFactor;
                int playerSpeed = Mathf.Min(player.Stats.LiveSpeed, 98);    // Cap speed at 98%

                adjustedSpeed = (int)(playerSpeed - (playerSpeed * speedReductionPerc / 90));
            }
            float frameSpeed = 3 * (115 - adjustedSpeed);

#if UNITY_EDITOR
            Debug.LogFormat("anim= {0}ms/frame, speed={1} strength={2} weight={3} adjustedSpeed={4}", frameSpeed / FormulaHelper.classicFrameUpdate, player.Stats.LiveSpeed, player.Stats.LiveStrength, weaponWeight, adjustedSpeed);
#endif
            return frameSpeed / FormulaHelper.classicFrameUpdate;
        }


        static int[] blunt = new int[] { (int)Weapons.Mace, (int)Weapons.Flail, (int)Weapons.Warhammer };
        static int[] bluntWnew = new int[] { (int)Weapons.Mace, (int)Weapons.Flail, (int)Weapons.Warhammer, ItemLightFlail.templateIndex };
        static int[] axe = new int[] { (int)Weapons.Battle_Axe, (int)Weapons.War_Axe };
        static int[] axeWnew = new int[] { (int)Weapons.Battle_Axe, (int)Weapons.War_Axe, ItemArchersAxe.templateIndex };

        static bool CoinFlip()
        {
            return UnityEngine.Random.Range(0, 2) == 0;
        }
        static int OneOf(int[] array)
        {
            int i = UnityEngine.Random.Range(0, array.Length);
            return array[i];
        }

        static Armor RandomShield()
        {
            return (Armor)UnityEngine.Random.Range((int)Armor.Buckler, (int)Armor.Round_Shield + 1);
        }
        static int RandomLongblade()
        {
            return UnityEngine.Random.Range((int)Weapons.Broadsword, (int)Weapons.Longsword + 1);
        }
        static int RandomBlunt()
        {
            return OneOf(newWeapons ? bluntWnew : blunt);
        }
        static int RandomAxe()
        {
            return OneOf(newWeapons ? axeWnew : axe);
        }
        private static int RandomAxeOrBlade()
        {
            return CoinFlip() ? RandomAxe() : RandomLongblade();
        }
        private static int RandomBluntOrBlade()
        {
            return CoinFlip() ? RandomBlunt() : RandomLongblade();
        }

        static int RandomBigWeapon()
        {
            Weapons weapon = (Weapons)UnityEngine.Random.Range((int)Weapons.Claymore, (int)Weapons.War_Axe + 1);
            if (weapon == Weapons.Dai_Katana && Dice100.SuccessRoll(90))
                weapon = Weapons.Claymore;  // Dai-katana's are very rare.
            return (int)weapon;
        }

        static Weapons RandomBow()
        {
            return (Weapons)UnityEngine.Random.Range((int)Weapons.Short_Bow, (int)Weapons.Long_Bow + 1);
        }
        static int RandomShortblade()
        {
            if (Dice100.SuccessRoll(40))
                return (int)Weapons.Shortsword;
            else
                return UnityEngine.Random.Range((int)Weapons.Dagger, (int)Weapons.Wakazashi + 1);
        }
        static int SecondaryWeapon()
        {
            switch (UnityEngine.Random.Range(0, 4))
            {
                case 0:
                    return (int)Weapons.Dagger;
                case 1:
                    return (int)Weapons.Shortsword;
                case 2:
                    return newWeapons ? ItemArchersAxe.templateIndex : (int)Weapons.Short_Bow;
                default:
                    return (int)Weapons.Short_Bow;
            }
        }
        static int GetCombatClassWeapon(MobileTypes enemyType)
        {
            switch (enemyType)
            {
                case MobileTypes.Barbarian:
                    return RandomBigWeapon();
                case MobileTypes.Knight:
                    return CoinFlip() ? RandomBlunt() : RandomLongblade();
                case MobileTypes.Knight_CityWatch:
                    return RandomAxeOrBlade();
                case MobileTypes.Monk:
                    return RandomBlunt();
                default:
                    return RandomLongblade();
            }
        }

        static void AddOrEquipWornItem(DaggerfallEntity entity, DaggerfallUnityItem item, bool equip = false)
        {
            entity.Items.AddItem(item);
            if (item.ItemGroup == ItemGroups.Armor || item.ItemGroup == ItemGroups.Weapons ||
                item.ItemGroup == ItemGroups.MensClothing || item.ItemGroup == ItemGroups.WomensClothing)
            {
                item.currentCondition = (int)(UnityEngine.Random.Range(0.3f, 0.75f) * item.maxCondition);
            }
            if (equip)
                entity.ItemEquipTable.EquipItem(item, true, false);
        }

        static bool IsFighter(MobileTypes mob)
        {
            return mob == MobileTypes.Knight || mob == MobileTypes.Warrior;
        }

        static bool IsAgileFighter(MobileTypes mob)
        {
            return mob == MobileTypes.Monk || mob == MobileTypes.Rogue;
        }

        static void AssignEnemyStartingEquipment(PlayerEntity playerEntity, EnemyEntity enemyEntity, int variant)
        {
            // Use default code for non-class enemies.
            if (enemyEntity.EntityType != EntityTypes.EnemyClass)
            {
                DaggerfallUnity.Instance.ItemHelper.AssignEnemyStartingEquipment(playerEntity, enemyEntity, variant);
                ConvertOrcish(enemyEntity);
                return;
            }

            // Set item level, city watch never have items above iron or steel
            int itemLevel = (enemyEntity.MobileEnemy.ID == (int)MobileTypes.Knight_CityWatch) ? 1 : enemyEntity.Level;
            Genders playerGender = playerEntity.Gender;
            Races playerRace = playerEntity.Race;
            int chance = 50;
            int armored = 100;
            bool prefChain = false;
            bool prefLeather = false;

            // Held weapon(s) and shield/secondary:
            switch ((MobileTypes)enemyEntity.MobileEnemy.ID)
            {
                // Ranged specialists:
                case MobileTypes.Archer:
                case MobileTypes.Ranger:
                    AddOrEquipWornItem(enemyEntity, ItemBuilder.CreateWeapon(RandomBow(), FormulaHelper.RandomMaterial(itemLevel)), true);
                    AddOrEquipWornItem(enemyEntity, CreateWeapon((enemyEntity.MobileEnemy.ID == (int)MobileTypes.Ranger) ? RandomLongblade() : RandomShortblade(), FormulaHelper.RandomMaterial(itemLevel)));
                    DaggerfallUnityItem arrowPile = ItemBuilder.CreateWeapon(Weapons.Arrow, WeaponMaterialTypes.Iron);
                    arrowPile.stackCount = UnityEngine.Random.Range(4, 17);
                    enemyEntity.Items.AddItem(arrowPile);
                    armored = 60;
                    prefChain = true;
                    break;

                // Combat classes:
                case MobileTypes.Barbarian:
                case MobileTypes.Knight:
                case MobileTypes.Knight_CityWatch:
                case MobileTypes.Monk:
                case MobileTypes.Spellsword:
                case MobileTypes.Warrior:
                case MobileTypes.Rogue:
                    if (variant == 0)
                    {
                        AddOrEquipWornItem(enemyEntity, CreateWeapon(GetCombatClassWeapon((MobileTypes)enemyEntity.MobileEnemy.ID), FormulaHelper.RandomMaterial(itemLevel)), true);
                        // Left hand shield?
                        if (Dice100.SuccessRoll(chance))
                            AddOrEquipWornItem(enemyEntity, ItemBuilder.CreateArmor(playerGender, playerRace, RandomShield(), FormulaHelper.RandomArmorMaterial(itemLevel)), true);
                        // left-hand weapon?
                        else if (Dice100.SuccessRoll(chance))
                            AddOrEquipWornItem(enemyEntity, CreateWeapon(SecondaryWeapon(), FormulaHelper.RandomMaterial(itemLevel)));
                        if (!IsFighter((MobileTypes)enemyEntity.MobileEnemy.ID))
                            armored = 80;
                    }
                    else
                    {
                        AddOrEquipWornItem(enemyEntity, CreateWeapon(RandomBigWeapon(), FormulaHelper.RandomMaterial(itemLevel)), true);
                        if (!IsFighter((MobileTypes)enemyEntity.MobileEnemy.ID)) {
                            prefChain = true;
                            armored = 90;
                        }
                    }
                    if (enemyEntity.MobileEnemy.ID == (int)MobileTypes.Barbarian)
                    {   // Barbies tend to forgo armor or use leather
                        armored = 30;
                        prefLeather = true;
                    }
                    break;

                // Mage classes:
                case MobileTypes.Mage:
                case MobileTypes.Sorcerer:
                case MobileTypes.Healer:
                    AddOrEquipWornItem(enemyEntity, ItemBuilder.CreateWeapon(Weapons.Staff, FormulaHelper.RandomMaterial(itemLevel)), true);
                    if (Dice100.SuccessRoll(chance))
                        AddOrEquipWornItem(enemyEntity, CreateWeapon(RandomShortblade(), FormulaHelper.RandomMaterial(itemLevel)));
                    AddOrEquipWornItem(enemyEntity, (playerGender == Genders.Male) ? ItemBuilder.CreateMensClothing(MensClothing.Plain_robes, playerEntity.Race) : ItemBuilder.CreateWomensClothing(WomensClothing.Plain_robes, playerEntity.Race), true);
                    armored = 35;
                    prefLeather = true;
                    break;

                // Stealthy stabby classes:
                case MobileTypes.Assassin:
                    AddOrEquipWornItem(enemyEntity, CreateWeapon(RandomAxeOrBlade(), FormulaHelper.RandomMaterial(itemLevel)), true);
                    AddOrEquipWornItem(enemyEntity, CreateWeapon(RandomAxeOrBlade(), FormulaHelper.RandomMaterial(itemLevel)));
                    armored = 65;
                    prefChain = true;
                    break;
                case MobileTypes.Battlemage:
                    AddOrEquipWornItem(enemyEntity, CreateWeapon(RandomBluntOrBlade(), FormulaHelper.RandomMaterial(itemLevel)), true);
                    AddOrEquipWornItem(enemyEntity, CreateWeapon(RandomBluntOrBlade(), FormulaHelper.RandomMaterial(itemLevel)));
                    armored = 75;
                    prefChain = true;
                    break;

                // Sneaky classes:
                case MobileTypes.Acrobat:
                case MobileTypes.Bard:
                case MobileTypes.Burglar:
                case MobileTypes.Nightblade:
                case MobileTypes.Thief:
                    AddOrEquipWornItem(enemyEntity, CreateWeapon(RandomShortblade(), FormulaHelper.RandomMaterial(itemLevel)), true);
                    if (Dice100.SuccessRoll(chance))
                        AddOrEquipWornItem(enemyEntity, CreateWeapon(SecondaryWeapon(), FormulaHelper.RandomMaterial(itemLevel/2)));
                    armored = 50;
                    prefLeather = true;
                    if (enemyEntity.MobileEnemy.ID == (int)MobileTypes.Nightblade)
                        prefChain = true;
                    break;
            }

            // Torso
            if (Dice100.SuccessRoll(armored))
                AddOrEquipWornItem(enemyEntity, CreateArmor(playerGender, playerRace, GetArmorTemplateIndex(EquipSlots.ChestArmor, prefChain, prefLeather), FormulaHelper.RandomArmorMaterial(itemLevel)), true);
            armored -= 10;
            // Legs (Barbarians have a raised chance)
            if (Dice100.SuccessRoll(armored) || (enemyEntity.MobileEnemy.ID == (int)MobileTypes.Barbarian && Dice100.SuccessRoll(armored + 50)))
                AddOrEquipWornItem(enemyEntity, CreateArmor(playerGender, playerRace, GetArmorTemplateIndex(EquipSlots.LegsArmor, prefChain, prefLeather), FormulaHelper.RandomArmorMaterial(itemLevel)), true);
            armored -= 10;
            // Feet
            if (Dice100.SuccessRoll(armored))
                AddOrEquipWornItem(enemyEntity, CreateArmor(playerGender, playerRace, GetArmorTemplateIndex(EquipSlots.Feet, prefChain, prefLeather), FormulaHelper.RandomArmorMaterial(itemLevel)), true);
            armored -= 10;
            // Head (Barbarians have a raised chance)
            if (Dice100.SuccessRoll(armored) || (enemyEntity.MobileEnemy.ID == (int)MobileTypes.Barbarian && Dice100.SuccessRoll(armored + 50)))
                AddOrEquipWornItem(enemyEntity, CreateArmor(playerGender, playerRace, GetArmorTemplateIndex(EquipSlots.Head, prefChain, prefLeather), FormulaHelper.RandomArmorMaterial(itemLevel)), true);
            armored -= 20;

            if (armored > 0)
            {
                // right arm
                if (Dice100.SuccessRoll(armored))
                    AddOrEquipWornItem(enemyEntity, CreateArmor(playerGender, playerRace, GetArmorTemplateIndex(EquipSlots.RightArm, prefChain, prefLeather), FormulaHelper.RandomArmorMaterial(itemLevel)), true);
                // left arm
                if (Dice100.SuccessRoll(armored))
                    AddOrEquipWornItem(enemyEntity, CreateArmor(playerGender, playerRace, GetArmorTemplateIndex(EquipSlots.LeftArm, prefChain, prefLeather), FormulaHelper.RandomArmorMaterial(itemLevel)), true);
                // hands
                if (Dice100.SuccessRoll(armored))
                    AddOrEquipWornItem(enemyEntity, CreateArmor(playerGender, playerRace, GetArmorTemplateIndex(EquipSlots.Gloves, prefChain, prefLeather), FormulaHelper.RandomArmorMaterial(itemLevel)), true);
            }

            // Chance for poisoned weapon
            if (playerEntity.Level > 1)
            {
                DaggerfallUnityItem mainWeapon = enemyEntity.ItemEquipTable.GetItem(EquipSlots.RightHand);
                if (mainWeapon != null)
                {
                    int chanceToPoison = 5;
                    if (enemyEntity.MobileEnemy.ID == (int)MobileTypes.Assassin)
                        chanceToPoison = 60;

                    if (Dice100.SuccessRoll(chanceToPoison))
                    {
                        // Apply poison
                        mainWeapon.poisonType = (Poisons)UnityEngine.Random.Range(128, 135 + 1);
                    }
                }
            }
        }

        private static void ConvertOrcish(EnemyEntity enemyEntity)
        {
            // Orcs have any higher materials converted to Orcish 80% of the time.
            if (enemyEntity.MobileEnemy.Team == MobileTeams.Orcs && Dice100.SuccessRoll(80))
            {
                int convertFrom = (int)WeaponMaterialTypes.Ebony;
                if (enemyEntity.MobileEnemy.ID == (int)MobileTypes.OrcWarlord)
                    convertFrom = (int)WeaponMaterialTypes.Mithril;
                List<DaggerfallUnityItem> items = enemyEntity.Items.SearchItems(ItemGroups.Weapons);
                items.AddRange(enemyEntity.Items.SearchItems(ItemGroups.Armor));
                foreach (DaggerfallUnityItem item in items)
                {
                    int material = item.nativeMaterialValue & 0xFF;
                    if (material >= convertFrom)
                    {
                        Debug.LogFormat("Converted {0} to Orcish for a {1}", (WeaponMaterialTypes)material, item.shortName);
                        ItemTemplate template = item.ItemTemplate;
                        item.weightInKg = template.baseWeight;
                        item.value = template.basePrice;
                        item.currentCondition = template.hitPoints;
                        item.maxCondition = template.hitPoints;
                        item.enchantmentPoints = template.enchantmentPoints;

                        if (item.ItemGroup == ItemGroups.Armor)
                        {
                            ItemBuilder.ApplyArmorMaterial(item, ArmorMaterialTypes.Orcish);
                        }
                        else
                        {
                            if (GameManager.Instance.PlayerEntity.Gender == Genders.Female)
                                item.PlayerTextureArchive += 1;
                            ItemBuilder.ApplyWeaponMaterial(item, WeaponMaterialTypes.Orcish);
                        }
                    }
                }
            }
        }

        static DaggerfallUnityItem CreateWeapon(int templateIndex, WeaponMaterialTypes material)
        {
            DaggerfallUnityItem weapon = ItemBuilder.CreateItem(ItemGroups.Weapons, templateIndex);
            ItemBuilder.ApplyWeaponMaterial(weapon, material);
            return weapon;
        }

        static DaggerfallUnityItem CreateArmor(Genders gender, Races race, int templateIndex, ArmorMaterialTypes material)
        {
            DaggerfallUnityItem armor = ItemBuilder.CreateItem(ItemGroups.Armor, templateIndex);
            ItemBuilder.ApplyArmorSettings(armor, gender, race, material);
            return armor;
        }

        static int GetArmorTemplateIndex(EquipSlots type, bool prefChain = false, bool prefLeather = false)
        {
            bool rand = (prefChain || prefLeather) && newArmor; // Only random armor set if enabled and class preference.
            switch (type)
            {
                case EquipSlots.ChestArmor:
                    return rand ? (CoinFlip() ? (int)Armor.Cuirass : (prefChain ? ItemHauberk.templateIndex : ItemJerkin.templateIndex)) : (int)Armor.Cuirass;
                case EquipSlots.LegsArmor:
                    return rand ? (CoinFlip() ? (int)Armor.Greaves : (prefChain ? ItemChausses.templateIndex : ItemCuisse.templateIndex)) : (int)Armor.Greaves;
                case EquipSlots.LeftArm:
                    return rand ? (CoinFlip() ? (int)Armor.Left_Pauldron : (prefChain ? ItemLeftSpaulder.templateIndex : ItemLeftVambrace.templateIndex)) : (int)Armor.Left_Pauldron;
                case EquipSlots.RightArm:
                    return rand ? (CoinFlip() ? (int)Armor.Right_Pauldron : (prefChain ? ItemRightSpaulder.templateIndex : ItemRightVambrace.templateIndex)) : (int)Armor.Right_Pauldron;
                case EquipSlots.Feet:
                    return rand ? (CoinFlip() ? (int)Armor.Boots : (prefChain ? ItemSollerets.templateIndex : ItemBoots.templateIndex)) : (int)Armor.Boots;
                case EquipSlots.Gloves:
                    return rand ? (CoinFlip() ? (int)Armor.Gauntlets : ItemGloves.templateIndex) : (int)Armor.Gauntlets;
                case EquipSlots.Head:
                    return rand ? (CoinFlip() ? (int)Armor.Helm : ItemHelmet.templateIndex) : (int)Armor.Helm;
                default:
                    return -1;
            }
        }

        static void AssignSkillEquipment(PlayerEntity playerEntity, CharacterDocument characterDocument)
        {
            Debug.Log("Starting Equipment: Assigning Based on Skills");

            // Set condition of ebony dagger if player has one from char creation questions
            IList daggers = playerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Dagger);
            foreach (DaggerfallUnityItem dagger in daggers)
                if (dagger.NativeMaterialValue > (int)WeaponMaterialTypes.Steel)
                    dagger.currentCondition = (int)(dagger.maxCondition * 0.2);

            // Skill based items
            AssignSkillItems(playerEntity, playerEntity.Career.PrimarySkill1);
            AssignSkillItems(playerEntity, playerEntity.Career.PrimarySkill2);
            AssignSkillItems(playerEntity, playerEntity.Career.PrimarySkill3);

            AssignSkillItems(playerEntity, playerEntity.Career.MajorSkill1);
            AssignSkillItems(playerEntity, playerEntity.Career.MajorSkill2);
            AssignSkillItems(playerEntity, playerEntity.Career.MajorSkill3);

            // Starting clothes are gender-specific, randomise shirt dye and pants variant
            DaggerfallUnityItem shortShirt = null;
            DaggerfallUnityItem casualPants = null;
            if (playerEntity.Gender == Genders.Female)
            {
                shortShirt = ItemBuilder.CreateWomensClothing(WomensClothing.Short_shirt_closed, playerEntity.Race, 0, ItemBuilder.RandomClothingDye());
                casualPants = ItemBuilder.CreateWomensClothing(WomensClothing.Casual_pants, playerEntity.Race);
            }
            else
            {
                shortShirt = ItemBuilder.CreateMensClothing(MensClothing.Short_shirt, playerEntity.Race, 0, ItemBuilder.RandomClothingDye());
                casualPants = ItemBuilder.CreateMensClothing(MensClothing.Casual_pants, playerEntity.Race);
            }
            ItemBuilder.RandomizeClothingVariant(casualPants);
            AddOrEquipWornItem(playerEntity, shortShirt, true);
            AddOrEquipWornItem(playerEntity, casualPants, true);

            // Add spellbook, all players start with one - also a little gold and a crappy iron dagger for those with no weapon skills.
            playerEntity.Items.AddItem(ItemBuilder.CreateItem(ItemGroups.MiscItems, (int)MiscItems.Spellbook));
            playerEntity.GoldPieces += UnityEngine.Random.Range(5, playerEntity.Career.Luck);
            playerEntity.Items.AddItem(ItemBuilder.CreateWeapon(Weapons.Dagger, WeaponMaterialTypes.Iron));

            // Add some torches and candles if player torch is from items setting enabled
            if (DaggerfallUnity.Settings.PlayerTorchFromItems)
            {
                for (int i = 0; i < 6; i++)
                    playerEntity.Items.AddItem(ItemBuilder.CreateItem(ItemGroups.UselessItems2, (int)UselessItems2.Torch));
                for (int i = 0; i < 4; i++)
                    playerEntity.Items.AddItem(ItemBuilder.CreateItem(ItemGroups.UselessItems2, (int)UselessItems2.Candle));
            }

            Debug.Log("Starting Equipment: Assigning Finished");
        }

        static void AssignSkillItems(PlayerEntity playerEntity, DFCareer.Skills skill)
        {
            ItemCollection items = playerEntity.Items;
            Genders gender = playerEntity.Gender;
            Races race = playerEntity.Race;

            bool upgrade = Dice100.SuccessRoll(playerEntity.Career.Luck / (playerEntity.Career.Luck < 56 ? 2 : 1));
            WeaponMaterialTypes weaponMaterial = WeaponMaterialTypes.Iron;
            if ((upgrade && !playerEntity.Career.IsMaterialForbidden(DFCareer.MaterialFlags.Steel)) || playerEntity.Career.IsMaterialForbidden(DFCareer.MaterialFlags.Iron))
            {
                weaponMaterial = WeaponMaterialTypes.Steel;
            }
            ArmorMaterialTypes armorMaterial = ArmorMaterialTypes.Leather;
            if ((upgrade && !playerEntity.Career.IsArmorForbidden(DFCareer.ArmorFlags.Chain)) || playerEntity.Career.IsArmorForbidden(DFCareer.ArmorFlags.Leather))
            {
                armorMaterial = ArmorMaterialTypes.Chain;
            }

            switch (skill)
            {
                case DFCareer.Skills.Archery:
                    AddOrEquipWornItem(playerEntity, ItemBuilder.CreateWeapon(Weapons.Short_Bow, weaponMaterial));
                    DaggerfallUnityItem arrowPile = ItemBuilder.CreateWeapon(Weapons.Arrow, WeaponMaterialTypes.Iron);
                    arrowPile.stackCount = 30;
                    items.AddItem(arrowPile);
                    return;
                case DFCareer.Skills.Axe:
                    AddOrEquipWornItem(playerEntity, CreateWeapon(RandomAxe(), weaponMaterial)); return;
                case DFCareer.Skills.Backstabbing:
                    AddOrEquipWornItem(playerEntity, ItemBuilder.CreateArmor(gender, race, Armor.Right_Pauldron, armorMaterial)); return;
                case DFCareer.Skills.BluntWeapon:
                    AddOrEquipWornItem(playerEntity, CreateWeapon(RandomBlunt(), weaponMaterial)); return;
                case DFCareer.Skills.Climbing:
                    AddOrEquipWornItem(playerEntity, ItemBuilder.CreateArmor(gender, race, Armor.Helm, armorMaterial, -1)); return;
                case DFCareer.Skills.CriticalStrike:
                    AddOrEquipWornItem(playerEntity, ItemBuilder.CreateArmor(gender, race, Armor.Greaves, armorMaterial)); return;
                case DFCareer.Skills.Dodging:
                    AddOrEquipWornItem(playerEntity, (gender == Genders.Male) ? ItemBuilder.CreateMensClothing(MensClothing.Casual_cloak, race) : ItemBuilder.CreateWomensClothing(WomensClothing.Casual_cloak, race)); return;
                case DFCareer.Skills.Etiquette:
                    AddOrEquipWornItem(playerEntity, (gender == Genders.Male) ? ItemBuilder.CreateMensClothing(MensClothing.Formal_tunic, race) : ItemBuilder.CreateWomensClothing(WomensClothing.Evening_gown, race)); return;
                case DFCareer.Skills.HandToHand:
                    AddOrEquipWornItem(playerEntity, ItemBuilder.CreateArmor(gender, race, Armor.Gauntlets, armorMaterial)); return;
                case DFCareer.Skills.Jumping:
                    AddOrEquipWornItem(playerEntity, ItemBuilder.CreateArmor(gender, race, Armor.Boots, armorMaterial)); return;
                case DFCareer.Skills.Lockpicking:
                    items.AddItem(ItemBuilder.CreateRandomPotion()); return;
                case DFCareer.Skills.LongBlade:
                    AddOrEquipWornItem(playerEntity, ItemBuilder.CreateWeapon(Dice100.SuccessRoll(50) ? Weapons.Saber : Weapons.Broadsword, weaponMaterial)); return;
                case DFCareer.Skills.Medical:
                    DaggerfallUnityItem bandages = ItemBuilder.CreateItem(ItemGroups.UselessItems2, (int)UselessItems2.Bandage);
                    bandages.stackCount = 4;
                    items.AddItem(bandages);
                    return;
                case DFCareer.Skills.Mercantile:
                    playerEntity.GoldPieces += UnityEngine.Random.Range(playerEntity.Career.Luck, playerEntity.Career.Luck * 4); return;
                case DFCareer.Skills.Pickpocket:
                    items.AddItem(ItemBuilder.CreateRandomGem()); return;
                case DFCareer.Skills.Running:
                    AddOrEquipWornItem(playerEntity, (gender == Genders.Male) ? ItemBuilder.CreateMensClothing(MensClothing.Shoes, race) : ItemBuilder.CreateWomensClothing(WomensClothing.Shoes, race)); return;
                case DFCareer.Skills.ShortBlade:
                    AddOrEquipWornItem(playerEntity, ItemBuilder.CreateWeapon(Dice100.SuccessRoll(50) ? Weapons.Shortsword : Weapons.Tanto, weaponMaterial)); return;
                case DFCareer.Skills.Stealth:
                    AddOrEquipWornItem(playerEntity, (gender == Genders.Male) ? ItemBuilder.CreateMensClothing(MensClothing.Khajiit_suit, race) : ItemBuilder.CreateWomensClothing(WomensClothing.Khajiit_suit, race)); return;
                case DFCareer.Skills.Streetwise:
                    AddOrEquipWornItem(playerEntity, ItemBuilder.CreateArmor(gender, race, Armor.Cuirass, armorMaterial)); return;
                case DFCareer.Skills.Swimming:
                    items.AddItem((gender == Genders.Male) ? ItemBuilder.CreateMensClothing(MensClothing.Loincloth, race) : ItemBuilder.CreateWomensClothing(WomensClothing.Loincloth, race)); return;

                case DFCareer.Skills.Daedric:
                case DFCareer.Skills.Dragonish:
                case DFCareer.Skills.Giantish:
                case DFCareer.Skills.Harpy:
                case DFCareer.Skills.Impish:
                case DFCareer.Skills.Orcish:
                    items.AddItem(ItemBuilder.CreateRandomBook());
                    for (int i = 0; i < 4; i++)
                        items.AddItem(ItemBuilder.CreateRandomIngredient(ItemGroups.CreatureIngredients1));
                    return;
                case DFCareer.Skills.Centaurian:
                case DFCareer.Skills.Nymph:
                case DFCareer.Skills.Spriggan:
                    items.AddItem(ItemBuilder.CreateRandomBook());
                    for (int i = 0; i < 4; i++)
                        items.AddItem(ItemBuilder.CreateRandomIngredient(ItemGroups.PlantIngredients1));
                    return;
            }
        }

        static void AssignSkillSpellbook(PlayerEntity playerEntity, CharacterDocument characterDocument)
        {
            Debug.Log("Starting Spells: Assigning Based on Skills");

            // Skill based items
            AssignSkillSpells(playerEntity, playerEntity.Career.PrimarySkill1, true);
            AssignSkillSpells(playerEntity, playerEntity.Career.PrimarySkill2, true);
            AssignSkillSpells(playerEntity, playerEntity.Career.PrimarySkill3, true);

            AssignSkillSpells(playerEntity, playerEntity.Career.MajorSkill1);
            AssignSkillSpells(playerEntity, playerEntity.Career.MajorSkill2);
            AssignSkillSpells(playerEntity, playerEntity.Career.MajorSkill3);

            Debug.Log("Starting Spells: Assigning Finished");
        }

        static void AssignSkillSpells(PlayerEntity playerEntity, DFCareer.Skills skill, bool primary = false)
        {
            ItemCollection items = playerEntity.Items;
            Genders gender = playerEntity.Gender;
            Races race = playerEntity.Race;

            switch (skill)
            {
                // Classic spell indexes are on https://en.uesp.net/wiki/Daggerfall:SPELLS.STD_indices under Spell Catalogue
                case DFCareer.Skills.Alteration:
                    playerEntity.AddSpell(gentleFallSpell);         // Gentle Fall
                    if (primary)
                        playerEntity.AddSpell(GetClassicSpell(38)); // Jumping
                    return;
                case DFCareer.Skills.Destruction:
                    playerEntity.AddSpell(arcaneArrowSpell);        // Arcane Arrow
                    if (primary)
                        playerEntity.AddSpell(minorShockSpell);     // Minor shock
                    return;
                case DFCareer.Skills.Illusion:
                    playerEntity.AddSpell(candleSpell);             // Candle
                    if (primary)
                        playerEntity.AddSpell(GetClassicSpell(44)); // Chameleon
                    return;
                case DFCareer.Skills.Mysticism:
                    playerEntity.AddSpell(knockSpell);              // Knock
                    playerEntity.AddSpell(knickKnackSpell);         // Knick-Knack
                    if (primary) {
                        playerEntity.AddSpell(GetClassicSpell(1));  // Fenrik's Door Jam
                        playerEntity.AddSpell(GetClassicSpell(94)); // Recall!
                    }
                    return;
                case DFCareer.Skills.Restoration:
                    playerEntity.AddSpell(salveBruiseSpell);         // Salve Bruise
                    if (primary) {
                        playerEntity.AddSpell(smellingSaltsSpell);  // Smelling Salts
                        playerEntity.AddSpell(GetClassicSpell(97)); // Balyna's Balm
                    }
                    return;
                case DFCareer.Skills.Thaumaturgy:
                    playerEntity.AddSpell(GetClassicSpell(2));      // Buoyancy
                    if (primary)
                        playerEntity.AddSpell(riseSpell);           // Rise
                    return;
            }
        }

        static EffectBundleSettings GetClassicSpell(int spellId)
        {
            SpellRecord.SpellRecordData spellData;
            GameManager.Instance.EntityEffectBroker.GetClassicSpellRecord(spellId, out spellData);
            EffectBundleSettings bundle;
            GameManager.Instance.EntityEffectBroker.ClassicSpellRecordDataToEffectBundleSettings(spellData, BundleTypes.Spell, out bundle);
            return bundle;
        }

        // New spell definitions:
        static EffectBundleSettings minorShockSpell => new EffectBundleSettings()
        {
            Name = Localize("minorShock"),
            Version = EntityEffectBroker.CurrentSpellVersion,
            BundleType = BundleTypes.Spell,
            TargetType = TargetTypes.ByTouch,
            ElementType = ElementTypes.Shock,
            Icon = new SpellIcon() { index = 37 },
            Effects = new EffectEntry[]
            {   // Uses damage health effect class which needs magnitude defined in settings.
                new EffectEntry(DamageHealth.EffectKey, new EffectSettings() {
                        // 2-10 + 1-2 per 1 lev
                        MagnitudeBaseMin = 2, MagnitudeBaseMax = 10,
                        MagnitudePlusMin = 1, MagnitudePlusMax = 2, MagnitudePerLevel = 1
                    })
            },
        };
        static EffectBundleSettings arcaneArrowSpell => new EffectBundleSettings()
        {
            Name = Localize("arcaneArrow"),
            Version = EntityEffectBroker.CurrentSpellVersion,
            BundleType = BundleTypes.Spell,
            TargetType = TargetTypes.SingleTargetAtRange,
            ElementType = ElementTypes.Magic,
            Icon = new SpellIcon() { index = 57 },
            Effects = new EffectEntry[]
            {
                new EffectEntry(DamageHealth.EffectKey, new EffectSettings() {
                    MagnitudeBaseMin = 5, MagnitudeBaseMax = 6,
                    MagnitudePlusMin = 1, MagnitudePlusMax = 1, MagnitudePerLevel = 2
                })
            },
        };
        static EffectBundleSettings gentleFallSpell => new EffectBundleSettings()
        {
            Name = Localize("gentleFall"),
            Version = EntityEffectBroker.CurrentSpellVersion,
            BundleType = BundleTypes.Spell,
            TargetType = TargetTypes.CasterOnly,
            ElementType = ElementTypes.Magic,
            Icon = new SpellIcon() { index = 31 },
            Effects = new EffectEntry[]
            {
                new EffectEntry(Slowfall.EffectKey, new EffectSettings() {
                        DurationBase = 2, DurationPlus = 1, DurationPerLevel = 2
                    })
            },
        };
        static EffectBundleSettings candleSpell => new EffectBundleSettings()
        {
            Name = Localize("candle"),
            Version = EntityEffectBroker.CurrentSpellVersion,
            BundleType = BundleTypes.Spell,
            TargetType = TargetTypes.CasterOnly,
            ElementType = ElementTypes.Magic,
            Icon = new SpellIcon() { index = 22 },
            Effects = new EffectEntry[]
            {
                new EffectEntry(LightNormal.EffectKey, new EffectSettings() {
                    DurationBase = 4, DurationPlus = 4, DurationPerLevel = 1
                })
            },
        };
        static EffectBundleSettings knickKnackSpell => new EffectBundleSettings()
        {
            Name = Localize("knickKnack"),
            Version = EntityEffectBroker.CurrentSpellVersion,
            BundleType = BundleTypes.Spell,
            TargetType = TargetTypes.CasterOnly,
            ElementType = ElementTypes.Magic,
            Icon = new SpellIcon() { index = 26 },
            Effects = new EffectEntry[]
            {
                new EffectEntry(CreateItem.EffectKey, new EffectSettings() {
                    DurationBase = 4, DurationPlus = 1, DurationPerLevel = 2
                })
            },
        };
        static EffectBundleSettings salveBruiseSpell => new EffectBundleSettings()
        {
            Name = Localize("salveBruise"),
            Version = EntityEffectBroker.CurrentSpellVersion,
            BundleType = BundleTypes.Spell,
            TargetType = TargetTypes.CasterOnly,
            ElementType = ElementTypes.Magic,
            Icon = new SpellIcon() { index = 13 },
            Effects = new EffectEntry[]
            {
                new EffectEntry(HealHealth.EffectKey, new EffectSettings() {
                    MagnitudeBaseMin = 3, MagnitudeBaseMax = 6, MagnitudePerLevel = 1
                })
             },
        };
        static EffectBundleSettings smellingSaltsSpell => new EffectBundleSettings()
        {
            Name = Localize("smellingSalts"),
            Version = EntityEffectBroker.CurrentSpellVersion,
            BundleType = BundleTypes.Spell,
            TargetType = TargetTypes.CasterOnly,
            ElementType = ElementTypes.Magic,
            Icon = new SpellIcon() { index = 10 },
            Effects = new EffectEntry[]
            {
                new EffectEntry(HealFatigue.EffectKey, new EffectSettings() {
                    MagnitudeBaseMin = 3, MagnitudeBaseMax = 6, MagnitudePerLevel = 1
                })
             },
        };
        static EffectBundleSettings riseSpell => new EffectBundleSettings()
        {
            Name = Localize("rise"),
            Version = EntityEffectBroker.CurrentSpellVersion,
            BundleType = BundleTypes.Spell,
            TargetType = TargetTypes.CasterOnly,
            ElementType = ElementTypes.Magic,
            Icon = new SpellIcon() { index = 13 },
            Effects = new EffectEntry[]
            {
                new EffectEntry(Levitate.EffectKey, new EffectSettings() {
                    DurationBase = 1, DurationPlus = 1, DurationPerLevel = 2
                })
            },
        };
        static EffectBundleSettings knockSpell => new EffectBundleSettings()
        {
            Name = Localize("knock"),
            Version = EntityEffectBroker.CurrentSpellVersion,
            BundleType = BundleTypes.Spell,
            TargetType = TargetTypes.CasterOnly,
            ElementType = ElementTypes.Magic,
            Icon = new SpellIcon() { index = 3 },
            Effects = new EffectEntry[]
            {
                new EffectEntry(Open.EffectKey, new EffectSettings() {
                    DurationBase = 1, DurationPlus = 1, ChanceBase = 8, ChancePerLevel = 2
                })
            },
        };

        static IDictionary MobLootKeys = new Hashtable()
        {
            {(int) MobileTypes.Giant, "E"},      
            {(int) MobileTypes.Nymph, "B"},      
            {(int) MobileTypes.Mummy, "J"},      
            {(int) MobileTypes.OrcShaman, "LR2"},
            {(int) MobileTypes.Lich, "J"},      
            {(int) MobileTypes.FireDaedra, "S"}, 
            {(int) MobileTypes.Daedroth, "S"},   
            {(int) MobileTypes.DaedraSeducer, "S"},
            {(int) MobileTypes.Mage-G, "LR3"},
            {(int) MobileTypes.Battlemage-G, "LR3"},
            {(int) MobileTypes.Sorcerer-G, "LR3"},
            {(int) MobileTypes.Healer-G, "LR2"},
            {(int) MobileTypes.Nightblade-G, "LR3"},
            {(int) MobileTypes.Monk-G, "O"},
            {(int) MobileTypes.Barbarian-G, "LR1"}, 
            {(int) MobileTypes.Warrior-G, "LR1"},
        };

        static readonly LootChanceMatrix[] LootRealismTables = {
            new LootChanceMatrix() {key = "-",   MinGold = 0,   MaxGold = 0,    P1 = 0, P2 = 0, C1 = 0, C2 = 0, C3 = 0, M1 = 0, AM = 0,  WP = 0,  MI = 0, CL = 0, BK = 0, M2 = 0, RL = 0 }, //None
            new LootChanceMatrix() {key = "A",   MinGold = 0,   MaxGold = 5,    P1 = 0, P2 = 0, C1 = 1, C2 = 1, C3 = 1, M1 = 0, AM = 1,  WP = 5,  MI = 1, CL = 5, BK = 0, M2 = 1, RL = 0 }, //Orcs
            new LootChanceMatrix() {key = "B",   MinGold = 0,   MaxGold = 0,    P1 = 5, P2 = 5, C1 = 0, C2 = 0, C3 = 0, M1 = 0, AM = 0,  WP = 0,  MI = 0, CL = 0, BK = 0, M2 = 0, RL = 0 }, //Nature
            new LootChanceMatrix() {key = "C",   MinGold = 0,   MaxGold = 10,   P1 = 5, P2 = 5, C1 = 3, C2 = 3, C3 = 3, M1 = 3, AM = 10, WP = 20, MI = 1, CL = 50,BK = 2, M2 = 2, RL = 1 }, //Rangers
            new LootChanceMatrix() {key = "D",   MinGold = 0,   MaxGold = 0,    P1 = 2, P2 = 2, C1 = 2, C2 = 2, C3 = 2, M1 = 2, AM = 0,  WP = 0,  MI = 0, CL = 0, BK = 0, M2 = 0, RL = 0 }, //Harpy
            new LootChanceMatrix() {key = "E",   MinGold = 0,   MaxGold = 10,   P1 = 2, P2 = 2, C1 = 5, C2 = 5, C3 = 5, M1 = 2, AM = 0,  WP = 5,  MI = 0, CL = 0, BK = 0, M2 = 1, RL = 2 }, //Giant
            new LootChanceMatrix() {key = "F",   MinGold = 2,   MaxGold = 15,   P1 = 2, P2 = 2, C1 = 5, C2 = 5, C3 = 5, M1 = 2, AM = 80, WP = 70, MI = 2, CL = 0, BK = 0, M2 = 3, RL = 10 },//Giant Loot
            new LootChanceMatrix() {key = "G",   MinGold = 0,   MaxGold = 8,    P1 = 0, P2 = 0, C1 = 0, C2 = 0, C3 = 0, M1 = 0, AM = 10, WP = 1,  MI = 1, CL = 10,BK = 0, M2 = 3, RL = 5 }, //Undead naked
            new LootChanceMatrix() {key = "H",   MinGold = 0,   MaxGold = 5,    P1 = 0, P2 = 0, C1 = 0, C2 = 0, C3 = 0, M1 = 0, AM = 5,  WP = 30, MI = 1, CL = 2, BK = 1, M2 = 0, RL = 5 }, //Undead armed
            new LootChanceMatrix() {key = "I",   MinGold = 0,   MaxGold = 0,    P1 = 0, P2 = 0, C1 = 0, C2 = 0, C3 = 0, M1 = 0, AM = 0,  WP = 0,  MI = 1, CL = 0, BK = 0, M2 = 0, RL = 5 }, //Undead spirits
            new LootChanceMatrix() {key = "J",   MinGold = 10,  MaxGold = 40,   P1 = 3, P2 = 3, C1 = 3, C2 = 3, C3 = 3, M1 = 5, AM = 1,  WP = 1,  MI = 5, CL = 5, BK = 5, M2 = 2, RL = 10 },//Undead bosses
            new LootChanceMatrix() {key = "K",   MinGold = 10,  MaxGold = 20,   P1 = 3, P2 = 3, C1 = 3, C2 = 3, C3 = 3, M1 = 3, AM = 70, WP = 50, MI = 3, CL = 0, BK = 2, M2 = 2, RL = 80 },//Undead Loot
            new LootChanceMatrix() {key = "L",   MinGold = 0,   MaxGold = 10,   P1 = 0, P2 = 0, C1 = 3, C2 = 3, C3 = 3, M1 = 3, AM = 70, WP = 50, MI = 2, CL = 20,BK = 0, M2 = 3, RL = 3 }, //Nest Loot
            new LootChanceMatrix() {key = "M",   MinGold = 0,   MaxGold = 7,    P1 = 1, P2 = 1, C1 = 1, C2 = 1, C3 = 1, M1 = 2, AM = 80, WP = 40, MI = 2, CL = 15,BK = 2, M2 = 3, RL = 1 }, //Cave Loot
            new LootChanceMatrix() {key = "N",   MinGold = 1,   MaxGold = 40,   P1 = 5, P2 = 5, C1 = 5, C2 = 5, C3 = 5, M1 = 5, AM = 90, WP = 60, MI = 2, CL = 20,BK = 5, M2 = 2, RL = 5 }, //Castle Loot
            new LootChanceMatrix() {key = "O",   MinGold = 0,   MaxGold = 30,   P1 = 1, P2 = 1, C1 = 1, C2 = 1, C3 = 1, M1 = 1, AM = 5,  WP = 10, MI = 1, CL = 60,BK = 0, M2 = 0, RL = 0 }, //Rogues
            new LootChanceMatrix() {key = "P",   MinGold = 2,   MaxGold = 10,   P1 = 5, P2 = 2, C1 = 2, C2 = 2, C3 = 2, M1 = 2, AM = 10, WP = 10, MI = 2, CL = 50,BK = 9, M2 = 2, RL = 0 }, //Spellsword
            new LootChanceMatrix() {key = "Q",   MinGold = 10,  MaxGold = 40,   P1 = 2, P2 = 2, C1 = 4, C2 = 4, C3 = 4, M1 = 2, AM = 0,  WP = 0,  MI = 2, CL = 70,BK = 5, M2 = 3, RL = 5 }, //Vampires
            new LootChanceMatrix() {key = "R",   MinGold = 2,   MaxGold = 10,   P1 = 0, P2 = 0, C1 = 3, C2 = 3, C3 = 3, M1 = 5, AM = 0,  WP = 0,  MI = 1, CL = 0, BK = 0, M2 = 2, RL = 0 }, //Water monsters
            new LootChanceMatrix() {key = "S",   MinGold = 30,  MaxGold = 70,   P1 = 5, P2 = 5, C1 = 5, C2 = 5, C3 = 5, M1 = 15,AM = 0,  WP = 0,  MI = 8, CL = 5, BK = 5, M2 = 2, RL = 10 },//Dragon Loot, Daedras
            new LootChanceMatrix() {key = "T",   MinGold = 10,  MaxGold = 50,   P1 = 0, P2 = 0, C1 = 0, C2 = 0, C3 = 0, M1 = 0, AM = 60, WP = 40, MI = 0, CL = 50,BK = 10,M2 = 0, RL = 10}, //Knight Orc Warlord
            new LootChanceMatrix() {key = "U",   MinGold = 0,   MaxGold = 40,   P1 = 5, P2 = 5, C1 = 5, C2 = 5, C3 = 5, M1 = 10,AM = 20, WP = 20, MI = 4, CL = 20,BK = 90,M2 = 5, RL = 70 },//Laboratory Loot
            new LootChanceMatrix() {key = "LR1", MinGold = 0,   MaxGold = 20,   P1 = 0, P2 = 0, C1 = 0, C2 = 0, C3 = 0, M1 = 0, AM = 40, WP = 50, MI = 1, CL = 50,BK = 0, M2 = 0, RL = 5 }, //Warrior, Barbarian
            new LootChanceMatrix() {key = "LR2", MinGold = 0,   MaxGold = 5,    P1 = 3, P2 = 3, C1 = 1, C2 = 1, C3 = 3, M1 = 3, AM = 0,  WP = 20, MI = 1, CL = 60,BK = 10,M2 = 3, RL = 0 }, //Healer, Orc Shaman
            new LootChanceMatrix() {key = "LR3", MinGold = 0,   MaxGold = 30,   P1 = 3, P2 = 3, C1 = 1, C2 = 1, C3 = 1, M1 = 2, AM = 0,  WP = 20, MI = 1, CL = 95,BK = 45,M2 = 2, RL = 10 },//Spellcasters
        };

        static void LoadTextData()
        {
            const string csvFilename = "RoleplayRealismItemsModData.csv";

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
