// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2019 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using DaggerfallWorkshop.Game.Entity;
using System;
using UnityEngine;

namespace DaggerfallWorkshop.Game
{
    public class EnhancedRiding : MonoBehaviour
    {
        public float LookPitchRatio = 2.6f;
        public float extX = 0.06f;
        public float extW = 0.78f;

        const int nativeScreenHeight = 200;

        PlayerMotor playerMotor;
        TransportManager transportManager;

        bool lastRiding;

        public bool CanRunUnlessRidingCart()
        {
            return !(GameManager.Instance.TransportManager.TransportMode == TransportModes.Cart && playerMotor.IsRiding);
        }

        void Start()
        {
            playerMotor = GetComponent<PlayerMotor>();
            if (!playerMotor)
                throw new Exception("PlayerMotor not found.");
            lastRiding = playerMotor.IsRiding;

            transportManager = GetComponent<TransportManager>();
            if (!transportManager)
                throw new Exception("TransportManager not found.");
            transportManager.DrawHorse = false;

            GameManager.Instance.SpeedChanger.CanRun = CanRunUnlessRidingCart;
        }

        void Update()
        {
            if (lastRiding != playerMotor.IsRiding)
            {
                // Set min pitch.
                if (lastRiding = playerMotor.IsRiding)
                    GameManager.Instance.PlayerMouseLook.PitchMaxLimit = 18;
                else
                    GameManager.Instance.PlayerMouseLook.PitchMaxLimit = PlayerMouseLook.PitchMax;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (playerMotor.IsRiding && playerMotor.IsRunning)
            {
                Debug.Log("Ridden over NPC!");
                PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
                Transform npcTransform = other.gameObject.transform;
                MobilePersonNPC mobileNpc = npcTransform.GetComponent<MobilePersonNPC>();
                if (mobileNpc)
                {
                    if (!mobileNpc.Billboard.IsUsingGuardTexture)
                    {
                        EnemyBlood blood = npcTransform.GetComponent<EnemyBlood>();
                        if (blood)
                            blood.ShowBloodSplash(0, playerMotor.transform.position + (playerMotor.transform.forward * 2) + playerMotor.transform.up);
                        playerEntity.SpawnCityGuards(true);
                    }
                    else
                    {
                        GameObject guard = playerEntity.SpawnCityGuard(mobileNpc.transform.position, mobileNpc.transform.forward);
                    }
                    playerEntity.CrimeCommitted = PlayerEntity.Crimes.Assault;  // Nearest to manslaughter
                    mobileNpc.Motor.gameObject.SetActive(false);
                }
            }
        }

        void OnGUI()
        {
            if (Event.current.type.Equals(EventType.Repaint) && !GameManager.IsGamePaused)
            {
                ImageData ridingTexture = transportManager.RidingTexture;
                if ((transportManager.TransportMode == TransportModes.Horse || transportManager.TransportMode == TransportModes.Cart) && ridingTexture.texture != null)
                {
                    // Draw horse texture behind other HUD elements & weapons.
                    GUI.depth = 2;
                    // Get horse texture scaling factor. (base on height to avoid aspect ratio issues like fat horses)
                    float horseScaleY = (float)Screen.height / (float)nativeScreenHeight;
                    float horseScaleX = horseScaleY * TransportManager.ScaleFactorX;

                    float yAdj = 0;
                    PlayerMouseLook playerMouseLook = GameManager.Instance.PlayerMouseLook;
                    if (playerMouseLook)
                    {
                        yAdj = (playerMouseLook.Pitch - 10) * LookPitchRatio;
                    }

                    // Calculate position for horse texture and draw it.
                    Rect pos = new Rect(
                                    Screen.width / 2f - (ridingTexture.width * horseScaleX) / 2f,
                                    Screen.height - ((ridingTexture.height + yAdj) * horseScaleY),
                                    ridingTexture.width * horseScaleX,
                                    ridingTexture.height * horseScaleY);
                    GUI.DrawTexture(pos, ridingTexture.texture);

                    float drawBottom = pos.y + pos.height - horseScaleY;
                    //Debug.LogFormat("db= {0} ({1}+{2}) sh={3}", drawBottom, pos.y, pos.height, Screen.height);
                    //Debug.Log(yAdj);
                    if (drawBottom < Screen.height)
                    {
                        float yAdjExt = yAdj / 100;
                        Rect posExt = new Rect(pos.x, drawBottom, (ridingTexture.width - 14) * horseScaleX, Screen.height - drawBottom + horseScaleY);
                        GUI.DrawTextureWithTexCoords(posExt, ridingTexture.texture, new Rect(extX, 0.2f - yAdjExt, extW, yAdjExt));
                    }

                }
            }

        }
    }
}
