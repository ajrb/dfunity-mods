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
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;

namespace TravelOptions
{
    public class TravelOptionsMod : MonoBehaviour
    {
        public const string PAUSE_TRAVEL = "pauseTravel";
        public const string IS_TRAVEL_ACTIVE = "isTravelActive";

        public const string ROADS_MODNAME = "BasicRoads";

        // Path type and direction constants from BasicRoadsTexturing.
        public const int path_roads = 0;
        public const int path_tracks = 1;
        public const int path_rivers = 2;
        public const int path_streams = 3;
        public const byte N = 128; //0b_1000_0000;
        public const byte NE = 64; //0b_0100_0000;
        public const byte E = 32;  //0b_0010_0000;
        public const byte SE = 16; //0b_0001_0000;
        public const byte S = 8;   //0b_0000_1000;
        public const byte SW = 4;  //0b_0000_0100;
        public const byte W = 2;   //0b_0000_0010;
        public const byte NW = 1;  //0b_0000_0001;

        private const int MPhalfWunits = 16384;
        const int TSize = 256;
        const int PSize = TSize * 2;
        const int MidLo = MPhalfWunits - TSize;
        const int MidHi = MPhalfWunits + TSize;
        const int SSize = 1024;
        const int SDSize = SSize * 2;
        const int SloDo = MPhalfWunits - SDSize;
        const float AngUnit = 22.5f; // 45/2
        const int SlowdownSpeed = 20;


        private const string MsgArrived = "You have arrived at your destination.";
        private const string MsgEnemies = "Enemies are seeking to prevent your travel...";
        private const string MsgAvoidFail = "You failed to avoid an encounter!";
        private const string MsgAvoidSuccess = "You successfully avoided an encounter.";
        private const string MsgLowHealth = "You are close to the point of death!";
        private const string MsgLowFatigue = "You are exhausted and should rest.";
        private const string MsgOcean = "You've found yourself in the sea, maybe you should travel on a ship.";
        private const string MsgNearLocation = "Paused the journey since a {0} called {1} is nearby.";
        private const string MsgEnterLocation = "Paused the journey as you've entered a {0} called {1}.";

        private const int LocPauseOff = 0;
        private const int LocPauseNear = 1;
        private const int LocPauseEnter = 2;

        public static TravelOptionsMod Instance { get; private set; }

        public string DestinationName { get; private set; }
        public ContentReader.MapSummary DestinationSummary { get; private set; }
        public bool DestinationCautious { get; private set; }

        public bool PathsTravel { get; private set; }
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
        //private Rect slowdownRect = Rect.zero;

        private bool enableWeather;
        private bool enableSounds;
        private bool enableRealGrass;
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

            Mod roadsMod = ModManager.Instance.GetMod(ROADS_MODNAME);
            bool roadsModEnabled = roadsMod != null && roadsMod.Enabled;

            ModSettings settings = mod.GetSettings();

            PathsTravel = settings.GetValue<bool>("GeneralOptions", "PathsTravel") && roadsModEnabled;
            enableWeather = settings.GetValue<bool>("GeneralOptions", "AllowWeather");
            enableSounds = settings.GetValue<bool>("GeneralOptions", "AllowAnnoyingSounds");
            enableRealGrass = settings.GetValue<bool>("GeneralOptions", "AllowRealGrass");
            locationPause = settings.GetValue<int>("GeneralOptions", "LocationPause");

            CautiousTravel = settings.GetValue<bool>("CautiousTravel", "PlayerControlledCautiousTravel");
            int speedPenalty = settings.GetValue<int>("CautiousTravel", "SpeedPenalty");
            CautiousTravelMultiplier = 1 - ((float)speedPenalty / 100);
            maxAvoidChance = settings.GetValue<int>("CautiousTravel", "MaxChanceToAvoidEncounter");
            CautiousHealthMinPc = settings.GetValue<int>("CautiousTravel", "HealthMinimumPercentage");
            CautiousFatigueMin = settings.GetValue<int>("CautiousTravel", "FatigueMinimumValue") + 1;

            StopAtInnsTravel = settings.GetValue<bool>("StopAtInnsTravel", "PlayerControlledInnsTravel");
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

        #region Path following

