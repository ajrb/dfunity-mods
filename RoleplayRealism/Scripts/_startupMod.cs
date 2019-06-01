// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2018 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
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

            // Override adjust to hit mod and damage formulas
            FormulaHelper.formula_2de_2i.Add("AdjustWeaponHitChanceMod", AdjustWeaponHitChanceMod);
            FormulaHelper.formula_2de_2i.Add("AdjustWeaponAttackDamage", AdjustWeaponAttackDamage);

            Debug.Log("Finished mod init: RoleplayRealism");
        }

        private static void BedActivation(Transform transform)
        {
            Debug.Log("zzzzzzzzzz!");
            IUserInterfaceManager uiManager = DaggerfallUI.UIManager;
            uiManager.PushWindow(new DaggerfallRestWindow(uiManager, true));
        }

        private static int AdjustWeaponHitChanceMod(DaggerfallEntity attacker, DaggerfallEntity target, int hitChanceMod, int weaponAnimTime, DaggerfallUnityItem weapon)
        {
            if (weaponAnimTime > 0 && (weapon.TemplateIndex == (int)Weapons.Short_Bow || weapon.TemplateIndex == (int)Weapons.Long_Bow))
            {
                int adjustedHitChanceMod = hitChanceMod;
                if (weaponAnimTime < 200)
                    adjustedHitChanceMod -= 40;
                else if (weaponAnimTime < 500)
                    adjustedHitChanceMod -= 10;
                else if (weaponAnimTime < 1000)
                    adjustedHitChanceMod = hitChanceMod;
                else if (weaponAnimTime < 2000)
                    adjustedHitChanceMod += 10;
                else if (weaponAnimTime > 4000)
                    adjustedHitChanceMod -= 10;
                else if (weaponAnimTime > 6000)
                    adjustedHitChanceMod -= 20;

                Debug.LogFormat("Adjusted Weapon HitChanceMod for bow drawing from {0} to {1} (t={2}ms)", hitChanceMod, adjustedHitChanceMod, weaponAnimTime);
                return adjustedHitChanceMod;
            }

            return hitChanceMod;
        }

        private static int AdjustWeaponAttackDamage(DaggerfallEntity attacker, DaggerfallEntity target, int damage, int weaponAnimTime, DaggerfallUnityItem weapon)
        {
            if (weaponAnimTime > 0 && (weapon.TemplateIndex == (int)Weapons.Short_Bow || weapon.TemplateIndex == (int)Weapons.Long_Bow))
            {
                double adjustedDamage = damage;
                if (weaponAnimTime < 1000)
                    adjustedDamage *= (double)weaponAnimTime / 1000;
                else if (weaponAnimTime > 4000)
                    adjustedDamage *= 0.85;
                else if (weaponAnimTime > 6000)
                    adjustedDamage *= 0.75;
                else if (weaponAnimTime > 8000)
                    adjustedDamage *= 0.5;
                else if (weaponAnimTime > 10000)
                    adjustedDamage *= 0.25;

                Debug.LogFormat("Adjusted Weapon Damage for bow drawing from {0} to {1} (t={2}ms)", damage, (int)adjustedDamage, weaponAnimTime);
                return (int)adjustedDamage;
            }
            Debug.Log("AdjustWeaponAttackDamage for bow drawing. " + damage);

            return damage;
        }


    }
}