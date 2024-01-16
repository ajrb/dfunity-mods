// Project:         RoleplayRealism mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2021 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility;

namespace RoleplayRealism
{
    public class GuildServiceTrainingRR : DaggerfallGuildServiceTraining, IMacroContextProvider
    {
        protected const DaggerfallMessageBox.MessageBoxButtons weekButton = (DaggerfallMessageBox.MessageBoxButtons)21;
        protected static TextFile.Token newLine = TextFile.CreateFormatToken(TextFile.Formatting.JustifyCenter);

        DFCareer.Skills skillToTrain;
        int trainingCost = 0;
        int intensiveCost = 0;

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
            Mod rrMod = ModManager.Instance.GetMod("RoleplayRealism");
            bool skillPrices = rrMod.GetSettings().GetBool("RefinedTraining", "variableTrainingPrice");
            bool intensive = rrMod.GetSettings().GetBool("RefinedTraining", "intensiveTraining");

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
                if (skillPrices)
                {
                    float skillOfMax = 1 - ((float)skillValue / trainingMax);
                    trainingCost -= (int)(trainingCost * skillOfMax / 2);
                }

                // Offer training and cost to player
                TextFile.Token[] tokens = DaggerfallUnity.Instance.TextProvider.GetRSCTokens(TrainingOfferId);
                int pos = tokens[0].text.IndexOf(" ");
                tokens[0].text = tokens[0].text.Substring(0, pos) + " " + skillToTrain + tokens[0].text.Substring(pos);

                intensiveCost = (trainingCost + (playerEntity.Level * 8) + 72) * 5;
                TextFile.Token[] trainingTokens =
                {
                    TextFile.CreateTextToken(string.Format(RoleplayRealism.Localize("trainingSkill1"), skillToTrain)), newLine, newLine,
                    TextFile.CreateTextToken(RoleplayRealism.Localize("trainingSkill2")), newLine,
                    TextFile.CreateTextToken(string.Format(RoleplayRealism.Localize("trainingSkill3"), intensiveCost)), newLine, newLine,
                    TextFile.CreateTextToken(string.Format(RoleplayRealism.Localize("trainingSkill4"), skillToTrain)), newLine,
                };

                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, uiManager.TopWindow);
                if (intensive && skillValue < trainingMax - 4)
                {
                    messageBox.SetTextTokens(trainingTokens, this);
                    messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                    messageBox.AddButton(weekButton);
                }
                else
                {
                    messageBox.SetTextTokens(tokens, this);
                    messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                }
                messageBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
                messageBox.OnButtonClick += ConfirmTrainingPayment_OnButtonClick;
                messageBox.Show();
            }

        }

        protected void ConfirmTrainingPayment_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            CloseWindow();
            if (skillToTrain != DFCareer.Skills.None)
            {
                if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
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
                else if (messageBoxButton == weekButton)
                {
                    UnityEngine.Debug.Log("Train for a week!");
                    if (playerEntity.GetGoldAmount() >= intensiveCost)
                    {
                        // Take payment
                        playerEntity.DeductGoldAmount(intensiveCost);
                        // Train the skill
                        TrainSkillIntense(skillToTrain);
                    }
                    else
                        DaggerfallUI.MessageBox(DaggerfallTradeWindow.NotEnoughGoldId);
                }
            }
        }

        protected void TrainSkillIntense(DFCareer.Skills skillToTrain)
        {
            DaggerfallDateTime now = DaggerfallUnity.Instance.WorldTime.Now;
            now.RaiseTime(DaggerfallDateTime.SecondsPerDay * 4);
            playerEntity.Skills.SetPermanentSkillValue(skillToTrain, (short)(playerEntity.Skills.GetPermanentSkillValue(skillToTrain) + 4));

            TrainSkill(skillToTrain);

            TextFile.Token[] intenseTokens =
            {
                TextFile.CreateTextToken(RoleplayRealism.Localize("trainingSkillIntense1")), newLine,
                TextFile.CreateTextToken(string.Format(RoleplayRealism.Localize("trainingSkillIntense2"), skillToTrain)), newLine, newLine,
                TextFile.CreateTextToken(RoleplayRealism.Localize("trainingSkillIntense3"))
            };
            DaggerfallUI.MessageBox(intenseTokens);
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
