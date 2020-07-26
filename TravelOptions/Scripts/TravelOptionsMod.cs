// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut
// Contributor:     Jedidia

using System;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Utility;

namespace TravelOptions
{
    public class TravelOptionsMod : MonoBehaviour
    {
        private const string MsgArrived = "You have arrived at your destination";
        private const string MsgEnemies = "Enemies are seeking to prevent your travel...";
        private const string MsgAvoidAttempt = "You suspect enemies are close, attempt to avoid them?";
        private const string MsgAvoidFail = "You failed to avoid the encounter!";

        static Mod mod;
        static readonly int[] startAccelVals = { 1, 5, 10, 20, 30, 40, 50 };

        public static TravelOptionsMod Instance { get; private set; }

        public ContentReader.MapSummary DestinationSummary { get; private set; }
        public string DestinationName { get; private set; }

        private PlayerAutoPilot playerAutopilot;
        private TravelControlUI travelControlUI;
        internal TravelControlUI GetTravelControlUI() { return travelControlUI; }

        private int defaultStartingAccel;
        private int accelerationLimit;
        private float baseFixedDeltaTime;
        private bool encounterAvoidanceSystem;
        private int maxSuccessChance;
        private bool ignoreEncounters;
        private uint ignoreEncountersTime;
        private int diseaseCount = 0;
        private uint beginTime = 0;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            Instance = go.AddComponent<TravelOptionsMod>();
        }

