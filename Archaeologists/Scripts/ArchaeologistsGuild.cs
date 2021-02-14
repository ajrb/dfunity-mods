// Project:         Archaeologists Guild for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.FallExe;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using Archaeologists;

namespace DaggerfallWorkshop.Game.Guilds
{
    public class ArchaeologistsGuild : Guild
    {
        #region Constants

        private const int factionId = 1000;
        private const int LocatorServiceId = 1001;
        private const int RepairServiceId = 1003;

        const int notEnoughGoldId = 454;
        const int recallEffectId = 94;
        private const int replaceMarkCost = 10000;
        private const int ArchGuildLocationsBookId = 1000;

        #endregion

        #region Static Data

        protected static TextFile.Token newLine = TextFile.CreateFormatToken(TextFile.Formatting.JustifyCenter);

        // Guild messages - must clone any that contain macros before returning.

        protected static TextFile.Token[] welcomeTokens =
        {
            TextFile.CreateTextToken("Excellent, %pcn, welcome to the Archaeologists! "), newLine, newLine,

            TextFile.CreateTextToken("We'll get you started on field work as soon as possible. "), newLine,
            TextFile.CreateTextToken("You'll begin your Archaeologists career with the title of "), newLine,
            TextFile.CreateTextToken("%lev. However, with hard work and dedication, "), newLine,
            TextFile.CreateTextToken("you may be recognised for promotion soon enough. Please"), newLine,
            TextFile.CreateTextToken("do make use of our training facilities to study languages "), newLine,
            TextFile.CreateTextToken("or to improve your field work skills. "), newLine, newLine,

            TextFile.CreateTextToken("Here's a book containing guild ranks and locations of all guild "), newLine,
            TextFile.CreateTextToken("halls. Also here's a Mark of Recall, and a free locator device "), newLine,
            TextFile.CreateTextToken("to try out the next time you find yourself lost in a labyrinth "), newLine,
            TextFile.CreateTextToken("unable to find whatever it is you're seeking. Locators look like"), newLine,
            TextFile.CreateTextToken("Ankh symbols and will activate once you've explored enough. "), newLine, newLine,

            TextFile.CreateTextToken("Locators are provided free for guild quests involving dungeon "), newLine,
            TextFile.CreateTextToken("delving, but you can also purchase them from us for a price, "), newLine,
            TextFile.CreateTextToken("with discounts given for any relics you can find for the guild. "), newLine, newLine
        };

        protected static TextFile.Token[] eligibleTokens =
        {
            TextFile.CreateTextToken("Hmm, yes, you seem like a suitable candidate to assist us with"), newLine,
            TextFile.CreateTextToken("our field work and finding the relics that we need for research. "), newLine, newLine,

            TextFile.CreateTextToken("We offer classes in all the various obscure languages "), newLine,
            TextFile.CreateTextToken("of Tamriel, as well as some of the more practical skills "), newLine,
            TextFile.CreateTextToken("required in the field while searching remote locations "), newLine,
            TextFile.CreateTextToken("for interesting antiquities and rare artifacts. "), newLine, newLine,

            TextFile.CreateTextToken("New members receive a free recall mark with 30 charges "), newLine,
            TextFile.CreateTextToken("to assist with transport without dabbling in magic arts. "), newLine,
            TextFile.CreateTextToken("Once you rise in our ranks to Field Officer level, then you'll "), newLine,
            TextFile.CreateTextToken("be able to get your recall mark repaired and recharged. "), newLine, newLine,

            TextFile.CreateTextToken("Beyond field work, our higher ranks are open to the more "), newLine,
            TextFile.CreateTextToken("accomplished scholars among us, and provide a large "), newLine,
            TextFile.CreateTextToken("reduction to the cost of locator devices. Note that "), newLine,
            TextFile.CreateTextToken("only those with sufficient intellect will be promoted. "), newLine, newLine,
        };

        protected static TextFile.Token[] ineligibleLowSkillTokens =
        {
            TextFile.CreateTextToken("I am sad to say that you are not eligible to join our guild."), newLine,
            TextFile.CreateTextToken("We only accept members who have studied languages or the "), newLine,
            TextFile.CreateTextToken("other skills useful for field work; such as climbing, "), newLine,
            TextFile.CreateTextToken("lockpicking, or stealth. "), newLine,
        };

        protected static TextFile.Token[] ineligibleBadRepTokens =
        {
            TextFile.CreateTextToken("I am sad to say that you are ineligible to join our guild."), newLine,
            TextFile.CreateTextToken("Your reputation amongst scholars is such that we do not "), newLine,
            TextFile.CreateTextToken("wish to be associated with you, even for simple field work. "), newLine,
        };

