// Project:         RoleplayRealism:Items mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using UnityEngine;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop;

namespace RoleplayRealism
{
    /*
        Leather, Hide, Hardened Leather, Studded Leather, Brigandine

            Leather     = 0x0000,
            Chain       = 0x0100,
            Chain2      = 0x0103,
            Iron        = 0x0200,
            Steel       = 0x0201,
            Silver      = 0x0202,
            Elven       = 0x0203,
            Dwarven     = 0x0204,
            Mithril     = 0x0205,
            Adamantium  = 0x0206,
            Ebony       = 0x0207,
            Orcish      = 0x0208,
            Daedric     = 0x0209,

    */
    public class ItemJerkin : DaggerfallUnityItem
    {
        public const int templateIndex = 520;
        public const string fur = "Fur ";
        public const string brig = "Brigandine ";

        public ItemJerkin() : base(ItemGroups.Armor, templateIndex)
        {
        }

        // Add brig prefix to name for Iron+ materials.
        public override int CurrentVariant
        {
            set {
                base.CurrentVariant = value;
                if (nativeMaterialValue >= (int)ArmorMaterialTypes.Iron)
                    shortName = ItemJerkin.brig + shortName;
                // Switch chain to hide using leather material and message of 1
                if (nativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                {
                    shortName = ItemJerkin.fur + shortName;
                    nativeMaterialValue = (int)ArmorMaterialTypes.Leather;
                    message = 1;
                    weightInKg -= 2f;
                }
            }
        }

        // Always use same archive for both genders as the same image set is used
        public override int InventoryTextureArchive
        {
            get { return templateIndex; }
        }

        // Use 0-7 for fur and 8-15 for brigandine, 16-17 for normal leather.
        public override int InventoryTextureRecord
        {
            get
            {
                int offset = PlayerTextureArchive - ItemBuilder.firstFemaleArchive;
                // Only use 2 & 6 / 10 & 14 human morphology for now..
                offset = (offset < 4) ? 2 : 6;
                if (nativeMaterialValue == (int)ArmorMaterialTypes.Leather && message == 1)
                    return offset;
                else if (nativeMaterialValue >= (int)ArmorMaterialTypes.Iron)
                    return 8 + offset;
                else
                    return (offset < 4) ? 16 : 17;
            }
        }

        // Gets native material value, modifying it to use the 'leather' value for first byte if plate or chain.
        // This fools the DFU code into treating this item as leather for forbidden checks etc.
        public override int NativeMaterialValue
        {
            get { return nativeMaterialValue >= (int)ArmorMaterialTypes.Iron ? nativeMaterialValue - 0x0200 : nativeMaterialValue; }
        }

        public override EquipSlots GetEquipSlot()
        {
            return EquipSlots.ChestArmor;
        }

        public override int GetMaterialArmorValue()
        {
            return GetLeatherMaterialArmorValue(nativeMaterialValue, message);
        }

        public override int GetEnchantmentPower()
        {
            float multiplier = FormulaHelper.GetArmorEnchantmentMultiplier((ArmorMaterialTypes)nativeMaterialValue);
            return enchantmentPoints + Mathf.FloorToInt(enchantmentPoints * multiplier);
        }

        public override SoundClips GetEquipSound()
        {
            return SoundClips.EquipLeather;
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemJerkin).ToString();
            return data;
        }

        public static int GetLeatherMaterialArmorValue(int material, int message)
        {
            switch (material)
            {
                case (int)ArmorMaterialTypes.Leather:   // Leather (0) / Fur (1)
                    return message == 0 ? 3 : 5;
                case (int)ArmorMaterialTypes.Chain:     // Chain (unused)
                case (int)ArmorMaterialTypes.Chain2:
                    return 6;
                case (int)ArmorMaterialTypes.Iron:      // Brigandine
                    return 5;
                case (int)ArmorMaterialTypes.Steel:
                case (int)ArmorMaterialTypes.Silver:
                    return 7;
                case (int)ArmorMaterialTypes.Elven:
                    return 8;
                case (int)ArmorMaterialTypes.Dwarven:
                    return 9;
                case (int)ArmorMaterialTypes.Mithril:
                    return 11;
                case (int)ArmorMaterialTypes.Adamantium:
                    return 11;
                case (int)ArmorMaterialTypes.Ebony:
                    return 12;
                case (int)ArmorMaterialTypes.Orcish:
                    return 13;
                case (int)ArmorMaterialTypes.Daedric:
                    return 14;
                default:
                    return 0;
            }
        }

    }
}

