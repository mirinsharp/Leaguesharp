﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Warwick.Modes;
using SharpDX;
using Color = SharpDX.Color;

namespace Warwick.Common
{
    internal static class CommonManaManager
    {
        public static Menu MenuLocal { get; private set; }
        public static Spell Q => Champion.PlayerSpells.Q;

        public enum FromMobClass
        {
            ByName,
            ByType
        }

        public enum MobTypes
        {
            None,
            Small,
            Red,
            Blue,
            Baron,
            Dragon,
            Big

        }

        public enum GameObjectTeam
        {
            Unknown = 0,
            Order = 100,
            Chaos = 200,
            Neutral = 300,
        }

        private static Dictionary<Vector2, GameObjectTeam> mobTeams;

        public static void Init(Menu mainMenu)
        {
            MenuLocal = new Menu("Mana Settings", "MinMana").SetFontStyle(FontStyle.Regular, Color.Aquamarine);
            mainMenu.AddSubMenu(MenuLocal);

            //MenuQ.AddItem(new MenuItem("MinMana.Mode", "Min. Mana Control Mode: ").SetValue(new StringList(new[] { "Simple Mode", "Advanced Mode" }, 0)).SetFontStyle(FontStyle.Bold, Color.Aqua).SetTag(9)).ValueChanged +=
            //    delegate(object sender, OnValueChangeEventArgs args)
            //    {
            //        InitRefreshMenuItems();
            //    };

            //InitSimpleMenu();
            InitAdvancedMenu();

            MenuLocal.AddItem(new MenuItem("MinMana.Jungle.DontCheckEnemyBuff", "Don't check min. mana if I'm taking:").SetValue(new StringList(new[] {"Off", "Ally Buff", "Enemy Buff", "Both"}, 2))).SetFontStyle(FontStyle.Regular, Color.Wheat).SetTag(9);
            MenuLocal.AddItem(new MenuItem("MinMana.Jungle.DontCheckBlueBuff", "Don't check min. mana if I have Blue Buff").SetValue(true)).SetFontStyle(FontStyle.Regular, Color.Wheat).SetTag(9);

            MenuLocal.AddItem(new MenuItem("MinMana.Jungle.Default", "Load Recommended Settings").SetValue(true).SetTag(9))
                .SetFontStyle(FontStyle.Bold, Color.GreenYellow)
                .ValueChanged +=
                (sender, args) =>
                {
                    if (args.GetNewValue<bool>() == true)
                    {
                        LoadDefaultSettings();
                    }
                };
        }

        static void InitRefreshMenuItems()
        {
            int argsValue = MenuLocal.Item("MinMana.Mode").GetValue<StringList>().SelectedIndex;

            foreach (var item in MenuLocal.Items)
            {
                item.Show(false);

                if (item.Tag == 9)
                {
                    item.Show(true);
                }

                switch (argsValue)
                {
                    case 0:
                        if (item.Tag == 1)
                        {
                            item.Show(true);
                        }
                        break;
                    case 1:
                        if (item.Tag == 2)
                        {
                            item.Show(true);
                        }

                        break;
                }
            }
        }

        //static void InitSimpleMenu()
        //{
        //    MenuQ.AddItem(new MenuItem("MinMana.Simple.Harass", "Harass %").SetValue(new Slider(60, 100, 0))).SetFontStyle(FontStyle.Regular, Color.IndianRed).SetTag(1);
        //    MenuQ.AddItem(new MenuItem("MinMana.Simple.Lane", "Lane Clear %").SetValue(new Slider(30, 100, 0))).SetFontStyle(FontStyle.Regular, Color.LightSkyBlue).SetTag(1);
        //    MenuQ.AddItem(new MenuItem("MinMana.Simple.Jungle", "Jungle Clear %").SetValue(new Slider(60, 100, 0))).SetFontStyle(FontStyle.Regular, Color.IndianRed).SetTag(1);
        //}

