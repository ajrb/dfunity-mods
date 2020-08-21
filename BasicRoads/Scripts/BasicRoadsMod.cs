// Project:         BasicRoads mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

namespace BasicRoads
{
    public class BasicRoadsMod : MonoBehaviour
    {
        public const string GET_PATH_DATA = "getPathData";

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

            ModSettings settings = mod.GetSettings();
            bool smoothRoads = settings.GetBool("Settings", "SmoothRoads");
            bool editingEnabled = settings.GetBool("Editing", "EditingEnabled");

            roadTexturing = new BasicRoadsTexturing(smoothRoads, editingEnabled);
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
            switch (message)
            {
                case GET_PATH_DATA:
                    callBack?.Invoke(GET_PATH_DATA, roadTexturing.GetPathData((int)data));
                    break;

                default:
                    Debug.LogErrorFormat("{0}: unknown message received ({1}).", this, message);
                    break;
            }
        }
    }
}
