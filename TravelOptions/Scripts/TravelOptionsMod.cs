// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut
// Contributor:     Jedidia

using System;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Guilds;

namespace TravelOptions
{
    public class TravelOptionsMod : MonoBehaviour
    {
        public const string PAUSE_TRAVEL = "pauseTravel";
        public const string NOUISTOP_WINDOW = "noStopForUIWindow";
        public const string NOUISTOP_REMOVE = "removeNoStopWindow";
        public const string IS_TRAVEL_ACTIVE = "isTravelActive";
        public const string IS_PATH_FOLLOWING = "isPathFollowing";
        public const string IS_FOLLOWING_ROAD = "isFollowingRoad";
        public const string SHOW_MESSAGE = "showMessage";
        public const string HAS_PORT = "hasPort";

        public const string ROADS_MODNAME = "BasicRoads";

        public const int MaxCircumnavigationAccel = 15;

        protected const string MsgArrived = "You have arrived at your destination.";
        protected const string MsgEnemies = "Enemies are seeking to prevent your travel...";
        protected const string MsgAvoidFail = "You failed to avoid an encounter!";
        protected const string MsgAvoidSuccess = "You successfully avoided an encounter.";
        protected const string MsgLowHealth = "You are close to the point of death!";
        protected const string MsgLowFatigue = "You are exhausted and should rest.";
        protected const string MsgOcean = "You've found yourself in the sea, maybe you should travel on a ship.";
        protected const string MsgNearLocation = "Paused the journey since a {0} called {1} is nearby.";
        protected const string MsgEnterLocation = "Paused the journey as you've entered a {0} called {1}.";
        protected const string MsgCircumnavigate = "Circumnavigating {0}.";
        protected const string MsgNoPath = "There's no path here to follow in that direction.";
        protected const string MsgFollowRoad = "Following a road.";
        protected const string MsgFollowTrack = "Following a dirt track.";
        protected const string MsgTargetCoords = "Map coordinates: {0}, {1}.";
        protected const string MsgNewRegion = "You have entered the region of {0}.";

        // Path type and direction constants copied from BasicRoadsTexturing
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

        // Map pixel world unit constants
        const int MPworldUnits = 32768;
        const int HalfMPworldUnits = MPworldUnits / 2;
        const int TSize = MPworldUnits / MapsFile.WorldMapTileDim;  // Tile size
        const int PSize = TSize * 2;                                // Path size
        const int MidLo = HalfMPworldUnits - TSize;
        const int MidHi = HalfMPworldUnits + TSize;
        const float AngUnit = 22.5f; // = 45 / 2

        // Location pause mode enum-alike
        const int LocPauseOff = 0;
        const int LocPauseNear = 1;
        const int LocPauseEnter = 2;

        public static TravelOptionsMod Instance { get; private set; }

        public string DestinationName { get; private set; }
        public bool DestinationCautious { get; private set; }
        public ContentReader.MapSummary DestinationSummary { get; private set; }

        public bool CautiousTravel { get; private set; }
        public bool StopAtInnsTravel { get; private set; }
        public bool ShipTravelPortsOnly { get; private set; }
        public bool ShipTravelDestinationPortsOnly { get; private set; }
        public bool RoadsIntegration { get; private set; }
        public bool VariableSizeDots { get; private set; }
        public bool WaterwaysEnabled { get; private set; }
        public bool StreamsToggle { get; private set; }
        public bool TargetCoordsAllowed { get; private set; }

        public float RecklessTravelMultiplier { get; private set; } = 1f;
        public float CautiousTravelMultiplier { get; private set; } = 0.8f;
        public float GetTravelSpeedMultiplier() { return DestinationCautious ? CautiousTravelMultiplier : RecklessTravelMultiplier; }
        public int CautiousHealthMinPc { get; private set; } = 5;
        public int CautiousFatigueMin { get; private set; } = 6;

        public bool TeleportCost { get; private set; }

        static readonly int[] startAccelVals = { 1, 2, 3, 5, 10, 15, 20, 25, 30, 40, 50 };
        static readonly KeyCode[] followKeys = { KeyCode.None, KeyCode.F, KeyCode.G, KeyCode.K, KeyCode.O, KeyCode.X };

        static Mod mod;

        PlayerAutoPilot playerAutopilot;
        TravelControlUI travelControlUI;
        internal TravelControlUI GetTravelControlUI() { return travelControlUI; }
        IUserInterfaceWindow noStopForUIWindow;

        KeyCode followKeyCode = KeyCode.None;
        DFLocation lastLocation;
        Rect locationRect = Rect.zero;
        Rect locationBorderRect = Rect.zero;
        Rect locBorderNERect = Rect.zero;
        Rect locBorderSERect = Rect.zero;
        Rect locBorderSWRect = Rect.zero;
        Rect locBorderNWRect = Rect.zero;

        bool enableWeather;
        bool enableSounds;
        bool enableRealGrass;
        int locationPause;
        int defaultStartingAccel;
        bool alwaysUseStartingAccel;
        int accelerationLimit;

        float baseFixedDeltaTime;
        int maxAvoidChance;
        bool ignoreEncounters;
        uint ignoreEncountersTime;
        int diseaseCount = 0;
        uint beginTime = 0;
        byte circumnavigatePathsDataPt = 0;
        byte lastCrossed = 0;
        bool road = false;
        float ridingVolume = 0.6f;
        bool uiCloseWhenTop = false;

