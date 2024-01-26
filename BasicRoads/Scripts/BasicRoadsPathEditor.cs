// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2020 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Lypyl (lypyl@dfworkshop.net), Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    Hazelnut, TheLacus
// 
// Notes:
//

using System;
using UnityEngine;
using Wenzil.Console;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Utility;
using DaggerfallConnect;
using System.IO;
using DaggerfallWorkshop.Utility.AssetInjection;

namespace BasicRoads
{
    /// <summary>
    /// Implements a road path editor based on DFUs travel map.
    /// </summary>
    public class BasicRoadsPathEditor : DaggerfallTravelMapWindow
    {
        public const bool allowDeletions = false;    // Allows edits of zero to be loaded over existing data if true.

        public const string RoadDataFilename = "roadData.txt";
        public const string TrackDataFilename = "trackData.txt";

        static Color32 roadColor = new Color32(60, 60, 60, 255);
        static Color32 trackColor = new Color32(160, 118, 74, 255);

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

        public const string RiverDataFilename = "riverData.txt";
        public const string StreamDataFilename = "streamData.txt";

        static Color32 riverColor = new Color32(48, 79, 250, 255);
        static Color32 streamColor = new Color32(48, 135, 250, 255);

        const string riversOffName = "riversOff.png";
        const string riversOnName = "riversOn.png";
        const string streamsOffName = "streamsOff.png";
        const string streamsOnName = "streamsOn.png";
        Texture2D riversOffTexture;
        Texture2D riversOnTexture;
        Texture2D streamsOffTexture;
        Texture2D streamsOnTexture;

        protected Vector2 riversButtonPos = new Vector2(272, 0);
        protected Vector2 streamsButtonPos = new Vector2(215, 0);

        protected Rect pathsOverlayPanelRect = new Rect(0, regionPanelOffset, 320 * 5, 160 * 5);
        protected Panel pathsOverlayPanel;

        protected Button roadsButton;
        protected Button tracksButton;
        protected Button riversButton;
        protected Button streamsButton;

        static BasicRoadsPathEditor instance;
        public static BasicRoadsPathEditor Instance {
            get {
                if (instance == null)
                    instance = new BasicRoadsPathEditor(DaggerfallUI.UIManager);
                return instance;
            }
        }
        public static bool waterEditing = false;

        public static byte[][] pathsData = new byte[4][];

        static BasicRoadsTexturing roadsTexturing;

        int mouseX = 0;
        int mouseY = 0;
        int currPathType = -1;
        bool outlineBackup;
        bool changed;
        bool[] showPaths = { true, true, false, false };

        public BasicRoadsPathEditor(IUserInterfaceManager uiManager) : base(uiManager)
        {
            // Register console commands
            try
            {
                RoadPathEditorCommands.RegisterCommands();
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("Error Registering Travelmap Console commands: {0}", ex.Message));
            }

            roadsTexturing = (BasicRoadsTexturing)DaggerfallUnity.Instance.TerrainTexturing;
            pathsData[BasicRoadsTexturing.roads] = roadsTexturing.GetPathData(BasicRoadsTexturing.roads);
            pathsData[BasicRoadsTexturing.tracks] = roadsTexturing.GetPathData(BasicRoadsTexturing.tracks);

            if (waterEditing)
            {
                showPaths[BasicRoadsTexturing.rivers] = true;
                showPaths[BasicRoadsTexturing.streams] = true;
                pathsData[BasicRoadsTexturing.rivers] = roadsTexturing.GetPathData(BasicRoadsTexturing.rivers);
                pathsData[BasicRoadsTexturing.streams] = roadsTexturing.GetPathData(BasicRoadsTexturing.streams);
                ReadEditedPathData(BasicRoadsTexturing.rivers, RiverDataFilename);
                ReadEditedPathData(BasicRoadsTexturing.streams, StreamDataFilename);
            }
            else
            {
                ReadEditedPathData(BasicRoadsTexturing.roads, RoadDataFilename);
                ReadEditedPathData(BasicRoadsTexturing.tracks, TrackDataFilename);
            }
        }

