// Project:         Archaeologists Guild for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2018 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Utility;
using UnityEngine;

namespace Archaeologists
{
    public class LocatorDevice : MonoBehaviour
    {
        const string indicatorFilename = "SUN_00I0.IMG";

        private float lastTime = 0;

        Texture2D indicatorTexure;
        Vector2 indicatorSize;

        private Vector3 questTargetPos = Vector3.zero;

        void Start()
        {
            indicatorTexure = DaggerfallUI.GetTextureFromImg(indicatorFilename);
            indicatorSize = new Vector2(indicatorTexure.width, indicatorTexure.height);

            PlayerEnterExit.OnTransitionDungeonInterior += OnTransitionToDungeonInterior;
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransitionToDungeonExterior;

            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon && questTargetPos == Vector3.zero)
            {
                questTargetPos = GetQuestTargetLocation();
                Debug.LogFormat("Quest target is at: {0} {1} {2}", questTargetPos.x, questTargetPos.y, questTargetPos.z);
            }

        }

        void OnGUI()
        {
            if (Event.current.type.Equals(EventType.Repaint) && !GameManager.IsGamePaused)
            {
                if (questTargetPos != Vector3.zero)
                {
                    Camera mainCamera = GameManager.Instance.MainCamera;
                    Vector3 screenPos = mainCamera.WorldToScreenPoint(questTargetPos);
                    if (screenPos.z > 0)
                    {
                        // Draw texture behind other HUD elements & weapons.
                        GUI.depth = 2;
                        // Calculate position for indicator and draw it.
                        float dist = Mathf.Clamp(screenPos.z / 16, 1, 16);
                        Rect pos = new Rect(new Vector2(screenPos.x, Screen.height - screenPos.y), indicatorSize / dist);
                        GUI.DrawTexture(pos, indicatorTexure);
                    }
                }
            }
        }

        void Update()
        {
            if (Time.realtimeSinceStartup > lastTime + 10)
                lastTime = Time.realtimeSinceStartup;
        }

        public Vector3 GetQuestTargetLocation()
        {
            QuestMarker targetMarker;
            Vector3 buildingOrigin;
            bool result = QuestMachine.Instance.GetCurrentLocationQuestMarker(MarkerTypes.QuestItem, out targetMarker, out buildingOrigin);
            if (!result)
            {
                result = QuestMachine.Instance.GetCurrentLocationQuestMarker(MarkerTypes.QuestSpawn, out targetMarker, out buildingOrigin);
                if (!result)
                    return Vector3.zero;
            }

            Vector3 dungeonBlockPosition = new Vector3(targetMarker.dungeonX * RDBLayout.RDBSide, 0, targetMarker.dungeonZ * RDBLayout.RDBSide);
            return dungeonBlockPosition + targetMarker.flatPosition + buildingOrigin;
        }


        private void OnTransitionToDungeonInterior(PlayerEnterExit.TransitionEventArgs args)
        {
            questTargetPos = GetQuestTargetLocation();
            Debug.LogFormat("Quest target is at: {0} {1} {2}", questTargetPos.x, questTargetPos.y, questTargetPos.z);
        }

        private void OnTransitionToDungeonExterior(PlayerEnterExit.TransitionEventArgs args)
        {
            questTargetPos = Vector3.zero;
        }

    }
}