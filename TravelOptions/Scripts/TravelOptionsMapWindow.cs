// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Guilds;

namespace TravelOptions
{
    public class TravelOptionsMapWindow : DaggerfallTravelMapWindow
    {
        private const string MsgResume = "Resume your journey to {0}?";
        private const string MsgFollow = "Do you want to follow this road?";
        private const string MsgTeleportCost = "Teleportation will cost you {0} gold, is that acceptable?";
        private const int notEnoughGoldId = 454;

        // Path type and direction constants from BasicRoadsTexturing.
        const int path_roads = TravelOptionsMod.path_roads;
        const int path_tracks = TravelOptionsMod.path_tracks;
        const int path_rivers = TravelOptionsMod.path_rivers;
        const int path_streams = TravelOptionsMod.path_streams;

        public static Color32 roadColor = new Color32(60, 60, 60, 255);
        public static Color32 trackColor = new Color32(160, 118, 74, 255);

        public Vector2 buttonSize = new Vector2(47, 11);
        public Vector2 portsSize = new Vector2(45, 11);
        public Vector2 streamsSize = new Vector2(57, 11);

        const string roadsOffName = "roadsOff.png";
        const string roadsOnName = "roadsOn.png";
        const string tracksOffName = "tracksOff.png";
        const string tracksOnName = "tracksOn.png";
        Texture2D roadsOffTexture;
        Texture2D roadsOnTexture;
        Texture2D tracksOffTexture;
        Texture2D tracksOnTexture;

        protected Vector2 roadsButtonPos = new Vector2(1, 0);
        protected Vector2 tracksButtonPos = new Vector2(48, 0);

        protected Rect pathsOverlayPanelRect = new Rect(0, regionPanelOffset, 320 * 5, 160 * 5);
        protected Panel pathsOverlayPanel;

        protected Button roadsButton;
        protected Button tracksButton;

        const string portsOffName = "TOportsOff.png";
        const string portsOnName = "TOportsOn.png";
        Texture2D portsOffTexture;
        Texture2D portsOnTexture;

        protected Vector2 portFilterPos = new Vector2(231, 180);
        protected Vector2 portFilterMoved = new Vector2(231, 173);

        protected Vector2 horizArrowPos = new Vector2(231, 176);
        protected Vector2 horizArrowMoved = new Vector2(231, 184);

        protected Vector2 vertArrowPos = new Vector2(254, 176);
        protected Vector2 vertArrowMoved = new Vector2(254, 184);

        protected Button portsFilterButton;

        protected bool portsFilter = false;

        protected bool onlyLargeDots = false;

        protected bool teleportCharge = false;

        internal static byte[][] pathsData = new byte[4][];
        protected bool[] showPaths = { true, true, false, false };

        // Hidden map locations mod compatibility.
        public const string HIDDEN_MAP_LOCATIONS_MODNAME = "Hidden Map Locations";
        protected bool hiddenMapLocationsEnabled;
        protected HashSet<ContentReader.MapSummary> discoveredMapSummaries;
        protected HashSet<DFRegion.LocationTypes> revealedLocationTypes;

        public TravelOptionsMapWindow(IUserInterfaceManager uiManager)
            : base(uiManager)
        {
            if (TravelOptionsMod.Instance.RoadsIntegration)
            {
                // Try to get path data from BasicRoads mod
                ModManager.Instance.SendModMessage(TravelOptionsMod.ROADS_MODNAME, "getPathData", path_roads,
                    (string message, object data) => { pathsData[path_roads] = (byte[])data; });
                ModManager.Instance.SendModMessage(TravelOptionsMod.ROADS_MODNAME, "getPathData", path_tracks,
                    (string message, object data) => { pathsData[path_tracks] = (byte[])data; });
            }
            
            Mod hiddenMapLocationsMod = ModManager.Instance.GetMod(HIDDEN_MAP_LOCATIONS_MODNAME);
            hiddenMapLocationsEnabled = hiddenMapLocationsMod != null && hiddenMapLocationsMod.Enabled;

            if (hiddenMapLocationsEnabled)
            {
                discoveredMapSummaries = new HashSet<ContentReader.MapSummary>();
                revealedLocationTypes = new HashSet<DFRegion.LocationTypes>();

                ModManager.Instance.SendModMessage(HIDDEN_MAP_LOCATIONS_MODNAME, "getRevealedLocationTypes", null,
                    (string message, object data) => { revealedLocationTypes = (HashSet<DFRegion.LocationTypes>)data; });
            }

            onlyLargeDots = !TravelOptionsMod.Instance.VariableSizeDots;
        }

