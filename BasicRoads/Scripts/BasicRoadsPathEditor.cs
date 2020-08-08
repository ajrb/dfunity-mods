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

namespace BasicRoads
{
    /// <summary>
    /// Implements a road path editor based on DFUs travel map.
    /// </summary>
    public class BasicRoadsPathEditor : DaggerfallTravelMapWindow
    {
        Color32 roadColor = new Color32(60, 60, 60, 255);

        static BasicRoadsPathEditor instance;
        public static BasicRoadsPathEditor Instance {
            get {
                if (instance == null)
                    instance = new BasicRoadsPathEditor(DaggerfallUI.UIManager);
                return instance;
            }
        }

        public static byte[] roadData = new byte[MapsFile.MaxMapPixelX * MapsFile.MaxMapPixelY];

        int mouseX = 0;
        int mouseY = 0;

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
        }
        
        public override void OnPush()
        {
            base.OnPush();

            ReadRoadData();
        }

        private static void ReadRoadData()
        {
            using (System.IO.StreamReader file = new System.IO.StreamReader(@"roadData.txt"))
            {
                for (int l = 0; l < MapsFile.MaxMapPixelY; l++)
                {
                    string line = file.ReadLine();
                    try
                    {
                        for (int i = 0; i < MapsFile.MaxMapPixelX; i++)
                        {
                            int index = (l * MapsFile.MaxMapPixelX) + i;
                            roadData[index] = Convert.ToByte(line.Substring(i * 2, 2), 16);
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

        public override void OnPop()
        {
            base.OnPop();

            WriteRoadData();
        }

        private static void WriteRoadData()
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"roadData.txt"))
            {
                for (int i = 0; i < roadData.Length; i++)
                {
                    if (i != 0 && i % MapsFile.MaxMapPixelX == 0)
                        file.WriteLine();
                    file.Write(roadData[i].ToString("x2"));
                }
            }
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
            }
            else
                base.ClickHandler(sender, position);
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

                    // Set location pixel if inside region area
                    if (sampleRegion == selectedRegion)
                    {
                        int rIdx = originX + x + ((originY + y) * MapsFile.MaxMapPixelX);
                        byte roads = roadData[rIdx];
                        if (roads != 0)
                        {
                            locationDotsPixelBuffer[offset] = roadColor;
                            //Debug.LogFormat("Found road at x:{0} y:{1}  index:{2}", originX + x, originY + y, rIdx);
                        }

                        ContentReader.MapSummary summary;
                        if (DaggerfallUnity.Instance.ContentReader.HasLocation(originX + x, originY + y, out summary))
                        {
                            int index = GetPixelColorIndex(summary.LocationType);
                            if (index == -1)
                                continue;
                            else
                            {
                                if (DaggerfallUnity.Settings.TravelMapLocationsOutline && roads != 0)
                                    locationDotsOutlinePixelBuffer[offset] = dotOutlineColor;
                                locationDotsPixelBuffer[offset] = locationPixelColors[index];
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


        #region console_commands

        public static class RoadPathEditorCommands
        {
            public static void RegisterCommands()
            {
                try
                {
                    ConsoleCommandsDatabase.RegisterCommand(RoadPathEditorCmd.name, RoadPathEditorCmd.description, RoadPathEditorCmd.usage, RoadPathEditorCmd.Execute);
                }
                catch (System.Exception ex)
                {
                    DaggerfallUnity.LogMessage(ex.Message, true);
                }
            }

            private static class RoadPathEditorCmd
            {
                public static readonly string name = "roadeditor";
                public static readonly string description = "Opens a map window for editing road paths";
                public static readonly string usage = "roadeditor";

                public static string Execute(params string[] args)
                {
                    DaggerfallUI.UIManager.PushWindow(Instance);
                    return "Finished";
                }
            }
        }

        #endregion
    }
}
