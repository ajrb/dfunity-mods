// Project:         Loot Realism for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;

namespace LootRealism
{
    public class ItemRightSpaulder : DaggerfallUnityItem
    {
        public const int templateIndex = 518;

        public ItemRightSpaulder() : base(ItemGroups.Armor, templateIndex)
        {
        }

        // Always use chainmail record unless leather.
        public override int InventoryTextureRecord
        {
            get {
                if (nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    return 22;
                else
                    return 26;
            }
        }

        // Gets native material value, modifying it to use the 'chain' value for first byte if plate.
        // This fools the DFU code into treating this item as chainmail for forbidden checks etc.
        public override int NativeMaterialValue
        {
            get { return nativeMaterialValue >= (int)ArmorMaterialTypes.Iron ? nativeMaterialValue - 0x0100 : nativeMaterialValue; }
        }

        public override EquipSlots GetEquipSlot()
        {
            return EquipSlots.RightArm;
        }

        public override int GetMaterialArmorValue()
        {
            return ItemHauberk.GetChainmailMaterialArmorValue(nativeMaterialValue);
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemRightSpaulder).ToString();
            return data;
        }

    }
}

