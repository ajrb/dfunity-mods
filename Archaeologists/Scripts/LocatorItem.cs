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

        internal const int BASEVALUE = 600;             // Base value of a locator device. Actual cost will depend on guild rank.
        internal const int ACTIVATION_EXPLORATION = 25; // Percentage of dungeon that must be explored before device will activate.

        internal const string NAME = "Locator device";

        internal const string DEACTIVATION_MSG =
            "The locator devices falls quiet before turning to dust in your hand.";
        internal const string FAIL_ACTIVATE_MSG =
            "Locator devices can only be activated in dungeon labyrinths.";
        internal static string[] EXPLORATION_NEEDED_MSG = {
            "Locator devices can only be activated once you have explored a",
            "sufficient amount of the dungeon. This is to enable the magic",
            " in the device to become attuned to this particular dungeon." };
        internal static string[] ACTIVATION_MSG = {
            " The locator device vibrates and hums into action.", "",
            "You now see a bright disk in your mind when looking",
            "     in the direction of your desired target." };


        public LocatorItem() : this(BASEVALUE)
        {
        }

        public LocatorItem(int baseValue) : base(ItemGroups.Jewellery, 7)
        {
            shortName = NAME;
            value = baseValue;
            nativeMaterialValue = INACTIVE;
        }

        public override bool IsStackable()
        {
            return true;
        }

        public override bool UseItem(ItemCollection collection)
        {
            if (!GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
            {
                DaggerfallUI.MessageBox(FAIL_ACTIVATE_MSG);
            }
            else
            {
                int exploredPercent = Automap.instance.ExploredPercentage();
                Debug.Log("Explored: " + exploredPercent);
                LocatorDevice locatorDevice = Object.FindObjectOfType<LocatorDevice>();
                if (locatorDevice != null)
                {
                    if (nativeMaterialValue == INACTIVE)
                    {
                        if (exploredPercent < ACTIVATION_EXPLORATION)
                        {
                            DaggerfallUI.MessageBox(EXPLORATION_NEEDED_MSG);
                        }
                        else if (!locatorDevice.enabled)
                        {
                            if (stackCount > 1)
                                stackCount -= 1;
                            else
                                collection.RemoveItem(this);

                            LocatorItem activeLocator = new LocatorItem();
                            activeLocator.nativeMaterialValue = ACTIVATED;
                            collection.AddItem(activeLocator, ItemCollection.AddPosition.DontCare, true);
                            locatorDevice.ActivateDevice();
                            DaggerfallUI.MessageBox(ACTIVATION_MSG);
                        }
                    }
                    else
                    {
                        collection.RemoveItem(this);
                        locatorDevice.DeactivateDevice();
                        DaggerfallUI.MessageBox(DEACTIVATION_MSG);
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

