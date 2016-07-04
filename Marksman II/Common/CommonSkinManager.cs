using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Champions;

namespace Marksman.Common
{
    internal class CommonSkinManager
    {
        public static Menu MenuLocal { get; private set; }

        public static void Init(Menu MenuParent)
        {
            MenuLocal = new Menu("Skins", "MenuSkin");
            {
                MenuLocal.AddItem(new MenuItem("Settings.Skin", "Skin:").SetValue(false)).ValueChanged +=
                    (sender, args) =>
                    {
                        if (!args.GetNewValue<bool>())
                        {
                            ObjectManager.Player.SetSkin(ObjectManager.Player.CharData.BaseSkinName, ObjectManager.Player.BaseSkinId);
                        }
                    };

                string[] strSkins = new[] {""};

                switch (ObjectManager.Player.ChampionName.ToLower())
                {
                    case "caitlyn":
                    {
                        strSkins = new[]
                        {
                            "Classic Caitlyn", "Resistance Caitlyn", "Sheriff Caitlyn", "Artic Warfare Caitlyn",
                            "Office Caitlyn", "Headhunter Caitlyn", "Safari Caitlyn", "Lunar Wraith Caitlyn", "sss", "aaa", "bbb", "ccc"
                        };
                        break;
                    }

                    case "ezreal":
                    {
                        strSkins = new[]
                        {
                            "Classic Ezreal", "Nottingham Ezreal", "Striker Ezreal", "Frosted Ezreal", "Explorer Ezreal",
                            "Pulsefire Ezreal", "TPA Ezreal", "Deboniar Ezreal", "Ace of Spades Ezreal"
                        };
                        break;
                    }

                    case "kogmaw":
                    {
                        strSkins = new[]
                        {
                            "Classic Kog'Maw", "Caterpillar Kog'Maw", "Sonoran Kog'Maw", "Monarch Kog'Maw",
                            "Reindeer Kog'Maw", "Lion Dance Kog'Maw", "Deep Sea Kog'Maw", "Jurassic Kog'Maw",
                            "Battlecast Kog'Maw"
                        };
                            break;
                        }


                    case "jinx":
                        {
                            strSkins = new[]
                            {
                            "Classic Jinx", "Mafia Jinx", "Firecracker Jinx", "Slayer Jinx"
                        };
                            break;
                        }
                    case "lucian":
                    {
                        strSkins = new[]
                        {
                            "Classic Lucian", "Hired Gun Lucian", "Striker Lucian", "Chrome Pack: Yellow Lucian",
                            "Chrome Pack: Red Lucian", "Chrome Pack: Blue Lucian", "Project: Lucian"
                        };
                        break;
                    }

                    case "sivir":
                    {
                        strSkins = new[]
                        {
                            "Classic Sivir", "Warrior Princess Sivir", "Spectacutar Sivir", "Huntress Sivir",
                            "Bandit Sivir", "PAX Sivir", "Showstorm Sivir", "Warden Sivir", "Victorious Sivir"
                        };
                            break;
                        }

                    case "tristana":
                    {
                        strSkins = new[]
                        {
                            "Classic Tristana", "Riot Girl Tristana", "Earnest Elf Tristana", "Firefighter Tristana",
                            "Guerilla Tristana", "Buccaneer Tristana", "Rocket Girl Tristana", "Dragon Trainer Tristana"
                        };
                        break;
                    }

                    case "twitch":
                    {
                        strSkins = new[]
                        {
                            "Classic Twitch", "Kingpin Twitch", "Whistler Village Twitch", "Medieval Twitch",
                            "Gangster Twitch", "Vandal Twitch", "Pickpocket Twitch", "SSW Twitch"
                        };
                        break;
                    }


                    case "vayne":
                    {
                        strSkins = new[]
                        {
                            "Classic Vayne", "Vindicator Vayne", "Aristocrat Vayne", "Dragonslayer Vayne",
                            "Heartseeker Vayne", "SKT T1 Vayne", "Arclight Vayne"
                        };
                        break;
                    }
                }

                MenuLocal.AddItem(new MenuItem("Settings.SkinID", "Skin Name:").SetValue(new StringList(strSkins, 0)));
            }
            MenuParent.AddSubMenu(MenuLocal);

            Game.OnUpdate += GameOnOnUpdate;
        }

        private static void GameOnOnUpdate(EventArgs args)
        {
            if (MenuLocal.Item("Settings.Skin").GetValue<bool>())
            {
                ObjectManager.Player.SetSkin(ObjectManager.Player.CharData.BaseSkinName, MenuLocal.Item("Settings.SkinID").GetValue<StringList>().SelectedIndex);
            }
        }
    }
}