        public override void OnPush()
        {
            base.OnPush();

            outlineBackup = DaggerfallUnity.Settings.TravelMapLocationsOutline;
            DaggerfallUnity.Settings.TravelMapLocationsOutline = false;

            if (waterEditing)
            {
                ReadEditedPathData(BasicRoadsTexturing.rivers, RiverDataFilename);
                ReadEditedPathData(BasicRoadsTexturing.streams, StreamDataFilename);
            }
            else
            {
                ReadEditedPathData(BasicRoadsTexturing.roads, RoadDataFilename);
                ReadEditedPathData(BasicRoadsTexturing.tracks, TrackDataFilename);
            }
            changed = false;
        }

        public override void OnPop()
        {
            base.OnPop();

            DaggerfallUnity.Settings.TravelMapLocationsOutline = outlineBackup;
        }

        protected override void Setup()
        {
            base.Setup();

            SetupPathButtons();
            UpdatePathButtons();
            if (waterEditing)
            {
                SetupWaterButtons();
                UpdateWaterButtons();
            }

            locationDotsPixelBuffer = new Color32[(int)regionTextureOverlayPanelRect.width * (int)regionTextureOverlayPanelRect.height * 25];
            locationDotsTexture = new Texture2D((int)regionTextureOverlayPanelRect.width * 5, (int)regionTextureOverlayPanelRect.height * 5, TextureFormat.ARGB32, false);
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
            roadsButton.Tag = BasicRoadsTexturing.roads;
            roadsButton.Position = roadsButtonPos;
            roadsButton.Size = new Vector2(roadsOnTexture.width, roadsOnTexture.height);
            roadsButton.BackgroundColor = Color.white;
            roadsButton.OnMouseClick += PathTypeButton_OnMouseClick;
            NativePanel.Components.Add(roadsButton);

            tracksButton = new Button();
            tracksButton.Tag = BasicRoadsTexturing.tracks;
            tracksButton.Position = tracksButtonPos;
            tracksButton.Size = new Vector2(tracksOnTexture.width, tracksOnTexture.height);
            tracksButton.BackgroundColor = Color.white;
            tracksButton.OnMouseClick += PathTypeButton_OnMouseClick;
            NativePanel.Components.Add(tracksButton);
        }

        protected void SetupWaterButtons()
        {
            // Water buttons
            if (!TextureReplacement.TryImportImage(riversOffName, true, out riversOffTexture))
                return;
            if (!TextureReplacement.TryImportImage(riversOnName, true, out riversOnTexture))
                return;
            if (!TextureReplacement.TryImportImage(streamsOffName, true, out streamsOffTexture))
                return;
            if (!TextureReplacement.TryImportImage(streamsOnName, true, out streamsOnTexture))
                return;

            riversButton = new Button();
            riversButton.Tag = BasicRoadsTexturing.rivers;
            riversButton.Position = riversButtonPos;
            riversButton.Size = new Vector2(riversOnTexture.width, riversOnTexture.height);
            riversButton.BackgroundColor = Color.white;
            riversButton.OnMouseClick += PathTypeButton_OnMouseClick;
            NativePanel.Components.Add(riversButton);

            streamsButton = new Button();
            streamsButton.Tag = BasicRoadsTexturing.streams;
            streamsButton.Position = streamsButtonPos;
            streamsButton.Size = new Vector2(streamsOnTexture.width, streamsOnTexture.height);
            streamsButton.BackgroundColor = Color.white;
            streamsButton.OnMouseClick += PathTypeButton_OnMouseClick;
            NativePanel.Components.Add(streamsButton);
        }

