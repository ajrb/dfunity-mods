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
using System;
using DaggerfallWorkshop.Game.Utility.ModSupport;

namespace TravelOptions
{
    public class TravelOptionsPopUp : DaggerfallTravelPopUp
    {
        private static string MsgPlayerControlled => TravelOptionsMod.Localize("MsgPlayerControlled");
        private static string MsgTimeFormat => TravelOptionsMod.Localize("MsgTimeFormat");
        private static string MsgTimeFormatNoSDF => TravelOptionsMod.Localize("MsgTimeFormatNoSDF");
        private static string MsgNoPort => TravelOptionsMod.Localize("MsgNoPort");
        private static string MsgNoDestPort => TravelOptionsMod.Localize("MsgNoDestPort");
        private static string MsgNoSailing => TravelOptionsMod.Localize("MsgNoSailing");
        private static string MsgNotVisited => TravelOptionsMod.Localize("MsgNotVisited");

        protected TravelOptionsMapWindow travelWindowTO;

        public new DFPosition EndPos { get { return base.EndPos; } protected internal set { base.EndPos = value; } }

        public void SetScaleFactors(int inns, int ships) { ((TravelTimeCalculatorTO)travelTimeCalculator).SetScaleFactors(inns, ships); }

        public TravelOptionsPopUp(IUserInterfaceManager uiManager, IUserInterfaceWindow previousWindow = null, DaggerfallTravelMapWindow travelWindow = null)
            : base(uiManager, previousWindow, travelWindow)
        {
            travelWindowTO = (TravelOptionsMapWindow)travelWindow;
            travelTimeCalculator = new TravelTimeCalculatorTO();
        }

        protected override void Setup()
        {
            base.Setup();

            if (DaggerfallWorkshop.DaggerfallUnity.Settings.SDFFontRendering)
            {
                travelTimeLabel.MaxCharacters = 28;
                tripCostLabel.MaxCharacters = 25;
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

        public override void Update()
        {
            base.Update();

            if (travelWindowTO.LocationSelected)
            {
                if (travelWindowTO.infoBox == null && Input.GetKey(KeyCode.I))
                {
                    travelWindowTO.DisplayLocationInfo();
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
            return location.Loaded == false || !TravelOptionsMapWindow.HasPort(location.MapTableData);
        }

        public bool HasNoOceanTravel()
        {
            return travelTimeCalculator.OceanPixels == 0 && !GameManager.Instance.TransportManager.IsOnShip() && !TravelOptionsMapWindow.HasPort(TravelWindow.LocationSummary);
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
                if (DaggerfallWorkshop.DaggerfallUnity.Settings.SDFFontRendering)
                {
                    travelTimeLabel.Text = string.Format(MsgTimeFormat, travelTimeHours, travelTimeMinutes);
                }
                else
                {
                    travelTimeLabel.Text = string.Format(MsgTimeFormatNoSDF, travelTimeHours, travelTimeMinutes);
                }
            }
            else
            {
                base.UpdateLabels();
            }
        }

        protected override void CallFastTravelGoldCheck()
        {
            // Hidden Map Locations: Check if player has visited before allowing fast travel.
            if (TravelOptionsMod.Instance.HiddenMapLocationsEnabled && !IsPlayerControlledTravel())
            {
                bool hasVisitedLocation = false;
                ModManager.Instance.SendModMessage(TravelOptionsMod.HIDDEN_MAP_LOCATIONS_MODNAME, "hasVisitedLocation",
                    new Tuple<int, int, bool>(EndPos.X, EndPos.Y, TravelShip),
                    (string _, object result) => { hasVisitedLocation = (bool)result; });

                if (!hasVisitedLocation)
                {
                    DaggerfallUI.MessageBox(MsgNotVisited);
                    return;
                }
            }

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
            else if (IsDestNotValidPort())
            {
                DaggerfallUI.MessageBox(MsgNoDestPort);
                return false;
            }
            else if (HasNoOceanTravel())
            {
                DaggerfallUI.MessageBox(MsgNoSailing);
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
