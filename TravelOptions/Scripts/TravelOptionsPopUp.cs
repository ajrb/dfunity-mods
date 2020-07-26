// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallConnect;

namespace TravelOptions
{
    public class TravelOptionsPopUp : DaggerfallTravelPopUp
    {
        private const string PlayerControlled = "Player Controlled Travel";
        private string TimeFormat = " {0} hours {1} mins (approx)";
        private const string MsgNoShip = "You cannot travel by ship from here, as there's no port.";
        private const string MsgNoSailing = "Your journey doesn't cross any ocean, so a ship is not needed.";

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

        public override void OnPush()
        {
            base.OnPush();

            // Ensure ship travel not selected if restricted
            if (TravelOptionsMod.Instance.ShipTravelPortsOnly)
            {
                if (IsNotAtPort() || HasNoOceanTravel())
                {
                    TravelShip = false;
                    if (IsSetup)
                        Refresh();
                }
            }
        }

        public bool IsPlayerControlledTravel()
        {
            return (TravelOptionsMod.Instance.CautiousTravel || !SpeedCautious) && !SleepModeInn && !TravelShip;
        }

        public bool IsNotAtPort()
        {
            DFLocation location = GameManager.Instance.PlayerGPS.CurrentLocation;
            return location.Loaded == false || location.Exterior.ExteriorData.PortTownAndUnknown == 0;
        }

        public bool HasNoOceanTravel()
        {
            return travelTimeCalculator.OceanPixels == 0;
        }

        protected override void UpdateLabels()
        {
            if (IsPlayerControlledTravel())
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
            if (IsPlayerControlledTravel())
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

        public override void TransportModeButtonOnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            if (TravelOptionsMod.Instance.ShipTravelPortsOnly && sender == shipToggleButton)
            {
                DFLocation location = GameManager.Instance.PlayerGPS.CurrentLocation;
                if (IsNotAtPort())
                {
                    DaggerfallUI.MessageBox(MsgNoShip);
                    return;
                }
                else if (HasNoOceanTravel())
                {
                    DaggerfallUI.MessageBox(MsgNoSailing);
                    return;
                }
            }
            base.TransportModeButtonOnClickHandler(sender, position);
        }

        public override void ToggleTransportModeButtonOnScrollHandler(BaseScreenComponent sender)
        {
            if (TravelOptionsMod.Instance.ShipTravelPortsOnly && TravelShip == false)
            {
                if (IsNotAtPort())
                {
                    DaggerfallUI.MessageBox(MsgNoShip);
                    return;
                }
                else if (HasNoOceanTravel())
                {
                    DaggerfallUI.MessageBox(MsgNoSailing);
                    return;
                }
            }
            base.ToggleTransportModeButtonOnScrollHandler(sender);
        }

        public override void SleepModeButtonOnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            if (TravelOptionsMod.Instance.ShipTravelPortsOnly && sender == campOutToggleButton)
            {
                if (IsNotAtPort() || HasNoOceanTravel())
                    TravelShip = false;
            }

            base.SleepModeButtonOnClickHandler(sender, position);
        }

        public override void ToggleSleepModeButtonOnScrollHandler(BaseScreenComponent sender)
        {
            if (TravelOptionsMod.Instance.ShipTravelPortsOnly && sender == campOutToggleButton)
                TravelShip = false;

            base.ToggleSleepModeButtonOnScrollHandler(sender);
        }

    }
}
