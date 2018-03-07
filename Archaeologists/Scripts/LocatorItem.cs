// Project:         Archaeologists Guild for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2018 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;
using UnityEngine;

namespace Archaeologists
{
    public class LocatorItem : DaggerfallUnityItem
    {
        internal const int INACTIVE = 1;
        internal const int ACTIVATED = 2;

        //public static LocatorDevice locatorDevice;

        public LocatorItem() : base(ItemGroups.Jewellery, 7)
        {
            shortName = "Locator device";
            value = 50; //00;
            nativeMaterialValue = INACTIVE;
        }

        public override bool IsStackable()
        {
            return true;
        }

        public override bool UseItem(ItemCollection collection)
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
            {
                LocatorDevice locatorDevice = Object.FindObjectOfType<LocatorDevice>();
                if (locatorDevice != null)
                {
                    if (nativeMaterialValue == INACTIVE)
                    {
                        if (!locatorDevice.enabled)
                        {
                            if (stackCount > 1)
                                stackCount -= 1;
                            else
                                collection.RemoveItem(this);

                            LocatorItem activeLocator = new LocatorItem();
                            activeLocator.nativeMaterialValue = ACTIVATED;
                            collection.AddItem(activeLocator, ItemCollection.AddPosition.DontCare, true);
                            locatorDevice.ActivateDevice();
                        }
                    }
                    else
                    {
                        collection.RemoveItem(this);
                        locatorDevice.DeactivateDevice();
                    }
                }
                else
                    Debug.Log("Can't find locator device object.");
            }
            return true;
        }

        public override bool IsEnchanted
        {
            get { return (nativeMaterialValue == ACTIVATED); }
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(LocatorItem).ToString();
            return data;
        }
    }
}

