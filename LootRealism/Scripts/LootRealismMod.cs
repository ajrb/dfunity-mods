// Project:         LootRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2019
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Authors:         Hazelnut & Ralzar

using System;
using System.Collections;
using UnityEngine;
using DaggerfallConnect;
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

namespace LootRealism
{
    public class LootRealismMod : MonoBehaviour
    {
        const int G = 85;   // Mob Array Gap from 42 .. 128 = 85
        static Mod mod;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<LootRealismMod>();
        }

        void Awake()
        {
            ModSettings settings = mod.GetSettings();
            bool lootRebalance = settings.GetBool("Modules", "lootRebalance");
            bool bandaging = settings.GetBool("Modules", "bandaging");
            bool skillStartEquip = settings.GetBool("Modules", "skillBasedStartingEquipment");
            bool skillStartSpells = settings.GetBool("Modules", "skillBasedStartingSpells");

            InitMod(lootRebalance, bandaging, skillStartEquip, skillStartSpells);

            mod.IsReady = true;
        }

        private static void InitMod(bool lootRebalance, bool bandaging, bool skillStartEquip, bool skillStartSpells)
        {
            Debug.Log("Begin mod init: LootRealism");

            if (lootRebalance)
            {
                // Iterate over the new mob enemy data array and load into DFU enemies data.
                foreach (int mobDataId in MobLootKeys.Keys)
                {
                    // Log a message indicating the enemy mob being updated and update the loot key.
                    Debug.LogFormat("Updating enemy loot key for {0} to {1}.", EnemyBasics.Enemies[mobDataId].Name, MobLootKeys[mobDataId]);
                    EnemyBasics.Enemies[mobDataId].LootTableKey = (string) MobLootKeys[mobDataId];
                }
                // Replace the default loot matrix table with custom data.
                LootTables.DefaultLootTables = LootRealismTables;
            }

            if (bandaging)
            {
                if (DaggerfallUnity.Instance.ItemHelper.RegisterItemUseHander((int)UselessItems2.Bandage, UseBandage))
                    FormulaHelper.RegisterOverride(mod, "IsItemStackable", (Func<DaggerfallUnityItem, bool>)IsItemStackable);
                else
                    Debug.LogWarning("LootRealism: Unable to register bandage use handler.");
            }

            StartGameBehaviour startGameBehaviour = GameManager.Instance.StartGameBehaviour;
            if (skillStartEquip)
            {
                startGameBehaviour.AllocateStartingEquipment = AssignStartingEquipment;
            }

            Debug.Log("Finished mod init: LootRealism");
        }

