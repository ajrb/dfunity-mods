// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using UnityEngine;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Utility;
using System;
using DaggerfallWorkshop.Utility.AssetInjection;

namespace TravelOptions
{
    public class TravelOptionsMapWindow : DaggerfallTravelMapWindow
    {
        const string portsOffName = "TOportsOff.png";
        const string portsOnName = "TOportsOn.png";

        Texture2D portsOffTexture;
        Texture2D portsOnTexture;

        protected Vector2 portFilterPos = new Vector2(231, 180);
        protected Vector2 portFilterMoved = new Vector2(231, 173);

        protected Vector2 horizArrowPos = new Vector2(231, 176);
        protected Vector2 horizArrowMoved = new Vector2(231, 184);

        protected Vector2 vertArrowPos = new Vector2(254, 176);
        protected Vector2 vertArrowMoved = new Vector2(254, 184);

        protected Button portsFilterButton;

        protected bool portsFilter = false;

        public TravelOptionsMapWindow(IUserInterfaceManager uiManager)
            : base(uiManager)
        {
        }

        protected override void Setup()
        {
            base.Setup();

            if (TravelOptionsMod.Instance.ShipTravelPortsOnly)
            {
                // Towns filter button
                if (!TextureReplacement.TryImportImage(portsOffName, true, out portsOffTexture))
                    return;
                if (!TextureReplacement.TryImportImage(portsOnName, true, out portsOnTexture))
                    return;

                portsFilterButton = new Button();
                portsFilterButton.Position = portFilterPos;
                portsFilterButton.Size = new Vector2(portsOffTexture.width, portsOffTexture.height);
                portsFilterButton.BackgroundTexture = portsOffTexture;
                portsFilterButton.OnMouseClick += PortsFilterButton_OnMouseClick;
                NativePanel.Components.Add(portsFilterButton);
            }
        }

        private void PortsFilterButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            portsFilter = !portsFilter;
            portsFilterButton.BackgroundTexture = portsFilter ? portsOnTexture : portsOffTexture;

            UpdateMapLocationDotsTexture();
        }

        protected override void SetupArrowButtons()
        {
            base.SetupArrowButtons();

            if (TravelOptionsMod.Instance.ShipTravelPortsOnly)
            {
                // Move the port filter button and arrow buttons if needed
                if (verticalArrowButton.Enabled || horizontalArrowButton.Enabled)
                {
                    portsFilterButton.Position = portFilterMoved;
                    horizontalArrowButton.Position = horizArrowMoved;
                    verticalArrowButton.Position = vertArrowMoved;
                }
                else
                {
                    portsFilterButton.Position = portFilterPos;
                    horizontalArrowButton.Position = horizArrowPos;
                    verticalArrowButton.Position = vertArrowPos;
                }
            }
        }

        public override void OnPush()
        {
            // Check if there's an active destination
            TravelOptionsMod travelModInstance = TravelOptionsMod.Instance;
            if (!string.IsNullOrEmpty(travelModInstance.DestinationName) && !travelModInstance.GetTravelControlUI().isShowing) //&& travelMod.TravelUi != null 
            {
                Debug.Log("Active destination: " + travelModInstance.DestinationName);

                string resume = string.Format("Resume travel to {0}?", travelModInstance.DestinationName);
                DaggerfallMessageBox resumeMsgBox = new DaggerfallMessageBox(uiManager, DaggerfallMessageBox.CommonMessageBoxButtons.YesNo, resume, uiManager.TopWindow);
                resumeMsgBox.OnButtonClick += (_sender, button) =>
                {
                    CloseWindow();
                    if (button == DaggerfallMessageBox.MessageBoxButtons.Yes)
                    {
                        CloseWindow();
                        travelModInstance.BeginTravel();
                    }
                };
                resumeMsgBox.Show();
            }

            base.OnPush();
        }

        protected override bool checkLocationDiscovered(ContentReader.MapSummary summary)
        {
            // If ports filter is on, only return true if it's a port
            if (portsFilter && !Array.Exists(portLocationIds, n => n == summary.ID))
                return false;

            return base.checkLocationDiscovered(summary);
        }