        protected override void Setup()
        {
            base.Setup();

            if (TravelOptionsMod.Instance.ShipTravelPortsOnly)
            {
                // Towns filter button
                if (!TextureReplacement.TryImportImage(portsOffName, true, out portsOffTexture))
                    return;
                if (!TextureReplacement.TryImportImage(portsOnName, true, out portsOnTexture))
                    return;

                portsFilterButton = new Button();
                portsFilterButton.Position = portFilterPos;
                portsFilterButton.Size = portsSize; //new Vector2(portsOffTexture.width, portsOffTexture.height);
                portsFilterButton.BackgroundTexture = portsOffTexture;
                portsFilterButton.OnMouseClick += PortsFilterButton_OnMouseClick;
                NativePanel.Components.Add(portsFilterButton);
            }

            if (TravelOptionsMod.Instance.RoadsIntegration)
            {
                SetupPathButtons();
                UpdatePathButtons();

                locationDotsPixelBuffer = new Color32[(int)regionTextureOverlayPanelRect.width * (int)regionTextureOverlayPanelRect.height * 25];
                locationDotsTexture = new Texture2D((int)regionTextureOverlayPanelRect.width * 5, (int)regionTextureOverlayPanelRect.height * 5, TextureFormat.ARGB32, false);
            }
        }

        private void PortsFilterButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            portsFilter = !portsFilter;
            portsFilterButton.BackgroundTexture = portsFilter ? portsOnTexture : portsOffTexture;

            UpdateMapLocationDotsTexture();
        }

        protected override void SetupArrowButtons()
        {
            base.SetupArrowButtons();

            if (TravelOptionsMod.Instance.ShipTravelPortsOnly)
            {
                // Move the port filter button and arrow buttons if needed
                if (verticalArrowButton.Enabled || horizontalArrowButton.Enabled)
                {
                    portsFilterButton.Position = portFilterMoved;
                    horizontalArrowButton.Position = horizArrowMoved;
                    verticalArrowButton.Position = vertArrowMoved;
                }
                else
                {
                    portsFilterButton.Position = portFilterPos;
                    horizontalArrowButton.Position = horizArrowPos;
                    verticalArrowButton.Position = vertArrowPos;
                }
            }
        }

        protected void SetupPathButtons()
        {
            // Paths buttons
            if (!TextureReplacement.TryImportImage(roadsOffName, true, out roadsOffTexture))
                return;
            if (!TextureReplacement.TryImportImage(roadsOnName, true, out roadsOnTexture))
                return;
            if (!TextureReplacement.TryImportImage(tracksOffName, true, out tracksOffTexture))
                return;
            if (!TextureReplacement.TryImportImage(tracksOnName, true, out tracksOnTexture))
                return;

            roadsButton = new Button();
            roadsButton.Tag = path_roads;
            roadsButton.Position = roadsButtonPos;
            roadsButton.Size = buttonSize;  //new Vector2(roadsOnTexture.width, roadsOnTexture.height);
            roadsButton.OnMouseClick += PathTypeButton_OnMouseClick;
            NativePanel.Components.Add(roadsButton);

            tracksButton = new Button();
            tracksButton.Tag = path_tracks;
            tracksButton.Position = tracksButtonPos;
            tracksButton.Size = buttonSize; //new Vector2(tracksOnTexture.width, tracksOnTexture.height);
            tracksButton.OnMouseClick += PathTypeButton_OnMouseClick;
            NativePanel.Components.Add(tracksButton);
        }