        public static bool IsItemStackable(DaggerfallUnityItem item)
        {
            return item.IsOfTemplate(ItemGroups.UselessItems2, (int)UselessItems2.Bandage);
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
                Debug.LogFormat("Applied a Bandage and healed {0} health.", heal);
            }
            return true;
        }

        static void AssignStartingEquipment(PlayerEntity playerEntity, CharacterDocument characterDocument)
        {
            Debug.Log("Starting Equipment: Assigning Based on Skills");

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
            playerEntity.Items.AddItem(shortShirt);
            playerEntity.Items.AddItem(casualPants);

            // Add spellbook, all players start with one - also a little gold
            playerEntity.Items.AddItem(ItemBuilder.CreateItem(ItemGroups.MiscItems, (int)MiscItems.Spellbook));
            playerEntity.Items.AddItem(ItemBuilder.CreateGoldPieces(UnityEngine.Random.Range(10, 20)));

            // Add some torches and candles if player torch is from items setting enabled
            if (DaggerfallUnity.Settings.PlayerTorchFromItems)
            {
                for (int i = 0; i < 2; i++)
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

            switch (skill)
            {
                case DFCareer.Skills.Archery:
                    items.AddItem(ItemBuilder.CreateWeapon(Weapons.Short_Bow, WeaponMaterialTypes.Iron));
                    DaggerfallUnityItem arrowPile = ItemBuilder.CreateWeapon(Weapons.Arrow, WeaponMaterialTypes.Iron);
                    arrowPile.stackCount = 30;
                    items.AddItem(arrowPile);
                    return;
                case DFCareer.Skills.Axe:
                    items.AddItem(ItemBuilder.CreateWeapon(Weapons.Battle_Axe, WeaponMaterialTypes.Iron)); return;
                case DFCareer.Skills.Backstabbing:
                    items.AddItem(ItemBuilder.CreateArmor(gender, race, Armor.Right_Pauldron, ArmorMaterialTypes.Leather)); return;
                case DFCareer.Skills.BluntWeapon:
                    items.AddItem(ItemBuilder.CreateWeapon(Dice100.SuccessRoll(50) ? Weapons.Mace : Weapons.Flail, WeaponMaterialTypes.Iron)); return;
                case DFCareer.Skills.Climbing:
                    items.AddItem(ItemBuilder.CreateArmor(gender, race, Armor.Helm, ArmorMaterialTypes.Leather, -1)); return;
                case DFCareer.Skills.CriticalStrike:
                    items.AddItem(ItemBuilder.CreateArmor(gender, race, Armor.Right_Pauldron, ArmorMaterialTypes.Leather)); return;
                case DFCareer.Skills.Dodging:
                    items.AddItem((gender == Genders.Male) ? ItemBuilder.CreateMensClothing(MensClothing.Casual_cloak, race) : ItemBuilder.CreateWomensClothing(WomensClothing.Casual_cloak, race)); return;
                case DFCareer.Skills.Etiquette:
                    items.AddItem((gender == Genders.Male) ? ItemBuilder.CreateMensClothing(MensClothing.Formal_tunic, race) : ItemBuilder.CreateWomensClothing(WomensClothing.Evening_gown, race)); return;
                case DFCareer.Skills.HandToHand:
                    items.AddItem(ItemBuilder.CreateArmor(gender, race, Armor.Gauntlets, ArmorMaterialTypes.Leather)); return;
                case DFCareer.Skills.Jumping:
                    items.AddItem(ItemBuilder.CreateArmor(gender, race, Armor.Boots, ArmorMaterialTypes.Leather)); return;
                case DFCareer.Skills.Lockpicking:
                    items.AddItem(ItemBuilder.CreateRandomPotion()); return;
                case DFCareer.Skills.LongBlade:
                    items.AddItem(ItemBuilder.CreateWeapon(Dice100.SuccessRoll(50) ? Weapons.Saber : Weapons.Broadsword, WeaponMaterialTypes.Iron)); return;
                case DFCareer.Skills.Medical:
                    DaggerfallUnityItem bandages = ItemBuilder.CreateItem(ItemGroups.UselessItems2, (int)UselessItems2.Bandage);
                    bandages.stackCount = 4;
                    items.AddItem(bandages);
                    return;
                case DFCareer.Skills.Mercantile:
                    items.AddItem(ItemBuilder.CreateGoldPieces(UnityEngine.Random.Range(50, 250))); return;
                case DFCareer.Skills.Pickpocket:
                    items.AddItem(ItemBuilder.CreateRandomGem()); return;
                case DFCareer.Skills.Running:
                    items.AddItem((gender == Genders.Male) ? ItemBuilder.CreateMensClothing(MensClothing.Shoes, race) : ItemBuilder.CreateWomensClothing(WomensClothing.Shoes, race)); return;
                case DFCareer.Skills.ShortBlade:
                    items.AddItem(ItemBuilder.CreateWeapon(Weapons.Dagger, WeaponMaterialTypes.Iron)); return;
                case DFCareer.Skills.Stealth:
                    items.AddItem((gender == Genders.Male) ? ItemBuilder.CreateMensClothing(MensClothing.Khajiit_suit, race) : ItemBuilder.CreateWomensClothing(WomensClothing.Khajiit_suit, race)); return;
                case DFCareer.Skills.Streetwise:
                    items.AddItem(ItemBuilder.CreateArmor(gender, race, Armor.Greaves, ArmorMaterialTypes.Leather)); return;
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

        static IDictionary MobLootKeys = new Hashtable()
        {
            {9, "F"},       // Giant
            {10, "B"},      // Nymph
            {19, "J"},      // Mummy
            {21, "LR2"},    // Orc Shaman
            {32, "J"},      // Lich
            {33, "S"},      // Ancient Lich
            {26, "S"},      // Fire Daedra
            {27, "S"},      // Daedroth
            {29, "S"},      // Daedra Seducer
            {128-G, "LR3"}, // Mage
            {130-G, "LR3"}, // Battlemage
            {131-G, "LR3"}, // Sorcerer
            {132-G, "LR2"}, // Healer
            {133-G, "LR3"}, // Nightblade
            {140-G, "O"},   // Monk
            {143-G, "LR1"}, // Barbarian
            {144-G, "LR1"}, // Warrior
        };

        static readonly LootChanceMatrix[] LootRealismTables = {
            new LootChanceMatrix() {key = "-",   MinGold = 0,   MaxGold = 0,    P1 = 0, P2 = 0, C1 = 0, C2 = 0, C3 = 0, M1 = 0, AM = 0,  WP = 0,  MI = 0, CL = 0, BK = 0, M2 = 0, RL = 0 }, //None
            new LootChanceMatrix() {key = "A",   MinGold = 0,   MaxGold = 5,    P1 = 0, P2 = 0, C1 = 1, C2 = 1, C3 = 1, M1 = 0, AM = 1,  WP = 5,  MI = 1, CL = 5, BK = 0, M2 = 1, RL = 0 }, //Orcs
            new LootChanceMatrix() {key = "B",   MinGold = 0,   MaxGold = 0,    P1 = 5, P2 = 5, C1 = 0, C2 = 0, C3 = 0, M1 = 0, AM = 0,  WP = 0,  MI = 0, CL = 0, BK = 0, M2 = 0, RL = 0 }, //Nature
            new LootChanceMatrix() {key = "C",   MinGold = 0,   MaxGold = 10,   P1 = 5, P2 = 5, C1 = 3, C2 = 3, C3 = 3, M1 = 3, AM = 10, WP = 40, MI = 1, CL = 50,BK = 2, M2 = 2, RL = 1 }, //Rangers
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
            new LootChanceMatrix() {key = "T",   MinGold = 10,  MaxGold = 50,   P1 = 0, P2 = 0, C1 = 0, C2 = 0, C3 = 0, M1 = 0, AM = 80, WP = 40, MI = 0, CL = 50,BK = 10,M2 = 0, RL = 10}, //Knight Orc Warlord
            new LootChanceMatrix() {key = "U",   MinGold = 0,   MaxGold = 40,   P1 = 5, P2 = 5, C1 = 5, C2 = 5, C3 = 5, M1 = 10,AM = 20, WP = 20, MI = 4, CL = 20,BK = 90,M2 = 5, RL = 70 },//Laboratory Loot
            new LootChanceMatrix() {key = "LR1", MinGold = 0,   MaxGold = 20,   P1 = 0, P2 = 0, C1 = 1, C2 = 1, C3 = 1, M1 = 0, AM = 60, WP = 50, MI = 1, CL = 50,BK = 0, M2 = 1, RL = 5 }, //Warrior, Barbarian
            new LootChanceMatrix() {key = "LR2", MinGold = 0,   MaxGold = 5,    P1 = 3, P2 = 3, C1 = 1, C2 = 1, C3 = 1, M1 = 1, AM = 0,  WP = 20, MI = 1, CL = 30,BK = 10,M2 = 1, RL = 0 }, //Healer, Orc Shaman
            new LootChanceMatrix() {key = "LR3", MinGold = 0,   MaxGold = 30,   P1 = 3, P2 = 3, C1 = 1, C2 = 1, C3 = 1, M1 = 1, AM = 0,  WP = 40, MI = 1, CL = 80,BK = 70,M2 = 2, RL = 10 },//Spellcasters
        };
    }
}
