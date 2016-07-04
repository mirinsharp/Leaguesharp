#region

using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Champions;
using Marksman.Utils;
using SharpDX;
using SharpDX.Direct3D9;


#endregion

namespace Marksman
{
    using System.Collections.Generic;

    using Color = SharpDX.Color;

    internal class Program
    {
        public static Menu Config;

        public static Menu OrbWalking;

        public static Menu MenuActivator;

        public static Menu MenuExtraTools { get; set; }
        public static Menu MenuExtraToolsActivePackets { get; set; }

        public static Champion ChampionClass;

        static SpellSlot IgniteSlot = ObjectManager.Player.GetSpellSlot("summonerdot");


        //public static Utils.EarlyEvade EarlyEvade;

        public static Spell Smite;

        public static SpellSlot SmiteSlot = SpellSlot.Unknown;

        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };

        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };

        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };

        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Config = new Menu("Marksman", "Marksman", true).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);
            ChampionClass = new Champion();
            Common.CommonGeometry.Init();
            var BaseType = ChampionClass.GetType();

            /* Update this with Activator.CreateInstance or Invoke
               http://stackoverflow.com/questions/801070/dynamically-invoking-any-function-by-passing-function-name-as-string 
               For now stays cancer.
             */
            var championName = ObjectManager.Player.ChampionName.ToLowerInvariant();

            switch (championName)
            {
                case "ashe":
                    ChampionClass = new Ashe();
                    break;
                case "caitlyn":
                    ChampionClass = new Caitlyn();
                    break;
                case "corki":
                    ChampionClass = new Corki();
                    break;
                case "draven":
                    ChampionClass = new Draven();
                    break;
                case "ezreal":
                    ChampionClass = new Ezreal();
                    break;
                case "graves":
                    ChampionClass = new Graves();
                    break;
                case "gnar":
                    ChampionClass = new Gnar();
                    break;
                case "jinx":
                    ChampionClass = new Jinx();
                    break;
                case "kalista":
                    ChampionClass = new Kalista();
                    break;
                case "kindred":
                    ChampionClass = new Kindred();
                    break;
                case "kogmaw":
                    ChampionClass = new Kogmaw();
                    break;
                case "lucian":
                    ChampionClass = new Lucian();
                    break;
                case "missfortune":
                    ChampionClass = new MissFortune();
                    break;
                case "quinn":
                    ChampionClass = new Quinn();
                    break;
                case "sivir":
                    ChampionClass = new Sivir();
                    break;
                case "teemo":
                    ChampionClass = new Teemo();
                    break;
                case "tristana":
                    ChampionClass = new Tristana();
                    break;
                case "twitch":
                    ChampionClass = new Twitch();
                    break;
                case "urgot":
                    ChampionClass = new Urgot();
                    break;
                case "vayne":
                    ChampionClass = new Vayne();
                    break;
                case "varus":
                    ChampionClass = new Varus();
                    break;
            }
            //Config.DisplayName = "Marksman Lite | " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(championName);
            Config.DisplayName = "Marksman Lite";

            ChampionClass.Id = ObjectManager.Player.CharData.BaseSkinName;
            ChampionClass.Config = Config;


            MenuExtraTools = new Menu("Extra Tools", "ExtraTools").SetFontStyle(FontStyle.Regular, Color.Aqua);
            {
                var nMenuExtraToolsPackets = new Menu("Available Tools", "MenuExtraTools.Available");
                nMenuExtraToolsPackets.AddItem(new MenuItem("ExtraTools.Orbwalker", "Orbwalker:")).SetValue(new StringList(new[] { "LeagueSharp Common", "Marksman Orbwalker (With Attack Speed Limiter)" })).SetFontStyle(FontStyle.Regular, Color.Gray);
                nMenuExtraToolsPackets.AddItem(new MenuItem("ExtraTools.Prediction", "Prediction:")).SetValue(new StringList(new[] { "LeagueSharp Common", "SPrediction (Synx)"})).SetFontStyle(FontStyle.Regular, Color.Gray);
                nMenuExtraToolsPackets.AddItem(new MenuItem("ExtraTools.AutoLevel", "Auto Leveller:")).SetValue(false);
                nMenuExtraToolsPackets.AddItem(new MenuItem("ExtraTools.AutoBush", "Auto Bush Ward:")).SetValue(false);
                nMenuExtraToolsPackets.AddItem(new MenuItem("ExtraTools.AutoPink", "Auto Pink Ward:")).SetValue(false).SetTooltip("For rengar / vayne / shaco etc.");
                nMenuExtraToolsPackets.AddItem(new MenuItem("ExtraTools.Skin", "Skin Manager:")).SetValue(false);
                nMenuExtraToolsPackets.AddItem(new MenuItem("ExtraTools.Emote", "Emote:")).SetValue(false);
                nMenuExtraToolsPackets.AddItem(new MenuItem("ExtraTools.BuffTimer", "Buff Time Manager:")).SetValue(false).SetFontStyle(FontStyle.Regular, Color.Gray);
                nMenuExtraToolsPackets.AddItem(new MenuItem("ExtraTools.Potition", "Potition Manager:")).SetValue(false).SetFontStyle(FontStyle.Regular, Color.Gray);
                nMenuExtraToolsPackets.AddItem(new MenuItem("ExtraTools.Summoners", "Summoner Manager:")).SetValue(false).SetFontStyle(FontStyle.Regular, Color.Gray);
                nMenuExtraToolsPackets.AddItem(new MenuItem("ExtraTools.Tracker", "Tracker:")).SetValue(false).SetFontStyle(FontStyle.Regular, Color.Gray);
                
                nMenuExtraToolsPackets.AddItem(new MenuItem("ExtraTools.Reload", "Press F5 for Load Extra Tools!")).SetFontStyle(FontStyle.Bold, Color.GreenYellow);

                MenuExtraTools.AddSubMenu(nMenuExtraToolsPackets);

                MenuExtraToolsActivePackets = new Menu("Installed Tools", "MenuExtraTools.Installed").SetFontStyle(FontStyle.Regular, Color.GreenYellow);
                MenuExtraTools.AddSubMenu(MenuExtraToolsActivePackets);
            }
            Config.AddSubMenu(MenuExtraTools);

            Common.CommonSettings.Init(Config);

            OrbWalking = Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            ChampionClass.Orbwalker = new Orb.Orbwalking.Orbwalker(OrbWalking);
            //Orb.Orbwalking.Orbwalker Orbwalker = new Orb.Orbwalking.Orbwalker(OrbWalking);


            //if (MenuExtraTools.Item("ExtraTools.Orbwalker").GetValue<StringList>().SelectedIndex == 0)
            //{
            //    ChampionClass.OrbwalkerM = new Orbwalking.Orbwalker(OrbWalking);
            //    Orbwalking.Orbwalker OrbwalkerM = new Orbwalking.Orbwalker(OrbWalking);
            //}

            //if (MenuExtraTools.Item("ExtraTools.Orbwalker").GetValue<StringList>().SelectedIndex == 1)
            //{
            //    ChampionClass.Orbwalker = new Orb.Orbwalking.Orbwalker(OrbWalking);
            //    Orb.Orbwalking.Orbwalker Orbwalker = new Orb.Orbwalking.Orbwalker(OrbWalking);
            //}

            MenuActivator = new Menu("Activator", "Activator").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua);
            {
                if (MenuExtraTools.Item("ExtraTools.AutoLevel").GetValue<bool>())
                {
                    Common.CommonAutoLevel.Init(MenuExtraToolsActivePackets);
                }

                if (MenuExtraTools.Item("ExtraTools.AutoPink").GetValue<bool>())
                {
                    Common.CommonAutoPink.Initialize(MenuExtraToolsActivePackets);
                }

                if (MenuExtraTools.Item("ExtraTools.AutoBush").GetValue<bool>())
                {
                    Common.CommonAutoBush.Init(MenuExtraToolsActivePackets);
                }

                if (MenuExtraTools.Item("ExtraTools.Skin").GetValue<bool>())
                {
                    Common.CommonSkinManager.Init(MenuExtraToolsActivePackets);
                }

                if (MenuExtraTools.Item("ExtraTools.Emote").GetValue<bool>())
                {
                    Common.CommonEmote.Init(MenuExtraToolsActivePackets);
                }

                /* Menu Items */
                var items = MenuActivator.AddSubMenu(new Menu("Items", "Items"));
                items.AddItem(new MenuItem("BOTRK", "BOTRK").SetValue(true));
                items.AddItem(new MenuItem("GHOSTBLADE", "Ghostblade").SetValue(true));
                items.AddItem(new MenuItem("SWORD", "Sword of the Divine").SetValue(true));
                items.AddItem(new MenuItem("MURAMANA", "Muramana").SetValue(true));
                items.AddItem(
                    new MenuItem("UseItemsMode", "Use items on").SetValue(
                        new StringList(new[] { "No", "Mixed mode", "Combo mode", "Both" }, 2)));
            }
            Config.AddSubMenu(MenuActivator);
            
            // If Champion is supported draw the extra menus
            if (BaseType != ChampionClass.GetType())
            {
                SetSmiteSlot();

                var combo = new Menu("Combo", "Combo").SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);
                if (ChampionClass.ComboMenu(combo))
                {
                    if (SmiteSlot != SpellSlot.Unknown)
                        combo.AddItem(new MenuItem("ComboSmite", "Use Smite").SetValue(true));

                    Config.AddSubMenu(combo);
                }

                var harass = new Menu("Harass", "Harass");
                if (ChampionClass.HarassMenu(harass))
                {
                    harass.AddItem(new MenuItem("HarassMana", "Min. Mana Percent").SetValue(new Slider(50, 100, 0)));
                    Config.AddSubMenu(harass);
                }

                var laneclear = new Menu("Lane Mode", "LaneClear");
                if (ChampionClass.LaneClearMenu(laneclear))
                {
                    laneclear.AddItem(new MenuItem("Lane.Enabled", ":: Enable Lane Farm!").SetValue(new KeyBind("L".ToCharArray()[0], KeyBindType.Toggle, true))).Permashow(true, "Marksman | Enable Lane Farm", SharpDX.Color.Aqua);

                    var minManaMenu = new Menu("Min. Mana Settings", "Lane.MinMana.Title");
                    {
                        minManaMenu.AddItem(new MenuItem("LaneMana.Alone", "If I'm Alone %:").SetValue(new Slider(30, 100, 0))).SetFontStyle(FontStyle.Regular, Color.LightSkyBlue);
                        minManaMenu.AddItem(new MenuItem("LaneMana.Enemy", "If Enemy Close %:").SetValue(new Slider(60, 100, 0))).SetFontStyle(FontStyle.Regular, Color.IndianRed);
                        laneclear.AddSubMenu(minManaMenu);
                    }
                    Config.AddSubMenu(laneclear);
                }

                var jungleClear = new Menu("Jungle Mode", "JungleClear");
                if (ChampionClass.JungleClearMenu(jungleClear))
                {
                    var minManaMenu = new Menu("Min. Mana Settings", "Jungle.MinMana.Title");
                    {
                        minManaMenu.AddItem(new MenuItem("Jungle.Mana.Ally", "Ally Mobs %:").SetValue(new Slider(50, 100, 0))).SetFontStyle(FontStyle.Regular, Color.LightSkyBlue);
                        minManaMenu.AddItem(new MenuItem("Jungle.Mana.Enemy", "Enemy Mobs %:").SetValue(new Slider(30, 100, 0))).SetFontStyle(FontStyle.Regular, Color.IndianRed);
                        minManaMenu.AddItem(new MenuItem("Jungle.Mana.BigBoys", "Baron/Dragon %:").SetValue(new Slider(70, 100, 0))).SetFontStyle(FontStyle.Regular, Color.HotPink);
                        jungleClear.AddSubMenu(minManaMenu);
                    }
                    jungleClear.AddItem(new MenuItem("Jungle.Items", ":: Use Items:").SetValue(new StringList(new[] { "Off", "Use for Baron", "Use for Baron", "Both" }, 3)));
                    jungleClear.AddItem(new MenuItem("Jungle.Enabled", ":: Enable Jungle Farm!").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Toggle, true))).Permashow(true, "Marksman | Enable Jungle Farm", SharpDX.Color.Aqua);
                    Config.AddSubMenu(jungleClear);
                }

                /*----------------------------------------------------------------------------------------------------------*/
                Obj_AI_Base ally = (from aAllies in HeroManager.Allies
                    from aSupportedChampions in
                        new[]
                        {
                            "janna", "tahm", "leona", "lulu", "lux", "nami", "shen", "sona", "braum", "bard"
                        }
                    where aSupportedChampions == aAllies.ChampionName.ToLower()
                    select aAllies).FirstOrDefault();

                if (ally != null)
                {
                    var menuAllies = new Menu("Ally Combo", "Ally.Combo").SetFontStyle(FontStyle.Regular, SharpDX.Color.Crimson);
                    {
                        Obj_AI_Hero Leona = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "leona");
                        if (Leona != null)
                        {
                            var menuLeona = new Menu("Leona", "Leona");
                            menuLeona.AddItem(new MenuItem("Leona.ComboBuff", "Force Focus Marked Enemy for Bonus Damage").SetValue(true));
                            menuAllies.AddSubMenu(menuLeona);
                        }

                        Obj_AI_Hero Lux = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "lux");
                        if (Lux != null)
                        {
                            var menuLux = new Menu("Lux", "Lux");
                            menuLux.AddItem(new MenuItem("Lux.ComboBuff", "Force Focus Marked Enemy for Bonus Damage").SetValue(true));
                            menuAllies.AddSubMenu(menuLux);
                        }

                        Obj_AI_Hero Shen = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "shen");
                        if (Shen != null)
                        {
                            var menuShen = new Menu("Shen", "Shen");
                            menuShen.AddItem(new MenuItem("Shen.ComboBuff", "Force Focus Q Marked Enemy Objects for Heal").SetValue(true));
                            menuShen.AddItem(new MenuItem("Shen.ComboBuff", "Minimum Heal:").SetValue(new Slider(80)));
                            menuAllies.AddSubMenu(menuShen);
                        }

                        Obj_AI_Hero Tahm = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "Tahm");
                        if (Tahm != null)
                        {
                            var menuTahm = new Menu("Tahm", "Tahm");
                            menuTahm.AddItem(new MenuItem("Tahm.ComboBuff", "Force Focus Marked Enemy for Stun").SetValue(true));
                            menuAllies.AddSubMenu(menuTahm);
                        }

                        Obj_AI_Hero Sona = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "Sona");
                        if (Sona != null)
                        {
                            var menuSona = new Menu("Sona", "Sona");
                            menuSona.AddItem(new MenuItem("Sona.ComboBuff", "Force Focus to Marked Enemy").SetValue(true));
                            menuAllies.AddSubMenu(menuSona);
                        }

                        Obj_AI_Hero Lulu = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "Lulu");
                        if (Lulu != null)
                        {
                            var menuLulu = new Menu("Lulu", "Lulu");
                            menuLulu.AddItem(new MenuItem("Lulu.ComboBuff", "Force Focus to Enemy If I have E buff").SetValue(true));
                            menuAllies.AddSubMenu(menuLulu);
                        }

                        Obj_AI_Hero Nami = HeroManager.Allies.Find(e => e.ChampionName.ToLower() == "nami");
                        if (Nami != null)
                        {
                            var menuNami = new Menu("Nami", "Nami");
                            menuNami.AddItem(new MenuItem("Nami.ComboBuff", "Force Focus to Enemy If I have E Buff").SetValue(true));
                            menuAllies.AddSubMenu(menuNami);
                        }
                    }
                    Config.AddSubMenu(menuAllies);
                }
                /*----------------------------------------------------------------------------------------------------------*/

                var misc = new Menu("Misc", "Misc").SetFontStyle(FontStyle.Regular, SharpDX.Color.DarkOrange);
                if (ChampionClass.MiscMenu(misc))
                {
                    misc.AddItem(new MenuItem("Misc.SaveManaForUltimate", "Save Mana for Ultimate").SetValue(false));                    
                    Config.AddSubMenu(misc);
                }
                /*
                                var extras = new Menu("Extras", "Extras");
                                if (ChampionClass.ExtrasMenu(extras))
                                {
                                    Config.AddSubMenu(extras);
                                }
                 */

                var marksmanDrawings = new Menu("Drawings", "MDrawings");
                Config.AddSubMenu(marksmanDrawings);

                var drawing = new Menu(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(championName), "Drawings").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aquamarine);
                if (ChampionClass.DrawingMenu(drawing))
                {
                    marksmanDrawings.AddSubMenu(drawing);
                }

                var GlobalDrawings = new Menu("Global", "GDrawings");
                {
                    marksmanDrawings.AddItem(new MenuItem("Draw.TurnOff", "Drawings").SetValue(new StringList(new[] { "Disable", "Enable", "Disable on Combo Mode", "Disable on Lane/Jungle Mode", "Both" }, 1)));
                    var menuCompare = new Menu("Compare me with", "Menu.Compare");
                    {
                        string[] strCompare = new string[HeroManager.Enemies.Count + 1];
                        strCompare[0] = "Off";
                        var i = 1;
                        foreach (var e in HeroManager.Enemies)
                        {
                            strCompare[i] = e.ChampionName;
                            i += 1;
                        }
                        menuCompare.AddItem(new MenuItem("Marksman.Compare.Set", "Set").SetValue(new StringList(new[] { "Off", "Auto Compare at Startup" }, 1)));
                        menuCompare.AddItem(new MenuItem("Marksman.Compare", "Compare me with").SetValue(new StringList(strCompare, 0)));
                        GlobalDrawings.AddSubMenu(menuCompare);
                    }

                    GlobalDrawings.AddItem(new MenuItem("Draw.KillableEnemy", "Killable Enemy Text").SetValue(false));
                    GlobalDrawings.AddItem(new MenuItem("Draw.MinionLastHit", "Minion Last Hit").SetValue(new StringList(new[] { "Off", "On", "Just Out of AA Range Minions" }, 2)));



                    //GlobalDrawings.AddItem(new MenuItem("Draw.JunglePosition", "Jungle Farm Position").SetValue(new StringList(new[] { "Off", "If I'm Close to Mobs", "If Jungle Clear Active" }, 2)));
                    GlobalDrawings.AddItem(new MenuItem("Draw.DrawMinion", "Draw Minions Sprite").SetValue(false));
                    GlobalDrawings.AddItem(new MenuItem("Draw.DrawTarget", "Draw Target Sprite").SetValue(true));
                    marksmanDrawings.AddSubMenu(GlobalDrawings);

                }
            }

            ChampionClass.MainMenu(Config);

            //Evade.Evade.Initiliaze();
            //Config.AddSubMenu(Evade.Config.Menu);

            Config.AddToMainMenu();

            foreach (var i in Config.Children.Cast<Menu>().SelectMany(GetChildirens))
            {
                i.DisplayName = ":: " + i.DisplayName;
            }

            //CheckAutoWindUp();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Game.OnUpdate += eventArgs =>
            {
                if (ChampionClass.LaneClearActive)
                {
                    ExecuteLaneClear();
                }

                if (ChampionClass.JungleClearActive)
                {
                    ExecuteJungleClear();
                }

                PermaActive();
            };

            Orb.Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Orb.Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            GameObject.OnCreate += OnCreateObject;
            GameObject.OnDelete += OnDeleteObject;

            Obj_AI_Base.OnBuffAdd += Obj_AI_Base_OnBuffAdd;
            Obj_AI_Base.OnBuffRemove += Obj_AI_Base_OnBuffRemove;

            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            Console.Clear();
        }

        private static IEnumerable<Menu> GetChildirens(Menu menu)
        {
            yield return menu;

            foreach (var childChild in menu.Children.SelectMany(GetChildirens))
                yield return childChild;
        }

        private static void CheckAutoWindUp()
        {
            var additional = 0;

            if (Game.Ping >= 100)
            {
                additional = Game.Ping / 100 * 10;
            }
            else if (Game.Ping > 40 && Game.Ping < 100)
            {
                additional = Game.Ping / 100 * 20;
            }
            else if (Game.Ping <= 40)
            {
                additional = +20;
            }
            var windUp = Game.Ping + additional;
            if (windUp < 40)
            {
                windUp = 40;
            }

            
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var turnOffDrawings = Config.Item("Draw.TurnOff").GetValue<StringList>().SelectedIndex;

            if (turnOffDrawings == 0)
            {
                return;
            }

            if ((turnOffDrawings == 2 || turnOffDrawings == 4) && ChampionClass.Orbwalker.ActiveMode == Orb.Orbwalking.OrbwalkingMode.Combo)
            {
                return;
            }

            if ((turnOffDrawings == 3 || turnOffDrawings == 4) && (ChampionClass.Orbwalker.ActiveMode == Orb.Orbwalking.OrbwalkingMode.LastHit || ChampionClass.Orbwalker.ActiveMode == Orb.Orbwalking.OrbwalkingMode.LaneClear))
            {
                return;
            }

            var drawMinionLastHit = Config.Item("Draw.MinionLastHit").GetValue<StringList>().SelectedIndex;
            if (drawMinionLastHit != 0)
            {
                var mx = ObjectManager.Get<Obj_AI_Minion>().Where(m => !m.IsDead && m.IsEnemy).Where(m => m.Health <= ObjectManager.Player.TotalAttackDamage);

                if (drawMinionLastHit == 1)
                {
                    mx = mx.Where(m => m.IsValidTarget(Orb.Orbwalking.GetRealAutoAttackRange(null) + 65));
                }
                else
                {
                    mx = mx.Where(m => m.IsValidTarget(Orb.Orbwalking.GetRealAutoAttackRange(null) + 65 + 300) && m.Distance(ObjectManager.Player.Position) > Orb.Orbwalking.GetRealAutoAttackRange(null) + 65);
                }

                foreach (var minion in mx)
                {
                    Render.Circle.DrawCircle(minion.Position, minion.BoundingRadius, System.Drawing.Color.GreenYellow, 1);
                }
            }

            if (ChampionClass != null)
            {
                ChampionClass.Drawing_OnDraw(args);
            }
        }

        private void MySupport()
        {

        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //Update the combo and harass values.
            ChampionClass.ComboActive = ChampionClass.Config.Item("Orbwalk").GetValue<KeyBind>().Active;
            
            var vHarassManaPer = Config.Item("HarassMana").GetValue<Slider>().Value;
            ChampionClass.HarassActive = ChampionClass.Config.Item("Farm").GetValue<KeyBind>().Active &&
                                  ObjectManager.Player.ManaPercent >= vHarassManaPer;

            ChampionClass.ToggleActive = ObjectManager.Player.ManaPercent >= vHarassManaPer && ChampionClass.Orbwalker.ActiveMode != Orb.Orbwalking.OrbwalkingMode.Combo && !ObjectManager.Player.IsRecalling();

            var vLaneClearManaPer = HeroManager.Enemies.Find(e => e.IsValidTarget(2000) && !e.IsZombie) == null
                ? Config.Item("LaneMana.Alone").GetValue<Slider>().Value
                : Config.Item("LaneMana.Enemy").GetValue<Slider>().Value;

            ChampionClass.LaneClearActive = ChampionClass.Config.Item("LaneClear").GetValue<KeyBind>().Active && ObjectManager.Player.ManaPercent >= vLaneClearManaPer && Config.Item("Lane.Enabled").GetValue<KeyBind>().Active;

            ChampionClass.JungleClearActive = false;
            if (ChampionClass.Config.Item("LaneClear").GetValue<KeyBind>().Active && Config.Item("Jungle.Enabled").GetValue<KeyBind>().Active)
            {
                List<Obj_AI_Base> mobs = MinionManager.GetMinions(ObjectManager.Player.Position, 1000, MinionTypes.All, MinionTeam.Neutral);

                if (mobs.Count > 0)
                {
                    var minMana = Config.Item("Jungle.Mana.Enemy").GetValue<Slider>().Value;

                    if (mobs[0].SkinName.ToLower().Contains("baron") || mobs[0].SkinName.ToLower().Contains("dragon") || mobs[0].Team() == Jungle.GameObjectTeam.Neutral)
                    {
                        minMana = Config.Item("Jungle.Mana.BigBoys").GetValue<Slider>().Value;
                    }

                    else if (mobs[0].Team() == (Jungle.GameObjectTeam)ObjectManager.Player.Team)
                    {
                        minMana = Config.Item("Jungle.Mana.Ally").GetValue<Slider>().Value;
                    }

                    else if (mobs[0].Team() != (Jungle.GameObjectTeam)ObjectManager.Player.Team)
                    {
                        minMana = Config.Item("Jungle.Mana.Enemy").GetValue<Slider>().Value;
                    }

                    if (ObjectManager.Player.ManaPercent >= minMana)
                    {
                        ChampionClass.JungleClearActive = true;
                    }
                }
            }
            //ChampionClass.JungleClearActive = ChampionClass.Config.Item("LaneClear").GetValue<KeyBind>().Active && ObjectManager.Player.ManaPercent >= Config.Item("Jungle.Mana").GetValue<Slider>().Value;

            ChampionClass.Game_OnGameUpdate(args);

            UseSummoners();
            var useItemModes = Config.Item("UseItemsMode").GetValue<StringList>().SelectedIndex;

            //Items
            if (
                !((ChampionClass.Orbwalker.ActiveMode == Orb.Orbwalking.OrbwalkingMode.Combo &&
                   (useItemModes == 2 || useItemModes == 3))
                  ||
                  (ChampionClass.Orbwalker.ActiveMode == Orb.Orbwalking.OrbwalkingMode.Mixed &&
                   (useItemModes == 1 || useItemModes == 3))))
                return;

            var botrk = Config.Item("BOTRK").GetValue<bool>();
            var ghostblade = Config.Item("GHOSTBLADE").GetValue<bool>();
            var sword = Config.Item("SWORD").GetValue<bool>();
            var muramana = Config.Item("MURAMANA").GetValue<bool>();
            var target = ChampionClass.Orbwalker.GetTarget() as Obj_AI_Base;

            var smiteReady = (SmiteSlot != SpellSlot.Unknown &&
                              ObjectManager.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready);

            if (smiteReady && ChampionClass.Orbwalker.ActiveMode == Orb.Orbwalking.OrbwalkingMode.Combo)
                Smiteontarget(target as Obj_AI_Hero);

            if (botrk)
            {
                if (target != null && target.Type == ObjectManager.Player.Type &&
                    target.ServerPosition.Distance(ObjectManager.Player.ServerPosition) < 550)
                {
                    var hasCutGlass = Items.HasItem(3144);
                    var hasBotrk = Items.HasItem(3153);

                    if (hasBotrk || hasCutGlass)
                    {
                        var itemId = hasCutGlass ? 3144 : 3153;
                        var damage = ObjectManager.Player.GetItemDamage(target, Damage.DamageItems.Botrk);
                        if (hasCutGlass || ObjectManager.Player.Health + damage < ObjectManager.Player.MaxHealth)
                            Items.UseItem(itemId, target);
                    }
                }
            }

            if (ghostblade && target != null && target.Type == ObjectManager.Player.Type &&
                !ObjectManager.Player.HasBuff("ItemSoTD") /*if Sword of the divine is not active */
                && Orb.Orbwalking.InAutoAttackRange(target))
                Items.UseItem(3142);

            if (sword && target != null && target.Type == ObjectManager.Player.Type &&
                !ObjectManager.Player.HasBuff("spectralfury") /*if ghostblade is not active*/
                && Orb.Orbwalking.InAutoAttackRange(target))
                Items.UseItem(3131);

            if (muramana && Items.HasItem(3042))
            {
                if (target != null && ChampionClass.ComboActive &&
                    target.Position.Distance(ObjectManager.Player.Position) < 1200)
                {
                    if (!ObjectManager.Player.HasBuff("Muramana"))
                    {
                        Items.UseItem(3042);
                    }
                }
                else
                {
                    if (ObjectManager.Player.HasBuff("Muramana"))
                    {
                        Items.UseItem(3042);
                    }
                }
            }
        }

        public static void UseSummoners()
        {
            if (ChampionClass.Orbwalker.ActiveMode != Orb.Orbwalking.OrbwalkingMode.Combo)
            {
                return;
            }

            var t = ChampionClass.Orbwalker.GetTarget() as Obj_AI_Hero;

            if (t != null && IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (ObjectManager.Player.Distance(t) < 650 &&
                    ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) >=
                    t.Health)
                {
                    ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, t);
                }
            }
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            ChampionClass.Orbwalking_AfterAttack(unit, target);
        }

        private static void Orbwalking_BeforeAttack(Orb.Orbwalking.BeforeAttackEventArgs args)
        {
            ChampionClass.Orbwalking_BeforeAttack(args);
        }

        private static void ExecuteJungleClear()
        {
            ChampionClass.ExecuteJungleClear();
        }
        private static void ExecuteLaneClear()
        {
            ChampionClass.ExecuteLaneClear();
        }
        private static void PermaActive()
        {
            ChampionClass.PermaActive();
        }
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            ChampionClass.Obj_AI_Base_OnProcessSpellCast(sender, args);
        }
        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (Config.Item("Misc.SaveManaForUltimate").GetValue<bool>() &&
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level > 0 &&
                Math.Abs(ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Cooldown) < 0.00001 &&
                args.Slot != SpellSlot.R)
            {
                var lastMana = ObjectManager.Player.Mana - ObjectManager.Player.Spellbook.GetSpell(args.Slot).ManaCost;
                if (lastMana < ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).ManaCost)
                {
                    args.Process = false;
                }
            }
            
            ChampionClass.Spellbook_OnCastSpell(sender, args);
        }

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            ChampionClass.OnCreateObject(sender, args);
        }

        private static void OnDeleteObject(GameObject sender, EventArgs args)
        {
            ChampionClass.OnDeleteObject(sender, args);
        }

        private static void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            ChampionClass.Obj_AI_Base_OnBuffAdd(sender, args);
        }

        private static void Obj_AI_Base_OnBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args)
        {
            ChampionClass.Obj_AI_Base_OnBuffRemove(sender, args);
        }

        private static string Smitetype
        {
            get
            {
                if (SmiteBlue.Any(i => Items.HasItem(i)))
                    return "s5_summonersmiteplayerganker";

                if (SmiteRed.Any(i => Items.HasItem(i)))
                    return "s5_summonersmiteduel";

                if (SmiteGrey.Any(i => Items.HasItem(i)))
                    return "s5_summonersmitequick";

                if (SmitePurple.Any(i => Items.HasItem(i)))
                    return "itemsmiteaoe";

                return "summonersmite";
            }
        }

        private static void SetSmiteSlot()
        {
            foreach (
                var spell in
                    ObjectManager.Player.Spellbook.Spells.Where(
                        spell => String.Equals(spell.Name, Smitetype, StringComparison.CurrentCultureIgnoreCase)))
            {
                SmiteSlot = spell.Slot;
                Smite = new Spell(SmiteSlot, 700);
            }
        }

        private static void Smiteontarget(Obj_AI_Hero t)
        {
            var useSmite = Config.Item("ComboSmite").GetValue<bool>();
            var itemCheck = SmiteBlue.Any(i => Items.HasItem(i)) || SmiteRed.Any(i => Items.HasItem(i));
            if (itemCheck && useSmite &&
                ObjectManager.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready &&
                t.Distance(ObjectManager.Player.Position) < Smite.Range)
            {
                ObjectManager.Player.Spellbook.CastSpell(SmiteSlot, t);
            }
        }
        public static void DrawBox(Vector2 position, int width, int height, System.Drawing.Color color, int borderwidth, System.Drawing.Color borderColor)
        {
            Drawing.DrawLine(position.X, position.Y, position.X + width, position.Y, height, color);

            if (borderwidth > 0)
            {
                Drawing.DrawLine(position.X, position.Y, position.X + width, position.Y, borderwidth, borderColor);
                Drawing.DrawLine(position.X, position.Y + height, position.X + width, position.Y + height, borderwidth, borderColor);
                Drawing.DrawLine(position.X, position.Y + 1, position.X, position.Y + height, borderwidth, borderColor);
                Drawing.DrawLine(position.X + width, position.Y + 1, position.X + width, position.Y + height, borderwidth, borderColor);
            }
        }
    }
}
