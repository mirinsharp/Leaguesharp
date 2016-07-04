#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Collision = LeagueSharp.Common.Collision;
using Color = System.Drawing.Color;
using Font = SharpDX.Direct3D9.Font;
using Marksman.Common;

#endregion

namespace Marksman.Champions
{
    using Marksman.Utils;

    internal class Ezreal : Champion
    {
        public static Spell Q;

        public static Spell E;

        public static Spell W;

        public static Spell R;

        private static bool haveIceBorn = false;

        public Ezreal()
        {
            Q = new Spell(SpellSlot.Q, 1190);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 950);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E);

            R = new Spell(SpellSlot.R, 2500);
            R.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            Obj_AI_Base.OnBuffAdd += (sender, args) =>
            {
                //if (sender.IsMe)
                //Game.PrintChat(args.Buff.Name);
            };

            Utils.PrintMessage("Ezreal loaded");
        }
      
        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t != null && (ComboActive || HarassActive) && unit.IsMe && !t.HasKindredUltiBuff())
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));

                if (Q.IsReady() && useQ)
                {
                    Q.CastIfHitchanceGreaterOrEqual(t);
                }
                else if (W.IsReady() && useW)
                {
                    W.CastIfHitchanceGreaterOrEqual(t);
                }
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            foreach (var enemy in HeroManager.Enemies.Where(enemy => R.IsReady() && enemy.IsValidTarget() && R.GetDamage(enemy) > enemy.Health))
            {
                Marksman.Common.CommonGeometry.DrawBox(new Vector2(Drawing.Width*0.43f, Drawing.Height*0.80f), 185, 18, Color.FromArgb(242, 255, 236, 6), 1, System.Drawing.Color.Black);
                Marksman.Common.CommonGeometry.DrawText(Marksman.Common.CommonGeometry.Text, "Killable enemy with ultimate: " + enemy.ChampionName, Drawing.Width*0.435f, Drawing.Height*0.803f, SharpDX.Color.Black);
            }

            Spell[] spellList = {Q, W};
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }

            var drawRMin = Program.Config.SubMenu("Combo").Item("DrawRMin").GetValue<Circle>();
            if (drawRMin.Active)
            {
                var minRRange = Program.Config.SubMenu("Combo").Item("UseRCMinRange").GetValue<Slider>().Value;
                Render.Circle.DrawCircle(ObjectManager.Player.Position, minRRange, drawRMin.Color, 2);
            }

            var drawRMax = Program.Config.SubMenu("Combo").Item("DrawRMax").GetValue<Circle>();
            if (drawRMax.Active)
            {
                var maxRRange = Program.Config.SubMenu("Combo").Item("UseRCMaxRange").GetValue<Slider>().Value;
                Render.Circle.DrawCircle(ObjectManager.Player.Position, maxRRange, drawRMax.Color, 2);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            Console.WriteLine(Q.GetHitchance().ToString());
            haveIceBorn = ObjectManager.Player.InventoryItems.Any(i => i.Id == ItemId.Iceborn_Gauntlet);
            
            if (GetValue<bool>("ChargeR.Enable") && Orbwalker.ActiveMode != Orb.Orbwalking.OrbwalkingMode.Combo)
            {
                var rCooldown = GetValue<Slider>("ChargeR.Cooldown").Value;
                var rMinMana = GetValue<Slider>("ChargeR.MinMana").Value;

                if (ObjectManager.Player.ManaPercent >= rMinMana && R.Cooldown >= rCooldown)
                {
                    var vMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range);
                    foreach (var hit in from minions in vMinions
                        select Q.GetPrediction(minions)
                        into qP
                        let hit = qP.CastPosition.Extend(ObjectManager.Player.Position, -140)
                        where qP.Hitchance >= HitChance.High
                        select hit)
                    {
                        Q.Cast(hit);
                    }
                }
            }



            // 3070 tear of the goddess
         //  foreach (var i in ObjectManager.Player.InventoryItems)
          //  {
                //Game.PrintChat(i.IData.Id.ToString());
        //    }

            if (GetValue<bool>("PingCH"))
            {
                foreach (var enemy in
                    HeroManager.Enemies.Where(
                        enemy =>
                            R.IsReady() && enemy.IsValidTarget() && R.GetDamage(enemy) > enemy.Health
                            && enemy.Distance(ObjectManager.Player) > Q.Range))
                {
                    Utils.MPing.Ping(enemy.Position.To2D());
                }
            }

            Obj_AI_Hero t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            var toggleQ = Program.Config.Item("UseQTH").GetValue<KeyBind>().Active;
            var toggleW = Program.Config.Item("UseWTH").GetValue<KeyBind>().Active;
            if ((toggleQ || toggleW) && t.IsValidTarget(Q.Range) && ToggleActive)
            {
                if (Q.IsReady() && toggleQ)
                {
                    if (ObjectManager.Player.HasBuff("Recall")) return;

                    var useQt = (Program.Config.Item("DontQToggleHarass" + t.ChampionName) != null
                                 && Program.Config.Item("DontQToggleHarass" + t.ChampionName).GetValue<bool>() == false);
                    if (useQt)
                    {
                        Q.CastIfHitchanceGreaterOrEqual(t);
                    }
                }

                if (W.IsReady() && t.IsValidTarget(W.Range) && toggleW)
                {
                    if (ObjectManager.Player.HasBuff("Recall")) return;
                    var useWt = (Program.Config.Item("DontWToggleHarass" + t.ChampionName) != null
                                 && Program.Config.Item("DontWToggleHarass" + t.ChampionName).GetValue<bool>() == false);
                    if (useWt)
                    {
                        W.Cast(t);
                    }
                }
            }

            if (ComboActive || HarassActive)
            {
                t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));
                var useR = Program.Config.SubMenu("Combo").Item("UseRC").GetValue<bool>();

                if (Orb.Orbwalking.CanMove(100) && !t.HasKindredUltiBuff())
                {
                    if (useQ && Q.IsReady() && t.IsValidTarget(Q.Range))
                    {
                        Q.CastIfHitchanceGreaterOrEqual(t);
                    }

                    if (useW && W.IsReady() && t.IsValidTarget(W.Range))
                    {
                        W.Cast(t);
                    }

                    if (R.IsReady() && useR)
                    {
                        var maxRRange = Program.Config.SubMenu("Combo").Item("UseRCMaxRange").GetValue<Slider>().Value;
                        var minRRange = Program.Config.SubMenu("Combo").Item("UseRCMinRange").GetValue<Slider>().Value;

                        if (Q.IsReady() && t.IsValidTarget(Q.Range) && Q.GetPrediction(t).CollisionObjects.Count == 0
                            && t.Health < ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q)) return;

                        if (t.IsValidTarget() && ObjectManager.Player.Distance(t) >= minRRange
                            && ObjectManager.Player.Distance(t) <= maxRRange
                            && t.Health <= ObjectManager.Player.GetSpellDamage(t, SpellSlot.R))
                        {
                            R.Cast(t);
                        }
                    }
                }
            }

            if (R.IsReady() && GetValue<KeyBind>("CastR").Active)
            {
                t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget()) R.Cast(t);
            }
        }

        public override bool LaneClearMenu(Menu config)
        {
            var qSubMenu = new Menu("Q Farm", "Lane.QFarm");
            {
                qSubMenu.AddItem(new MenuItem("Lane.UseQ" + Id, "Q: Everytime").SetValue(true)).SetFontStyle(FontStyle.Regular, Q.MenuColor());
                qSubMenu.AddItem(new MenuItem("Lane.UseQ.AARange" + Id, "Q: Auto of AA Range").SetValue(true)).SetFontStyle(FontStyle.Regular, Q.MenuColor());
                qSubMenu.AddItem(new MenuItem("Lane.Q.HeatlhPrediction" + Id, "Q: Health Prediciton").SetValue(true)).SetFontStyle(FontStyle.Regular, Q.MenuColor());
            }
            config.AddSubMenu(qSubMenu);
            return true;
        }

        public override void ExecuteLaneClear()
        {
            if (!Q.IsReady())
            {
                return;
            }

            if (!GetValue<bool>("Lane.UseQ") && !GetValue<bool>("Lane.UseQ.AARange") && !GetValue<bool>("Lane.UseQ.HeatlhPrediction"))
            {
                return;
            }

            var vMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range);

            if (GetValue<bool>("Lane.UseQ"))
            {
                foreach (var minions in
                    vMinions.Where(
                        minions => minions.Health < ObjectManager.Player.GetSpellDamage(minions, SpellSlot.Q)))
                {
                    var qP = Q.GetPrediction(minions);
                    var hit = qP.CastPosition.Extend(ObjectManager.Player.Position, -140);
                    if (qP.Hitchance >= HitChance.High) Q.Cast(hit);
                }
            }

            if (GetValue<bool>("Lane.UseQ.AARange"))
            {
                foreach (var minions in
                    vMinions.Where(
                        minions =>
                            minions.Health < ObjectManager.Player.GetSpellDamage(minions, SpellSlot.Q) &&
                            !minions.IsValidTarget(Orb.Orbwalking.GetRealAutoAttackRange(null) + 65)))
                {
                    var qP = Q.GetPrediction(minions);
                    var hit = qP.CastPosition.Extend(ObjectManager.Player.Position, -140);
                    if (qP.Hitchance >= HitChance.High) Q.Cast(hit);
                }
            }

            if (GetValue<bool>("Lane.Q.HeatlhPrediction"))
            {
                foreach (var n in vMinions)
                {
                    var xH = HealthPrediction.GetHealthPrediction(n, (int)(ObjectManager.Player.AttackCastDelay * 1000), Game.Ping + (int)Q.Delay);
                    if (xH < 0)
                    {
                        if (n.Health < Q.GetDamage(n) && Q.CanCast(n))
                        {
                            Q.Cast(n);
                        }
                    }
                }
            }
        }

        public override void ExecuteJungleClear()
        {
            if (!Q.IsReady() || GetValue<StringList>("Jungle.Q").SelectedIndex == 0)
            {
                return;
            }



            var jungleMobs = Utils.GetMobs(Q.Range, Utils.MobTypes.All);

            if (jungleMobs != null)
            {
                if (haveIceBorn)
                {
                    Q.Cast(jungleMobs);
                }
                else
                {
                    switch (GetValue<StringList>("Jungle.Q").SelectedIndex)
                    {
                        case 1:
                        {
                            Q.Cast(jungleMobs);
                            break;
                        }
                        case 2:
                        {
                            jungleMobs = Utils.GetMobs(Q.Range, Utils.MobTypes.BigBoys);
                            if (jungleMobs != null)
                            {
                                Q.Cast(jungleMobs);
                            }
                            break;
                        }
                    }
                }
            }
        }

        private static float GetComboDamage(Obj_AI_Hero t)
        {
            var fComboDamage = 0f;

            if (Q.IsReady()) fComboDamage += (float) ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q);

            if (W.IsReady()) fComboDamage += (float) ObjectManager.Player.GetSpellDamage(t, SpellSlot.W);

            if (E.IsReady()) fComboDamage += (float) ObjectManager.Player.GetSpellDamage(t, SpellSlot.E);

            if (R.IsReady()) fComboDamage += (float) ObjectManager.Player.GetSpellDamage(t, SpellSlot.R);

            if (ObjectManager.Player.GetSpellSlot("summonerdot") != SpellSlot.Unknown
                && ObjectManager.Player.Spellbook.CanUseSpell(ObjectManager.Player.GetSpellSlot("summonerdot"))
                == SpellState.Ready && ObjectManager.Player.Distance(t) < 550)
                fComboDamage += (float) ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);

            if (Items.CanUseItem(3144) && ObjectManager.Player.Distance(t) < 550)
                fComboDamage += (float) ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Bilgewater);

            if (Items.CanUseItem(3153) && ObjectManager.Player.Distance(t) < 550)
                fComboDamage += (float) ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Botrk);

            return fComboDamage;
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "W").SetValue(true));

            var xRMenu = new Menu("R", "ComboR");
            {
                xRMenu.AddItem(new MenuItem("UseRC", "Use").SetValue(true));
                xRMenu.AddItem(new MenuItem("UseRCMinRange", "Min. Range").SetValue(new Slider(200, 200, 1000)));
                xRMenu.AddItem(new MenuItem("UseRCMaxRange", "Max. Range").SetValue(new Slider(1500, 500, 2000)));
                xRMenu.AddItem(new MenuItem("DrawRMin", "Draw Min. R Range").SetValue(new Circle(true, Color.DarkRed)));
                xRMenu.AddItem(
                    new MenuItem("DrawRMax", "Draw Max. R Range").SetValue(new Circle(true, Color.DarkMagenta)));

                config.AddSubMenu(xRMenu);
            }
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            var qSubMenu = new Menu("Q:", "Harass.Q");
            {
                qSubMenu.AddItem(new MenuItem("UseQH" + Id, "Use:").SetValue(true));
                qSubMenu.AddItem(new MenuItem("UseQTH", "Toggle:").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle))).Permashow(true, "Marksman | Toggle Q");
                qSubMenu.AddSubMenu(new Menu("Don't Toggle:", "DontQToggleHarass"));
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
                {
                    qSubMenu.SubMenu("DontQToggleHarass").AddItem(new MenuItem("DontQToggleHarass" + enemy.ChampionName, enemy.ChampionName).SetValue(false));
                }
                config.AddSubMenu(qSubMenu);
            }

            var wSubMenu = new Menu("W:", "Harass.W");
            {
                wSubMenu.AddItem(new MenuItem("UseWH" + Id, "Use:").SetValue(true));
                wSubMenu.AddItem(new MenuItem("UseWTH", "Toggle:").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle))).Permashow(true, "Marksman | Toggle W");
                wSubMenu.AddSubMenu(new Menu("Don't Toggle:", "DontWToggleHarass"));
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
                {
                    wSubMenu.SubMenu("DontWToggleHarass").AddItem(new MenuItem("DontWToggleHarass" + enemy.ChampionName, enemy.ChampionName).SetValue(false));
                }
                config.AddSubMenu(wSubMenu);
            }

            
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("ChargeR.Enable" + Id, "Charge R with Q").SetValue(false).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow));
            config.AddItem(new MenuItem("ChargeR.Cooldown" + Id, Utils.Tab + "if R cooldown >").SetValue(new Slider(20, 10, 120)));
            config.AddItem(new MenuItem("ChargeR.MinMana" + Id, Utils.Tab + "And Min. Mana > %").SetValue(new Slider(50, 0, 100)));
            
            config.AddItem(
                new MenuItem("CastR" + Id, "Cast R (2000 Range)").SetValue(
                    new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            config.AddItem(new MenuItem("PingCH" + Id, "Ping Killable Enemy with R").SetValue(true));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawW" + Id, "W range").SetValue(new Circle(false, Color.FromArgb(100, 255, 255, 255))));
            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Damage After Combo").SetValue(true);

            config.AddItem(dmgAfterComboItem);
            return true;
        }

   
        public override bool JungleClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("Jungle.Q" + Id, "Use Q").SetValue(new StringList(new[] {"Off", "On", "Just big Monsters"}, 1)))
                .SetFontStyle(FontStyle.Regular, Q.MenuColor());
            return true;
        }

        public override void PermaActive()
        {
          
        }
    }
}
