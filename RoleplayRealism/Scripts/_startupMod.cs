// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2018 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using UnityEngine;

namespace RoleplayRealism
{
    public class _startupMod : MonoBehaviour
    {
        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void InitStart(InitParams initParams)
        {
            InitMod();
        }

        /* 
        *   used for debugging
        *   howto debug:
        *       -) add a dummy GameObject to DaggerfallUnityGame scene
        *       -) attach this script (_startupMod) as component
        *       -) deactivate mod in mod list (since dummy gameobject will start up mod)
        *       -) attach debugger and set breakpoint to one of the mod's cs files and debug
        */
        void Awake()
        {
            InitMod(true);
        }

        public static void InitMod(bool debug = false)
        {
            Debug.Log("Begin mod init: RoleplayRealism");

            PlayerActivate.RegisterModelActivation(41000, BedActivation);
            PlayerActivate.RegisterModelActivation(41001, BedActivation);
            PlayerActivate.RegisterModelActivation(41002, BedActivation);

            Debug.Log("Finished mod init: RoleplayRealism");
        }

        private static void BedActivation(Transform transform)
        {
            Debug.Log("zzzzzzzzzz!");
            IUserInterfaceManager uiManager = DaggerfallUI.UIManager;
            uiManager.PushWindow(new DaggerfallRestWindow(uiManager, true));
        }

    }
}