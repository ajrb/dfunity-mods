// Project:         Loot Realism for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;

namespace LootRealism
{
    public class ItemArchersAxe : DaggerfallUnityItem
    {
        public const int templateIndex = 513;

        public ItemArchersAxe() : base(ItemGroups.Weapons, templateIndex)
        {
        }

        // Act like an short sword (number 3 of Weapons enum) for equip times etc.
        public override int GroupIndex
        {
            get { return 3; }
        }

        // Set weapon damage to 2-10.
        public override int GetBaseDamageMin()
        {
            return 2;
        }
        public override int GetBaseDamageMax()
        {
            return 10;
        }

        public override int GetWeaponSkillUsed()
        {
            return (int)DFCareer.ProficiencyFlags.Axes;
        }

        public override ItemHands GetItemHands()
        {
            return ItemHands.Either;
        }

        public override WeaponTypes GetWeaponType()
        {
            return IsEnchanted ? WeaponTypes.Battleaxe_Magic : WeaponTypes.Battleaxe;
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemArchersAxe).ToString();
            return data;
        }
    }
}