        // Junction map variables
        bool roadsJunctionMap = false;
        bool persistentJunctionMap = false;
        bool junctionMapCircular = true;
        const int junctionMapWidth = 20;
        const int junctionMapHeight = 20;
        const int junctionMapWidthD2 = junctionMapWidth / 2;
        const int junctionMapHeightD2 = junctionMapHeight / 2;
        const int junctionMapWidthX5 = junctionMapWidth * 5;
        const int herePt = (junctionMapHeightD2 * junctionMapWidth * 25) - (3 * junctionMapWidth * 5) + (junctionMapWidthD2 * 5) + 2;
        static readonly FilterMode[] filterModes = { FilterMode.Point, FilterMode.Bilinear, FilterMode.Trilinear };
        static Color32 playerColor = Color.red;

        Rect junctionMapPanelRect = new Rect(107.5f, 47.5f, 100, 100);
        Panel junctionMapPanel;
        Texture2D junctionMapTexture;
        Color32[] junctionMapPixelBuffer = new Color32[junctionMapWidth * junctionMapHeight * 25];
        byte lastPlayerFacing = 0;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            Instance = go.AddComponent<TravelOptionsMod>();

            mod.LoadSettingsCallback = Instance.LoadSettings;
        }

        // Load dynamic settings that can be changed at runtime.
        void LoadSettings(ModSettings settings, ModSettingsChange change)
        {
            TargetCoordsAllowed = settings.GetValue<bool>("GeneralOptions", "AllowTargetingMapCoordinates");
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

            if (RoadsIntegration)
            {
                followKeyCode = followKeys[settings.GetValue<int>("RoadsIntegration", "FollowPathsKey")];

                if (roadsJunctionMap)
                {
                    persistentJunctionMap = settings.GetValue<bool>("RoadsJunctionMap", "PersistentMap");

                    if (junctionMapTexture != null)
                        junctionMapTexture.filterMode = filterModes[settings.GetValue<int>("RoadsJunctionMap", "FilterMode")];

                    junctionMapCircular = settings.GetValue<bool>("RoadsJunctionMap", "Circular");
                    playerColor = settings.GetValue<Color32>("RoadsJunctionMap", "PlayerColor");
                }
            }
        }