        static void InitAdvancedMenu()
        {
            var menuHarass = new Menu("Harass Min. Mana Control", "MinMana.Menu.Harass").SetFontStyle(FontStyle.Regular, Color.DarkSalmon);
            {
                menuHarass.AddItem(new MenuItem("MinMana.Advanced.Harass", "Harass %").SetValue(new Slider(30, 100, 0))).SetFontStyle(FontStyle.Regular, Color.LightSkyBlue);
                MenuLocal.AddSubMenu(menuHarass);
            }

            var menuLane = new Menu("Lane Clear Min. Mana Control", "MinMana.Menu.Lane").SetFontStyle(FontStyle.Regular, Color.Coral);
            {
                menuLane.AddItem(new MenuItem("MinMana.Lane.Alone", "I'm Alone %").SetValue(new Slider(30, 100, 0))).SetFontStyle(FontStyle.Regular, Color.LightSkyBlue).SetTag(2);
                menuLane.AddItem(new MenuItem("MinMana.Lane.Enemy", "I'm NOT Alone (Enemy Close) %").SetValue(new Slider(60, 100, 0))).SetFontStyle(FontStyle.Regular, Color.IndianRed).SetTag(2);
                MenuLocal.AddSubMenu(menuLane);
            }

            var menuJungle = new Menu("Jungle Clear Min. Mana Control", "MinMana.Menu.Jungle").SetFontStyle(FontStyle.Regular, Color.DarkSalmon);
            {
                menuJungle.AddItem(new MenuItem("MinMana.Jungle.AllyBig", "Ally: Big Mob %").SetValue(new Slider(50, 100, 0))).SetFontStyle(FontStyle.Regular, Color.LightGreen).SetTag(2);
                menuJungle.AddItem(new MenuItem("MinMana.Jungle.AllySmall", "Ally: Small Mob %").SetValue(new Slider(50, 100, 0))).SetFontStyle(FontStyle.Regular, Color.LightGreen).SetTag(2);

                menuJungle.AddItem(new MenuItem("MinMana.Jungle.EnemyBig", "Enemy: Big Mob %").SetValue(new Slider(30, 100, 0))).SetFontStyle(FontStyle.Regular, Color.IndianRed).SetTag(2);
                menuJungle.AddItem(new MenuItem("MinMana.Jungle.EnemySmall", "Enemy: Small Mob %").SetValue(new Slider(30, 100, 0))).SetFontStyle(FontStyle.Regular, Color.IndianRed).SetTag(2);

                menuJungle.AddItem(new MenuItem("MinMana.Jungle.BigBoys", "Baron/Dragon/RH %").SetValue(new Slider(70, 100, 0))).SetFontStyle(FontStyle.Regular, Color.Aqua).SetTag(2);
                MenuLocal.AddSubMenu(menuJungle);
            }
        }
        public static void LoadDefaultSettings()
        {
            ModeConfig.MenuFarm.Item("Farm.MinMana.Enable")
                .SetValue("Quick Enable/Disable Minimum Mana Control!")
                .SetValue(new KeyBind("M".ToCharArray()[0], KeyBindType.Toggle, true));

            MenuLocal.Item("MinMana.Lane.Alone").SetValue(new Slider(30, 100, 0));
            MenuLocal.Item("MinMana.Lane.Enemy").SetValue(new Slider(60, 100, 0));

            MenuLocal.Item("MinMana.Jungle.AllyBig").SetValue(new Slider(50, 100, 0));
            MenuLocal.Item("MinMana.Jungle.EnemyBig").SetValue(new Slider(30, 100, 0));
            MenuLocal.Item("MinMana.Jungle.AllySmall").SetValue(new Slider(50, 100, 0));
            MenuLocal.Item("MinMana.Jungle.EnemySmall").SetValue(new Slider(30, 100, 0));
            MenuLocal.Item("MinMana.Jungle.BigBoys").SetValue(new Slider(70, 100, 0));

            MenuLocal.Item("MinMana.Jungle.DontCheckEnemyBuff")
                .SetValue(new StringList(new[] {"Off", "Ally Buff", "Enemy Buff", "Both"}, 3));
            MenuLocal.Item("MinMana.Jungle.DontCheckBlueBuff").SetValue(true);
        }