        protected static TextFile.Token[] ineligibleLowIntTokens =
        {
            TextFile.CreateTextToken("Sorry, %pcf, you do not exhibit the intellect we require "), newLine,
            TextFile.CreateTextToken("from our recruits. Perhaps a less scholarly guild, such "), newLine,
            TextFile.CreateTextToken("as the Fighters guild, would be more suited to your aptitude. "), newLine,
        };

        protected static TextFile.Token[] promotionTokens =
        {
            TextFile.CreateTextToken("Congratulations, %pcf. Because of your outstanding work for "), newLine,
            TextFile.CreateTextToken("the guild, we have promoted you to the rank of %lev. "), newLine,
            TextFile.CreateTextToken("Keep up the good work, and continue to study hard. "), newLine,
        };

        protected static int[] intReqs = { 30, 50, 55, 60, 60, 65, 65, 70, 70, 75 };

        protected static string[] rankTitles = {
            "Field Assistant", "Field Agent", "Field Officer", "Field Director", "Apprentice", "Novice", "Journeyman", "Associate", "Professor", "Master"
        };

        protected static int[] RankLocatorCosts = { 2100, 1800, 1500, 1200, 1050, 900, 750, 600, 200, 180 };

        protected static List<DFCareer.Skills> guildSkills = new List<DFCareer.Skills>() {
                DFCareer.Skills.Centaurian,
                DFCareer.Skills.Climbing,
                DFCareer.Skills.Daedric,
                DFCareer.Skills.Dodging,
                DFCareer.Skills.Dragonish,
                DFCareer.Skills.Giantish,
                DFCareer.Skills.Harpy,
                DFCareer.Skills.Impish,
                DFCareer.Skills.Lockpicking,
                DFCareer.Skills.Nymph,
                DFCareer.Skills.Orcish,
                DFCareer.Skills.Stealth,
            };

        protected static List<DFCareer.Skills> trainingSkills = new List<DFCareer.Skills>() {
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

        #endregion

        #region Properties

        public override string[] RankTitles { get { return rankTitles; } }

        public override List<DFCareer.Skills> GuildSkills { get { return guildSkills; } }

        public override List<DFCareer.Skills> TrainingSkills { get { return trainingSkills; } }

        #endregion

        #region Guild Membership and Faction

        public static int FactionId { get { return factionId; } }

        public override int GetFactionId()
        {
            return factionId;
        }

        #endregion

        #region Guild Ranks

        protected override int CalculateNewRank(PlayerEntity playerEntity)
        {
            int newRank = base.CalculateNewRank(playerEntity);
            int magesRank = GameManager.Instance.GuildManager.GetGuild(FactionFile.GuildGroups.MagesGuild).Rank;
            if (magesRank > 5)
                newRank = Mathf.Min(5, rank);   // Cap rank at 5 when above rank 5 in mages guild.
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
            // Free identification at rank 5
            return (rank >= 5) ? 0 : price;
        }

        public override bool AvoidDeath()
        {
            if (rank >= 4 && Random.Range(0, 50) < rank &&
                GameManager.Instance.PlayerEntity.FactionData.GetReputation((int) FactionFile.FactionIDs.Stendarr) >= 0 &&
                !GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged)
            {
                DaggerfallUI.AddHUDText(TextManager.Instance.GetLocalizedText("avoidDeath"));
                return true;
            }
            return false;
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
            if ((int)service == LocatorServiceId)
                return true;
            if ((int)service == RepairServiceId)
                return (rank >= 2);

            return false;
        }

        #endregion

        #region Service: Locator

        public static void LocatorService(IUserInterfaceWindow window)
        {
            // Get the guild instance.
            IGuild thisGuild = GameManager.Instance.GuildManager.GetGuild(FactionFile.GuildGroups.GGroup0);

            // Check how many holy items the player has and offer that many discounted.
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            List<DaggerfallUnityItem> tomes = playerEntity.Items.SearchItems(ItemGroups.ReligiousItems, (int)ReligiousItems.Holy_tome);
            tomes.AddRange(playerEntity.WagonItems.SearchItems(ItemGroups.ReligiousItems, (int)ReligiousItems.Holy_tome));
            List<DaggerfallUnityItem> daggers = playerEntity.Items.SearchItems(ItemGroups.ReligiousItems, (int)ReligiousItems.Holy_dagger);
            daggers.AddRange(playerEntity.WagonItems.SearchItems(ItemGroups.ReligiousItems, (int)ReligiousItems.Holy_dagger));
            int holyCount = tomes.Count + daggers.Count;

            // Show trade window and a popup message to inform player how many discounted locators they can purchase.
            DaggerfallTradeWindow tradeWindow = (DaggerfallTradeWindow)
                UIWindowFactory.GetInstanceWithArgs(UIWindowType.Trade, new object[] { DaggerfallUI.UIManager, null, DaggerfallTradeWindow.WindowModes.Buy, thisGuild });
            tradeWindow.MerchantItems = GetLocatorDevices(holyCount, RankLocatorCosts[thisGuild.Rank]);
            DaggerfallUI.UIManager.PushWindow(tradeWindow);
            tradeWindow.OnTrade += LocatorPurchase_OnTrade;

            if (thisGuild.Rank < 8)
            {
                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, window);
                string[] message = {
                    "   If you can supply the guild with either a holy tome or holy dagger,",
                    "  then we can sell locator devices at a third of normal price per relic.",
                    "    Locator prices will reduce as you gain higher ranks in the guild.",
                    "",
                    "      You currently have " + holyCount + " holy relics in your posession, so you can",
                    "     purchase up to that many discounted locators at this time.",
                };
                if (holyCount == 0)
                {
                    message[4] = "       You don't have any holy relics with you right now, so";
                    message[5] = "     unfortunately we can't offer you any discounts this time.";
                }
                messageBox.SetText(message);
                messageBox.ClickAnywhereToClose = true;
                messageBox.Show();
            }
        }