        protected virtual void PathTypeButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            int pathType = (int)sender.Tag;
            if (pathType >= BasicRoadsTexturing.roads && pathType <= BasicRoadsTexturing.streams)
            {
                roadsButton.BackgroundColor = Color.white;
                tracksButton.BackgroundColor = Color.white;
                if (waterEditing) {
                    riversButton.BackgroundColor = Color.white;
                    streamsButton.BackgroundColor = Color.white;
                }
                if (showPaths[pathType])
                {
                    if (currPathType != pathType &&
                        ((!waterEditing && (pathType == BasicRoadsTexturing.roads || pathType == BasicRoadsTexturing.tracks)) ||
                         (waterEditing && (pathType == BasicRoadsTexturing.rivers || pathType == BasicRoadsTexturing.streams))))
                    {
                        sender.BackgroundColor = Color.red;
                        currPathType = pathType;
                    }
                    else
                        showPaths[pathType] = !showPaths[pathType];
                }
                else
                    showPaths[pathType] = !showPaths[pathType];

                if (!showPaths[pathType])
                    currPathType = -1;
            }

            UpdatePathButtons();
            if (waterEditing)
                UpdateWaterButtons();
            UpdateMapLocationDotsTexture();
        }

        private void UpdatePathButtons()
        {
            roadsButton.BackgroundColorTexture = showPaths[BasicRoadsTexturing.roads] ? roadsOnTexture : roadsOffTexture;
            tracksButton.BackgroundColorTexture = showPaths[BasicRoadsTexturing.tracks] ? tracksOnTexture : tracksOffTexture;
        }

        private void UpdateWaterButtons()
        {
            riversButton.BackgroundColorTexture = showPaths[BasicRoadsTexturing.rivers] ? riversOnTexture : riversOffTexture;
            streamsButton.BackgroundColorTexture = showPaths[BasicRoadsTexturing.streams] ? streamsOnTexture : streamsOffTexture;
        }

        protected override void ExitButtonClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            if (RegionSelected)
                CloseRegionPanel();
            else if (changed)
            {
                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, this);
                messageBox.SetText(BasicRoadsMod.mod.Localize("saveChanges"));
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
                messageBox.OnButtonClick += SaveBoxClick;
                uiManager.PushWindow(messageBox);
            }
            else
                CloseTravelWindows();
        }

        protected virtual void SaveBoxClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            sender.CloseWindow();
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                if (!waterEditing)
                {
                    WriteEditedPathData(BasicRoadsTexturing.roads, RoadDataFilename);
                    WriteEditedPathData(BasicRoadsTexturing.tracks, TrackDataFilename);
                }
                else
                {
                    WriteEditedPathData(BasicRoadsTexturing.rivers, RiverDataFilename);
                    WriteEditedPathData(BasicRoadsTexturing.streams, StreamDataFilename);
                }
            }
            CloseTravelWindows();
        }

        protected override void UpdateRegionLabel()
        {
            if (RegionSelected && !MouseOverOtherRegion)
            {
                ContentReader.MapSummary loc;
                if (DaggerfallUnity.ContentReader.HasLocation(mouseX, mouseY, out loc))
                    regionLabel.Text = string.Format("X: {0}   Y: {1}  - {2} : {3}", mouseX, mouseY, DaggerfallUnity.ContentReader.MapFileReader.GetRegionName(mouseOverRegion), currentDFRegion.MapNames[loc.MapIndex]);
                else
                    regionLabel.Text = string.Format("X: {0}   Y: {1}", mouseX, mouseY);
            }
            else
                base.UpdateRegionLabel();
        }

        // Handle clicks on the main panel
        protected override void ClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            Vector2 myPos = position;
            myPos.y -= regionPanelOffset;

            // Ensure clicks are inside region texture
            if (myPos.x < 0 || myPos.x > regionTextureOverlayPanelRect.width || myPos.y < 0 || myPos.y > regionTextureOverlayPanelRect.height)
                return;

            if (RegionSelected && !MouseOverOtherRegion)
            {
                if (currPathType == -1)
                    return;

                byte[] pathData = pathsData[currPathType];
                int xDim = MapsFile.MaxMapPixelX;
                int rIdx = mouseX + (mouseY * xDim);
                int n = rIdx - xDim;
                int ne = rIdx - xDim + 1;
                int e = rIdx + 1;
                int se = rIdx + xDim + 1;
                int s = rIdx + xDim;
                int sw = rIdx + xDim - 1;
                int w = rIdx - 1;
                int nw = rIdx - xDim - 1;

                if (pathData[rIdx] == 0)
                {
                    if (waterEditing)   // Prefer diagonal connections for water, excluding perpendicular connections
                    {
                        bool rne = ConnectPath(pathData, rIdx, ne, BasicRoadsTexturing.NE, BasicRoadsTexturing.SW, BasicRoadsTexturing.NW, BasicRoadsTexturing.SE);
                        bool rse = ConnectPath(pathData, rIdx, se, BasicRoadsTexturing.SE, BasicRoadsTexturing.NW, BasicRoadsTexturing.NE, BasicRoadsTexturing.SW);
                        bool rsw = ConnectPath(pathData, rIdx, sw, BasicRoadsTexturing.SW, BasicRoadsTexturing.NE, BasicRoadsTexturing.NW, BasicRoadsTexturing.SE);
                        bool rnw = ConnectPath(pathData, rIdx, nw, BasicRoadsTexturing.NW, BasicRoadsTexturing.SE, BasicRoadsTexturing.NE, BasicRoadsTexturing.SW);

                        if (!rne && !rse) ConnectPath(pathData, rIdx, e, BasicRoadsTexturing.E, BasicRoadsTexturing.W);
                        if (!rse && !rsw) ConnectPath(pathData, rIdx, s, BasicRoadsTexturing.S, BasicRoadsTexturing.N);
                        if (!rsw && !rnw) ConnectPath(pathData, rIdx, w, BasicRoadsTexturing.W, BasicRoadsTexturing.E);
                        if (!rnw && !rne) ConnectPath(pathData, rIdx, n, BasicRoadsTexturing.N, BasicRoadsTexturing.S);
                    }
                    else                // Prefer cardinal connections for paths
                    {
                        bool rn = ConnectPath(pathData, rIdx, n, BasicRoadsTexturing.N, BasicRoadsTexturing.S);
                        bool re = ConnectPath(pathData, rIdx, e, BasicRoadsTexturing.E, BasicRoadsTexturing.W);
                        bool rs = ConnectPath(pathData, rIdx, s, BasicRoadsTexturing.S, BasicRoadsTexturing.N);
                        bool rw = ConnectPath(pathData, rIdx, w, BasicRoadsTexturing.W, BasicRoadsTexturing.E);

                        if (!rn && !re) ConnectPath(pathData, rIdx, ne, BasicRoadsTexturing.NE, BasicRoadsTexturing.SW);
                        if (!rs && !re) ConnectPath(pathData, rIdx, se, BasicRoadsTexturing.SE, BasicRoadsTexturing.NW);
                        if (!rs && !rw) ConnectPath(pathData, rIdx, sw, BasicRoadsTexturing.SW, BasicRoadsTexturing.NE);
                        if (!rn && !rw) ConnectPath(pathData, rIdx, nw, BasicRoadsTexturing.NW, BasicRoadsTexturing.SE);
                    }

                    if (pathData[rIdx] == 0)
                        pathData[rIdx] = 0xFF;

                    Debug.LogFormat("Marked path at x:{0} y:{1}  index:{2}  byte: {3}", mouseX, mouseY, rIdx, Convert.ToString(pathData[rIdx], 2));
                }
                else
                {
                    pathData[rIdx] = 0;
                    DisconnectPath(pathData, n, BasicRoadsTexturing.S);
                    DisconnectPath(pathData, e, BasicRoadsTexturing.W);
                    DisconnectPath(pathData, s, BasicRoadsTexturing.N);
                    DisconnectPath(pathData, w, BasicRoadsTexturing.E);
                    DisconnectPath(pathData, ne, BasicRoadsTexturing.SW);
                    DisconnectPath(pathData, se, BasicRoadsTexturing.NW);
                    DisconnectPath(pathData, sw, BasicRoadsTexturing.NE);
                    DisconnectPath(pathData, nw, BasicRoadsTexturing.SE);
                    Debug.LogFormat("Unmarked road at x:{0} y:{1}  index:{2}", mouseX, mouseY, rIdx);
                }

                UpdateMapLocationDotsTexture();
                changed = true;
            }
            else
            {
                if (zoom)
                {
                    zoom = false;
                    ZoomMapTextures();
                }
                base.ClickHandler(sender, position);
            }
        }

        private static bool ConnectPath(byte[] pathData, int rIdx, int dIdx, byte rDir, byte dDir, byte excl1 = 0, byte excl2 = 0)
        {
            if (dIdx >= 0 && dIdx < pathData.Length)
            {
                bool road = pathData[dIdx] != 0;
                bool exclude = pathData[dIdx] != 0xFF && (pathData[dIdx] & excl1) != 0 && (pathData[dIdx] & excl2) != 0;
                if (road && !exclude)
                {
                    pathData[rIdx] |= rDir;

                    if (pathData[dIdx] == 0xFF)
                        pathData[dIdx] = dDir;
                    else
                        pathData[dIdx] |= dDir;

                    return true;
                }
            }
            return false;
        }

        private static void DisconnectPath(byte[] pathData, int dIdx, byte dDir)
        {
            if (dIdx >= 0 && dIdx < pathData.Length && pathData[dIdx] != 0)
            {
                pathData[dIdx] &= (byte)~dDir;

                if (pathData[dIdx] == 0)
                    pathData[dIdx] = 0xFF;
            }
        }

        // Updates location dots
        protected override void UpdateMapLocationDotsTexture()
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

                    ContentReader.MapSummary summary;
                    if (DaggerfallUnity.ContentReader.HasLocation(originX + x, originY + y, out summary))
                    {
                        int index = GetPixelColorIndex(summary.LocationType);
                        if (index != -1)
                        {
//                            if (DaggerfallUnity.Settings.TravelMapLocationsOutline && roadDataPt != 0 && IsLocationLarge(summary.LocationType))
//                                locationDotsOutlinePixelBuffer[offset] = dotOutlineColor;
                            DrawLocation(offset5, width5, locationPixelColors[index], IsLocationLarge(summary.LocationType));
                        }
                    }

                    int pIdx = originX + x + ((originY + y) * MapsFile.MaxMapPixelX);
                    if (showPaths[BasicRoadsTexturing.streams])
                        DrawPath(offset5, width5, pathsData[BasicRoadsTexturing.streams][pIdx], streamColor, ref locationDotsPixelBuffer);
                    if (showPaths[BasicRoadsTexturing.tracks])
                        DrawPath(offset5, width5, pathsData[BasicRoadsTexturing.tracks][pIdx], trackColor, ref locationDotsPixelBuffer);
                    if (showPaths[BasicRoadsTexturing.rivers])
                        DrawPath(offset5, width5, pathsData[BasicRoadsTexturing.rivers][pIdx], riverColor, ref locationDotsPixelBuffer);
                    if (showPaths[BasicRoadsTexturing.roads])
                        DrawPath(offset5, width5, pathsData[BasicRoadsTexturing.roads][pIdx], roadColor, ref locationDotsPixelBuffer);

                    //Debug.LogFormat("Found road at x:{0} y:{1}  index:{2}", originX + x, originY + y, rIdx);
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

        void DrawLocation(int offset, int width, Color32 color, bool large)
        {
            int st = large ? 0 : 1;
            int en = large ? 5 : 4;
            for (int y = st; y < en; y++)
            {
                for (int x = st; x < en; x++)
                {
                    locationDotsPixelBuffer[offset + (y * width) + x] = color;
                }
            }
        }

        bool IsLocationLarge(DFRegion.LocationTypes locationType)
        {
            return locationType == DFRegion.LocationTypes.TownCity || locationType == DFRegion.LocationTypes.TownHamlet;
        }

        private static void DrawPath(int offset, int width, byte pathDataPt, Color32 pathColor, ref Color32[] pixelBuffer)
        {
            if (pathDataPt == 0)
                return;

            pixelBuffer[offset + (width * 2) + 2] = pathColor;
            if ((pathDataPt & BasicRoadsTexturing.S) != 0)
            {
                pixelBuffer[offset + 2] = pathColor;
                pixelBuffer[offset + width + 2] = pathColor;
            }
            if ((pathDataPt & BasicRoadsTexturing.SE) != 0)
            {
                pixelBuffer[offset + 4] = pathColor;
                pixelBuffer[offset + width + 3] = pathColor;
            }
            if ((pathDataPt & BasicRoadsTexturing.E) != 0)
            {
                pixelBuffer[offset + (width * 2) + 3] = pathColor;
                pixelBuffer[offset + (width * 2) + 4] = pathColor;
            }
            if ((pathDataPt & BasicRoadsTexturing.NE) != 0)
            {
                pixelBuffer[offset + (width * 3) + 3] = pathColor;
                pixelBuffer[offset + (width * 4) + 4] = pathColor;
            }
            if ((pathDataPt & BasicRoadsTexturing.N) != 0)
            {
                pixelBuffer[offset + (width * 3) + 2] = pathColor;
                pixelBuffer[offset + (width * 4) + 2] = pathColor;
            }
            if ((pathDataPt & BasicRoadsTexturing.NW) != 0)
            {
                pixelBuffer[offset + (width * 3) + 1] = pathColor;
                pixelBuffer[offset + (width * 4)] = pathColor;
            }
            if ((pathDataPt & BasicRoadsTexturing.W) != 0)
            {
                pixelBuffer[offset + (width * 2)] = pathColor;
                pixelBuffer[offset + (width * 2) + 1] = pathColor;
            }
            if ((pathDataPt & BasicRoadsTexturing.SW) != 0)
            {
                pixelBuffer[offset] = pathColor;
                pixelBuffer[offset + width + 1] = pathColor;
            }
        }

        // Zoom and pan region texture
        protected override void ZoomMapTextures()
        {
            base.ZoomMapTextures();

            if (RegionSelected && zoom)
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

        protected override void UpdateMouseOverLocation()
        {
            if (RegionSelected == false || FindingLocation)
                return;

            mouseOverRegion = selectedRegion;

            if (lastMousePos.x < 0 ||
                lastMousePos.x > regionTextureOverlayPanelRect.width ||
                lastMousePos.y < regionPanelOffset ||
                lastMousePos.y > regionTextureOverlayPanel.Size.y + regionPanelOffset)
                return;

            float scale = GetRegionMapScale(selectedRegion);
            Vector2 coordinates = GetCoordinates();
            int x = (int)(coordinates.x / scale);
            int y = (int)(coordinates.y / scale);

            if (selectedRegion == betonyIndex)      // Manually correct Betony offset
            {
                x += 60;
                y += 212;
            }

            if (selectedRegion == 61)               // Fix for Cybiades zoom-in map. Map is more zoomed in than for other regions but the pixel coordinates are not scaled to match.
            {
                int xDiff = x - 440;
                int yDiff = y - 340;
                xDiff /= 4;
                yDiff /= 4;
                x = 440 + xDiff;
                y = 340 + yDiff;
            }

            int sampleRegion = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetPoliticIndex(x, y) - 128;

            if (sampleRegion != selectedRegion && sampleRegion >= 0 && sampleRegion < DaggerfallUnity.Instance.ContentReader.MapFileReader.RegionCount)
            {
                mouseOverRegion = sampleRegion;
                return;
            }

            mouseX = x;
            mouseY = y;
        }

        private static void ReadEditedPathData(int pathType, string filename)
        {
            string filePath = Path.Combine(WorldDataReplacement.WorldDataPath, filename);
            if (File.Exists(filePath))
            {
                using (StreamReader file = new StreamReader(filePath))
                {
                    for (int l = 0; l < MapsFile.MaxMapPixelY; l++)
                    {
                        string line = file.ReadLine();
                        try
                        {
                            for (int i = 0; i < MapsFile.MaxMapPixelX; i++)
                            {
                                int index = (l * MapsFile.MaxMapPixelX) + i;
                                byte b = Convert.ToByte(line.Substring(i * 2, 2), 16);
                                if (allowDeletions || b != 0)
                                    pathsData[pathType][index] = b;
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning(line);
                            Debug.LogWarning(e.Message);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarningFormat("Edited path data not found in {0}, initialising editing using existing data.", filename);
            }
        }

        private static void WriteEditedPathData(int pathType, string filename)
        {
            using (StreamWriter file = new StreamWriter(Path.Combine(WorldDataReplacement.WorldDataPath, filename)))
            {
                byte[] existingData = roadsTexturing.GetPathData(pathType);
                for (int i = 0; i < pathsData[pathType].Length; i++)
                {
                    if (i != 0 && i % MapsFile.MaxMapPixelX == 0)
                        file.WriteLine();
                    file.Write((!allowDeletions && existingData[i] == pathsData[pathType][i]) ? "00" : pathsData[pathType][i].ToString("x2"));
                }
            }
        }


        #region console_commands

        public static class RoadPathEditorCommands
        {
            public static void RegisterCommands()
            {
                try
                {
                    ConsoleCommandsDatabase.RegisterCommand(PathEditorCmd.name, PathEditorCmd.description, PathEditorCmd.usage, PathEditorCmd.Execute);
                    ConsoleCommandsDatabase.RegisterCommand(ExportRoadDataCmd.name, ExportRoadDataCmd.description, ExportRoadDataCmd.usage, ExportRoadDataCmd.Execute);
                    ConsoleCommandsDatabase.RegisterCommand(ExportTrackDataCmd.name, ExportTrackDataCmd.description, ExportTrackDataCmd.usage, ExportTrackDataCmd.Execute);
                    ConsoleCommandsDatabase.RegisterCommand(ExportRiverDataCmd.name, ExportRiverDataCmd.description, ExportRiverDataCmd.usage, ExportRiverDataCmd.Execute);
                    ConsoleCommandsDatabase.RegisterCommand(ExportStreamDataCmd.name, ExportStreamDataCmd.description, ExportStreamDataCmd.usage, ExportStreamDataCmd.Execute);
                    ConsoleCommandsDatabase.RegisterCommand(ExportPathsPngCmd.name, ExportPathsPngCmd.description, ExportPathsPngCmd.usage, ExportPathsPngCmd.Execute);
                }
                catch (Exception ex)
                {
                    DaggerfallUnity.LogMessage(ex.Message, true);
                }
            }

            private static class PathEditorCmd
            {
                public static readonly string name = "patheditor";
                public static readonly string description = "Opens a map window for editing paths";
                public static readonly string usage = "patheditor";

                static ConsoleController controller;

                public static string Execute(params string[] args)
                {
                    if (FindController())
                        controller.ui.CloseConsole();

                    DaggerfallUI.UIManager.PushWindow(Instance);

                    return "Finished";
                }

                private static bool FindController()
                {
                    if (controller)
                        return true;

                    GameObject console = GameObject.Find("Console");
                    if (console && (controller = console.GetComponent<ConsoleController>()))
                        return true;

                    Debug.LogError("Failed to find console controller.");
                    return false;
                }
            }

            private static class ExportRoadDataCmd
            {
                public static readonly string name = "ExportRoadData";
                public static readonly string description = "Exports edited road paths as a binary file";
                public static readonly string usage = "exportroaddata";

                public static string Execute(params string[] args)
                {
                    File.WriteAllBytes(Path.Combine(WorldDataReplacement.WorldDataPath, BasicRoadsTexturing.RoadDataFilename), pathsData[BasicRoadsTexturing.roads]);
                    return "Exported edited road path data to: " + Path.Combine(WorldDataReplacement.WorldDataPath, BasicRoadsTexturing.RoadDataFilename);
                }
            }
            private static class ExportTrackDataCmd
            {
                public static readonly string name = "ExportTrackData";
                public static readonly string description = "Exports edited dirt track paths as a binary file";
                public static readonly string usage = "exporttrackdata";

                public static string Execute(params string[] args)
                {
                    File.WriteAllBytes(Path.Combine(WorldDataReplacement.WorldDataPath, BasicRoadsTexturing.TrackDataFilename), pathsData[BasicRoadsTexturing.tracks]);
                    return "Exported edited dirt track path data to: " + Path.Combine(WorldDataReplacement.WorldDataPath, BasicRoadsTexturing.TrackDataFilename);
                }
            }

            private static class ExportRiverDataCmd
            {
                public static readonly string name = "ExportRiverData";
                public static readonly string description = "Exports edited river paths as a binary file";
                public static readonly string usage = "exportriverdata";

                public static string Execute(params string[] args)
                {
                    File.WriteAllBytes(Path.Combine(WorldDataReplacement.WorldDataPath, BasicRoadsTexturing.RiverDataFilename), pathsData[BasicRoadsTexturing.rivers]);
                    return "Exported edited river path data to: " + Path.Combine(WorldDataReplacement.WorldDataPath, BasicRoadsTexturing.RiverDataFilename);
                }
            }
            private static class ExportStreamDataCmd
            {
                public static readonly string name = "ExportStreamData";
                public static readonly string description = "Exports edited stream paths as a binary file";
                public static readonly string usage = "exportstreamdata";

                public static string Execute(params string[] args)
                {
                    File.WriteAllBytes(Path.Combine(WorldDataReplacement.WorldDataPath, BasicRoadsTexturing.StreamDataFilename), pathsData[BasicRoadsTexturing.streams]);
                    return "Exported edited stream path data to: " + Path.Combine(WorldDataReplacement.WorldDataPath, BasicRoadsTexturing.StreamDataFilename);
                }
            }

            private static class ExportPathsPngCmd
            {
                public static readonly string name = "ExportPathsPng";
                public static readonly string description = "Exports path data to PNG file";
                public static readonly string usage = "exportpathspng";

                public static string Execute(params string[] args)
                {
                    int scale = 5;
                    int width5 = MapsFile.MaxMapPixelX * scale;
                    Color32[] pixelBuffer = new Color32[MapsFile.MaxMapPixelX * scale * MapsFile.MaxMapPixelY * scale];
                    for (int y = 0; y < MapsFile.MaxMapPixelY; y++)
                    {
                        for (int x = 0; x < MapsFile.MaxMapPixelX; x++)
                        {
                            int offset = (x * scale) + ((MapsFile.MaxMapPixelY - y - 1) * scale * width5);
                            int pIdx = x + (y * MapsFile.MaxMapPixelX);
                            DrawPath(offset, width5, pathsData[BasicRoadsTexturing.tracks][pIdx], trackColor, ref pixelBuffer);
                            DrawPath(offset, width5, pathsData[BasicRoadsTexturing.roads][pIdx], roadColor, ref pixelBuffer);
                        }
                    }
                    Texture2D pathTex = new Texture2D(MapsFile.MaxMapPixelX * scale, MapsFile.MaxMapPixelY * scale);
                    pathTex.SetPixels32(pixelBuffer);
                    pathTex.Apply();

                    byte[] png = pathTex.EncodeToPNG();

                    File.WriteAllBytes(Path.Combine(DaggerfallUnity.Settings.PersistentDataPath, "BasicRoads-paths.png"), png);
                    return "Exported path data to PNG file.";
                }
            }

        }

        #endregion
    }
}
