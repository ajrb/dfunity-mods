// Project:         RoleplayRealism:Items mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;

namespace RoleplayRealism
{
    public class ItemLightFlail : DaggerfallUnityItem
    {
        public const int templateIndex = 514;

        public ItemLightFlail() : base(ItemGroups.Weapons, templateIndex)
        {
        }

        // Always use same archive for both genders as the same image set is used
        public override int InventoryTextureArchive
        {
            get { return templateIndex; }
        }

        // Act like an saber (number 6 of Weapons enum) for equip times etc.
        public override int GroupIndex
        {
            get { return 6; }
        }

        // Set weapon damage to 3-10.
        public override int GetBaseDamageMin()
        {
            return 3;
        }
        public override int GetBaseDamageMax()
        {
            return 10;
        }

        public override int GetWeaponSkillUsed()
        {
            return (int)DFCareer.ProficiencyFlags.BluntWeapons;
        }

        public override ItemHands GetItemHands()
        {
            return ItemHands.Either;
        }

        public override WeaponTypes GetWeaponType()
        {
            return IsEnchanted ? WeaponTypes.Flail_Magic : WeaponTypes.Flail;
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemLightFlail).ToString();
            return data;
        }
    }
}