        public static readonly int[] portLocationIds = {
            443401, 280614, 285597, 485856, 86496, 137544, 139547, 143535, 143542, 149513,
            150625, 158629, 162631, 162646, 164648, 166644, 168652, 169640, 170654, 178663,
            182685, 188727, 192653, 195681, 201654, 202646, 203671, 225685, 234712, 22763,
            184263, 189097, 192248, 194242, 194279, 196245, 199102, 199111, 201125, 210132,
            212138, 213207, 226205, 228209, 235146, 236143, 239139, 239144, 239146, 241140,
            91170, 93168, 96150, 96212, 107137, 109167, 325404, 325406, 328409, 341392,
            342399, 343397, 344387, 345378, 345383, 346398, 347375, 348372, 348396, 350370,
            351392, 352369, 353387, 354364, 361382, 364381, 369385, 369388, 370441, 372411,
            372439, 373407, 373415, 373422, 373425, 373427, 373429, 374419, 120375, 121377,
            148460, 148463, 150459, 158499, 168357, 172455, 187406, 192361, 193358, 193366,
            195353, 195361, 197366, 200356, 277751, 278764, 279644, 279697, 279749, 279754,
            279766, 280747, 281656, 281658, 281663, 281699, 281702, 281704, 281741, 281770,
            282712, 282724, 282728, 282731, 282734, 282737, 283687, 283707, 284685, 285682,
            286674, 289737, 292695, 293697, 310763, 311766, 194855, 195860, 223828, 225840,
            229847, 236854, 240841, 242856, 243846, 244859, 247836, 249839, 249861, 249866,
            250875, 255876, 256887, 256900, 257889, 258892, 258907, 261923, 261925, 262907,
            262931, 264900, 264902, 264940, 264942, 265956, 266964, 273975, 5222, 5224,
            11215, 14210, 23240, 35152, 49219, 157795, 181800, 187807, 193793, 210785,
            215821, 216791, 112707, 133701, 133718, 134711, 135713, 135717, 135735, 138745,
            140758, 140760, 148782, 151788, 83668, 125675, 111631, 111645, 112652, 113637,
            113646, 113649, 115622, 118573, 134553, 137558, 137561, 137593, 138583, 139588,
            145609, 146607, 147614, 148589, 151591, 152587, 56637, 35449, 41483, 121473,
            129449, 29347, 40361, 69406, 160305, 451180, 451186, 453173, 455174, 457179,
            458198, 460176, 461173, 463171, 468168, 468188, 473169, 474207, 476162, 476164,
            477177, 478159, 483153, 493144, 495141, 422217, 432218, 433205, 435202, 455199,
            459220, 405246, 405263, 406266, 407241, 408235, 408249, 417227, 393300, 397296,
            403279, 406276, 418291, 364449, 370446, 402451, 276583, 279596, 290582, 294569,
            295564, 296558, 297552, 305534, 308524, 308527, 308530, 309521, 312518, 313516,
            316550, 318514, 334515, 339496, 341496, 346475, 351470, 337704, 263832, 264825,
            269847, 269849, 276835, 277798, 278817, 278843, 279815, 283779, 283782, 287827,
            287829, 289866, 294842, 302839, 306854, 337914, 338912, 341916, 346918, 351919,
            354916, 357915, 357918, 361913, 363915, 364868, 370908, 379876, 379888, 380885,
            381881, 382879, 278962, 281872, 281969, 324981, 469891, 437653, 446471, 472431,
            480415, 217966, 100086, 121067, 123073, 144059, 75104, 77077, 83137, 86218,
            86334, 89333, 343439,
/*
            // Extras from TT:
            205676,     // "Isle of Balfiera", "Blackhead"
            278901,     // "Mournoth", "Zagoparia"
            263119,     // "Betony", "Whitefort"
            148062,     // "Tulune", "The Citadel of Hearthham"
            144059,     // "Tulune", "The Elyzanna Assembly"
*/
        };
    }
}
