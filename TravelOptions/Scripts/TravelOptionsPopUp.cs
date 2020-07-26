// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace TravelOptions
{
    public class TravelOptionsPopUp : DaggerfallTravelPopUp
    {
        private const string PlayerControlled = "Player Controlled Travel";
        private string TimeFormat = " {0} hours {1} mins (approx)";

        public TravelOptionsPopUp(IUserInterfaceManager uiManager, IUserInterfaceWindow previousWindow = null, DaggerfallTravelMapWindow travelWindow = null)
            : base(uiManager, previousWindow, travelWindow)
        {
        }

        protected override void Setup()
        {
            base.Setup();

            if (DaggerfallWorkshop.DaggerfallUnity.Settings.SDFFontRendering)
            {
                travelTimeLabel.MaxCharacters = 28;
                tripCostLabel.MaxCharacters = 24;
            }
            else
            {
                TimeFormat = '~' + TimeFormat;
            }
        }

        public bool PlayerControlledTravel()
        {
            return (TravelOptionsMod.Instance.CautiousTravel || !SpeedCautious) && !SleepModeInn && !TravelShip;
        }

        protected override void UpdateLabels()
        {
            if (PlayerControlledTravel())
            {
                TransportManager transportManager = GameManager.Instance.TransportManager;
                bool horse = transportManager.TransportMode == TransportModes.Horse;
                bool cart = transportManager.TransportMode == TransportModes.Cart;
                travelTimeTotalMins = travelTimeCalculator.CalculateTravelTime(EndPos, SpeedCautious && !TravelOptionsMod.Instance.CautiousTravel, SleepModeInn, TravelShip, horse, cart);
                travelTimeTotalMins = GameManager.Instance.GuildManager.FastTravel(travelTimeTotalMins);    // Players can have fast travel benefit from guild memberships
                travelTimeTotalMins /= 2;   // Manually controlled is roughly twice as fast, depending on player speed
                Debug.Log("Travel time: " + travelTimeTotalMins);

                availableGoldLabel.Text = GameManager.Instance.PlayerEntity.GoldPieces.ToString();
                tripCostLabel.Text = PlayerControlled;
                int travelTimeHours = travelTimeTotalMins / 60;
                int travelTimeMinutes = travelTimeTotalMins % 60;
                travelTimeLabel.Text = string.Format(TimeFormat, travelTimeHours, travelTimeMinutes);
            }
            else
            {
                base.UpdateLabels();
            }
        }

        protected override void CallFastTravelGoldCheck()
        {
            if (PlayerControlledTravel())
            {
                CloseWindow();
                TravelWindow.CloseTravelWindows(true);

                TravelOptionsMod.Instance.BeginTravel(TravelWindow.LocationSummary, SpeedCautious);
            }
            else
            {
                base.CallFastTravelGoldCheck();
            }
        }
    }
}
