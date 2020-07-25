// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2019 Jedidia
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Jedidia

using System;
using System.Reflection;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;

namespace TravelOptions
{
    public class PlayerAutoPilot
    {
        private ContentReader.MapSummary destinationSummary;
        private DFPosition destinationMapPixel = null;
        private Rect destinationWorldRect;
        private PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
        private DFPosition lastPlayerMapPixel = new DFPosition(int.MaxValue, int.MaxValue);
        private bool inDestinationMapPixel = false;
        private InputManager inputManager = InputManager.Instance;
        private Transform cameraTransform = GameManager.Instance.PlayerMouseLook.GetComponent<Transform>();
        private PlayerMouseLook mouseLook = GameManager.Instance.PlayerMouseLook;
        private Vector3 pitchVector = new Vector3(0, 0, 0);
        private Vector3 yawVector = new Vector3(0, 0, 0);


        // some reflection-fu to get access to a private function. Don't judge me, if there was another way I'd use it.
        private MethodInfo applyHorizontalForce = InputManager.Instance.GetType().GetMethod("ApplyVerticalForce", BindingFlags.NonPublic | BindingFlags.Instance);

        public PlayerAutoPilot(ContentReader.MapSummary destinationSummary)
        {
            this.destinationSummary = destinationSummary;
            Init();
        }

        private void Init()
        {
            destinationMapPixel = MapsFile.GetPixelFromPixelID(destinationSummary.ID);

            // get exact coordinates of destination
            destinationWorldRect = GetLocationRect(destinationSummary);
            //grow the rect a bit so fast travel cancels shortly before entering the location
            destinationWorldRect.xMin -= 1000;
            destinationWorldRect.xMax += 1000;
            destinationWorldRect.yMin -= 1000;
            destinationWorldRect.yMax += 1000;
        }

        public void Update()
        {
            if (inDestinationMapPixel)
            {
                if (isPlayerInArrivalRect())
                {
                    // note that event will be raised whenever player is inside destination rect when update is called.
                    RaiseOnArrivalEvent();
                    return;
                }
            }

            // get the players current map position and reorient if it changed. Just  to make sure we're staying on track.
            var playerPos = GameManager.Instance.PlayerGPS.CurrentMapPixel;
            if (playerPos.X != lastPlayerMapPixel.X || playerPos.Y != lastPlayerMapPixel.Y)
            {
                lastPlayerMapPixel = playerPos;
                SetNewYaw();

                inDestinationMapPixel = lastPlayerMapPixel.X == destinationMapPixel.X && lastPlayerMapPixel.Y == destinationMapPixel.Y;
            }

            SetPlayerOrientation();

            // keep mouselook shut off
            mouseLook.simpleCursorLock = true;
            mouseLook.enableMouseLook = false;
            // make the player move forward
            applyHorizontalForce.Invoke(inputManager, new object[] { 1 });

        }

        /// <summary>
        /// Checks if player is in arrival rect.
        /// Does not use the playerGPS function because the arrival rect is a bit bigger than the location rect,
        /// so the player stops a bit outside it.
        /// </summary>
        /// <returns></returns>
        private bool isPlayerInArrivalRect()
        {
            return (destinationWorldRect.Contains(new Vector2(playerGPS.WorldX, playerGPS.WorldZ)));
        }

 

        private void SetNewYaw()
        {
            var playerPos = new DFPosition(playerGPS.WorldX, playerGPS.WorldZ);
            yawVector.y = CalculateYaw(playerPos,
                new DFPosition(
                    (int)destinationWorldRect.center.x,
                    (int)destinationWorldRect.center.y));
        }

        private void SetPlayerOrientation()
        {
            cameraTransform.localEulerAngles = pitchVector;
            mouseLook.characterBody.transform.localEulerAngles = yawVector;
        }


        private float CalculateYaw(DFPosition fromWorldPos, DFPosition toWorldPos)
        {
            double angleRad = Math.Atan2(fromWorldPos.X - toWorldPos.X, fromWorldPos.Y - toWorldPos.Y);
            double angleDeg = angleRad * 180.0 / Math.PI + 180;
            Debug.Log((float)angleDeg);
            return (float)angleDeg;
        }


        public static Rect GetLocationRect(ContentReader.MapSummary mapSummary)
        {
            if (DaggerfallUnity.Instance.ContentReader.GetLocation(mapSummary.RegionIndex, mapSummary.MapIndex, out DFLocation targetLocation))
                return DaggerfallLocation.GetLocationRect(targetLocation);

            throw new ArgumentException("Travel destination not found!");
        }

        // events
        public delegate void OnArrivalHandler();
        public event OnArrivalHandler OnArrival;
        void RaiseOnArrivalEvent()
        {
            MouseLookAtDestination();
            if (OnArrival != null)
                OnArrival();
        }

        public void MouseLookAtDestination()
        {
            // set the player up so he's facing the destination.
            mouseLook.Pitch = 0f;
            mouseLook.Yaw = yawVector.y;
        }
    }

}