        void FollowPath(bool onPath = false)    // TODO - need the param?
        {
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
            DFPosition currMapPixel = playerGPS.CurrentMapPixel;
            byte pathsDataPt = GetPathsDataPoint(currMapPixel);

            if (pathsDataPt != 0 && !onPath)
            {
                DFPosition worldOrigin = MapsFile.MapPixelToWorldCoord(currMapPixel.X, currMapPixel.Y);
                DFPosition posInMp = new DFPosition(playerGPS.WorldX - worldOrigin.X, playerGPS.WorldZ - worldOrigin.Y);
                if ((pathsDataPt & N) != 0 && posInMp.X > MidLo && posInMp.X < MidHi && posInMp.Y > MidLo)
                    onPath = true;
                if ((pathsDataPt & E) != 0 && posInMp.Y > MidLo && posInMp.Y < MidHi && posInMp.X > MidLo)
                    onPath = true;
                if ((pathsDataPt & S) != 0 && posInMp.X > MidLo && posInMp.X < MidHi && posInMp.Y < MidHi)
                    onPath = true;
                if ((pathsDataPt & W) != 0 && posInMp.Y > MidLo && posInMp.Y < MidHi && posInMp.X < MidHi)
                    onPath = true;
                if ((pathsDataPt & NE) != 0 && (Mathf.Abs(posInMp.X - posInMp.Y) < PSize) && posInMp.X > MidLo)
                    onPath = true;
                if ((pathsDataPt & SW) != 0 && (Mathf.Abs(posInMp.X - posInMp.Y) < PSize) && posInMp.X < MidHi)
                    onPath = true;
                if ((pathsDataPt & NW) != 0 && (Mathf.Abs(posInMp.X - (32768 - posInMp.Y)) < PSize) && posInMp.X < MidHi)
                    onPath = true;
                if ((pathsDataPt & SE) != 0 && (Mathf.Abs(posInMp.X - (32768 - posInMp.Y)) < PSize) && posInMp.X > MidLo)
                    onPath = true;
            }
            if (onPath)
            {
                byte playerDirection = GetDirection(GetNormalisedPlayerYaw());
                Debug.Log("Following path in dir: " + playerDirection);
                if ((pathsDataPt & playerDirection) != 0)
                {
                    BeginTravel(GetTargetPixel(playerDirection, currMapPixel));
                }
                else
                {
                    byte fromDirection = GetDirection(GetNormalisedPlayerYaw(true));
                    if ((pathsDataPt & fromDirection) != 0)
                    {
                        BeginTravel(GetTargetPixel(0, currMapPixel));
                    }
                }
            }
        }

        private static byte GetPathsDataPoint(DFPosition currMapPixel)
        {
            int pathsIndex = currMapPixel.X + (currMapPixel.Y * MapsFile.MaxMapPixelX);
            byte pathsDataPt = TravelOptionsMapWindow.pathsData[path_roads][pathsIndex];
            pathsDataPt = (byte)(pathsDataPt | TravelOptionsMapWindow.pathsData[path_tracks][pathsIndex]);
            return pathsDataPt;
        }

        public void BeginTravel(DFPosition targetPixel, bool speedCautious = false)
        {
            if (targetPixel != null)
            {
                travelControlUI.SetDestination("Following a path.");
                Rect targetRect = new Rect(targetPixel.X + MidLo, targetPixel.Y + MidLo, PSize, PSize);
                //slowdownRect = new Rect(targetPixel.X + SloDo, targetPixel.Y + SloDo, SDSize, SDSize);
                DestinationCautious = speedCautious;
                if (alwaysUseStartingAccel)
                    travelControlUI.TimeAcceleration = defaultStartingAccel;

                playerAutopilot = new PlayerAutoPilot(targetRect, GetTravelSpeedMultiplier());
                playerAutopilot.OnArrival += SelectNextPath;
                SetTimeScale(travelControlUI.TimeAcceleration);
                DisableWeatherAndSound();
                diseaseCount = GameManager.Instance.PlayerEffectManager.DiseaseCount;
            }
        }

        DFPosition GetTargetPixel(byte direction, DFPosition currMapPixel)
        {
            switch (direction)
            {
                case N:
                    return MapsFile.MapPixelToWorldCoord(currMapPixel.X, currMapPixel.Y - 1);
                case NE:
                    return MapsFile.MapPixelToWorldCoord(currMapPixel.X + 1, currMapPixel.Y - 1);
                case E:
                    return MapsFile.MapPixelToWorldCoord(currMapPixel.X + 1, currMapPixel.Y);
                case SE:
                    return MapsFile.MapPixelToWorldCoord(currMapPixel.X + 1, currMapPixel.Y + 1);
                case S:
                    return MapsFile.MapPixelToWorldCoord(currMapPixel.X, currMapPixel.Y + 1);
                case SW:
                    return MapsFile.MapPixelToWorldCoord(currMapPixel.X - 1, currMapPixel.Y + 1);
                case W:
                    return MapsFile.MapPixelToWorldCoord(currMapPixel.X - 1, currMapPixel.Y);
                case NW:
                    return MapsFile.MapPixelToWorldCoord(currMapPixel.X - 1, currMapPixel.Y - 1);
                default:
                    return MapsFile.MapPixelToWorldCoord(currMapPixel.X, currMapPixel.Y);
            }
        }

        void SelectNextPath()
        {
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
            DFPosition currMapPixel = playerGPS.CurrentMapPixel;

            byte pathsDataPt = GetPathsDataPoint(currMapPixel);
            byte playerDirection = GetDirection(GetNormalisedPlayerYaw());
            if (CountSetBits(pathsDataPt) == 2)
            {
                playerDirection = (byte)(pathsDataPt & playerDirection);
                if (playerDirection == 0)
                {
                    byte fromDirection = GetDirection(GetNormalisedPlayerYaw(true));
                    playerDirection = (byte)(pathsDataPt ^ fromDirection);
                    Debug.Log("Changing to dir: " + playerDirection);
                }
                Debug.Log("Heading in dir: " + playerDirection);
                BeginTravel(GetTargetPixel(playerDirection, currMapPixel));
                return;
            }
            // An intersection or path end, close travel
            //slowdownRect = Rect.zero;
            travelControlUI.CloseWindow();
        }

