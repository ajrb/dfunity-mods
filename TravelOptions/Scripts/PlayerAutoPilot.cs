// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2019 Jedidia
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Jedidia
// Contributors:    Hazelnut

using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using System;

namespace TravelOptions
{
    public class PlayerAutoPilot
    {
        private const int ArrivalBuffer = 800;

        private ContentReader.MapSummary destinationSummary;
        private float travelSpeedMultiplier;
        private DFPosition destinationMapPixel = null;
        private Rect destinationWorldRect;
        private DFPosition destinationCentre;
        private PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
        private DFPosition lastPlayerMapPixel = new DFPosition(int.MaxValue, int.MaxValue);
        private bool inDestinationMapPixel = false;
        private Transform cameraTransform = GameManager.Instance.PlayerMouseLook.GetComponent<Transform>();
        private PlayerMouseLook mouseLook = GameManager.Instance.PlayerMouseLook;
        private Vector3 pitchVector = new Vector3(0, 0, 0);
        private Vector3 yawVector = new Vector3(0, 0, 0);

        public PlayerAutoPilot(DFPosition targetPixel, Rect targetRect, float travelSpeedMultiplier = 1f)
        {
            InitTargetRect(targetPixel, targetRect, travelSpeedMultiplier);
        }

        public void InitTargetRect(DFPosition targetPixel, Rect targetRect, float travelSpeedMultiplier)
        {
            destinationMapPixel = targetPixel;
            destinationWorldRect = targetRect;
            destinationCentre = new DFPosition((int)targetRect.center.x, (int)targetRect.center.y);
            this.travelSpeedMultiplier = travelSpeedMultiplier;
            mouseLook.Pitch = 0;
        }

        public PlayerAutoPilot(ContentReader.MapSummary destinationSummary, float travelSpeedMultiplier = 1f)
        {
            this.destinationSummary = destinationSummary;
            this.travelSpeedMultiplier = travelSpeedMultiplier;
            InitDestination();
        }

        private void InitDestination()
        {
            destinationMapPixel = MapsFile.GetPixelFromPixelID(destinationSummary.ID);

            // Set rect coordinates of destination
            destinationWorldRect = GetLocationRect(destinationSummary);
            destinationCentre = new DFPosition((int)destinationWorldRect.center.x, (int)destinationWorldRect.center.y);

            // Grow the rect a bit so fast travel cancels shortly before entering the location
            destinationWorldRect.xMin -= ArrivalBuffer;
            destinationWorldRect.xMax += ArrivalBuffer;
            destinationWorldRect.yMin -= ArrivalBuffer;
            destinationWorldRect.yMax += ArrivalBuffer;
        }

        public void Update()
        {
            if (inDestinationMapPixel)
            {
                if (IsPlayerInArrivalRect())
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
            InputManager.Instance.ApplyVerticalForce(travelSpeedMultiplier);

        }

        /// <summary>
        /// Checks if player is in arrival rect.
        /// Does not use the playerGPS function because the arrival rect is a bit bigger than the location rect,
        /// so the player stops a bit outside it.
        /// </summary>
        /// <returns></returns>
        private bool IsPlayerInArrivalRect()
        {
            var playerPos = new DFPosition(playerGPS.WorldX, playerGPS.WorldZ);
            float yaw = CalculateYaw(playerPos, destinationCentre);
            float dy = Mathf.Abs(yaw - yawVector.y);
            if (dy > 5)
                return true;    // If yaw changes more than 5 degrees in one update, must have overshot target
            else
                yawVector.y = yaw;

            return destinationWorldRect.Contains(new Vector2(playerGPS.WorldX, playerGPS.WorldZ));
        }

        private float CalculateYaw(DFPosition fromWorldPos, DFPosition toWorldPos)
        {
            float angleRad = Mathf.Atan2(fromWorldPos.X - toWorldPos.X, fromWorldPos.Y - toWorldPos.Y);
            return angleRad * 180 / Mathf.PI + 180;
        }

        private void SetNewYaw()
        {
            var playerPos = new DFPosition(playerGPS.WorldX, playerGPS.WorldZ);
            yawVector.y = CalculateYaw(playerPos, destinationCentre);
#if UNITY_EDITOR
            Debug.Log("Set yaw = " + yawVector.y);
#endif
        }

        private void SetPlayerOrientation()
        {
            mouseLook.Yaw = yawVector.y;
            cameraTransform.localEulerAngles = pitchVector;
            mouseLook.characterBody.transform.localEulerAngles = yawVector;
        }

        public static Rect GetLocationRect(ContentReader.MapSummary mapSummary)
        {
            DFLocation targetLocation;
            if (DaggerfallUnity.Instance.ContentReader.GetLocation(mapSummary.RegionIndex, mapSummary.MapIndex, out targetLocation))
                return DaggerfallLocation.GetLocationRect(targetLocation);

            throw new ArgumentException("Travel destination not found!");
        }

        // events
        public delegate void OnArrivalHandler();
        public event OnArrivalHandler OnArrival;
        void RaiseOnArrivalEvent()
        {
            if (destinationSummary.ID != 0)
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
