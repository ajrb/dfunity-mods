// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2023 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Utility;
using UnityEngine;

namespace TravelOptions
{
    // Override DFU travel time calculator to allow cost to be manipulated.

    public class TravelTimeCalculatorTO : TravelTimeCalculator
    {
        private int scaleInns = 1;
        private int scaleShips = 1;

        public void SetScaleFactors(int inns, int ships)
        {
            Debug.LogFormat("setting scale factors inns={0}, ships={1}", inns, ships);
            scaleInns = inns;
            scaleShips = ships;
        }

        public override void CalculateTripCost(int travelTimeInMinutes, bool sleepModeInn, bool hasShip, bool travelShip)
        {
            base.CalculateTripCost(travelTimeInMinutes, sleepModeInn, hasShip, travelShip);

            Debug.LogFormat("scaleInns={0}, scaleShips={1}", scaleInns, scaleShips);
            Debug.LogFormat("Before: pieces={0}, total={1}", piecesCost, totalCost);

            int shipCost = totalCost - piecesCost;

            piecesCost *= scaleInns;
            piecesCost = FormulaHelper.CalculateTradePrice(piecesCost, 10, false);

            shipCost *= scaleShips;
            shipCost = FormulaHelper.CalculateTradePrice(shipCost, 10, false);

            totalCost = piecesCost + shipCost;

            Debug.LogFormat("After: pieces={0}, total={1}", piecesCost, totalCost);
        }
    }
}