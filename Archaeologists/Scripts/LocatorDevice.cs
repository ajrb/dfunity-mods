// Project:         Archaeologists Guild for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2018 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace Archaeologists
{
    public class LocatorDevice : MonoBehaviour
    {
        const string indicatorFilename = "SUN_00I0.IMG";

        private Vector3 questTargetPos = Vector3.zero;

        Texture2D indicatorTexure;
        Vector2 indicatorSize;

        void Start()
        {
            indicatorTexure = DaggerfallUI.GetTextureFromImg(indicatorFilename);
            indicatorSize = new Vector2(indicatorTexure.width, indicatorTexure.height);

            SaveLoadManager.OnLoad += SaveLoadManager_OnLoad;
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransitionToDungeonExterior;

            enabled = false;
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

        public void ActivateDevice()
        {
            questTargetPos = GetQuestTargetLocation();
            enabled = true;
            Debug.LogFormat("Locator device activated, target is at: {0} {1} {2}", questTargetPos.x, questTargetPos.y, questTargetPos.z);
        }

        public void DeactivateDevice()
        {
            questTargetPos = Vector3.zero;
            enabled = false;
            Debug.Log("Locator device deactivated.");
        }

        private Vector3 GetQuestTargetLocation()
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

        private void SaveLoadManager_OnLoad(SaveData_v1 saveData)
        {
            DeactivateDevice();
            List<DaggerfallUnityItem> wands = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Jewellery, 140);
            foreach(DaggerfallUnityItem item in wands)
            {
                if (item.nativeMaterialValue == LocatorItem.ACTIVATED)
                {
                    ActivateDevice();
                    return;
                }
            }
        }

        private void OnTransitionToDungeonExterior(PlayerEnterExit.TransitionEventArgs args)
        {
            DeactivateDevice();
            RemoveActiveDevices(GameManager.Instance.PlayerEntity.Items);
            RemoveActiveDevices(GameManager.Instance.PlayerEntity.WagonItems);
        }

        private static void RemoveActiveDevices(ItemCollection collection)
        {
            List<DaggerfallUnityItem> wands = collection.SearchItems(ItemGroups.Jewellery, 140);
            foreach (DaggerfallUnityItem item in wands)
            {
                if (item.nativeMaterialValue == LocatorItem.ACTIVATED)
                    collection.RemoveItem(item);
            }
        }
    }
}