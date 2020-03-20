// Project:         Loot Realism for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;

namespace LootRealism
{
    public class ItemChausses : DaggerfallUnityItem
    {
        public ItemChausses() : base(ItemGroups.Armor, 516)
        {
            PlayerTextureArchive = (GameManager.Instance.PlayerEntity.Gender == Genders.Female) ? ItemBuilder.firstFemaleArchive : ItemBuilder.firstMaleArchive;
        }

        // Always use chainmail record unless leather.
        public override int InventoryTextureRecord
        {
            get {
                if (nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    return 10;
                else if (nativeMaterialValue >= (int)ArmorMaterialTypes.Chain && nativeMaterialValue < (int)ArmorMaterialTypes.Silver)
                    return 11;
                else
                    return 16;
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
            return EquipSlots.LegsArmor;
        }

        public override int GetMaterialArmorValue()
        {
            return ItemHauberk.GetChainmailMaterialArmorValue(nativeMaterialValue);
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemChausses).ToString();
            return data;
        }

    }
}

