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
        public const string RoadDataFilename = "roadData.txt";

        Color32 roadColor = new Color32(60, 60, 60, 255);

        protected Rect roadOverlayPanelRect = new Rect(0, regionPanelOffset, 320 * 5, 160 * 5);
        protected Panel roadOverlayPanel;

        static BasicRoadsPathEditor instance;
        public static BasicRoadsPathEditor Instance {
            get {
                if (instance == null)
                    instance = new BasicRoadsPathEditor(DaggerfallUI.UIManager);
                return instance;
            }
        }

        public static byte[] roadData;

        static BasicRoadsTexturing roadTexturing;

        int mouseX = 0;
        int mouseY = 0;
        bool outlineBackup;
        bool changed;

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

            roadTexturing = (BasicRoadsTexturing)DaggerfallUnity.Instance.TerrainTexturing;
            roadData = roadTexturing.GetRoadData();
            ReadEditedRoadData();
        }

        public override void OnPush()
        {
            base.OnPush();

            outlineBackup = DaggerfallUnity.Settings.TravelMapLocationsOutline;
            DaggerfallUnity.Settings.TravelMapLocationsOutline = false;

            ReadEditedRoadData();

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

            locationDotsPixelBuffer = new Color32[(int)regionTextureOverlayPanelRect.width * (int)regionTextureOverlayPanelRect.height * 25];
            locationDotsTexture = new Texture2D((int)regionTextureOverlayPanelRect.width * 5, (int)regionTextureOverlayPanelRect.height * 5, TextureFormat.ARGB32, false);
        }

        protected override void ExitButtonClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            if (RegionSelected)
                CloseRegionPanel();
            else if (changed)
            {
                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, this);
                messageBox.SetText("Do you want to save changes?");
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
                WriteEditedRoadData();
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

                if (roadData[rIdx] == 0)
                {
                    bool rn = ConnectRoad(rIdx, n, BasicRoadsTexturing.N, BasicRoadsTexturing.S);
                    bool re = ConnectRoad(rIdx, e, BasicRoadsTexturing.E, BasicRoadsTexturing.W);
                    bool rs = ConnectRoad(rIdx, s, BasicRoadsTexturing.S, BasicRoadsTexturing.N);
                    bool rw = ConnectRoad(rIdx, w, BasicRoadsTexturing.W, BasicRoadsTexturing.E);

                    if (!rn && !re) ConnectRoad(rIdx, ne, BasicRoadsTexturing.NE, BasicRoadsTexturing.SW);
                    if (!rs && !re) ConnectRoad(rIdx, se, BasicRoadsTexturing.SE, BasicRoadsTexturing.NW);
                    if (!rs && !rw) ConnectRoad(rIdx, sw, BasicRoadsTexturing.SW, BasicRoadsTexturing.NE);
                    if (!rn && !rw) ConnectRoad(rIdx, nw, BasicRoadsTexturing.NW, BasicRoadsTexturing.SE);

                    if (roadData[rIdx] == 0)
                        roadData[rIdx] = 0xFF;

                    Debug.LogFormat("Marked road at x:{0} y:{1}  index:{2}  byte: {3}", mouseX, mouseY, rIdx, Convert.ToString(roadData[rIdx], 2));
                }
                else
                {
                    roadData[rIdx] = 0;
                    DisconnectRoad(n, BasicRoadsTexturing.S);
                    DisconnectRoad(e, BasicRoadsTexturing.W);
                    DisconnectRoad(s, BasicRoadsTexturing.N);
                    DisconnectRoad(w, BasicRoadsTexturing.E);
                    DisconnectRoad(ne, BasicRoadsTexturing.SW);
                    DisconnectRoad(se, BasicRoadsTexturing.NW);
                    DisconnectRoad(sw, BasicRoadsTexturing.NE);
                    DisconnectRoad(nw, BasicRoadsTexturing.SE);
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

        private static bool ConnectRoad(int rIdx, int dIdx, byte rDir, byte dDir)
        {
            if (dIdx >= 0 && dIdx < roadData.Length)
            {
                bool road = roadData[dIdx] != 0;
                if (road)
                {
                    roadData[rIdx] |= rDir;

                    if (roadData[dIdx] == 0xFF)
                        roadData[dIdx] = dDir;
                    else
                        roadData[dIdx] |= dDir;
                }
                return road;
            }
            return false;
        }

        private static void DisconnectRoad(int dIdx, byte dDir)
        {
            if (dIdx >= 0 && dIdx < roadData.Length && roadData[dIdx] != 0)
            {
                roadData[dIdx] &= (byte)~dDir;

                if (roadData[dIdx] == 0)
                    roadData[dIdx] = 0xFF;
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
                    int sampleRegion = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetPoliticIndex(originX + x, originY + y) - 128;

                    int width5 = width * 5;
                    int offset5 = (int)((((height - y - 1) * 5 * width5) + (x * 5)) * scale);

                    int rIdx = originX + x + ((originY + y) * MapsFile.MaxMapPixelX);
                    byte roads = roadData[rIdx];

                    ContentReader.MapSummary summary;
                    if (DaggerfallUnity.Instance.ContentReader.HasLocation(originX + x, originY + y, out summary))
                    {
                        int index = GetPixelColorIndex(summary.LocationType);
                        if (index != -1)
                        {
                            if (DaggerfallUnity.Settings.TravelMapLocationsOutline && roads != 0 && IsLocationLarge(summary.LocationType))
                                locationDotsOutlinePixelBuffer[offset] = dotOutlineColor;

                            DrawLocation(offset5, width5, locationPixelColors[index], IsLocationLarge(summary.LocationType));
                        }
                    }

                    if (roads != 0)
                    {
                        DrawRoad(offset5, width5, roads);
                        //Debug.LogFormat("Found road at x:{0} y:{1}  index:{2}", originX + x, originY + y, rIdx);
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

        private void DrawRoad(int offset, int width, byte roads)
        {
            locationDotsPixelBuffer[offset + (width * 2) + 2] = roadColor;
            if ((roads & BasicRoadsTexturing.S) != 0)
            {
                locationDotsPixelBuffer[offset + 2] = roadColor;
                locationDotsPixelBuffer[offset + width + 2] = roadColor;
            }
            if ((roads & BasicRoadsTexturing.SE) != 0)
            {
                locationDotsPixelBuffer[offset + 4] = roadColor;
                locationDotsPixelBuffer[offset + width + 3] = roadColor;
            }
            if ((roads & BasicRoadsTexturing.E) != 0)
            {
                locationDotsPixelBuffer[offset + (width * 2) + 3] = roadColor;
                locationDotsPixelBuffer[offset + (width * 2) + 4] = roadColor;
            }
            if ((roads & BasicRoadsTexturing.NE) != 0)
            {
                locationDotsPixelBuffer[offset + (width * 3) + 3] = roadColor;
                locationDotsPixelBuffer[offset + (width * 4) + 4] = roadColor;
            }
            if ((roads & BasicRoadsTexturing.N) != 0)
            {
                locationDotsPixelBuffer[offset + (width * 3) + 2] = roadColor;
                locationDotsPixelBuffer[offset + (width * 4) + 2] = roadColor;
            }
            if ((roads & BasicRoadsTexturing.NW) != 0)
            {
                locationDotsPixelBuffer[offset + (width * 3) + 1] = roadColor;
                locationDotsPixelBuffer[offset + (width * 4)] = roadColor;
            }
            if ((roads & BasicRoadsTexturing.W) != 0)
            {
                locationDotsPixelBuffer[offset + (width * 2)] = roadColor;
                locationDotsPixelBuffer[offset + (width * 2) + 1] = roadColor;
            }
            if ((roads & BasicRoadsTexturing.SW) != 0)
            {
                locationDotsPixelBuffer[offset] = roadColor;
                locationDotsPixelBuffer[offset + width + 1] = roadColor;
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

        private static void ReadEditedRoadData()
        {
            string filePath = Path.Combine(WorldDataReplacement.WorldDataPath, RoadDataFilename);
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
                                if (b != 0)
                                    roadData[index] = b;
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
                Debug.LogWarning("Edited road data not found, initialising editing using existing data.");
            }
        }

        private static void WriteEditedRoadData()
        {
            using (StreamWriter file = new StreamWriter(Path.Combine(WorldDataReplacement.WorldDataPath, RoadDataFilename)))
            {
                byte[] existingData = roadTexturing.GetRoadData();
                for (int i = 0; i < roadData.Length; i++)
                {
                    if (i != 0 && i % MapsFile.MaxMapPixelX == 0)
                        file.WriteLine();
                    file.Write((existingData[i] == roadData[i]) ? "00" : roadData[i].ToString("x2"));
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
                    ConsoleCommandsDatabase.RegisterCommand(RoadPathEditorCmd.name, RoadPathEditorCmd.description, RoadPathEditorCmd.usage, RoadPathEditorCmd.Execute);
                    ConsoleCommandsDatabase.RegisterCommand(ExportRoadPathsCmd.name, ExportRoadPathsCmd.description, ExportRoadPathsCmd.usage, ExportRoadPathsCmd.Execute);
                }
                catch (Exception ex)
                {
                    DaggerfallUnity.LogMessage(ex.Message, true);
                }
            }

            private static class RoadPathEditorCmd
            {
                public static readonly string name = "roadeditor";
                public static readonly string description = "Opens a map window for editing road paths";
                public static readonly string usage = "roadeditor";

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

            private static class ExportRoadPathsCmd
            {
                public static readonly string name = "ExportRoadData";
                public static readonly string description = "Exports edited road paths as a binary file";
                public static readonly string usage = "exportroaddata";

                public static string Execute(params string[] args)
                {
                    File.WriteAllBytes(Path.Combine(WorldDataReplacement.WorldDataPath, BasicRoadsTexturing.RoadDataFilename), roadData);
                    return "Exported edited road path data to: " + Path.Combine(WorldDataReplacement.WorldDataPath, BasicRoadsTexturing.RoadDataFilename);
                }

            }
        }

        #endregion
    }
}
