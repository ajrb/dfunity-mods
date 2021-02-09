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
    public class ItemHelmet : DaggerfallUnityItem
    {
        public const int templateIndex = 522;

        public ItemHelmet() : base(ItemGroups.Armor, templateIndex)
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
                }
            }
        }

        // Always use same archive for both genders as the same image set is used
        public override int InventoryTextureArchive
        {
            get { return templateIndex; }
        }

        // Use 0-7 for fur and 8-15 for brigandine, 16-19 for normal leather.
        public override int InventoryTextureRecord
        {
            get {
                int offset = PlayerTextureArchive - ItemBuilder.firstFemaleArchive;
                int leather = 16;
                switch (offset)
                {
                    // argonian(male) & human & elf(female) & kajjit(female) use 2 & 6 / 10 & 14
                    case 1:
                    case 2:
                    case 3:
                        offset = 2; leather = 16; break;
                    case 4:
                    case 6:
                        offset = 6; leather = 17; break;
                    // argonian(female) & kajjit(male) & elf(male) use 1 & 5 / 9 & 13
                    case 0:
                        offset = 1; leather = 18; break;
                    case 5:
                    case 7:
                        offset = 5; leather = 19; break;
                }
                if (nativeMaterialValue == (int)ArmorMaterialTypes.Leather && message == 1)
                    return offset;
                else if (nativeMaterialValue >= (int)ArmorMaterialTypes.Iron)
                    return 8 + offset;
                else
                    return leather;
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
            return EquipSlots.Head;
        }

        public override int GetMaterialArmorValue()
        {
            return ItemJerkin.GetLeatherMaterialArmorValue(nativeMaterialValue, message);
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
            data.className = typeof(ItemHelmet).ToString();
            return data;
        }

    }
}

