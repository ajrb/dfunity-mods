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
        static Mod mod;

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

            DaggerfallUnity.Instance.TerrainTexturing = new BasicRoadsTexturing(smoothRoads, editingEnabled);

            if (editingEnabled)
            {
                BasicRoadsPathEditor editor = BasicRoadsPathEditor.Instance;
            }

            mod.IsReady = true;
            Debug.Log("Finished mod init: BasicRoads");
        }

    }
}
