// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using UnityEngine;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace TravelOptions
{
    public class TravelOptionsMapWindow : DaggerfallTravelMapWindow
    {
        public TravelOptionsMapWindow(IUserInterfaceManager uiManager)
            : base(uiManager)
        {
        }

        public override void OnPush()
        {
            // Check if there's an active destination
            TravelOptionsMod travelModInstance = TravelOptionsMod.Instance;
            if (!string.IsNullOrEmpty(travelModInstance.DestinationName) && !travelModInstance.GetTravelControlUI().isShowing) //&& travelMod.TravelUi != null 
            {
                Debug.Log("Active destination: " + travelModInstance.DestinationName);

                string resume = string.Format("Resume travel to {0}?", travelModInstance.DestinationName);
                DaggerfallMessageBox resumeMsgBox = new DaggerfallMessageBox(uiManager, DaggerfallMessageBox.CommonMessageBoxButtons.YesNo, resume, uiManager.TopWindow);
                resumeMsgBox.OnButtonClick += (_sender, button) =>
                {
                    CloseWindow();
                    if (button == DaggerfallMessageBox.MessageBoxButtons.Yes)
                    {
                        CloseWindow();
                        travelModInstance.BeginTravel();
                    }
                };
                resumeMsgBox.Show();
            }

            base.OnPush();
        }
    }
}