        void Awake()
        {
            Debug.Log("Begin mod init: TravelOptions");

            Mod roadsMod = ModManager.Instance.GetMod(ROADS_MODNAME);
            bool roadsModEnabled = roadsMod != null && roadsMod.Enabled;

            // Load non-dynamic settings.
            ModSettings settings = mod.GetSettings();

            RoadsIntegration = settings.GetValue<bool>("RoadsIntegration", "Enable") && roadsModEnabled;
            if (RoadsIntegration) {
                VariableSizeDots = settings.GetValue<bool>("RoadsIntegration", "VariableSizeDots");
                roadsJunctionMap = settings.GetValue<bool>("RoadsJunctionMap", "Enable");
                WaterwaysEnabled = settings.GetValue<bool>("RoadsIntegration", "EnableWaterways") && roadsModEnabled;
                StreamsToggle = settings.GetValue<bool>("RoadsIntegration", "EnableStreamsToggle") && roadsModEnabled;
            }

            // Load initial dynamic settings.
            LoadSettings(settings, new ModSettingsChange());

            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.TravelMap, typeof(TravelOptionsMapWindow));
            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.TravelPopUp, typeof(TravelOptionsPopUp));

            baseFixedDeltaTime = Time.fixedDeltaTime;

            // Allow teleportation for anyone if enabled
            if (settings.GetValue<bool>("Teleportation", "EnablePaidTeleportation"))
            {
                if (!GuildManager.RegisterCustomGuild(FactionFile.GuildGroups.MagesGuild, typeof(MagesGuildTO)))
                    throw new Exception("GuildGroup MagesGuild is already overridden, unable to register MagesGuildTO guild class.");
                TeleportCost = true;
            }

            GameManager.Instance.EntityEffectBroker.RegisterEffectTemplate(new CastWhenHeldTO(), true);

            mod.MessageReceiver = MessageReceiver;
            mod.IsReady = true;

            Debug.Log("Finished mod init: TravelOptions");
        }

        void Start()
        {
            // Setup Panel and Texture for roads map
            if (roadsJunctionMap)
            {
                ModSettings settings = mod.GetSettings();
                int size = settings.GetValue<int>("RoadsJunctionMap", "ScreenSize");
                int x = settings.GetValue<int>("RoadsJunctionMap", "ScreenPositionX");
                int y = settings.GetValue<int>("RoadsJunctionMap", "ScreenPositionY");
                junctionMapPanelRect = new Rect(x, y, size, size);

                DaggerfallHUD hud = DaggerfallUI.Instance.DaggerfallHUD;
                junctionMapPanel = DaggerfallUI.AddPanel(junctionMapPanelRect, hud.NativePanel);
                junctionMapPanel.Enabled = false;
                junctionMapTexture = new Texture2D(junctionMapWidth * 5, junctionMapHeight * 5, TextureFormat.ARGB32, false);

                junctionMapTexture.filterMode = filterModes[settings.GetValue<int>("RoadsJunctionMap", "FilterMode")];

                junctionMapCircular = settings.GetValue<bool>("RoadsJunctionMap", "Circular");
                playerColor = settings.GetValue<Color32>("RoadsJunctionMap", "PlayerColor");

                if (settings.GetValue<bool>("RoadsJunctionMap", "Opaque"))
                    junctionMapPanel.BackgroundColor = settings.GetValue<Color32>("RoadsJunctionMap", "BackgroundColor");
            }

            // Setup travel control UI
            travelControlUI = new TravelControlUI(DaggerfallUI.UIManager, defaultStartingAccel, accelerationLimit, junctionMapPanel);
            travelControlUI.OnCancel += (sender) => { ClearTravelDestination(); };
            travelControlUI.OnClose += () => { InterruptTravel(); };
            travelControlUI.OnTimeAccelerationChanged += (timeAcceleration) => { SetTimeScale(timeAcceleration); };

            // Clear destination on new game or load game.
            SaveLoadManager.OnLoad += (saveData) => { ClearTravelDestination(); };
            StartGameBehaviour.OnNewGame += () => { ClearTravelDestination(); };
            GameManager.OnEncounter += GameManager_OnEncounter;
            PlayerGPS.OnEnterLocationRect += PlayerGPS_OnEnterLocationRect;
            PlayerGPS.OnMapPixelChanged += PlayerGPS_OnMapPixelChanged;
            PlayerGPS.OnRegionIndexChanged += PlayerGPS_OnRegionIndexChanged;
            StreamingWorld.OnUpdateLocationGameObject += StreamingWorld_OnUpdateLocationGameObject;
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
            playerAutopilot = null;
            if (travelControlUI.isShowing)
                travelControlUI.CloseWindow();
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
            if (!string.IsNullOrEmpty(DestinationName) && locationPause == LocPauseEnter)
            {
                if (travelControlUI.isShowing)
                    travelControlUI.CloseWindow();
                DaggerfallUI.MessageBox(string.Format(MsgEnterLocation, MacroHelper.LocationTypeName(), dfLocation.Name));
            }
        }

        private void PlayerGPS_OnMapPixelChanged(DFPosition mapPixel)
        {
            DisableJunctionMap();
            InitLocationRects(mapPixel);
        }

        // Check for entering a different region.
        private void PlayerGPS_OnRegionIndexChanged(int regionIndex)
        {
            if (travelControlUI != null && travelControlUI.isShowing)
                travelControlUI.ShowMessage(string.Format(MsgNewRegion, GameManager.Instance.PlayerGPS.CurrentRegionName));
        }

        private void StreamingWorld_OnUpdateLocationGameObject(GameObject locationObject, bool allowYield)
        {
            if (allowYield == false && locationBorderRect.Equals(Rect.zero) && locationRect.Equals(Rect.zero))
            {
                DFPosition mapPixel = GameManager.Instance.PlayerGPS.CurrentMapPixel;
                InitLocationRects(mapPixel);
            }
        }

        private void InitLocationRects(DFPosition mapPixel)
        {
            if (RoadsIntegration && (playerAutopilot == null || DestinationName != null))
            {
                DFPosition worldOriginMP = MapsFile.MapPixelToWorldCoord(mapPixel.X, mapPixel.Y);
                SetLocationRects(mapPixel, worldOriginMP);
            }
        }

        // Begin travel to a location
        public void BeginTravel(ContentReader.MapSummary destinationSummary, bool speedCautious = false)
        {
            DFLocation targetLocation;
            if (DaggerfallUnity.Instance.ContentReader.GetLocation(destinationSummary.RegionIndex, destinationSummary.MapIndex, out targetLocation))
            {
                DestinationName = targetLocation.Name;
                travelControlUI.SetDestinationName(targetLocation.Name);
                DestinationSummary = destinationSummary;
                DestinationCautious = speedCautious;
                if (alwaysUseStartingAccel)
                    travelControlUI.TimeAcceleration = defaultStartingAccel;
                travelControlUI.HalfLimit = false;
                BeginTravel();
                beginTime = DaggerfallUnity.Instance.WorldTime.Now.ToClassicDaggerfallTime();
            }
            else throw new Exception("TravelOptions: destination not found!");
        }

        // Begin travel to configured location, used to continue journeys
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
                InitTravelUI();

                Debug.Log("Begun accelerated travel to " + DestinationName);
            }
        }

        // Begin travel to map pixel coordinates
        public void BeginTravelToCoords(DFPosition targetPixel, bool speedCautious = false)
        {
            string targetName = string.Format(MsgTargetCoords, targetPixel.X, targetPixel.Y);
            travelControlUI.SetDestinationName(targetName);
            DestinationCautious = speedCautious;
            if (alwaysUseStartingAccel)
                travelControlUI.TimeAcceleration = defaultStartingAccel;
            travelControlUI.HalfLimit = false;

            DFPosition targetMPworld = MapsFile.MapPixelToWorldCoord(targetPixel.X, targetPixel.Y);
            Rect targetRect = new Rect(targetMPworld.X + MidLo, targetMPworld.Y + MidLo, PSize, PSize);
            playerAutopilot = new PlayerAutoPilot(targetPixel, targetRect, road ? RecklessTravelMultiplier : CautiousTravelMultiplier);
            playerAutopilot.OnArrival += () =>
            {
                travelControlUI.CloseWindow();
                ClearTravelDestination();
                DaggerfallUI.MessageBox(MsgArrived);
            };

            lastLocation = GameManager.Instance.PlayerGPS.CurrentLocation;
            InitTravelUI();

            Debug.Log("Begun accelerated travel to " + targetName);
        }

        private void InitTravelUI(bool circumnavSpeedLimiter = false)
        {
            DisableJunctionMap(true);

            if (circumnavSpeedLimiter && travelControlUI.TimeAcceleration > MaxCircumnavigationAccel)
                SetTimeScale(MaxCircumnavigationAccel);
            else
                SetTimeScale(travelControlUI.TimeAcceleration);
            DisableWeatherAndSound();
            diseaseCount = GameManager.Instance.PlayerEffectManager.DiseaseCount;
            if (!travelControlUI.isShowing)
                DaggerfallUI.UIManager.PushWindow(travelControlUI);
        }

        #region Path following

        // Sets up rects for location area and border ready for circumnavigation
        protected bool SetLocationRects(DFPosition targetPixel, DFPosition targetMPworld)
        {
            GameObject terrainObject = GameManager.Instance.StreamingWorld.GetTerrainFromPixel(targetPixel);
            if (terrainObject)
            {
                DaggerfallTerrain dfTerrain = terrainObject.GetComponent<DaggerfallTerrain>();
                if (dfTerrain && dfTerrain.MapData.hasLocation)
                {
                    float locBorder = 1;
                    DFLocation location = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(dfTerrain.MapData.mapRegionIndex, dfTerrain.MapData.mapLocationIndex);
                    if (location.Loaded && location.MapTableData.LocationType == DFRegion.LocationTypes.TownCity)
                        locBorder = 1.5f;

                    Rect locationTileRect = dfTerrain.MapData.locationRect;
                    locationTileRect.xMin += 1;
                    locationTileRect.yMin += 1;
                    locationBorderRect.Set(targetMPworld.X + (locationTileRect.x * TSize), targetMPworld.Y + (locationTileRect.y * TSize), locationTileRect.width * TSize, locationTileRect.height * TSize);

                    locationTileRect.xMin += locBorder;
                    locationTileRect.xMax -= locBorder;
                    locationTileRect.yMin += locBorder;
                    locationTileRect.yMax -= locBorder;
                    locationRect.Set(targetMPworld.X + (locationTileRect.x * TSize), targetMPworld.Y + (locationTileRect.y * TSize), locationTileRect.width * TSize, locationTileRect.height * TSize);

                    return !location.HasCustomLocationPosition();   // Only aim for location center if it's centered in the map pixel
                }
            }
            locationBorderRect.Set(0, 0, 0, 0);
            locationRect.Set(0, 0, 0, 0);
            return false;
        }

        byte IsPlayerOnPath(PlayerGPS playerGPS, byte pathsDataPt)
        {
            if (pathsDataPt == 0)
                return 0;

            byte onPath = 0;
            DFPosition worldOriginMP = MapsFile.MapPixelToWorldCoord(playerGPS.CurrentMapPixel.X, playerGPS.CurrentMapPixel.Y);
            DFPosition posInMp = new DFPosition(playerGPS.WorldX - worldOriginMP.X, playerGPS.WorldZ - worldOriginMP.Y);
            if ((pathsDataPt & N) != 0 && posInMp.X > MidLo && posInMp.X < MidHi && posInMp.Y > MidLo)
                onPath = (byte)(onPath | N);
            if ((pathsDataPt & E) != 0 && posInMp.Y > MidLo && posInMp.Y < MidHi && posInMp.X > MidLo)
                onPath = (byte)(onPath | E);
            if ((pathsDataPt & S) != 0 && posInMp.X > MidLo && posInMp.X < MidHi && posInMp.Y < MidHi)
                onPath = (byte)(onPath | S);
            if ((pathsDataPt & W) != 0 && posInMp.Y > MidLo && posInMp.Y < MidHi && posInMp.X < MidHi)
                onPath = (byte)(onPath | W);
            if ((pathsDataPt & NE) != 0 && (Mathf.Abs(posInMp.X - posInMp.Y) < PSize) && posInMp.X > MidLo)
                onPath = (byte)(onPath | NE);
            if ((pathsDataPt & SW) != 0 && (Mathf.Abs(posInMp.X - posInMp.Y) < PSize) && posInMp.X < MidHi)
                onPath = (byte)(onPath | SW);
            if ((pathsDataPt & NW) != 0 && (Mathf.Abs(posInMp.X - (MPworldUnits - posInMp.Y)) < PSize) && posInMp.X < MidHi)
                onPath = (byte)(onPath | NW);
            if ((pathsDataPt & SE) != 0 && (Mathf.Abs(posInMp.X - (MPworldUnits - posInMp.Y)) < PSize) && posInMp.X > MidLo)
                onPath = (byte)(onPath | SE);

            return onPath;
        }

        protected void FollowPath()
        {
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
            DFPosition currMapPixel = playerGPS.CurrentMapPixel;

            bool inLoc = locationRect.Contains(new Vector2(playerGPS.WorldX, playerGPS.WorldZ));
            
            // Start following a path
            byte pathsDataPt = GetPathsDataPoint(currMapPixel);
            byte onPath = IsPlayerOnPath(playerGPS, pathsDataPt);

            if (onPath != 0)
            {
                byte playerDirection = GetDirection(GetNormalisedPlayerYaw());
                byte roadDataPt = GetRoadsDataPoint(currMapPixel);
#if UNITY_EDITOR
                Debug.LogFormat("Begun following path {0}", GetDirectionStr(playerDirection));
#endif
                if ((inLoc && (pathsDataPt & playerDirection) != 0) || (pathsDataPt & playerDirection & onPath) != 0)
                {
                    road = (roadDataPt & playerDirection) != 0;
                    DestinationName = null;     // Remove any specified destination
                    BeginPathTravel(GetTargetPixel(playerDirection, currMapPixel));
                    return;
                }
                else
                {
                    byte fromDirection = GetDirection(GetNormalisedPlayerYaw(true));
                    if ((inLoc && (pathsDataPt & fromDirection) != 0) || (pathsDataPt & fromDirection & onPath) != 0)
                    {
                        road = (roadDataPt & fromDirection) != 0;
                        DestinationName = null;     // Remove any specified destination
                        BeginPathTravel(GetTargetPixel(0, currMapPixel));
                        return;
                    }
                }
            }
            if (!inLoc && locationBorderRect.Contains(new Vector2(playerGPS.WorldX, playerGPS.WorldZ)))
            {
                // Player in location border, initiate location circumnavigation
                SetupLocBorderCornerRects();
                CircumnavigateLocation();
                return;
            }

            DaggerfallUI.AddHUDText(MsgNoPath);
        }

        protected void BeginPathTravel(DFPosition targetPixel, bool starting = true)
        {
            if (targetPixel != null)
            {
                lastCrossed = 0;
                travelControlUI.SetDestinationName(road ? MsgFollowRoad : MsgFollowTrack);
                DFPosition targetMPworld = MapsFile.MapPixelToWorldCoord(targetPixel.X, targetPixel.Y);

                Rect targetRect = SetLocationRects(targetPixel, targetMPworld) ? locationBorderRect : new Rect(targetMPworld.X + MidLo, targetMPworld.Y + MidLo, PSize, PSize);

                DestinationCautious = true;
                if (starting && alwaysUseStartingAccel)
                    travelControlUI.TimeAcceleration = defaultStartingAccel;
                travelControlUI.HalfLimit = true;

                if (playerAutopilot == null)
                {
                    playerAutopilot = new PlayerAutoPilot(targetPixel, targetRect, road ? RecklessTravelMultiplier : CautiousTravelMultiplier);
                    playerAutopilot.OnArrival += SelectNextPath;
                }
                else
                {
                    playerAutopilot.InitTargetRect(targetPixel, targetRect, road ? RecklessTravelMultiplier : CautiousTravelMultiplier);
                }
                InitTravelUI();

                if (roadsJunctionMap && persistentJunctionMap)
                {
                    DrawJunctionMap(GameManager.Instance.PlayerGPS.CurrentMapPixel);
                    junctionMapPanel.Enabled = true;
                }
            }
        }

        void SelectNextPath()
        {
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
            if (!playerGPS.HasCurrentLocation)
            {
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
                    }
#if UNITY_EDITOR
                    Debug.LogFormat("Heading {0}", GetDirectionStr(playerDirection));
#endif
                    byte roadDataPt = GetRoadsDataPoint(currMapPixel);
                    road = (roadDataPt & playerDirection) != 0;
                    BeginPathTravel(GetTargetPixel(playerDirection, currMapPixel), false);
                    return;
                }
                else
                {
                    DaggerfallUI.AddHUDText("You've arrived at a junction.");
                    if (roadsJunctionMap)
                    {
                        Debug.Log("Displaying junction map on HUD.");
                        DrawJunctionMap(currMapPixel);
                        junctionMapPanel.Enabled = true;
                    }
                }
            }
            else
            {
                lastCrossed = GetDirection(GetNormalisedPlayerYaw(true));
            }
            // An intersection, location, or path end then end travel
            travelControlUI.CloseWindow();
        }

        protected void CircumnavigateLocation()
        {
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
            DFPosition currMapPixel = playerGPS.CurrentMapPixel;
            if (circumnavigatePathsDataPt == 0)
                circumnavigatePathsDataPt = GetPathsDataPoint(currMapPixel);
            int yaw = (int)GetNormalisedPlayerYaw();

            Rect targetRect;
            Vector2 worldPos = new Vector2(playerGPS.WorldX, playerGPS.WorldZ);
            if (locBorderNERect.Contains(worldPos))                                                         // NE Corner
                targetRect = (yaw > 45 && yaw < 225) ? locBorderSERect : locBorderNWRect;
            else if (locBorderSERect.Contains(worldPos))                                                    // SE Corner
                targetRect = (yaw > 135 && yaw < 315) ? locBorderSWRect : locBorderNERect;
            else if (locBorderSWRect.Contains(worldPos))                                                    // SW Corner
                targetRect = (yaw > 45 && yaw < 225) ? locBorderSERect : locBorderNWRect;
            else if (locBorderNWRect.Contains(worldPos))                                                    // NW Corner
                targetRect = (yaw > 135 && yaw < 315) ? locBorderSWRect : locBorderNERect;

            else if (playerGPS.WorldZ > locationRect.yMax && playerGPS.WorldZ < locationBorderRect.yMax)    // North edge
                targetRect = (yaw > 180 && yaw < 360) ? locBorderNWRect : locBorderNERect;
            else if (playerGPS.WorldZ > locationBorderRect.yMin && playerGPS.WorldZ < locationRect.yMin)    // South edge
                targetRect = (yaw > 180 && yaw < 360) ? locBorderSWRect : locBorderSERect;
            else if (playerGPS.WorldX > locationRect.xMax && playerGPS.WorldX < locationBorderRect.xMax)    // East edge
                targetRect = (yaw > 90 && yaw < 270) ? locBorderSERect : locBorderNERect;
            else if (playerGPS.WorldX > locationBorderRect.xMin && playerGPS.WorldX < locationRect.xMin)    // West edge
                targetRect = (yaw > 90 && yaw < 270) ? locBorderSWRect : locBorderNWRect;
            else
                return;

            travelControlUI.SetDestinationName(string.Format(MsgCircumnavigate, GameManager.Instance.PlayerGPS.CurrentLocation.Name));

            DestinationCautious = false;
            if (alwaysUseStartingAccel)
                travelControlUI.TimeAcceleration = defaultStartingAccel;
            travelControlUI.HalfLimit = true;

            playerAutopilot = new PlayerAutoPilot(currMapPixel, targetRect, GetTravelSpeedMultiplier());
            playerAutopilot.OnArrival += () => { CircumnavigateLocation(); };

            InitTravelUI(true);

            if (roadsJunctionMap && persistentJunctionMap)
            {
                DrawJunctionMap(currMapPixel, GetDirection(GetNormalisedPlayerYaw()));
                junctionMapPanel.Enabled = true;
            }
        }

        void SetupLocBorderCornerRects()
        {
            locBorderNERect = Rect.MinMaxRect(locationRect.xMax, locationRect.yMax, locationBorderRect.xMax, locationBorderRect.yMax);
            locBorderSERect = Rect.MinMaxRect(locationRect.xMax, locationBorderRect.yMin, locationBorderRect.xMax, locationRect.yMin);
            locBorderSWRect = Rect.MinMaxRect(locationBorderRect.xMin, locationBorderRect.yMin, locationRect.xMin, locationRect.yMin);
            locBorderNWRect = Rect.MinMaxRect(locationBorderRect.xMin, locationRect.yMax, locationRect.xMin, locationBorderRect.yMax);
        }

        static byte GetPathsDataPoint(DFPosition currMapPixel)
        {
            int pathsIndex = currMapPixel.X + (currMapPixel.Y * MapsFile.MaxMapPixelX);
            byte pathsDataPt = TravelOptionsMapWindow.pathsData[path_roads][pathsIndex];
            pathsDataPt = (byte)(pathsDataPt | TravelOptionsMapWindow.pathsData[path_tracks][pathsIndex]);
            return pathsDataPt;
        }
        static byte GetRoadsDataPoint(DFPosition currMapPixel)
        {
            int pathsIndex = currMapPixel.X + (currMapPixel.Y * MapsFile.MaxMapPixelX);
            return TravelOptionsMapWindow.pathsData[path_roads][pathsIndex];
        }

        DFPosition GetTargetPixel(byte direction, DFPosition currMapPixel)
        {
            switch (direction)
            {
                case N:
                    return new DFPosition(currMapPixel.X, currMapPixel.Y - 1);
                case NE:
                    return new DFPosition(currMapPixel.X + 1, currMapPixel.Y - 1);
                case E:
                    return new DFPosition(currMapPixel.X + 1, currMapPixel.Y);
                case SE:
                    return new DFPosition(currMapPixel.X + 1, currMapPixel.Y + 1);
                case S:
                    return new DFPosition(currMapPixel.X, currMapPixel.Y + 1);
                case SW:
                    return new DFPosition(currMapPixel.X - 1, currMapPixel.Y + 1);
                case W:
                    return new DFPosition(currMapPixel.X - 1, currMapPixel.Y);
                case NW:
                    return new DFPosition(currMapPixel.X - 1, currMapPixel.Y - 1);
                default:
                    return new DFPosition(currMapPixel.X, currMapPixel.Y);
            }
        }

        float GetNormalisedPlayerYaw(bool invert = false)
        {
            int inv = invert ? 180 : 0;
            float yaw = (GameManager.Instance.PlayerMouseLook.Yaw + inv) % 360;
            if (yaw < 0)
                yaw += 360;
            return yaw;
        }

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
            if (yaw > 315 - AngUnit && yaw < 315 + AngUnit)
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

        string GetDirectionStr(byte direction)
        {
            switch (direction)
            {
                case N:
                    return "N";
                case NE:
                    return "NE";
                case E:
                    return "E";
                case SE:
                    return "SE";
                case S:
                    return "S";
                case SW:
                    return "SW";
                case W:
                    return "W";
                case NW:
                    return "NW";
                default:
                    return "none";
            }
        }

        private void DrawJunctionMap(DFPosition currMapPixel, byte playerDirection = 0)
        {
            TravelOptionsMapWindow travelMapWindow = (TravelOptionsMapWindow)DaggerfallUI.Instance.DfTravelMapWindow;
            if (!travelMapWindow.IsSetup)
                travelMapWindow.Update();

            int originX = currMapPixel.X - junctionMapWidthD2;
            int originY = currMapPixel.Y - junctionMapHeightD2;
            travelMapWindow.DrawMapSection(originX, originY, junctionMapWidth, junctionMapHeight, ref junctionMapPixelBuffer, junctionMapCircular);

            junctionMapPixelBuffer[herePt] = playerColor;
            if (playerDirection == 0)
                playerDirection = GetDirection(GetNormalisedPlayerYaw());
            junctionMapPixelBuffer[GetDirectionIndex(playerDirection)] = playerColor;

            junctionMapTexture.SetPixels32(junctionMapPixelBuffer);
            junctionMapTexture.Apply();

            junctionMapPanel.BackgroundTexture = junctionMapTexture;
        }

        int GetDirectionIndex(byte direction)
        {
            switch (direction)
            {
                case N:
                    return herePt + junctionMapWidthX5;
                case NE:
                    return herePt + junctionMapWidthX5 + 1;
                case E:
                    return herePt + 1;
                case SE:
                    return herePt - junctionMapWidthX5 + 1;
                case S:
                    return herePt - junctionMapWidthX5;
                case SW:
                    return herePt - junctionMapWidthX5 - 1;
                case W:
                    return herePt - 1;
                case NW:
                    return herePt + junctionMapWidthX5 - 1;
                default:
                    return 0;
            }
        }

        private void UpdateJunctionMap(DFPosition currMapPixel)
        {
            byte playerDirection = GetDirection(GetNormalisedPlayerYaw());
            if (lastPlayerFacing != playerDirection)
            {
                DrawJunctionMap(currMapPixel, playerDirection);
                lastPlayerFacing = playerDirection;
            }
        }

        internal void DisableJunctionMap(bool force = false)
        {
            if (force || (roadsJunctionMap && !persistentJunctionMap))
                if (junctionMapPanel != null)
                    junctionMapPanel.Enabled = false;
        }

        #endregion

        /// <summary>
        /// Stops travel, but leaves current destination active
        /// </summary>
        public void InterruptTravel()
        {
            Debug.Log("Travel interrupted");
            SetTimeScale(1);
            circumnavigatePathsDataPt = 0;
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

            if (roadsJunctionMap && persistentJunctionMap && junctionMapPanel.Enabled)
            {
                // Update junction map if persistent and travelling
                DrawJunctionMap(GameManager.Instance.PlayerGPS.CurrentMapPixel);
            }
        }

        void Update()
        {
            if (uiCloseWhenTop && DaggerfallUI.UIManager.TopWindow == travelControlUI)
            {
                uiCloseWhenTop = false;
                travelControlUI.CloseWindow();
                return;
            }
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;

            if (playerAutopilot != null)
            {
                // Run updates for playerAutopilot and HUD, return after autopilot update if game is paused (otherwise travel map breaks - no clue why)
                playerAutopilot.Update();
                if (GameManager.IsGamePaused)
                    return;
                DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.Update();

                // Ensure only the travel UI is showing, stop travel if not, but allow registered window and travel map to not halt travel.
                if (GameManager.Instance.IsPlayerOnHUD ||
                    (DaggerfallUI.UIManager.TopWindow != travelControlUI &&
                     DaggerfallUI.UIManager.TopWindow != noStopForUIWindow &&
                     DaggerfallUI.UIManager.TopWindow != DaggerfallUI.Instance.DfTravelMapWindow))
                {
                    Debug.Log("Other UI activity detected, stopping travel.");
                    InterruptTravel();
                    uiCloseWhenTop = travelControlUI.isShowing;
                    return;
                }

                if (DestinationName == null && followKeyCode != KeyCode.None && !InputManager.Instance.IsPaused && InputManager.Instance.GetKeyDown(followKeyCode))
                {
                    if (travelControlUI.isShowing)
                        travelControlUI.CloseWindow();
                }

                // If circumnavigating a location, check for path crossings
                if (circumnavigatePathsDataPt != 0)
                {
                    byte crossed = IsPlayerOnPath(playerGPS, circumnavigatePathsDataPt);
                    if (crossed != 0 && crossed != lastCrossed)
                    {
                        lastCrossed = crossed;
                        if (travelControlUI.isShowing)
                            travelControlUI.CloseWindow();
                        return;
                    }
                    lastCrossed = crossed;
                }

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

                // If location pause set to nearby and travelling to destination, check for a nearby location and stop if found
                if (locationPause == LocPauseNear && DestinationName != null && playerGPS.HasCurrentLocation && !playerGPS.CurrentLocation.Equals(lastLocation) && playerGPS.CurrentLocation.Name != DestinationName)
                {
                    // Store location so it doesn't trigger again and ensure discovered
                    lastLocation = playerGPS.CurrentLocation;
                    playerGPS.DiscoverLocation(playerGPS.CurrentLocation.RegionName, playerGPS.CurrentLocation.Name);

                    StopTravelWithMessage(string.Format(MsgNearLocation, LocationTypeString(), playerGPS.CurrentLocation.Name));
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
            else if (followKeyCode != KeyCode.None && !InputManager.Instance.IsPaused && InputManager.Instance.GetKeyDown(followKeyCode) && GameManager.Instance.IsPlayerOnHUD)
            {
                if (GameManager.Instance.PlayerEnterExit.IsPlayerInside)
                    return;
                if (GameManager.Instance.AreEnemiesNearby())
                    DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("cannotTravelWithEnemiesNearby"));
                else
                    FollowPath();
            }

            if (roadsJunctionMap && junctionMapPanel.Enabled)
            {
                // Disable junction map if enemies are near or player moved off path
                if (GameManager.Instance.AreEnemiesNearby())
                {
                    junctionMapPanel.Enabled = false;
                }
                else if (!travelControlUI.isShowing)
                {
                    // Disable junction map if player has moved off the path, or update it
                    DFPosition currMapPixel = playerGPS.CurrentMapPixel;
                    byte pathsDataPt = GetPathsDataPoint(currMapPixel);
                    if (IsPlayerOnPath(playerGPS, pathsDataPt) == 0)
                        junctionMapPanel.Enabled = false;
                    else
                        UpdateJunctionMap(currMapPixel);
                }
                else if (persistentJunctionMap)
                {
                    // Update junction map if persistent and travelling
                    UpdateJunctionMap(playerGPS.CurrentMapPixel);
                }
            }
        }

        string LocationTypeString()
        {
            switch (GameManager.Instance.PlayerGPS.CurrentLocationType)
            {
                case DFRegion.LocationTypes.DungeonKeep:
                    return "Keep";
                case DFRegion.LocationTypes.DungeonLabyrinth:
                    return "Labyrinth";
                case DFRegion.LocationTypes.DungeonRuin:
                    return "Ruin";
                default:
                    return MacroHelper.LocationTypeName();
            }
        }

        void StopTravelWithMessage(string message)
        {
            if (travelControlUI.isShowing)
                travelControlUI.CloseWindow();
            DaggerfallUI.MessageBox(message);
        }

        void AttemptAvoidEncounter()
        {
            int successChance = Mathf.Min(GameManager.Instance.PlayerEntity.Stats.LiveLuck + GameManager.Instance.PlayerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth) - 50, maxAvoidChance);

            if (Dice100.SuccessRoll(successChance))
            {
                Debug.LogWarning("Avoided enemies enountered during travel, chance: " + successChance);
                ignoreEncounters = true;
                ignoreEncountersTime = (uint)Time.unscaledTime + 15;
                lastPlayerFacing = 0;   // Ensure persistent map is re-enabled immediately
                if (DestinationName != null)
                    BeginTravel();
                else
                    FollowPath();

                travelControlUI.ShowMessage(MsgAvoidSuccess);
            }
            else
            {
                Debug.Log("Failed to avoid enemies enountered during travel, chance: " + successChance);
                DaggerfallUI.MessageBox(MsgAvoidFail);
            }
        }

        void DisableWeatherAndSound()
        {
            if (!enableWeather)
            {
                PlayerWeather playerWeather = GameManager.Instance.WeatherManager.PlayerWeather;
                playerWeather.RainParticles.SetActive(false);
                playerWeather.SnowParticles.SetActive(false);
                playerWeather.enabled = false;
            }

            if (!enableSounds)
            {
                GameManager.Instance.PlayerActivate.GetComponentInParent<PlayerFootsteps>().enabled = false;
                ridingVolume = GameManager.Instance.TransportManager.RidingVolumeScale;
                GameManager.Instance.TransportManager.RidingVolumeScale = 0f;
            }

            if (!enableRealGrass)
                ModManager.Instance.SendModMessage("Real Grass", "toggle", false);
        }

        void EnableWeatherAndSound()
        {
            if (!enableWeather)
            {
                PlayerWeather playerWeather = GameManager.Instance.WeatherManager.PlayerWeather;
                playerWeather.enabled = true;
                switch (playerWeather.WeatherType)
                {
                    case WeatherType.Rain:
                    case WeatherType.Thunder:
                        playerWeather.RainParticles.SetActive(true);
                        playerWeather.SnowParticles.SetActive(false);
                        break;
                    case WeatherType.Snow:
                        playerWeather.RainParticles.SetActive(false);
                        playerWeather.SnowParticles.SetActive(true);
                        break;
                    default:
                        playerWeather.RainParticles.SetActive(false);
                        playerWeather.SnowParticles.SetActive(false);
                        break;
                }
            }

            if (!enableSounds)
            {
                GameManager.Instance.PlayerActivate.GetComponentInParent<PlayerFootsteps>().enabled = true;
                GameManager.Instance.TransportManager.RidingVolumeScale = ridingVolume == 0 ? 0.6f : ridingVolume;
            }

            if (!enableRealGrass)
                ModManager.Instance.SendModMessage("Real Grass", "toggle", true);
        }

        void MessageReceiver(string message, object data, DFModMessageCallback callBack)
        {
            switch (message)
            {
                case PAUSE_TRAVEL:
                    if (travelControlUI.isShowing)
                        travelControlUI.CloseWindow();
                    break;

                case NOUISTOP_WINDOW:
                    IUserInterfaceWindow window = data as IUserInterfaceWindow;
                    if (window != null)
                        noStopForUIWindow = window;
                    break;

                case NOUISTOP_REMOVE:
                    IUserInterfaceWindow remove = data as IUserInterfaceWindow;
                    if (remove != null && remove == noStopForUIWindow)
                        noStopForUIWindow = null;
                    break;

                case IS_TRAVEL_ACTIVE:
                    callBack?.Invoke(IS_TRAVEL_ACTIVE, travelControlUI.isShowing);
                    break;

                case IS_PATH_FOLLOWING:
                    callBack?.Invoke(IS_PATH_FOLLOWING, travelControlUI.isShowing && DestinationName == null);
                    break;

                case IS_FOLLOWING_ROAD:
                    callBack?.Invoke(IS_FOLLOWING_ROAD, road);
                    break;

                case SHOW_MESSAGE:
                    string msg = data as string;
                    if (!string.IsNullOrEmpty(msg))
                        travelControlUI.ShowMessage(msg);
                    break;

                case HAS_PORT:
                    int mapId = (int)data;
                    callBack?.Invoke(HAS_PORT, TravelOptionsMapWindow.HasPort(mapId));
                    break;

                default:
                    Debug.LogErrorFormat("{0}: unknown message received ({1}).", this, message);
                    break;
            }
        }
    }
}
