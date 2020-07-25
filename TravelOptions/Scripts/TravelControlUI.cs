// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2019 Jedidia
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Jedidia
// Contributors:    Hazelnut

using System;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace TravelOptions
{
    public class TravelControlUI : DaggerfallPopupWindow
    {

        #region UI Rects
        Rect mainPanelRect = new Rect(0, 0, 215, 24);
        Rect destinationRect = new Rect(5, 2, 210, 10);
        Rect fasterButtonRect = new Rect(5, 12, 20, 10);
        Rect timeCompressionRect = new Rect(30, 12, 20, 10);
        Rect slowerButtonRect = new Rect(55, 12, 20, 10);
        Rect mapButtonRect = new Rect(80, 12, 40, 10);
        Rect interruptButtonRect = new Rect(125, 12, 40, 10);
        Rect cancelButtonRect = new Rect(170, 12, 40, 10);
        #endregion

        #region UI Controls

        Panel mainPanel = null;
        Button fasterButton;
        Button slowerButton;
        Button interruptButton;
        Button cancelButton;
        Button mapButton;
        TextBox destinationTextbox;
        TextBox timeCompressionTextbox;
        #endregion

        #region UI Textures

        Texture2D baseTexture;
        Texture2D disabledTexture;

        #endregion

        #region Fields

        public bool isShowing = false;

        Color mainPanelBackgroundColor = new Color(0.0f, 0f, 0.0f, 1.0f);
        Color buttonBackgroundColor = new Color(0.0f, 0.5f, 0.0f, 0.4f);
        Color cancelButtonBackgroundColor = new Color(0.7f, 0.0f, 0.0f, 0.4f);

        public int TimeAcceleration { get; private set; } = 10;

        string destinationStr = "";
        public void SetDestination(string destinationName)
        {
            destinationStr = "Travelling to " + destinationName;
            if (destinationTextbox != null)
                destinationTextbox.Text = destinationStr;
        }

        #endregion

        #region Constructors

        public TravelControlUI(IUserInterfaceManager uiManager)
            : base(uiManager)
        {
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

            // Create interface panel
            mainPanel = DaggerfallUI.AddPanel(mainPanelRect);
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Top;
            mainPanel.BackgroundColor = mainPanelBackgroundColor;

            // destination description
            destinationTextbox = DaggerfallUI.AddTextBox(destinationRect, destinationStr, mainPanel);
            destinationTextbox.ReadOnly = true;
            // increase time compression button
            fasterButton = DaggerfallUI.AddButton(fasterButtonRect, mainPanel);
            fasterButton.OnMouseClick += FasterButton_OnMouseClick;
            fasterButton.BackgroundColor = buttonBackgroundColor;
            fasterButton.Label.Text = "+";
            //display time compression
            timeCompressionTextbox = DaggerfallUI.AddTextBox(timeCompressionRect, TimeAcceleration + "x", mainPanel);
            timeCompressionTextbox.ReadOnly = true;

            // decrease time compression button
            slowerButton = DaggerfallUI.AddButton(slowerButtonRect, mainPanel);
            slowerButton.OnMouseClick += SlowerButton_OnMouseClick;
            slowerButton.BackgroundColor = buttonBackgroundColor;
            slowerButton.Label.Text = "-";

            // map button
            mapButton = DaggerfallUI.AddButton(mapButtonRect, mainPanel);
            mapButton.OnMouseClick += (_, __) => {
                DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenTravelMapWindow);
            };
            mapButton.BackgroundColor = buttonBackgroundColor;
            mapButton.Label.Text = "Map";

            // interrupt travel button
            interruptButton = DaggerfallUI.AddButton(interruptButtonRect, mainPanel);
            interruptButton.OnMouseClick += (_, __) => { CloseWindow(); };
            interruptButton.BackgroundColor = buttonBackgroundColor;
            interruptButton.Label.Text = "Interrupt";

            // cancel travel button
            cancelButton = DaggerfallUI.AddButton(cancelButtonRect, mainPanel);
            cancelButton.OnMouseClick += (_, __) => { CancelWindow(); };
            cancelButton.BackgroundColor = cancelButtonBackgroundColor;
            cancelButton.Label.Text = "Cancel";

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

        #endregion

        #region Event Handlers

        private void FasterButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (TimeAcceleration < 5)
                TimeAcceleration += 1;
            else
                TimeAcceleration = Math.Min(100, TimeAcceleration + 5);

            timeCompressionTextbox.Text = TimeAcceleration.ToString() + "x";
            RaiseOnTimeAccelerationChangeEvent(TimeAcceleration);
        }

        private void SlowerButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (TimeAcceleration <= 5)
                TimeAcceleration = Math.Max(1, TimeAcceleration - 1);
            else
                TimeAcceleration = Mathf.Max(1, TimeAcceleration - 5);

            timeCompressionTextbox.Text = TimeAcceleration.ToString() + "x";
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
        void RaiseOnTimeAccelerationChangeEvent(int newTimeCompression)
        {
            if (OnTimeAccelerationChanged != null)
                OnTimeAccelerationChanged(newTimeCompression);
        }


        #endregion
    }
}
