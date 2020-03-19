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
    public class ItemHauberk : DaggerfallUnityItem
    {
        public ItemHauberk() : base(ItemGroups.Armor, 515)
        {
            PlayerTextureArchive = (GameManager.Instance.PlayerEntity.Gender == Genders.Female) ? ItemBuilder.firstFemaleArchive : ItemBuilder.firstMaleArchive;
        }

        // Always use chainmail record unless leather.
        public override int InventoryTextureRecord
        {
            get {
                if (nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    return 3;
                else
                    return 7;
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
            return EquipSlots.ChestArmor;
        }

        public override int GetMaterialArmorValue()
        {
            return GetChainmailMaterialArmorValue(nativeMaterialValue);
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemHauberk).ToString();
            return data;
        }

        public static int GetChainmailMaterialArmorValue(int material)
        {
            switch (material)
            {
                case (int)ArmorMaterialTypes.Leather:
                    return 2;
                case (int)ArmorMaterialTypes.Iron:
                case (int)ArmorMaterialTypes.Chain:
                case (int)ArmorMaterialTypes.Chain2:
                    return 4;
                case (int)ArmorMaterialTypes.Steel:
                    return 6;
                case (int)ArmorMaterialTypes.Silver:
                    return 7;
                case (int)ArmorMaterialTypes.Elven:
                    return 9;
                case (int)ArmorMaterialTypes.Dwarven:
                    return 10;
                case (int)ArmorMaterialTypes.Adamantium:
                    return 12;
                case (int)ArmorMaterialTypes.Mithril:
                    return 13;
                case (int)ArmorMaterialTypes.Ebony:
                    return 14;
                case (int)ArmorMaterialTypes.Orcish:
                    return 16;
                case (int)ArmorMaterialTypes.Daedric:
                    return 17;
                default:
                    return 0;
            }
        }

    }
}