        static ItemCollection GetLocatorDevices(int number, int value)
        {
            ItemCollection locators = new ItemCollection();
            if (number > 0)
                for (int i = 0; i < number; i++)
                    locators.AddItem(new LocatorItem(value / 3), ItemCollection.AddPosition.DontCare, true);
            else
                for (int i = 0; i < 16; i++)
                    locators.AddItem(new LocatorItem(value), ItemCollection.AddPosition.DontCare, true);
            return locators;
        }

        static void LocatorPurchase_OnTrade(DaggerfallTradeWindow.WindowModes mode, int numItems, int value)
        {
            // Get the guild instance.
            IGuild thisGuild = GameManager.Instance.GuildManager.GetGuild(FactionFile.GuildGroups.GGroup0);

            if (mode == DaggerfallTradeWindow.WindowModes.Buy && numItems > 0 && thisGuild.Rank < 8)
            {   // Remove holy items from player items.
                ItemCollection coll = GameManager.Instance.PlayerEntity.Items;
                numItems = RemoveItems(coll, numItems, (int)ReligiousItems.Holy_tome);
                numItems = RemoveItems(coll, numItems, (int)ReligiousItems.Holy_dagger);
                coll = GameManager.Instance.PlayerEntity.WagonItems;
                numItems = RemoveItems(coll, numItems, (int)ReligiousItems.Holy_tome);
                numItems = RemoveItems(coll, numItems, (int)ReligiousItems.Holy_dagger);
            }
            // If this was an attempt to steal, then reduce guild reputation by 25.
            if (value == 0)
                GameManager.Instance.PlayerEntity.FactionData.ChangeReputation(factionId, -25, true);
        }

