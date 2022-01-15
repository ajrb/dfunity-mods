// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut
// Contributors:    Jedidia

using System;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Utility.AssetInjection;

namespace TravelOptions
{
    public class TravelControlUI : DaggerfallPopupWindow
    {
        #region UI Rects

        Rect destPanelRect = new Rect(5, 14, 152, 7);
        Rect mapButtonRect = new Rect(183, 3, 45, 21);
        Rect campButtonRect = new Rect(230, 3, 45, 21);
        Rect exitButtonRect = new Rect(279, 3, 38, 21);
        Vector2 timeAccelPos = new Vector2(163, 4);

        #endregion

        #region UI Controls

        Panel mainPanel = new Panel();
        Panel destPanel;
        TextLabel destinationLabel = new TextLabel();
        TextLabel messageLabel;
        UpDownSpinner timeAccelSpinner = new UpDownSpinner();
        Button mapButton;
        Button campButton;
        Button exitButton;

        #endregion

        #region Fields

        public const string TipMap = "Consult Map";
        public const string TipCamp = "Stop to Camp";

        const string baseTextureName = "TOcontrolUI.png";

        Texture2D baseTexture;
        Vector2 baseSize = new Vector2(320, 27);
        int accelLimit;
        int halfAccelLimit;
        uint messageTimer = 0;

        Panel junctionMapPanel;

        public bool isShowing = false;

        public int TimeAcceleration { get; internal set; }

        public void SetDestinationName(string destinationName)
        {
            destinationLabel.Text = destinationName;
        }

        public bool HalfLimit { get; set; } = false;

        public int GetAccelerationLimit()
        {
            return HalfLimit ? halfAccelLimit : accelLimit;
        }

        #endregion

        #region Constructors

        public TravelControlUI(IUserInterfaceManager uiManager, int defaultStartingAccel = 10, int accelerationLimit = 100, Panel junctionMapPanel = null)
            : base(uiManager)
        {
            TimeAcceleration = defaultStartingAccel;
            accelLimit = (accelerationLimit / 5) * 5;
            halfAccelLimit = (accelerationLimit / 10) * 5;
            this.junctionMapPanel = junctionMapPanel;

            // Clear background
            ParentPanel.BackgroundColor = Color.clear;
            pauseWhileOpened = false;

            // Override base.Update calling CancelWindow when esc is pressed. CancelWindow method otherwise uneffected.
            AllowCancel = false;
        }

        #endregion

        #region Setup Methods

        protected override void Setup()
        {
            // Load all textures
            LoadTextures();

            // Create interface panel
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Top;
            mainPanel.Position = new Vector2(0, 0);
            mainPanel.BackgroundTexture = baseTexture;
            mainPanel.Size = baseSize;

            // Add primary skill spinner
            mainPanel.Components.Add(timeAccelSpinner);
            timeAccelSpinner.Position = timeAccelPos;
            timeAccelSpinner.OnDownButtonClicked += SlowerButton_OnMouseClick;
            timeAccelSpinner.OnUpButtonClicked += FasterButton_OnMouseClick;
            timeAccelSpinner.Value = TimeAcceleration;

            // Destination label
            destPanel = DaggerfallUI.AddPanel(destPanelRect, mainPanel);
            destPanel.Components.Add(destinationLabel);
            destinationLabel.HorizontalAlignment = HorizontalAlignment.Center;

            // Message label
            messageLabel = DaggerfallUI.AddTextLabel(null, new Vector2(0, 32), "", NativePanel);
            messageLabel.HorizontalAlignment = HorizontalAlignment.Center;

            // Map button
            mapButton = DaggerfallUI.AddButton(mapButtonRect, mainPanel);
            mapButton.OnMouseClick += (_, __) => {
                DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenTravelMapWindow);
            };
            mapButton.ToolTip = defaultToolTip;
            mapButton.ToolTipText = TipMap;

            // Camp (pause travel) button
            campButton = DaggerfallUI.AddButton(campButtonRect, mainPanel);
            campButton.OnMouseClick += (_, __) => { CloseWindow(); };
            campButton.ToolTip = defaultToolTip;
            campButton.ToolTipText = TipCamp;

            // Exit travel button
            exitButton = DaggerfallUI.AddButton(exitButtonRect, mainPanel);
            exitButton.OnMouseClick += (_, __) => { CancelWindow(); };

            NativePanel.Components.Add(mainPanel);
        }

        #endregion

        #region Public Methods

        public void ShowMessage(string message)
        {
            if (IsSetup)
            {
                messageLabel.Text = message;
                messageTimer = (uint)Time.unscaledTime + 3;
            }
        }

        public override void Update()
        {
            base.Update();

            timeAccelSpinner.Value = TimeAcceleration;
            if (messageTimer > 0 && Time.unscaledTime > messageTimer)
            {
                messageLabel.Text = "";
                messageTimer = 0;
            }

            if (Input.GetKeyUp(exitKey))
                CloseWindow();
        }

        public override void Draw()
        {
            base.Draw();

            if (junctionMapPanel != null)
                junctionMapPanel.Draw();

            DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.Draw();
            DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Draw();
            //DaggerfallUI.Instance.DaggerfallHUD.ShowMidScreenText = true;
        }

        public override void OnPush()
        {
            base.OnPush();
            isShowing = true;

            TimeAcceleration = Mathf.Clamp(TimeAcceleration, 1, GetAccelerationLimit());
        }

        public override void OnPop()
        {
            base.OnPop();
            isShowing = false;
        }

        /// <summary>
        /// Game is stuck in paused mode when returning from modal UI, have to unstick it manually.
        /// </summary>
        public override void OnReturn()
        {
            base.OnReturn();
            GameManager.Instance.PauseGame(false);
        }

        #endregion

        #region Private Methods

        void LoadTextures()
        {
            if (!TextureReplacement.TryImportImage(baseTextureName, true, out baseTexture))
            {
                Debug.LogError("TravelOptions: Unable to load the base UI image.");
            }
        }

        #endregion

        #region Event Handlers

        private void FasterButton_OnMouseClick()
        {
            if (TimeAcceleration < 5)
                TimeAcceleration += 1;
            else
                TimeAcceleration = Math.Min(GetAccelerationLimit(), TimeAcceleration + 5);

            RaiseOnTimeAccelerationChangeEvent(TimeAcceleration);
        }

        private void SlowerButton_OnMouseClick()
        {
            if (TimeAcceleration <= 5)
                TimeAcceleration = Math.Max(1, TimeAcceleration - 1);
            else
                TimeAcceleration = Mathf.Max(1, TimeAcceleration - 5);

            RaiseOnTimeAccelerationChangeEvent(TimeAcceleration);
        }

        // events
        public delegate void OnTimeAccelerationChangeHandler(int newTimeAcceleration);
        public event OnTimeAccelerationChangeHandler OnTimeAccelerationChanged;
        void RaiseOnTimeAccelerationChangeEvent(int newTimeAcceleration)
        {
            if (OnTimeAccelerationChanged != null)
                OnTimeAccelerationChanged(newTimeAcceleration);
        }

        #endregion
    }
}
