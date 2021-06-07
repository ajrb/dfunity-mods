// Project:         BasicRoads mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using Unity.Collections;
using Unity.Jobs;

namespace BasicRoads
{
    public class BasicRoadsMod : MonoBehaviour
    {
        public const string GET_PATH_DATA = "getPathData";
        public const string GET_ROAD_POINT = "getRoadPoint";
        public const string GET_TRACK_POINT = "getTrackPoint";
        public const string GET_PATHS_POINT = "getPathsPoint";
        public const string SCHEDULE_ROADS_JOB = "scheduleRoadsJob";

        static Mod mod;
        static BasicRoadsTexturing roadTexturing;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<BasicRoadsMod>();
        }

        void Awake()
        {
            Debug.Log("Begin mod init: BasicRoads");

            // If splatmapping mod is enabled, paint thicker dirt tracks as thin diagonal ones look awful.
            Mod splatMod = ModManager.Instance.GetModFromGUID("21a4ed84-12c3-4979-b1ef-8b9169d03922");
            bool splatModEnabled = splatMod != null && splatMod.Enabled;

            ModSettings settings = mod.GetSettings();
            bool smoothRoads = settings.GetBool("Settings", "SmoothRoads");
            bool editingEnabled = settings.GetBool("Editing", "EditingEnabled");

            roadTexturing = new BasicRoadsTexturing(smoothRoads, editingEnabled, splatModEnabled);
            DaggerfallUnity.Instance.TerrainTexturing = roadTexturing;

            if (editingEnabled)
            {
                BasicRoadsPathEditor editor = BasicRoadsPathEditor.Instance;
            }

            mod.MessageReceiver = MessageReceiver;
            mod.IsReady = true;
            Debug.Log("Finished mod init: BasicRoads");
        }

        private void MessageReceiver(string message, object data, DFModMessageCallback callBack)
        {
            try {

                Vector2Int mpCoords;
                byte point;
                switch (message)
                {
                    case GET_PATH_DATA:
                        callBack?.Invoke(GET_PATH_DATA, roadTexturing.GetPathData((int)data));
                        break;

                    case GET_ROAD_POINT:
                        mpCoords = (Vector2Int)data;
                        point = roadTexturing.GetPathDataPoint(BasicRoadsTexturing.roads, mpCoords.x, mpCoords.y);
                        callBack?.Invoke(GET_ROAD_POINT, point);
                        break;

                    case GET_TRACK_POINT:
                        mpCoords = (Vector2Int)data;
                        point = roadTexturing.GetPathDataPoint(BasicRoadsTexturing.tracks, mpCoords.x, mpCoords.y);
                        callBack?.Invoke(GET_TRACK_POINT, point);
                        break;

                    case GET_PATHS_POINT:
                        mpCoords = (Vector2Int)data;
                        byte roadPt = roadTexturing.GetPathDataPoint(BasicRoadsTexturing.roads, mpCoords.x, mpCoords.y);
                        byte trackPt = roadTexturing.GetPathDataPoint(BasicRoadsTexturing.tracks, mpCoords.x, mpCoords.y);
                        point = (byte)(roadPt | trackPt);
                        callBack?.Invoke(GET_PATHS_POINT, point);
                        break;

                    case SCHEDULE_ROADS_JOB:
                        // Get the parameters
                        object[] paramArray = (object[])data;
                        MapPixelData mapData = (MapPixelData)paramArray[0];
                        NativeArray<byte> tileData = (NativeArray<byte>)paramArray[1];
                        JobHandle dependencies = (JobHandle)paramArray[2];

                        // Instantiate PaintRoadsJob, schedule, then return job handle
                        JobHandle paintRoadsHandle = roadTexturing.SchedulePaintRoadsJob(ref mapData, ref tileData, dependencies);
                        callBack?.Invoke(SCHEDULE_ROADS_JOB, paintRoadsHandle);
                        break;

                    default:
                        Debug.LogErrorFormat("{0}: unknown message received ({1}).", this, message);
                        break;
                }
            }
            catch
            {
                Debug.LogErrorFormat("{0}: error handling message ({1}).", this, message);
                callBack?.Invoke("error", "Data passed is invalid for " + message);
            }
        }
    }
}