        protected virtual void PathTypeButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            int pathType = (int)sender.Tag;
            if (pathType >= path_roads && pathType <= path_streams)
            {
                showPaths[pathType] = !showPaths[pathType];
            }
            UpdatePathButtons();
            UpdateMapLocationDotsTexture();
        }

        private void UpdatePathButtons()
        {
            roadsButton.BackgroundTexture = showPaths[path_roads] ? roadsOnTexture : roadsOffTexture;
            tracksButton.BackgroundTexture = showPaths[path_tracks] ? tracksOnTexture : tracksOffTexture;
        }

        public override void OnPush()
        {
            base.OnPush();

            teleportCharge = teleportationTravel && TravelOptionsMod.Instance.TeleportCost;

            // Open to region map if travel UI showing, else check if there's an active destination and ask to resume
            TravelOptionsMod travelModInstance = TravelOptionsMod.Instance;
            travelModInstance.DisableJunctionMap();
            if (travelModInstance.GetTravelControlUI().isShowing)
            {
                OpenRegionPanel(GetPlayerRegion());
            }
            else if (!string.IsNullOrEmpty(travelModInstance.DestinationName))
            {
                Debug.Log("Active destination: " + travelModInstance.DestinationName);

                string resume = string.Format(MsgResume, travelModInstance.DestinationName);
                DaggerfallMessageBox resumeMsgBox = new DaggerfallMessageBox(uiManager, DaggerfallMessageBox.CommonMessageBoxButtons.YesNo, resume, uiManager.TopWindow);
                resumeMsgBox.OnButtonClick += (_sender, button) =>
                {
                    CloseWindow();
                    if (button == DaggerfallMessageBox.MessageBoxButtons.Yes)
                    {
                        CloseWindow();
                        travelModInstance.BeginTravel();
                    }
                };
                resumeMsgBox.Show();
            }
        }

        public override void Update()
        {
            base.Update();

            // Had to move this to update and use a control flag since when popping a msg box inside OnPush when indoors the underlying UI is not rendered
            if (teleportCharge)
            {
                ChargeForTeleport();
            }
        }

        private void ChargeForTeleport()
        {
            MagesGuild magesGuild = (MagesGuild)GameManager.Instance.GuildManager.GetGuild(FactionFile.GuildGroups.MagesGuild);
            int underWizard = 8 - magesGuild.Rank;
            if (underWizard > 0)
            {
                int cost = underWizard * 200;
                if (GameManager.Instance.PlayerEntity.GetGoldAmount() >= cost)
                {
                    string costMsg = string.Format(MsgTeleportCost, cost);
                    DaggerfallMessageBox payMsgBox = new DaggerfallMessageBox(uiManager, DaggerfallMessageBox.CommonMessageBoxButtons.YesNo, costMsg, uiManager.TopWindow);
                    payMsgBox.OnButtonClick += (_sender, button) =>
                    {
                        CloseWindow();
                        if (button == DaggerfallMessageBox.MessageBoxButtons.Yes)
                        {
                            GameManager.Instance.PlayerEntity.DeductGoldAmount(cost);
                        }
                        else
                        {
                            CloseWindow();
                        }
                    };
                    payMsgBox.Show();
                }
                else
                {
                    CloseWindow();
                    DaggerfallUI.MessageBox(notEnoughGoldId);
                }
            }
            teleportCharge = false;
        }

        // Updates location dots
        protected override void UpdateMapLocationDotsTexture()
        {
            GetDiscoveredLocationsFromHiddenMapMod();

            if (TravelOptionsMod.Instance.RoadsIntegration && selectedRegion != 61)
            {
                UpdateMapLocationDotsTextureWithPaths();
            }
            else
            {
                base.UpdateMapLocationDotsTexture();
            }
        }

        protected virtual void UpdateMapLocationDotsTextureWithPaths()
        {
            // Get map and dimensions
            string mapName = selectedRegionMapNames[mapIndex];
            Vector2 origin = offsetLookup[mapName];
            int originX = (int)origin.x;
            int originY = (int)origin.y;
            int width = (int)regionTextureOverlayPanelRect.width;
            int height = (int)regionTextureOverlayPanelRect.height;

            // Plot locations to color array
            scale = GetRegionMapScale(selectedRegion);
            Array.Clear(locationDotsPixelBuffer, 0, locationDotsPixelBuffer.Length);
            Array.Clear(locationDotsOutlinePixelBuffer, 0, locationDotsOutlinePixelBuffer.Length);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = (int)((((height - y - 1) * width) + x) * scale);
                    if (offset >= (width * height))
                        continue;
                    int sampleRegion = DaggerfallUnity.ContentReader.MapFileReader.GetPoliticIndex(originX + x, originY + y) - 128;

                    int width5 = width * 5;
                    int offset5 = (int)((((height - y - 1) * 5 * width5) + (x * 5)) * scale);

                    int pIdx = originX + x + ((originY + y) * MapsFile.MaxMapPixelX);
                    if (showPaths[path_tracks])
                        DrawPath(offset5, width5, pathsData[path_tracks][pIdx], trackColor, ref locationDotsPixelBuffer);
                    if (showPaths[path_roads])
                        DrawPath(offset5, width5, pathsData[path_roads][pIdx], roadColor, ref locationDotsPixelBuffer);
                    //Debug.LogFormat("Found road at x:{0} y:{1}  index:{2}", originX + x, originY + y, rIdx);

                    ContentReader.MapSummary summary;
                    if (DaggerfallUnity.ContentReader.HasLocation(originX + x, originY + y, out summary))
                    {
                        if (checkLocationDiscovered(summary))
                        {
                            int index = GetPixelColorIndex(summary.LocationType);
                            if (index != -1)
                            {
                                if (DaggerfallUnity.Settings.TravelMapLocationsOutline)
                                    locationDotsOutlinePixelBuffer[offset] = dotOutlineColor;
                                DrawLocation(offset5, width5, locationPixelColors[index], IsLocationLarge(summary.LocationType), ref locationDotsPixelBuffer);
                            }
                        }
                    }
                }
            }

            // Apply updated color array to texture
            if (DaggerfallUnity.Settings.TravelMapLocationsOutline)
            {
                locationDotsOutlineTexture.SetPixels32(locationDotsOutlinePixelBuffer);
                locationDotsOutlineTexture.Apply();
            }
            locationDotsTexture.SetPixels32(locationDotsPixelBuffer);
            locationDotsTexture.Apply();

            // Present texture
            if (DaggerfallUnity.Settings.TravelMapLocationsOutline)
                for (int i = 0; i < outlineDisplacements.Length; i++)
                    regionLocationDotsOutlinesOverlayPanel[i].BackgroundTexture = locationDotsOutlineTexture;
            regionLocationDotsOverlayPanel.BackgroundTexture = locationDotsTexture;
        }

        void DrawLocation(int offset, int width, Color32 color, bool large, ref Color32[] pixelBuffer)
        {
            int st = large ? 0 : 1;
            int en = large ? 5 : 4;
            for (int y = st; y < en; y++)
            {
                for (int x = st; x < en; x++)
                {
                    pixelBuffer[offset + (y * width) + x] = color;
                }
            }
        }

        bool IsLocationLarge(DFRegion.LocationTypes locationType)
        {
            return locationType == DFRegion.LocationTypes.TownCity || locationType == DFRegion.LocationTypes.TownHamlet || onlyLargeDots;
        }

        public static void DrawPath(int offset, int width, byte pathDataPt, Color32 pathColor, ref Color32[] pixelBuffer)
        {
            if (pathDataPt == 0)
                return;

            pixelBuffer[offset + (width * 2) + 2] = pathColor;
            if ((pathDataPt & TravelOptionsMod.S) != 0)
            {
                pixelBuffer[offset + 2] = pathColor;
                pixelBuffer[offset + width + 2] = pathColor;
            }
            if ((pathDataPt & TravelOptionsMod.SE) != 0)
            {
                pixelBuffer[offset + 4] = pathColor;
                pixelBuffer[offset + width + 3] = pathColor;
            }
            if ((pathDataPt & TravelOptionsMod.E) != 0)
            {
                pixelBuffer[offset + (width * 2) + 3] = pathColor;
                pixelBuffer[offset + (width * 2) + 4] = pathColor;
            }
            if ((pathDataPt & TravelOptionsMod.NE) != 0)
            {
                pixelBuffer[offset + (width * 3) + 3] = pathColor;
                pixelBuffer[offset + (width * 4) + 4] = pathColor;
            }
            if ((pathDataPt & TravelOptionsMod.N) != 0)
            {
                pixelBuffer[offset + (width * 3) + 2] = pathColor;
                pixelBuffer[offset + (width * 4) + 2] = pathColor;
            }
            if ((pathDataPt & TravelOptionsMod.NW) != 0)
            {
                pixelBuffer[offset + (width * 3) + 1] = pathColor;
                pixelBuffer[offset + (width * 4)] = pathColor;
            }
            if ((pathDataPt & TravelOptionsMod.W) != 0)
            {
                pixelBuffer[offset + (width * 2)] = pathColor;
                pixelBuffer[offset + (width * 2) + 1] = pathColor;
            }
            if ((pathDataPt & TravelOptionsMod.SW) != 0)
            {
                pixelBuffer[offset] = pathColor;
                pixelBuffer[offset + width + 1] = pathColor;
            }
        }

        // Zoom and pan region texture
        protected override void ZoomMapTextures()
        {
            base.ZoomMapTextures();

            if (TravelOptionsMod.Instance.RoadsIntegration && RegionSelected && zoom)
            {
                // Adjust cropped location dots overlay to x5 version
                int width = (int)regionTextureOverlayPanelRect.width;
                int height = (int)regionTextureOverlayPanelRect.height;
                int zoomWidth = width / (zoomfactor * 2);
                int zoomHeight = height / (zoomfactor * 2);
                int startX = (int)zoomPosition.x - zoomWidth;
                int startY = (int)(height + (-zoomPosition.y - zoomHeight)) + regionPanelOffset;
                // Clamp to edges
                if (startX < 0)
                    startX = 0;
                else if (startX + width / zoomfactor >= width)
                    startX = width - width / zoomfactor;
                if (startY < 0)
                    startY = 0;
                else if (startY + height / zoomfactor >= height)
                    startY = height - height / zoomfactor;

                Rect locationDotsNewRect = new Rect(startX * 5, startY * 5, width * 5 / zoomfactor, height * 5 / zoomfactor);
                regionLocationDotsOverlayPanel.BackgroundCroppedRect = locationDotsNewRect;

                UpdateBorder();
            }
        }

        public void DrawMapSection(int originX, int originY, int width, int height, ref Color32[] pixelBuffer, bool circular = false)
        {
            GetDiscoveredLocationsFromHiddenMapMod();
        
            Array.Clear(pixelBuffer, 0, pixelBuffer.Length);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (circular && height == width && Mathf.Sqrt(Mathf.Pow(Mathf.Abs(x - (width / 2) + 0.5f), 2) + Mathf.Pow(Mathf.Abs(y - (height / 2) + 0.5f), 2)) >= (height + 1.5) / 2) 
                        continue;

                    int offset = ((height - y - 1) * width) + x;
                    if (offset >= (width * height))
                        continue;
                    int width5 = width * 5;
                    int offset5 = ((height - y - 1) * 5 * width5) + (x * 5);

                    int pIdx = originX + x + ((originY + y) * MapsFile.MaxMapPixelX);
                    if (showPaths[path_tracks])
                        DrawPath(offset5, width5, pathsData[path_tracks][pIdx], trackColor, ref pixelBuffer);
                    if (showPaths[path_roads])
                        DrawPath(offset5, width5, pathsData[path_roads][pIdx], roadColor, ref pixelBuffer);
                    //Debug.LogFormat("Found road at x:{0} y:{1}  index:{2}", originX + x, originY + y, rIdx);

                    ContentReader.MapSummary summary;
                    if (DaggerfallUnity.Instance.ContentReader.HasLocation(originX + x, originY + y, out summary))
                    {
                        if (checkLocationDiscovered(summary))
                        {
                            int index = GetPixelColorIndex(summary.LocationType);
                            if (index != -1)
                            {
                                DrawLocation(offset5, width5, locationPixelColors[index], IsLocationLarge(summary.LocationType), ref pixelBuffer);
                            }
                        }
                    }
                }
            }
        }

        protected override bool checkLocationDiscovered(ContentReader.MapSummary summary)
        {
            // If ports filter is on, only return true if it's a port
            if (portsFilter && !HasPort(summary))
                return false;
                
            if (hiddenMapLocationsEnabled)
            {
                return discoveredMapSummaries.Contains(summary) || revealedLocationTypes.Contains(summary.LocationType);
            }

            return base.checkLocationDiscovered(summary);
        }

        protected void GetDiscoveredLocationsFromHiddenMapMod()
        {
            if (hiddenMapLocationsEnabled)
            {
                ModManager.Instance.SendModMessage(HIDDEN_MAP_LOCATIONS_MODNAME, "getDiscoveredMapSummaries", null,
                    (string _, object result) => { discoveredMapSummaries = (HashSet<ContentReader.MapSummary>)result; });
            }
        }

        public static bool HasPort(ContentReader.MapSummary mapSummary)
        {
            return Array.Exists(portLocationIds, n => n == mapSummary.ID);
        }

        public static bool HasPortExtra(DFRegion.RegionMapTable mapTable)
        {
            return Array.Exists(portLocationExtraIds, n => n == (mapTable.MapId & 0xFFFFF));
        }

        public static readonly int[] portLocationExtraIds = {
            // Extras allowing travel to :
            2002,       // Small Ship
            5005,       // Large Ship
            205676,     // "Isle of Balfiera", "Blackhead"
            278901,     // "Mournoth", "Zagoparia"
            263119,     // "Betony", "Whitefort"
            148062,     // "Tulune", "The Citadel of Hearthham"
            144059,     // "Tulune", "The Elyzanna Assembly"
            343439,     // "Cybiades", "Ruins of Cosh Hall"
        };

        public static readonly int[] portLocationIds = {
            443401, 280614, 285597, 485856, 86496, 137544, 139547, 143535, 143542, 149513,
            150625, 158629, 162631, 162646, 164648, 166644, 168652, 169640, 170654, 178663,
            182685, 188727, 192653, 195681, 201654, 202646, 203671, 225685, 234712, 22763,
            184263, 189097, 192248, 194242, 194279, 196245, 199102, 199111, 201125, 210132,
            212138, 213207, 226205, 228209, 235146, 236143, 239139, 239144, 239146, 241140,
            91170, 93168, 96150, 96212, 107137, 109167, 325404, 325406, 328409, 341392,
            342399, 343397, 344387, 345378, 345383, 346398, 347375, 348372, 348396, 350370,
            351392, 352369, 353387, 354364, 361382, 364381, 369385, 369388, 370441, 372411,
            372439, 373407, 373415, 373422, 373425, 373427, 373429, 374419, 120375, 121377,
            148460, 148463, 150459, 158499, 168357, 172455, 187406, 192361, 193358, 193366,
            195353, 195361, 197366, 200356, 277751, 278764, 279644, 279697, 279749, 279754,
            279766, 280747, 281656, 281658, 281663, 281699, 281702, 281704, 281741, 281770,
            282712, 282724, 282728, 282731, 282734, 282737, 283687, 283707, 284685, 285682,
            286674, 289737, 292695, 293697, 310763, 311766, 194855, 195860, 223828, 225840,
            229847, 236854, 240841, 242856, 243846, 244859, 247836, 249839, 249861, 249866,
            250875, 255876, 256887, 256900, 257889, 258892, 258907, 261923, 261925, 262907,
            262931, 264900, 264902, 264940, 264942, 265956, 266964, 273975, 5222, 5224,
            11215, 14210, 23240, 35152, 49219, 157795, 181800, 187807, 193793, 210785,
            215821, 216791, 112707, 133701, 133718, 134711, 135713, 135717, 135735, 138745,
            140758, 140760, 148782, 151788, 83668, 125675, 111631, 111645, 112652, 113637,
            113646, 113649, 115622, 118573, 134553, 137558, 137561, 137593, 138583, 139588,
            145609, 146607, 147614, 148589, 151591, 152587, 56637, 35449, 41483, 121473,
            129449, 29347, 40361, 69406, 160305, 451180, 451186, 453173, 455174, 457179,
            458198, 460176, 461173, 463171, 468168, 468188, 473169, 474207, 476162, 476164,
            477177, 478159, 483153, 493144, 495141, 422217, 432218, 433205, 435202, 455199,
            459220, 405246, 405263, 406266, 407241, 408235, 408249, 417227, 393300, 397296,
            403279, 406276, 418291, 364449, 370446, 402451, 276583, 279596, 290582, 294569,
            295564, 296558, 297552, 305534, 308524, 308527, 308530, 309521, 312518, 313516,
            316550, 318514, 334515, 339496, 341496, 346475, 351470, 337704, 263832, 264825,
            269847, 269849, 276835, 277798, 278817, 278843, 279815, 283779, 283782, 287827,
            287829, 289866, 294842, 302839, 306854, 337914, 338912, 341916, 346918, 351919,
            354916, 357915, 357918, 361913, 363915, 364868, 370908, 379876, 379888, 380885,
            381881, 382879, 278962, 281872, 281969, 324981, 469891, 437653, 446471, 472431,
            480415, 217966, 100086, 121067, 123073, 144059, 75104, 77077, 83137, 86218,
            86334, 89333, 343439,
            // Extras allowing travel to:
            205676,     // "Isle of Balfiera", "Blackhead"
            278901,     // "Mournoth", "Zagoparia"
            263119,     // "Betony", "Whitefort"
            148062,     // "Tulune", "The Citadel of Hearthham"
            144059,     // "Tulune", "The Elyzanna Assembly"
            343439,     // "Cybiades", "Ruins of Cosh Hall"
        };
        
    }
}
