// Project:         RoleplayRealism:Items mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using UnityEngine;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;

namespace RoleplayRealism
{
    public class ItemHauberk : DaggerfallUnityItem
    {
        public const int templateIndex = 515;
        public const string mail = "Mail ";

        public ItemHauberk() : base(ItemGroups.Armor, templateIndex)
        {
        }

        // Add mail prefix to name for Iron+ materials.
        public override int CurrentVariant
        {
            set {
                base.CurrentVariant = value;
                if (nativeMaterialValue >= (int)ArmorMaterialTypes.Iron)
                    shortName = ItemHauberk.mail + shortName;
            }
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

        public override int GetEnchantmentPower()
        {
            float multiplier = FormulaHelper.GetArmorEnchantmentMultiplier((ArmorMaterialTypes)nativeMaterialValue);
            return enchantmentPoints + Mathf.FloorToInt(enchantmentPoints * multiplier);
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
                    return 3;
                case (int)ArmorMaterialTypes.Chain:
                case (int)ArmorMaterialTypes.Chain2:
                    return 5;
                case (int)ArmorMaterialTypes.Iron:
                    return 6;
                case (int)ArmorMaterialTypes.Steel:
                case (int)ArmorMaterialTypes.Silver:
                    return 8;
                case (int)ArmorMaterialTypes.Elven:
                    return 9;
                case (int)ArmorMaterialTypes.Dwarven:
                    return 11;
                case (int)ArmorMaterialTypes.Mithril:
                case (int)ArmorMaterialTypes.Adamantium:
                    return 13;
                case (int)ArmorMaterialTypes.Ebony:
                    return 15;
                case (int)ArmorMaterialTypes.Orcish:
                    return 17;
                case (int)ArmorMaterialTypes.Daedric:
                    return 18;
                default:
                    return 0;
            }
        }

    }
}