        void Awake()
        {
            Debug.Log("Begin mod init: TravelOptions");

            ModSettings settings = mod.GetSettings();
            defaultStartingAccel = startAccelVals[settings.GetValue<int>("TimeAcceleration", "DefaultStartingAcceleration")];
            accelerationLimit = settings.GetValue<int>("TimeAcceleration", "AccelerationLimiter");
            encounterAvoidanceSystem = settings.GetValue<bool>("RandomEncounterAvoidance", "AvoidRandomEncounters");
            maxSuccessChance = settings.GetValue<int>("RandomEncounterAvoidance", "MaxChanceToAvoidEncounter");

            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.TravelMap, typeof(TravelOptionsMapWindow));
            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.TravelPopUp, typeof(TravelOptionsPopUp));

            baseFixedDeltaTime = Time.fixedDeltaTime;

            travelControlUI = new TravelControlUI(DaggerfallUI.UIManager, defaultStartingAccel, accelerationLimit);
            travelControlUI.OnCancel += (sender) => { ClearTravelDestination(); };
            travelControlUI.OnClose += () => { InterruptTravel(); };
            travelControlUI.OnTimeAccelerationChanged += (timeAcceleration) => { SetTimeScale(timeAcceleration); };

            // Clear destination on new game or load game.
            SaveLoadManager.OnLoad += (saveData) => { ClearTravelDestination(); };
            StartGameBehaviour.OnNewGame += () => { ClearTravelDestination(); };
            GameManager.OnEncounter += GameManager_OnEncounter;

            Debug.Log("Finished mod init: TravelOptions");

            mod.IsReady = true;
        }

        private void SetTimeScale(int timeScale)
        {
            // Must set fixed delta time to scale the fixed (physics) updates as well.
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = timeScale * baseFixedDeltaTime; // Default is 0.02 or 50/s

            Debug.LogFormat("Set timescale= {0}, fixedDelta= {1}", timeScale, timeScale * baseFixedDeltaTime);
        }

        public void ClearTravelDestination()
        {
            DestinationName = null;
        }

        private void GameManager_OnEncounter()
        {
            if (travelControlUI.isShowing)
            {
                SetTimeScale(1);        // Essentially redundant, but still helpful, since the close window event takes longer to trigger the time downscale.
                travelControlUI.CloseWindow();
                DaggerfallUI.MessageBox(MsgEnemies);
            }
        }

        public void BeginTravel(ContentReader.MapSummary destinationSummary)
        {
            if (DaggerfallUnity.Instance.ContentReader.GetLocation(destinationSummary.RegionIndex, destinationSummary.MapIndex, out DFLocation targetLocation))
            {
                DestinationName = targetLocation.Name;
                travelControlUI.SetDestination(targetLocation.Name);
                DestinationSummary = destinationSummary;
                BeginTravel();
                beginTime = DaggerfallUnity.Instance.WorldTime.Now.ToClassicDaggerfallTime();
            }
            else throw new Exception("TravelOptions: destination not found!");

        }

        public void BeginTravel()
        {
            if (!string.IsNullOrEmpty(DestinationName))
            {
                playerAutopilot = new PlayerAutoPilot(DestinationSummary);
                playerAutopilot.OnArrival += () =>
                    {
                        travelControlUI.CancelWindow();
                        DaggerfallUI.Instance.DaggerfallHUD.SetMidScreenText(MsgArrived, 5f);
                        Debug.Log("Elapsed time for trip: " + (DaggerfallUnity.Instance.WorldTime.Now.ToClassicDaggerfallTime() - beginTime) );
                    };

                SetTimeScale(travelControlUI.TimeAcceleration);
                DisableWeatherAndSound();
                diseaseCount = GameManager.Instance.PlayerEffectManager.DiseaseCount;

                Debug.Log("Begun travel to " + DestinationName);
            }
        }

        /// <summary>
        /// Stops travel, but leaves current destination active
        /// </summary>
        public void InterruptTravel()
        {
            Debug.Log("Travel interrupted");
            SetTimeScale(1);
            GameManager.Instance.PlayerMouseLook.enableMouseLook = true;
            GameManager.Instance.PlayerMouseLook.lockCursor = true;
            GameManager.Instance.PlayerMouseLook.simpleCursorLock = false;
            playerAutopilot.MouseLookAtDestination();
            playerAutopilot = null;
            EnableWeatherAndSound();
        }


        void Update()
        {
            if (playerAutopilot != null)
            {
                // Ensure UI is showing
                if (!travelControlUI.isShowing)
                    DaggerfallUI.UIManager.PushWindow(travelControlUI);

                // Run updates for playerAutopilot and HUD
                playerAutopilot.Update();
                DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.Update();

                // Handle encounters.
                if (ignoreEncounters && DaggerfallUnity.Instance.WorldTime.Now.ToClassicDaggerfallTime() >= ignoreEncountersTime)
                {
                    ignoreEncounters = false;
                }

                if (!ignoreEncounters && GameManager.Instance.AreEnemiesNearby())
                {
                    // This happens when DFU spawns enemies nearby, however quest trigger encounters fire the OnEncounter event first so this code is never reached.
                    Debug.Log("Enemies enountered during travel");
                    travelControlUI.CloseWindow();
                    if (encounterAvoidanceSystem)
                    {
                        AttemptAvoidEncounter();
                    }
                    else
                    {
                        DaggerfallUI.MessageBox(MsgEnemies);
                        return;
                    }
                }

                // Check for diseases.
                var currentDiseaseCount = GameManager.Instance.PlayerEffectManager.DiseaseCount;
                if (currentDiseaseCount != diseaseCount)
                {
                    if (currentDiseaseCount > diseaseCount)
                    {
                        Debug.Log("New disease detected, interrupting travel!");
                        InterruptTravel();
                        DaggerfallUI.Instance.CreateHealthStatusBox(DaggerfallUI.Instance.UserInterfaceManager.TopWindow).Show();
                    }
                    diseaseCount = currentDiseaseCount;
                }
            }
        }

        private void AttemptAvoidEncounter()
        {
            DaggerfallSkills skills = GameManager.Instance.PlayerEntity.Skills;
            int successChance = Mathf.Max(skills.GetLiveSkillValue(DFCareer.Skills.Running), skills.GetLiveSkillValue(DFCareer.Skills.Stealth));

            successChance = successChance * maxSuccessChance / 100;

            DaggerfallMessageBox mb = new DaggerfallMessageBox(DaggerfallUI.Instance.UserInterfaceManager);
            mb.SetText(MsgAvoidAttempt);
            mb.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes, true);
            mb.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
            mb.ParentPanel.BackgroundColor = Color.clear;

            mb.OnButtonClick += (_sender, button) =>
            {
                _sender.CloseWindow();
                if (button == DaggerfallMessageBox.MessageBoxButtons.Yes)
                {
                    if (Dice100.SuccessRoll(successChance))
                    {
                        ignoreEncounters = true;
                        ignoreEncountersTime = DaggerfallUnity.Instance.WorldTime.Now.ToClassicDaggerfallTime() + 10;
                        BeginTravel();
                    }
                    else
                    {
                        DaggerfallUI.MessageBox(MsgAvoidFail);
                    }
                }
            };
            mb.Show();
        }

        private void DisableWeatherAndSound()
        {
            var playerWeather = GameManager.Instance.WeatherManager.PlayerWeather;
            playerWeather.RainParticles.SetActive(false);
            playerWeather.SnowParticles.SetActive(false);
            playerWeather.enabled = false;

            GameManager.Instance.PlayerActivate.GetComponentInParent<PlayerFootsteps>().enabled = false;
            GameManager.Instance.TransportManager.GetComponent<AudioSource>().enabled = false;
        }

        private void EnableWeatherAndSound()
        {
            GameManager.Instance.WeatherManager.PlayerWeather.enabled = true;

            GameManager.Instance.PlayerActivate.GetComponentInParent<PlayerFootsteps>().enabled = true;
            GameManager.Instance.TransportManager.GetComponent<AudioSource>().enabled = true;
        }

    }
}