        public static GameObjectTeam GetMobTeam(this Obj_AI_Base mob, float range)
        {
            mobTeams = new Dictionary<Vector2, GameObjectTeam>();
            if (Game.MapId == (GameMapId) 11)
            {
                mobTeams.Add(new Vector2(7756f, 4118f), GameObjectTeam.Order); // blue team :red;
                mobTeams.Add(new Vector2(3824f, 7906f), GameObjectTeam.Order); // blue team :blue
                mobTeams.Add(new Vector2(8356f, 2660f), GameObjectTeam.Order); // blue team :golems
                mobTeams.Add(new Vector2(3860f, 6440f), GameObjectTeam.Order); // blue team :wolfs
                mobTeams.Add(new Vector2(6982f, 5468f), GameObjectTeam.Order); // blue team :wariaths
                mobTeams.Add(new Vector2(2166f, 8348f), GameObjectTeam.Order); // blue team :Frog jQuery

                mobTeams.Add(new Vector2(4768, 10252), GameObjectTeam.Neutral); // Baron
                mobTeams.Add(new Vector2(10060, 4530), GameObjectTeam.Neutral); // Dragon

                mobTeams.Add(new Vector2(7274f, 11018f), GameObjectTeam.Chaos); // Red team :red;
                mobTeams.Add(new Vector2(11182f, 6844f), GameObjectTeam.Chaos); // Red team :Blue
                mobTeams.Add(new Vector2(6450f, 12302f), GameObjectTeam.Chaos); // Red team :golems
                mobTeams.Add(new Vector2(11152f, 8440f), GameObjectTeam.Chaos); // Red team :wolfs
                mobTeams.Add(new Vector2(7830f, 9526f), GameObjectTeam.Chaos); // Red team :wariaths
                mobTeams.Add(new Vector2(12568, 6274), GameObjectTeam.Chaos); // Red team : Frog jQuery

                return mobTeams.Where(hp => mob.Distance(hp.Key) <= (range)).Select(hp => hp.Value).FirstOrDefault();
            }

            return GameObjectTeam.Unknown;
        }


        public static MobTypes GetMobType(Obj_AI_Base mob, FromMobClass fromMobClass = FromMobClass.ByName)
        {
            if (mob == null)
            {
                return MobTypes.None;
            }
            if (fromMobClass == FromMobClass.ByName)
            {
                if (mob.SkinName.Contains("SRU_Baron") || mob.SkinName.Contains("SRU_RiftHerald"))
                {
                    return MobTypes.Baron;
                }

                if (mob.SkinName.Contains("SRU_Dragon"))
                {
                    return MobTypes.Dragon;
                }

                if (mob.SkinName.Contains("SRU_Blue"))
                {
                    return MobTypes.Blue;
                }

                if (mob.SkinName.Contains("SRU_Red"))
                {
                    return MobTypes.Red;
                }

                if (mob.SkinName.Contains("SRU_Red"))
                {
                    return MobTypes.Red;
                }
            }

            if (fromMobClass == FromMobClass.ByType)
            {
                Obj_AI_Base oMob =
                    (from fBigBoys in
                        new[]
                        {
                            "SRU_Baron", "SRU_Dragon", "SRU_RiftHerald", "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf",
                            "SRU_Razorbeak", "SRU_Red", "SRU_Krug", "Sru_Crab"
                        }
                        where
                            fBigBoys == mob.SkinName
                        select mob)
                        .FirstOrDefault();

                if (oMob != null)
                {
                    return MobTypes.Big;
                }
            }

            return MobTypes.Small;
        }

        public static float HarassMinManaPercent
            =>
                ModeConfig.MenuFarm.Item("Farm.MinMana.Enable").GetValue<KeyBind>().Active
                    ? MenuLocal.Item("MinMana.Advanced.Harass").GetValue<Slider>().Value
                    : 0f;

        public static float ToggleMinManaPercent
            =>
                ModeConfig.MenuFarm.Item("Farm.MinMana.Enable").GetValue<KeyBind>().Active
                    ? MenuLocal.Item("MinMana.Toggle").GetValue<Slider>().Value
                    : 0f;

        public static float LaneMinManaPercent
        {
            get
            {
                if (ModeConfig.MenuFarm.Item("Farm.MinMana.Enable").GetValue<KeyBind>().Active)
                {
                    return HeroManager.Enemies.Find(e => e.IsValidTarget(2000) && !e.IsZombie) == null
                        ? MenuLocal.Item("MinMana.Lane.Alone").GetValue<Slider>().Value
                        : MenuLocal.Item("MinMana.Lane.Enemy").GetValue<Slider>().Value;
                }

                return 0f;
            }
        }

        public enum FarmMode
        {
            Lane,
            Jungle
        }
        public static float MinManaPercent(FarmMode farmMode)
        {
            // Enable / Disable Min Mana
            if (!ModeConfig.MenuFarm.Item("Farm.MinMana.Enable").GetValue<KeyBind>().Active)
            {
                return 0f;
            }

            if (farmMode == FarmMode.Lane)
            {
                return ModeJungle.MenuLocal.Item("Jungle.MinMana").GetValue<Slider>().Value;
            }

            if (farmMode == FarmMode.Lane)
            {
                return ModeLane.MenuLocal.Item("Lane.MinMana").GetValue<Slider>().Value;
            }

            return 0f;
        }
    }
}