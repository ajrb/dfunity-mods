// Project:         Archaeologists Guild for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2018 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut
using System;
using DaggerfallWorkshop.Game.Items;
using UnityEngine;

namespace Archaeologists
{
    public class LocatorItem : DaggerfallUnityItem
    {
        public virtual bool IsStackable()
        {
            return true;
        }

        public override bool UseItem(ItemCollection collection)
        {
            Debug.Log("Using locator item!");

            if (stackCount > 1)
                stackCount--;
            else
                collection.RemoveItem(this);
            
            return true;
        }
    }
}

