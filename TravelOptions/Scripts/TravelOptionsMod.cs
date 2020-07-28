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
        public const string PAUSE_TRAVEL = "pauseTravel";

        private const string MsgArrived = "You have arrived at your destination.";
        private const string MsgEnemies = "Enemies are seeking to prevent your travel...";
        private const string MsgAvoidFail = "You failed to avoid an encounter!";
        private const string MsgAvoidSuccess = "You successfully avoided an encounter.";
        private const string MsgLowHealth = "You are close to the point of death!";
        private const string MsgLowFatigue = "You are exhausted and should rest.";
        private const string MsgNearLocation = "Paused the journey since a {0} called {1} is nearby.";
        private const string MsgEnterLocation = "Paused the journey as you've entered a {0} called {1}.";

        private const int LocPauseOff = 0;
        private const int LocPauseNear = 1;
        private const int LocPauseEnter = 2;

        public static TravelOptionsMod Instance { get; private set; }

        public string DestinationName { get; private set; }
        public ContentReader.MapSummary DestinationSummary { get; private set; }
        public bool DestinationCautious { get; private set; }

        public bool CautiousTravel { get; private set; }
        public bool StopAtInnsTravel { get; private set; }
        public bool ShipTravelPortsOnly { get; private set; }
        public bool ShipTravelDestinationPortsOnly { get; private set; }

        public float RecklessTravelMultiplier { get; private set; } = 1f;
        public float CautiousTravelMultiplier { get; private set; } = 0.8f;
        private float GetTravelSpeedMultiplier() { return DestinationCautious ? CautiousTravelMultiplier : RecklessTravelMultiplier; }
        public int CautiousHealthMinPc { get; private set; } = 5;
        public int CautiousFatigueMin { get; private set; } = 6;

        static readonly int[] startAccelVals = { 1, 2, 3, 5, 10, 20, 30, 40, 50 };

        static Mod mod;

        private PlayerAutoPilot playerAutopilot;
        private TravelControlUI travelControlUI;
        internal TravelControlUI GetTravelControlUI() { return travelControlUI; }
        private DFLocation lastLocation;

        private bool disableWeather;
        private bool disableSounds;
        private bool disableRealGrass;
        private int locationPause;
        private int defaultStartingAccel;
        private bool alwaysUseStartingAccel;
        private int accelerationLimit;
        private float baseFixedDeltaTime;
        private int maxAvoidChance;
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

            CautiousTravel = settings.GetValue<bool>("GeneralOptions", "CautiousTravel");
            StopAtInnsTravel = settings.GetValue<bool>("GeneralOptions", "StopAtInnsTravel");
            disableWeather = settings.GetValue<bool>("GeneralOptions", "DisableWeather");
            disableSounds = settings.GetValue<bool>("GeneralOptions", "DisableSounds");
            disableRealGrass = settings.GetValue<bool>("GeneralOptions", "DisableRealGrass");
            locationPause = settings.GetValue<int>("GeneralOptions", "LocationPause");

            int speedPenalty = settings.GetValue<int>("CautiousTravel", "SpeedPenalty");
            CautiousTravelMultiplier = 1 - ((float)speedPenalty / 100);
            maxAvoidChance = settings.GetValue<int>("CautiousTravel", "MaxChanceToAvoidEncounter");
            CautiousHealthMinPc = settings.GetValue<int>("CautiousTravel", "HealthMinimumPercentage");
            CautiousFatigueMin = settings.GetValue<int>("CautiousTravel", "FatigueMinimumValue") + 1;

            ShipTravelPortsOnly = settings.GetValue<bool>("ShipTravel", "OnlyFromPorts");
            ShipTravelDestinationPortsOnly = settings.GetValue<bool>("ShipTravel", "OnlyToPorts");

            defaultStartingAccel = startAccelVals[settings.GetValue<int>("TimeAcceleration", "DefaultStartingAcceleration")];
            alwaysUseStartingAccel = settings.GetValue<bool>("TimeAcceleration", "AlwaysUseStartingAcceleration");
            accelerationLimit = settings.GetValue<int>("TimeAcceleration", "AccelerationLimit");

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

            mod.MessageReceiver = MessageReceiver;
            mod.IsReady = true;

            Debug.Log("Finished mod init: TravelOptions");
        }

        private void SetTimeScale(int timeScale)
        {
            // Must set fixed delta time to scale the fixed (physics) updates as well.
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = timeScale * baseFixedDeltaTime; // Default is 0.02 or 50/s
#if UNITY_EDITOR
            Debug.LogFormat("Set timescale= {0}, fixedDelta= {1}", timeScale, timeScale * baseFixedDeltaTime);
#endif
        }

        public void ClearTravelDestination()
        {
            DestinationName = null;
        }

        private void GameManager_OnEncounter()
        {
            if (travelControlUI.isShowing && !ignoreEncounters)
            {
                SetTimeScale(1);        // Essentially redundant, but still helpful, since the close window event takes longer to trigger the time downscale.
                travelControlUI.CloseWindow();
                DaggerfallUI.MessageBox(MsgEnemies);
            }
        }

        private void PlayerGPS_OnEnterLocationRect(DFLocation dfLocation)
        {
            if (travelControlUI.isShowing)
                travelControlUI.CloseWindow();
            DaggerfallUI.MessageBox(string.Format(MsgEnterLocation, MacroHelper.LocationTypeName(), dfLocation.Name));
        }


        public void BeginTravel(ContentReader.MapSummary destinationSummary, bool speedCautious = false)
        {
            DFLocation targetLocation;
            if (DaggerfallUnity.Instance.ContentReader.GetLocation(destinationSummary.RegionIndex, destinationSummary.MapIndex, out targetLocation))
            {
                DestinationName = targetLocation.Name;
                travelControlUI.SetDestination(targetLocation.Name);
                DestinationSummary = destinationSummary;
                DestinationCautious = speedCautious;
                if (alwaysUseStartingAccel)
                    travelControlUI.TimeAcceleration = defaultStartingAccel;
                BeginTravel();
                beginTime = DaggerfallUnity.Instance.WorldTime.Now.ToClassicDaggerfallTime();
            }
            else throw new Exception("TravelOptions: destination not found!");

        }

        public void BeginTravel()
        {
            if (!string.IsNullOrEmpty(DestinationName))
            {
                playerAutopilot = new PlayerAutoPilot(DestinationSummary, GetTravelSpeedMultiplier());
                playerAutopilot.OnArrival += () =>
                {
                    travelControlUI.CloseWindow();
                    ClearTravelDestination();
                    DaggerfallUI.MessageBox(MsgArrived);
                    Debug.Log("Elapsed time for trip: " + (DaggerfallUnity.Instance.WorldTime.Now.ToClassicDaggerfallTime() - beginTime));
                };

                lastLocation = GameManager.Instance.PlayerGPS.CurrentLocation;
                SetTimeScale(travelControlUI.TimeAcceleration);
                DisableWeatherAndSound();
                diseaseCount = GameManager.Instance.PlayerEffectManager.DiseaseCount;

                // Register event for entering location rects
                if (locationPause == LocPauseEnter)
                    PlayerGPS.OnEnterLocationRect += PlayerGPS_OnEnterLocationRect;

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
            if (playerAutopilot != null)
                playerAutopilot.MouseLookAtDestination();
            playerAutopilot = null;
            EnableWeatherAndSound();

            // Remove event for entering location rects
            if (locationPause == LocPauseEnter)
                PlayerGPS.OnEnterLocationRect -= PlayerGPS_OnEnterLocationRect;
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

                // If travelling cautiously, check health and fatigue levels
                if (DestinationCautious)
                {
                    if (GameManager.Instance.PlayerEntity.CurrentHealthPercent * 100 < CautiousHealthMinPc)
                    {
                        if (travelControlUI.isShowing)
                            travelControlUI.CloseWindow();
                        DaggerfallUI.MessageBox(MsgLowHealth);
                        return;
                    }
                    if (GameManager.Instance.PlayerEntity.CurrentFatigue < DaggerfallEntity.FatigueMultiplier * CautiousFatigueMin)
                    {
                        if (travelControlUI.isShowing)
                            travelControlUI.CloseWindow();
                        DaggerfallUI.MessageBox(MsgLowFatigue);
                        return;
                    }
                }

                // If location pause set to nearby, check for a near location
                PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
                if (locationPause == LocPauseNear && playerGPS.HasCurrentLocation && !playerGPS.CurrentLocation.Equals(lastLocation))
                {
                    lastLocation = playerGPS.CurrentLocation;
                    if (travelControlUI.isShowing)
                        travelControlUI.CloseWindow();
                    DaggerfallUI.MessageBox(string.Format(MsgNearLocation, MacroHelper.LocationTypeName(), playerGPS.CurrentLocation.Name));
                    return;
                }

                // Handle encounters.
                if (ignoreEncounters && Time.unscaledTime >= ignoreEncountersTime)
                {
                    ignoreEncounters = false;
                }
                if (!ignoreEncounters && GameManager.Instance.AreEnemiesNearby())
                {
                    // This happens when DFU spawns enemies nearby, however quest trigger encounters fire the OnEncounter event first so this code is never reached.
                    Debug.Log("Enountered enemies during travel.");
                    travelControlUI.CloseWindow();
                    if (DestinationCautious)
                        AttemptAvoidEncounter();
                    else
                        DaggerfallUI.MessageBox(MsgEnemies);
                    return;
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
            int successChance = Mathf.Min((GameManager.Instance.PlayerEntity.Stats.LiveLuck + GameManager.Instance.PlayerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth) - 20), maxAvoidChance);

            if (Dice100.SuccessRoll(successChance))
            {
                Debug.LogWarning("Avoided enemies enountered during travel, chance: " + successChance);
                ignoreEncounters = true;
                ignoreEncountersTime = (uint)Time.unscaledTime + 15;
                BeginTravel();
                travelControlUI.ShowMessage(MsgAvoidSuccess);
            }
            else
            {
                Debug.Log("Failed to avoid enemies enountered during travel, chance: " + successChance);
                DaggerfallUI.MessageBox(MsgAvoidFail);
            }
        }

        private void DisableWeatherAndSound()
        {
            if (disableWeather)
            {
                var playerWeather = GameManager.Instance.WeatherManager.PlayerWeather;
                playerWeather.RainParticles.SetActive(false);
                playerWeather.SnowParticles.SetActive(false);
                playerWeather.enabled = false;
            }

            if (disableSounds)
            {
                GameManager.Instance.PlayerActivate.GetComponentInParent<PlayerFootsteps>().enabled = false;
                GameManager.Instance.TransportManager.GetComponent<AudioSource>().enabled = false;
            }

            if (disableRealGrass)
                ModManager.Instance.SendModMessage("Real Grass", "toggle", false);
        }

        private void EnableWeatherAndSound()
        {
            if (disableWeather)
                GameManager.Instance.WeatherManager.PlayerWeather.enabled = true;

            if (disableSounds)
            {
                GameManager.Instance.PlayerActivate.GetComponentInParent<PlayerFootsteps>().enabled = true;
                GameManager.Instance.TransportManager.GetComponent<AudioSource>().enabled = true;
            }

            if (disableRealGrass)
                ModManager.Instance.SendModMessage("Real Grass", "toggle", true);
        }

        private void MessageReceiver(string message, object data, DFModMessageCallback callBack)
        {
            switch (message)
            {
                case PAUSE_TRAVEL:
                    if (travelControlUI.isShowing)
                        travelControlUI.CloseWindow();
                    break;

                default:
                    Debug.LogErrorFormat("{0}: unknown message received ({1}).", this, message);
                    break;
            }
        }
    }
}
