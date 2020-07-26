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
        Vector2 baseSize;
        int accelerationLimit;

        public bool isShowing = false;

        public int TimeAcceleration { get; private set; }

        public void SetDestination(string destinationName)
        {
            destinationLabel.Text = destinationName;
        }

        #endregion

        #region Constructors

        public TravelControlUI(IUserInterfaceManager uiManager, int defaultStartingAccel = 10, int accelerationLimit = 100)
            : base(uiManager)
        {
            TimeAcceleration = defaultStartingAccel;
            this.accelerationLimit = (accelerationLimit / 5) * 5;

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

        #region Overrides

        public override void Update()
        {
            base.Update();

            if (Input.GetKeyUp(exitKey))
                CloseWindow();
        }

        public override void Draw()
        {
            base.Draw();
            DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.Draw();
            DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Draw();
            DaggerfallUI.Instance.DaggerfallHUD.ShowMidScreenText = true;
        }

        public override void OnPush()
        {
            base.OnPush();
            isShowing = true;
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
            if (TextureReplacement.TryImportImage(baseTextureName, true, out baseTexture))
            {
                baseSize = new Vector2(baseTexture.width, baseTexture.height);
            }
        }

        #endregion

        #region Event Handlers

        private void FasterButton_OnMouseClick()
        {
            if (TimeAcceleration < 5)
                TimeAcceleration += 1;
            else
                TimeAcceleration = Math.Min(accelerationLimit, TimeAcceleration + 5);

            timeAccelSpinner.Value = TimeAcceleration;

            RaiseOnTimeAccelerationChangeEvent(TimeAcceleration);
        }

        private void SlowerButton_OnMouseClick()
        {
            if (TimeAcceleration <= 5)
                TimeAcceleration = Math.Max(1, TimeAcceleration - 1);
            else
                TimeAcceleration = Mathf.Max(1, TimeAcceleration - 5);

            timeAccelSpinner.Value = TimeAcceleration;

            RaiseOnTimeAccelerationChangeEvent(TimeAcceleration);
        }

        private void CampButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
        }

        private void CancelButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
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
