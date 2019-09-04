// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2019 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallConnect;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
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
        GameObject cachedColliderHitObject;


        // Delegate for PlayerSpeedChanger - allows horse running.
        public bool CanRunUnlessRidingCart()
        {
            return !(GameManager.Instance.TransportManager.TransportMode == TransportModes.Cart && playerMotor.IsRiding);
        }

        // Initialise.
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

        // Update the mouse look pitch limit when riding status changes.
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

        // Handle trampling civilian NPCs.
        private void OnTriggerEnter(Collider other)
        {
            if (playerMotor.IsRiding && playerMotor.IsRunning)
            {
                PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
                Transform npcTransform = other.gameObject.transform;
                MobilePersonNPC mobileNpc = npcTransform.GetComponent<MobilePersonNPC>();
                if (mobileNpc)
                {
                    Debug.Log("Rode over an NPC trampling them!");
                    if (!mobileNpc.Billboard.IsUsingGuardTexture)
                    {
                        EnemyBlood blood = npcTransform.GetComponent<EnemyBlood>();
                        if (blood)
                            blood.ShowBloodSplash(0, BloodPos());
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

        // Handle charging into enemies.
        private void OnControllerColliderHit(ControllerColliderHit other)
        {
            if (cachedColliderHitObject != other.gameObject)
            {
                DaggerfallEntityBehaviour hitEntityBehaviour = other.gameObject.GetComponent<DaggerfallEntityBehaviour>();
                if (playerMotor.IsRiding && playerMotor.IsRunning && hitEntityBehaviour)
                {
                    if (hitEntityBehaviour.Entity is EnemyEntity)
                    {
                        EnemyEntity hitEnemyEntity = (EnemyEntity) hitEntityBehaviour.Entity;
                        if (!hitEnemyEntity.PickpocketByPlayerAttempted)
                        {
                            Debug.LogFormat("Charged down a {0}!", other.gameObject.name);
                            hitEnemyEntity.PickpocketByPlayerAttempted = true;
                            DaggerfallEntityBehaviour playerEntityBehaviour = GameManager.Instance.PlayerEntity.EntityBehaviour;
                            int damage = FormulaHelper.CalculateHandToHandMaxDamage(GameManager.Instance.PlayerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.HandToHand));
                            hitEntityBehaviour.DamageHealthFromSource(playerEntityBehaviour, damage * 2, true, BloodPos());
                            GameManager.Instance.PlayerEntity.DecreaseFatigue(PlayerEntity.DefaultFatigueLoss * 15);
                        }
                    }
                }
                else
                    cachedColliderHitObject = other.gameObject;
            }
        }

        private Vector3 BloodPos()
        {
            return playerMotor.transform.position + (playerMotor.transform.forward * 2) + playerMotor.transform.up;
        }

        // Handler for enhanced horse animations.
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

                    // Draw additional horse neck if required.
                    float drawBottom = pos.y + pos.height - horseScaleY;
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
