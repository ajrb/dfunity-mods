// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2024 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;
using System;
using UnityEngine;

namespace RoleplayRealism
{
    public class ItemHorseVariant : DaggerfallUnityItem
    {
        public const int templateIndex = 527;

        public enum HorseVariant
        {
            Bay,
            Appaloosa,
            Arabian,
        }

        public ItemHorseVariant() : base(ItemGroups.Transportation, templateIndex)
        {
            // Randomly set the horse variant
            int numVariants = Enum.GetNames(typeof(HorseVariant)).Length;
            CurrentVariant = UnityEngine.Random.Range(0, numVariants);
        }

        public HorseVariant GetHorseVariant()
        {
            return (HorseVariant) CurrentVariant;
        }

        public override int CurrentVariant
        {
            set
            {
                base.CurrentVariant = value;
                shortName = GetHorseVariant().ToString() + " Horse";
            }
        }

        public override int InventoryTextureArchive
        {
            get { return templateIndex; }
        }

        // Use the variant as the texture record index
        public override int InventoryTextureRecord
        {
            get { return CurrentVariant; }
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemHorseVariant).ToString();
            return data;
        }

    }
}