        float GetNormalisedPlayerYaw(bool invert = false)
        {
            int inv = invert ? 180 : 0;
            float yaw = (GameManager.Instance.PlayerMouseLook.Yaw + inv) % 360;
            if (yaw < 0)
                yaw += 360;
            return yaw;
        }

        /*
        byte GetNewDirection(byte direction, byte pathDataPt)
        {
            byte left = direction;
            byte right = direction;
            byte left = (byte)(direction << 1);
            if (left == 0) left = NW;
            if ((left & pathDataPt) != 0) return left;

            byte right = (byte)(direction >> 1);
            if (right == 0) right = N;
            if ((right & pathDataPt) != 0)
                return right;
        }
*/
        byte GetDirection(float yaw)
        {
            if ((yaw >= 360 - AngUnit && yaw <= 360) || (yaw >= 0 && yaw <= AngUnit))
                return N;
            if (yaw >= 90 - AngUnit && yaw <= 90 + AngUnit)
                return E;
            if (yaw >= 180 - AngUnit && yaw <= 180 + AngUnit)
                return S;
            if (yaw >= 270 - AngUnit && yaw <= 270 + AngUnit)
                return W;
            if (yaw > 45 - AngUnit && yaw < 45 + AngUnit)
                return NE;
            if (yaw > 135 - AngUnit && yaw < 135 + AngUnit)
                return SE;
            if (yaw > 225 - AngUnit && yaw < 225 + AngUnit)
                return SW;
            if (yaw > 295 - AngUnit && yaw < 295 + AngUnit)
                return NW;
            return 0;
        }

        static int CountSetBits(byte n)
        {
            int count = 0;
            while (n > 0)
            {
                count += n & 1;
                n >>= 1;
            }
            return count;
        }

        #endregion

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
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
            //if (slowdownRect != Rect.zero && Time.timeScale > SlowdownSpeed && slowdownRect.Contains(new Vector2(playerGPS.WorldX, playerGPS.WorldZ)))
            //    SetTimeScale(SlowdownSpeed);

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
                        StopTravelWithMessage(MsgLowHealth);
                        return;
                    }
                    if (GameManager.Instance.PlayerEntity.CurrentFatigue < DaggerfallEntity.FatigueMultiplier * CautiousFatigueMin)
                    {
                        StopTravelWithMessage(MsgLowFatigue);
                        return;
                    }
                }

                // If location pause set to nearby, check for a near location
                if (locationPause == LocPauseNear && playerGPS.HasCurrentLocation && !playerGPS.CurrentLocation.Equals(lastLocation))
                {
                    lastLocation = playerGPS.CurrentLocation;
                    StopTravelWithMessage(string.Format(MsgNearLocation, MacroHelper.LocationTypeName(), playerGPS.CurrentLocation.Name));
                    return;
                }

                // Check for ocean climate.
                if (playerGPS.CurrentClimateIndex == (int)MapsFile.Climates.Ocean)
                {
                    StopTravelWithMessage(MsgOcean);
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
            else if (PathsTravel && InputManager.Instance.GetKey(KeyCode.F))
            {
                FollowPath();
            }
        }

        private void StopTravelWithMessage(string message)
        {
            if (travelControlUI.isShowing)
                travelControlUI.CloseWindow();
            DaggerfallUI.MessageBox(message);
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
            if (!enableWeather)
            {
                var playerWeather = GameManager.Instance.WeatherManager.PlayerWeather;
                playerWeather.RainParticles.SetActive(false);
                playerWeather.SnowParticles.SetActive(false);
                playerWeather.enabled = false;
            }

            if (!enableSounds)
            {
                GameManager.Instance.PlayerActivate.GetComponentInParent<PlayerFootsteps>().enabled = false;
                GameManager.Instance.TransportManager.GetComponent<AudioSource>().enabled = false;
            }

            if (!enableRealGrass)
                ModManager.Instance.SendModMessage("Real Grass", "toggle", false);
        }

        private void EnableWeatherAndSound()
        {
            if (!enableWeather)
                GameManager.Instance.WeatherManager.PlayerWeather.enabled = true;

            if (!enableSounds)
            {
                GameManager.Instance.PlayerActivate.GetComponentInParent<PlayerFootsteps>().enabled = true;
                GameManager.Instance.TransportManager.GetComponent<AudioSource>().enabled = true;
            }

            if (!enableRealGrass)
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

                case IS_TRAVEL_ACTIVE:
                    callBack?.Invoke(IS_TRAVEL_ACTIVE, travelControlUI.isShowing);
                    break;

                default:
                    Debug.LogErrorFormat("{0}: unknown message received ({1}).", this, message);
                    break;
            }
        }
    }
}
