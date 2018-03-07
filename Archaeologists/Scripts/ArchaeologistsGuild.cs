// Project:         Archaeologists Guild for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2018 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Entity;
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Items;
using Archaeologists;

namespace DaggerfallWorkshop.Game.Guilds
{
    public class ArchaeologistsGuild : Guild
    {
        #region Constants

        private const int factionId = 1000;
        private const int LocatorServiceId = 1001;

        #endregion

        #region Static Data

        protected static TextFile.Token newLine = TextFile.CreateFormatToken(TextFile.Formatting.JustifyCenter);

        // Guild messages - must clone any that contain macros before returning.

        protected static TextFile.Token[] welcomeTokens = new TextFile.Token[]
        {
            TextFile.CreateTextToken("Excellent, %pcn, welcome to the Archaeologists! "), newLine,
            TextFile.CreateTextToken("We'll get you started on field work as soon as possible. "), newLine,
            TextFile.CreateTextToken("You'll begin your Archaeologists career with the title "), newLine,
            TextFile.CreateTextToken("of %lev, however, with hard work and dedication, "), newLine,
            TextFile.CreateTextToken("I'm sure that you'll be recognised for promotion soon enough. "), newLine,
            TextFile.CreateTextToken("Please ensure you make use of our training facilities to "), newLine,
            TextFile.CreateTextToken("study and improve your skills. "), newLine,
        };

        protected static TextFile.Token[] eligibleTokens = new TextFile.Token[]
        {
            TextFile.CreateTextToken("Yes, you seem like a good candidate to assist us "), newLine,
            TextFile.CreateTextToken("in the kinds of field work we engage in. "), newLine, newLine,
            TextFile.CreateTextToken("We offer classes in all the various obscure languages of"), newLine,
            TextFile.CreateTextToken("Tamriel, as well as some of the more practical skills "), newLine,
            TextFile.CreateTextToken("required in the field while searching remote locations "), newLine,
            TextFile.CreateTextToken("for interesting antiquities and rare artifacts. "), newLine, newLine,
            TextFile.CreateTextToken("Once you rise in our ranks to the title of Field Officer, "), newLine,
            TextFile.CreateTextToken("you will gain access to our magicka-powered locator device "), newLine,
            TextFile.CreateTextToken("to use for whatever purpose you wish. Until then you will be "), newLine,
            TextFile.CreateTextToken("restricted to a single charge for each task we set you. "), newLine, newLine,
            TextFile.CreateTextToken("Beyond field work, our higher ranks are open to the more "), newLine,
            TextFile.CreateTextToken("accomplished scholars among us, and provide a great reduction "), newLine,
            TextFile.CreateTextToken("in the cost of locator device charges. Be aware, only "), newLine,
            TextFile.CreateTextToken("those with sufficient intellect will be accepted. "), newLine,
        };

        protected static TextFile.Token[] ineligibleLowSkillTokens = new TextFile.Token[]
        {
            TextFile.CreateTextToken("I am sad to say that you are not eligible to join our guild."), newLine,
            TextFile.CreateTextToken("We only accept members who have studied languages or the "), newLine,
            TextFile.CreateTextToken("other skills useful for field work; such as climbing, "), newLine,
            TextFile.CreateTextToken("lockpicking and stealth. "), newLine,
        };

        protected static TextFile.Token[] ineligibleBadRepTokens = new TextFile.Token[]
        {
            TextFile.CreateTextToken("I am sad to say that you are ineligible to join our guild."), newLine,
            TextFile.CreateTextToken("Your reputation amongst scholars is such that we do not "), newLine,
            TextFile.CreateTextToken("wish to be associated with you, even for simple field work. "), newLine,
        };

        protected static TextFile.Token[] ineligibleLowIntTokens = new TextFile.Token[]
        {
            TextFile.CreateTextToken("Sorry, %pcf, you do not exhibit the intellect we require "), newLine,
            TextFile.CreateTextToken("from our recruits. Perhaps a less scholarly guild, such "), newLine,
            TextFile.CreateTextToken("as the Fighters guild, would be more suited to your aptitude. "), newLine,
        };

        protected static TextFile.Token[] promotionTokens = new TextFile.Token[]
        {
            TextFile.CreateTextToken("Congratulations, %pcf. Because of your outstanding work for "), newLine,
            TextFile.CreateTextToken("the guild, we have promoted you to the rank of %lev. "), newLine,
            TextFile.CreateTextToken("Keep up the good work, and continue to study hard. "), newLine,
        };

        static int[] intReqs = new int[] { 40, 40, 45, 50, 60, 60, 65, 65, 70, 70 };

        #endregion

        #region Properties & Data

        static string[] rankTitles = new string[] {
            "Field Assistant", "Field Agent", "Field Officer", "Field Director", "Apprentice", "Novice", "Journeyman", "Associate", "Professor", "Master"
        };

        static List<DFCareer.Skills> guildSkills = new List<DFCareer.Skills>() {
                DFCareer.Skills.Centaurian,
                DFCareer.Skills.Climbing,
                DFCareer.Skills.Daedric,
                DFCareer.Skills.Dragonish,
                DFCareer.Skills.Giantish,
                DFCareer.Skills.Harpy,
                DFCareer.Skills.Impish,
                DFCareer.Skills.Lockpicking,
                DFCareer.Skills.Nymph,
                DFCareer.Skills.Orcish,
                DFCareer.Skills.Stealth,
            };

