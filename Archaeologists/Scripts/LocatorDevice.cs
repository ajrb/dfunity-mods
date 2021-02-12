// Project:         Archaeologists Guild for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using UnityEngine;
using System.Collections.Generic;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Utility;

namespace Archaeologists
{
    public class LocatorDevice : MonoBehaviour
    {
        const string indicatorFilename = "SUN_00I0.IMG";
        internal const string SMALLDUNGEON_MSG = "Actually that dungeon is really small, so you wont be needing a free locator device.";

        private Vector3 questTargetPos = Vector3.zero;

        Texture2D indicatorTexure;
        Vector2 indicatorSize;

        void Start()
        {
            indicatorTexure = DaggerfallUI.GetTextureFromImg(indicatorFilename);
            indicatorSize = new Vector2(indicatorTexure.width, indicatorTexure.height);

            // Register listner for start quest event so free locator device can be given for dungeons
            QuestMachine.OnQuestStarted += QuestMachine_OnQuestStarted;

            // Register listeners for loading game and exiting dungeons - so that state can be updated
            SaveLoadManager.OnLoad += SaveLoadManager_OnLoad;
            PlayerEnterExit.OnPreTransition += OnTransitionToDungeonExterior;
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
            if (questTargetPos != Vector3.zero)
            {
                enabled = true;
                Debug.LogFormat("Locator device activated, target is at: {0} {1} {2}", questTargetPos.x, questTargetPos.y, questTargetPos.z);
            }
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
            bool result = QuestMachine.Instance.GetCurrentLocationQuestMarker(out targetMarker, out buildingOrigin);
            if (!result)
            {
                Debug.Log("Problem getting quest marker.");
                return Vector3.zero;
            }
            Vector3 dungeonBlockPosition = new Vector3(targetMarker.dungeonX * RDBLayout.RDBSide, 0, targetMarker.dungeonZ * RDBLayout.RDBSide);
            return dungeonBlockPosition + targetMarker.flatPosition + buildingOrigin;
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

        #region Event listeners

        private void QuestMachine_OnQuestStarted(Quest quest)
        {
            // If quest is from archaeologists and involves a dungeon place, give player a locator device
            if (quest.FactionId == ArchaeologistsGuild.FactionId)
            {
                QuestResource[] foundResources = quest.GetAllResources(typeof(Place));
                foreach (Place place in foundResources)
                {
                    if (place.SiteDetails.siteType == SiteTypes.Dungeon)
                    {
                        if (DaggerfallUnity.Settings.SmallerDungeons)
                            DaggerfallUI.MessageBox(SMALLDUNGEON_MSG);
                        else
                            GameManager.Instance.PlayerEntity.Items.AddItem(new LocatorItem(), ItemCollection.AddPosition.DontCare, true);
                        break;
                    }
                }
            }
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
            if (args.TransitionType == PlayerEnterExit.TransitionType.ToDungeonExterior)
            {
                DeactivateDevice();
                RemoveActiveDevices(GameManager.Instance.PlayerEntity.Items);
                RemoveActiveDevices(GameManager.Instance.PlayerEntity.WagonItems);
            }
        }

        #endregion
    }
}