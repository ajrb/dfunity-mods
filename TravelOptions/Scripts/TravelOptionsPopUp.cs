// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallConnect;
using DaggerfallConnect.Utility;

namespace TravelOptions
{
    public class TravelOptionsPopUp : DaggerfallTravelPopUp
    {
        private const string MsgPlayerControlled = "Player Controlled Journey";
        private string MsgTimeFormat = " {0} hours {1} mins (approx)";
        private const string MsgNoPort = "You cannot travel by ship from here, since there's no port.";
        private const string MsgNoDestPort = "You cannot travel by ship to there, as that location has no port.";
        private const string MsgNoSailing = "Your journey doesn't cross any ocean, so a ship is not needed.";

        protected TravelOptionsMapWindow travelWindowTO;

        public new DFPosition EndPos { get { return base.EndPos; } protected internal set { base.EndPos = value; } }

        public TravelOptionsPopUp(IUserInterfaceManager uiManager, IUserInterfaceWindow previousWindow = null, DaggerfallTravelMapWindow travelWindow = null)
            : base(uiManager, previousWindow, travelWindow)
        {
            travelWindowTO = (TravelOptionsMapWindow)travelWindow;
        }

        protected override void Setup()
        {
            base.Setup();

            if (DaggerfallWorkshop.DaggerfallUnity.Settings.SDFFontRendering)
            {
                travelTimeLabel.MaxCharacters = 28;
                tripCostLabel.MaxCharacters = 25;
            }
            else
            {
                MsgTimeFormat = '~' + MsgTimeFormat;
            }

            Refresh();
        }

        public override void OnPush()
        {
            base.OnPush();

            // Ensure ship travel not selected if restricted
            if (TravelOptionsMod.Instance.ShipTravelPortsOnly)
            {
                if (IsNotAtPort() || HasNoOceanTravel() || IsDestNotValidPort())
                {
                    TravelShip = false;
                    if (IsSetup)
                        Refresh();
                }
            }
        }

        public bool IsPlayerControlledTravel()
        {
            return (TravelOptionsMod.Instance.CautiousTravel || !SpeedCautious) && (TravelOptionsMod.Instance.StopAtInnsTravel || !SleepModeInn) && !TravelShip;
        }

        public bool IsNotAtPort()
        {
            DFLocation location = GameManager.Instance.PlayerGPS.CurrentLocation;
            return location.Loaded == false || (location.Exterior.ExteriorData.PortTownAndUnknown == 0 && !TravelOptionsMapWindow.HasPortExtra(location.MapTableData));
        }

        public bool HasNoOceanTravel()
        {
            return travelTimeCalculator.OceanPixels == 0 && !GameManager.Instance.TransportManager.IsOnShip();
        }

        public bool IsDestNotValidPort()
        {
            return TravelOptionsMod.Instance.ShipTravelDestinationPortsOnly && !TravelOptionsMapWindow.HasPort(TravelWindow.LocationSummary);
        }

        protected override void UpdateLabels()
        {
            if (IsPlayerControlledTravel() || !travelWindowTO.LocationSelected)
            {
                TransportManager transportManager = GameManager.Instance.TransportManager;
                bool horse = transportManager.TransportMode == TransportModes.Horse;
                bool cart = transportManager.TransportMode == TransportModes.Cart;
                travelTimeTotalMins = travelTimeCalculator.CalculateTravelTime(EndPos,
                    SpeedCautious && !TravelOptionsMod.Instance.CautiousTravel,
                    SleepModeInn && !TravelOptionsMod.Instance.StopAtInnsTravel,
                    TravelShip, horse, cart);
                travelTimeTotalMins = GameManager.Instance.GuildManager.FastTravel(travelTimeTotalMins);    // Players can have fast travel benefit from guild memberships

                // Manually controlled is roughly twice as fast, depending on player speed. So divide by 2 times the relevant speed multiplier
                float travelTimeMinsMult = ((SpeedCautious && TravelOptionsMod.Instance.CautiousTravel) ? TravelOptionsMod.Instance.CautiousTravelMultiplier : TravelOptionsMod.Instance.RecklessTravelMultiplier) * 2;
                travelTimeTotalMins = (int)(travelTimeTotalMins / travelTimeMinsMult);
#if UNITY_EDITOR
                Debug.Log("Travel time: " + travelTimeTotalMins);
#endif
                availableGoldLabel.Text = GameManager.Instance.PlayerEntity.GoldPieces.ToString();
                tripCostLabel.Text = MsgPlayerControlled;
                int travelTimeHours = travelTimeTotalMins / 60;
                int travelTimeMinutes = travelTimeTotalMins % 60;
                travelTimeLabel.Text = string.Format(MsgTimeFormat, travelTimeHours, travelTimeMinutes);
            }
            else
            {
                base.UpdateLabels();
            }
        }

        protected override void CallFastTravelGoldCheck()
        {
            if (!travelWindowTO.LocationSelected)
            {
                CloseWindow();
                TravelWindow.CloseTravelWindows(true);

                Debug.LogFormat("Start travel to MP coords: {0},{1}", EndPos.X, EndPos.Y);
                TravelOptionsMod.Instance.BeginTravelToCoords(EndPos, SpeedCautious);
            }
            else if (IsPlayerControlledTravel())
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

        protected virtual bool IsShipTravelValid()
        {
            if (IsNotAtPort())
            {
                DaggerfallUI.MessageBox(MsgNoPort);
                return false;
            }
            else if (HasNoOceanTravel())
            {
                DaggerfallUI.MessageBox(MsgNoSailing);
                return false;
            }
            else if (IsDestNotValidPort())
            {
                DaggerfallUI.MessageBox(MsgNoDestPort);
                return false;
            }
            return true;
        }

        public override void TransportModeButtonOnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            if (TravelOptionsMod.Instance.ShipTravelPortsOnly && sender == shipToggleButton && !IsShipTravelValid())
                return;

            base.TransportModeButtonOnClickHandler(sender, position);
        }

        public override void ToggleTransportModeButtonOnScrollHandler(BaseScreenComponent sender)
        {
            if (TravelOptionsMod.Instance.ShipTravelPortsOnly && TravelShip == false && !IsShipTravelValid())
                return;

            base.ToggleTransportModeButtonOnScrollHandler(sender);
        }

        public override void SleepModeButtonOnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            if (TravelOptionsMod.Instance.ShipTravelPortsOnly && sender == campOutToggleButton)
            {
                if (IsNotAtPort() || HasNoOceanTravel() || IsDestNotValidPort())
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