        static List<DFCareer.Skills> trainingSkills = new List<DFCareer.Skills>() {
                DFCareer.Skills.Centaurian,
                DFCareer.Skills.Climbing,
                DFCareer.Skills.Dodging,
                DFCareer.Skills.Daedric,
                DFCareer.Skills.Dragonish,
                DFCareer.Skills.Giantish,
                DFCareer.Skills.HandToHand,
                DFCareer.Skills.Harpy,
                DFCareer.Skills.Impish,
                DFCareer.Skills.Lockpicking,
                DFCareer.Skills.Nymph,
                DFCareer.Skills.Orcish,
                DFCareer.Skills.Running,
                DFCareer.Skills.Stealth,
                DFCareer.Skills.Swimming,
            };

        public override string[] RankTitles { get { return rankTitles; } }

        public override List<DFCareer.Skills> GuildSkills { get { return guildSkills; } }

        public override List<DFCareer.Skills> TrainingSkills { get { return trainingSkills; } }

        #endregion

        #region Guild Membership and Faction

        public override int GetFactionId()
        {
            return factionId;
        }

        #endregion

        #region Guild Ranks

        protected override int CalculateNewRank(PlayerEntity playerEntity)
        {
            int newRank = base.CalculateNewRank(playerEntity);
            int peINT = playerEntity.Stats.GetPermanentStatValue(DFCareer.Stats.Intelligence);
            while (peINT < intReqs[newRank])
                newRank--;
            return newRank;
        }

        public override TextFile.Token[] TokensPromotion(int newRank)
        {
            return (TextFile.Token[]) promotionTokens.Clone();
        }

        #endregion

        #region Benefits

        public override bool CanRest()
        {
            return IsMember();
        }

        public override bool HallAccessAnytime()
        {
            return (rank >= 4);
        }

        public override int ReducedIdentifyCost(int price)
        {
            // Free (1gp) identification at rank 5
            return (rank >= 5) ? 1 : price;
        }

        #endregion

        #region Service Access:

        public override bool CanAccessLibrary()
        {
            return (rank >= 2);
        }

        public override bool CanAccessService(GuildServices service)
        {
            switch (service)
            {
                case GuildServices.Training:
                    return IsMember();
                case GuildServices.Quests:
                    return true;
                case GuildServices.Identify:
                    return true;
                case GuildServices.BuyPotions:
                    return (rank >= 1);
                case GuildServices.MakePotions:
                    return (rank >= 3);
                case GuildServices.DaedraSummoning:
                    return (rank >= 7);
            }
            if ((int) service == LocatorServiceId)
                return (rank >= 2);

            return false;
        }

        #endregion

        #region Service: Locator

        public static void LocatorService()
        {
            Debug.Log("Locator service.");
            Guild thisGuild = GameManager.Instance.GuildManager.GetGuild(FactionFile.GuildGroups.GGroup0);
            DaggerfallTradeWindow tradeWindow = new DaggerfallTradeWindow(DaggerfallUI.UIManager, DaggerfallTradeWindow.WindowModes.Buy, null, thisGuild);
            tradeWindow.MerchantItems = GetLocatorCharges();
            DaggerfallUI.UIManager.PushWindow(tradeWindow);
        }

        static ItemCollection GetLocatorCharges()
        {
            ItemCollection charges = new ItemCollection();
            for (int i = 0; i < 16; i++)
                charges.AddItem(new LocatorItem(), ItemCollection.AddPosition.DontCare, true);
            return charges;
        }

        #endregion

            #region Service: Training

        public override int GetTrainingMax(DFCareer.Skills skill)
        {
            // Language skill training is capped by char intelligence instead of default
            int playerINT = GameManager.Instance.PlayerEntity.Stats.PermanentIntelligence;
            return (DaggerfallSkills.IsLanguageSkill(skill)) ? playerINT : defaultTrainingMax;
        }

        #endregion


        #region Joining

        public override bool IsEligibleToJoin(PlayerEntity playerEntity)
        {
            // Check reputation & skills
            int rep = playerEntity.FactionData.GetReputation(GetFactionId());
            int high, low;
            CalculateNumHighLowSkills(playerEntity, 0, out high, out low);
            return (rep >= rankReqReputation[0] && high > 0 && low + high > 1 &&
                    playerEntity.Stats.GetPermanentStatValue(DFCareer.Stats.Intelligence) >= intReqs[0]);
        }

        public override TextFile.Token[] TokensIneligible(PlayerEntity playerEntity)
        {
            TextFile.Token[] msg = ineligibleLowSkillTokens;
            if (GetReputation(playerEntity) < 0)
                msg = ineligibleBadRepTokens;
            if (playerEntity.Stats.GetPermanentStatValue(DFCareer.Stats.Intelligence) < intReqs[0])
                msg = (TextFile.Token[]) ineligibleLowIntTokens.Clone();
            return msg;
        }
        public override TextFile.Token[] TokensEligible(PlayerEntity playerEntity)
        {
            return eligibleTokens;
        }
        public override TextFile.Token[] TokensWelcome()
        {
            return (TextFile.Token[]) welcomeTokens.Clone();
        }

        #endregion

    }

}
