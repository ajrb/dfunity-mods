// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

// add mcp to alter amount?

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    public class GuildServiceTrainingRR : DaggerfallGuildServiceTraining, IMacroContextProvider
    {
        DFCareer.Skills skillToTrain;
        int trainingCost = 0;

        public GuildServiceTrainingRR(IUserInterfaceManager uiManager, GuildNpcServices npcService, IGuild guild)
            : base(uiManager, npcService, guild)
        {
        }

        protected override void TrainingService()
        {
            // Check enough time has passed since last trained
            DaggerfallDateTime now = DaggerfallUnity.Instance.WorldTime.Now;
            if ((now.ToClassicDaggerfallTime() - playerEntity.TimeOfLastSkillTraining) < 720)
            {
                TextFile.Token[] tokens = DaggerfallUnity.Instance.TextProvider.GetRandomTokens(TrainingToSoonId);
                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, uiManager.TopWindow);
                messageBox.SetTextTokens(tokens);
                messageBox.ClickAnywhereToClose = true;
                messageBox.Show();
            }
            else
            {
                // Show skill picker loaded with guild training skills
                DaggerfallListPickerWindow skillPicker = new DaggerfallListPickerWindow(uiManager, this);
                skillPicker.OnItemPicked += TrainingSkillPicker_OnItemPicked;

                foreach (DFCareer.Skills skill in GetTrainingSkills())
                    skillPicker.ListBox.AddItem(DaggerfallUnity.Instance.TextProvider.GetSkillName(skill));

                uiManager.PushWindow(skillPicker);
            }
        }

        protected void TrainingSkillPicker_OnItemPicked(int index, string skillName)
        {
            CloseWindow();
            List<DFCareer.Skills> trainingSkills = GetTrainingSkills();
            skillToTrain = trainingSkills[index];
            int trainingMax = Guild.GetTrainingMax(skillToTrain);
            int skillValue = playerEntity.Skills.GetPermanentSkillValue(skillToTrain);

            if (skillValue > trainingMax)
            {
                // Inform player they're too skilled to train
                TextFile.Token[] tokens = DaggerfallUnity.Instance.TextProvider.GetRandomTokens(TrainingTooSkilledId);
                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, uiManager.TopWindow);
                messageBox.SetTextTokens(tokens, Guild);
                messageBox.ClickAnywhereToClose = true;
                messageBox.Show();
            }
            else
            {
                // Calculate training price, modifying based on current skill value as well as player level if enabled
                trainingCost = Guild.GetTrainingPrice();

                Mod rrMod = ModManager.Instance.GetMod("RoleplayRealism");
                bool skillPrices = rrMod.GetSettings().GetBool("RefinedTraining", "variableTrainingPrice");
                if (skillPrices)
                {
                    float skillOfMax = 1 - ((float)skillValue / trainingMax);
                    trainingCost -= (int)(trainingCost * skillOfMax / 2);
                }

                // Offer training price
                TextFile.Token[] tokens = DaggerfallUnity.Instance.TextProvider.GetRSCTokens(TrainingOfferId);
                int pos = tokens[0].text.IndexOf(" ");
                tokens[0].text = tokens[0].text.Substring(0, pos) + " " + skillToTrain + tokens[0].text.Substring(pos);

                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, uiManager.TopWindow);
                messageBox.SetTextTokens(tokens, this);
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
                messageBox.OnButtonClick += ConfirmTrainingPayment_OnButtonClick;
                messageBox.Show();
            }

        }

        protected void ConfirmTrainingPayment_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            CloseWindow();
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes && skillToTrain != DFCareer.Skills.None)
            {
                if (playerEntity.GetGoldAmount() >= trainingCost)
                {
                    // Take payment
                    playerEntity.DeductGoldAmount(trainingCost);
                    // Train the skill
                    TrainSkill(skillToTrain);
                }
                else
                    DaggerfallUI.MessageBox(DaggerfallTradeWindow.NotEnoughGoldId);
            }
        }

        #region Macro handling

        public MacroDataSource GetMacroDataSource()
        {
            return new GuildServiceTraininRRMacroDataSource(this);
        }

        /// <summary>
        /// MacroDataSource context sensitive methods for guild service training UI. Override guild training amount method.
        /// </summary>
        private class GuildServiceTraininRRMacroDataSource : MacroDataSource
        {
            private GuildServiceTrainingRR parent;
            public GuildServiceTraininRRMacroDataSource(GuildServiceTrainingRR guildServiceWindow)
            {
                this.parent = guildServiceWindow;
            }

            public override string Amount()
            {
                return parent.trainingCost.ToString();
            }
        }

        #endregion

    }
}