        static int RemoveItems(ItemCollection coll, int numItems, int itemIndex)
        {
            foreach (DaggerfallUnityItem item in coll.SearchItems(ItemGroups.ReligiousItems, itemIndex))
            {
                if (numItems <= 0)
                    return 0;
                coll.RemoveItem(item);
                numItems--;
            }
            return numItems;
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

        #region Service: Repair Recall Mark

        public static void RepairMarkService(IUserInterfaceWindow window)
        {
            Debug.Log("Repair Recall Mark service.");

            DaggerfallUnityItem markOfRecall = FindRecallMark();
            if (markOfRecall == null)
            {
                if (GameManager.Instance.PlayerEntity.GetGoldAmount() < replaceMarkCost)
                {
                    DaggerfallUI.MessageBox(new string[] {
                        "You don't appear to have your Mark of Recall on you, or enough",
                        "for a replacement. If you have been careless and lost or broken",
                        "it, I can replace it for a price of 20,000 gold pieces. They're",
                        "expensive, and the guild only provides one free Mark per member."
                    });
                }
                else
                {
                    DaggerfallMessageBox replaceMarkBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, window, true);
                    string[] message = {
                        "   You don't appear to have your Mark of Recall on you.",
                        "   If you have been careless and lost or broken it, then",
                        "      I can replace it at a cost of 10,000 gold.",
                        "",
                        "      Would you like a replacement Mark of Recall?"
                    };
                    replaceMarkBox.SetText(message);
                    replaceMarkBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                    replaceMarkBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
                    replaceMarkBox.OnButtonClick += ReplaceMarkBox_OnButtonClick;
                    replaceMarkBox.Show();
                }
            }
            else
            {
                int cost = CalculateRepairCost(markOfRecall);
                if (GameManager.Instance.PlayerEntity.GetGoldAmount() < cost)
                {
                    DaggerfallUI.MessageBox(notEnoughGoldId);
                }
                else if (cost == 0)
                {
                    if (HasLocationsBook())
                    {
                        DaggerfallUI.MessageBox("Your Mark of Recall shows no signs of wear that I can see.");
                    }
                    else
                    {
                        DaggerfallMessageBox replaceBookBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, window, true);
                        string[] message = {
                            "Your Mark of Recall shows no signs of wear that I can see.",
                            "",
                            "  However, you do appear to have misplaced your guild hall",
                            "      locations book. Would you like a replacement?"
                        };
                        replaceBookBox.SetText(message);
                        replaceBookBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                        replaceBookBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
                        replaceBookBox.OnButtonClick += ReplaceBookBox_OnButtonClick;
                        replaceBookBox.Show();
                    }
                }
                else
                {
                    string message = "Repairing your Mark of Recall will cost " + cost + " gp, okay?";
                    DaggerfallMessageBox confirmRepairBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallMessageBox.CommonMessageBoxButtons.YesNo, message, window);
                    confirmRepairBox.OnButtonClick += ConfirmRepairBox_OnButtonClick;
                    confirmRepairBox.Show();
                }
            }
        }

        static void ReplaceMarkBox_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            sender.CloseWindow();
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                GameManager.Instance.PlayerEntity.DeductGoldAmount(replaceMarkCost);
                GivePlayerMarkOfRecall();
                DaggerfallUI.MessageBox("Here's your replacement Mark of Recall, take more care in future!");
            }
        }

        static void ConfirmRepairBox_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            sender.CloseWindow();
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                DaggerfallUnityItem markOfRecall = FindRecallMark();
                if (markOfRecall != null)
                {
                    int cost = CalculateRepairCost(markOfRecall);
                    GameManager.Instance.PlayerEntity.DeductGoldAmount(cost);
                    markOfRecall.currentCondition = markOfRecall.maxCondition;
                    DaggerfallUI.MessageBox("Your Mark of Recall is now as good as new.");
                }
            }
        }

        static void ReplaceBookBox_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            sender.CloseWindow();
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                GivePlayerLocationsBook();
                DaggerfallUI.MessageBox("Here's your replacement guild hall locations book.");
            }
        }

        private static DaggerfallUnityItem FindRecallMark()
        {
            List<DaggerfallUnityItem> marks = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Jewellery, (int)Jewellery.Mark);
            if (marks.Count > 0)
                foreach (DaggerfallUnityItem item in marks)
                    if (item.IsEnchanted)
                        foreach (DaggerfallEnchantment enchantment in item.LegacyEnchantments)
                            if (enchantment.type == EnchantmentTypes.CastWhenUsed && enchantment.param == recallEffectId)
                                return item;

            return null;
        }

        private static bool HasLocationsBook()
        {
            List<DaggerfallUnityItem> books = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Books, (int)Books.Book0);
            if (books.Count > 0)
                foreach (DaggerfallUnityItem item in books)
                    if (item.message == ArchGuildLocationsBookId)
                        return true;

            books = GameManager.Instance.PlayerEntity.WagonItems.SearchItems(ItemGroups.Books, (int)Books.Book0);
            if (books.Count > 0)
                foreach (DaggerfallUnityItem item in books)
                    if (item.message == ArchGuildLocationsBookId)
                        return true;

            return false;
        }

        private static int CalculateRepairCost(DaggerfallUnityItem markOfRecall)
        {
            int repairAmount = markOfRecall.maxCondition - markOfRecall.currentCondition;
            int cost = repairAmount * 16;    // 160gp per use, or 9600 for a full repair
            return cost;
        }

        #endregion

        #region Joining

        override public void Join()
        {
            base.Join();
            // Give a mark of recall item.
            GivePlayerMarkOfRecall();
            // Give a guild hall locations book.
            GivePlayerLocationsBook();
            // Give player a free locator device.
            GameManager.Instance.PlayerEntity.Items.AddItem(new LocatorItem(), ItemCollection.AddPosition.DontCare, true);
        }

        private static void GivePlayerMarkOfRecall()
        {
            DaggerfallUnityItem item = ItemBuilder.CreateItem(ItemGroups.Jewellery, (int)Jewellery.Mark);
            item.legacyMagic = new DaggerfallEnchantment[]
            {
                new DaggerfallEnchantment()
                {
                    type = EnchantmentTypes.CastWhenUsed,
                    param = recallEffectId
                }
            };
            item.shortName = "%it of Recall";
            item.IdentifyItem();
            item.currentCondition = item.maxCondition / 2;
            GameManager.Instance.PlayerEntity.Items.AddItem(item);
        }

        private static void GivePlayerLocationsBook()
        {
            GameManager.Instance.PlayerEntity.Items.AddItem(ItemBuilder.CreateBook(ArchGuildLocationsBookId));
        }

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
