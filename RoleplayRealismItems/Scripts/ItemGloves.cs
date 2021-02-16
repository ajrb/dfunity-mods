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
    public class ItemGloves : DaggerfallUnityItem
    {
        public const int templateIndex = 524;

        public ItemGloves() : base(ItemGroups.Armor, templateIndex)
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
            return EquipSlots.Gloves;
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
            data.className = typeof(ItemGloves).ToString();
            return data;
        }

    }
}

