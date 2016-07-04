#region

using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalking = Marksman.Orb.Orbwalking;

#endregion

namespace Marksman.Champions
{
    internal class Teemo : Champion
    {
        public Spell Q;
        public Spell R;
        private int LastRCast;
        public Teemo()
        {
            Q = new Spell(SpellSlot.Q, 680);
            R = new Spell(SpellSlot.R, 230);
            Q.SetTargetted(0f, 2000f);
            R.SetSkillshot(0.1f, 75f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
            {
                if (R.IsReady())
                {
                    BuffInstance aBuff =
                        (from fBuffs in
                             sender.Buffs.Where(
                                 s =>
                                 sender.Team != ObjectManager.Player.Team
                                 && sender.Distance(ObjectManager.Player.Position) < R.Range)
                         from b in new[]
                                           {
                                               "teleport", /* Teleport */ "pantheon_grandskyfall_jump", /* Pantheon */ 
                                               "crowstorm", /* FiddleScitck */
                                               "zhonya", "katarinar", /* Katarita */
                                               "MissFortuneBulletTime", /* MissFortune */
                                               "gate", /* Twisted Fate */
                                               "chronorevive" /* Zilean */
                                           }
                         where args.Buff.Name.ToLower().Contains(b)
                         select fBuffs).FirstOrDefault();

                    if (aBuff != null)
                    {
                        R.Cast(sender.Position);
                    }
                }
            };

            Utils.Utils.PrintMessage("Teemo loaded.");
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t != null && (ComboActive || HarassActive) && unit.IsMe)
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));

                if (useQ && Q.IsReady())
                    Q.CastOnUnit(t);
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = {Q};
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {

            //var lee = HeroManager.Allies.Find(l => l.ChampionName.ToLower() == "leesin");
            //if (lee != null && !lee.IsDead)
            //{
            //    if (lee.Distance(ObjectManager.Player.Position) > 250 && !ObjectManager.Player.IsRecalling() &&
            //        Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.None)
            //    {
            //        ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, lee.Position);
            //    }
            //}

            R.Range = 150 + (R.Level*250);
            
            if (Q.IsReady() && GetValue<KeyBind>("UseQTH").Active && ToggleActive)
            {
                if (ObjectManager.Player.HasBuff("Recall"))
                    return;
                var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (Q.IsReady() && qTarget.IsValidTarget())
                    Q.CastOnUnit(qTarget);
            }

            if (ComboActive || HarassActive)
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                if (useQ)
                {
                    var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                    if (Q.IsReady() && qTarget.IsValidTarget())
                        Q.CastOnUnit(qTarget);
                }
            }

            if (R.IsReady() && ComboActive)
            {
                foreach (var t in HeroManager.Enemies.Where(hero => hero.IsValidTarget(R.Range) && !hero.IsDead))
                {
                    if (GetValue<bool>("UseRC") && LeagueSharp.Common.Utils.TickCount > LastRCast + 1200)
                    {
                        if (t.HealthPercent > ObjectManager.Player.HealthPercent)
                            //if (t.HealthPercent > ObjectManager.Player.HealthPercent && t.IsFacing(ObjectManager.Player))
                            {
                            R.Cast(ObjectManager.Player, false, true);
                        }
                        else
                        {
                            R.Cast(t, false, true);
                        }
                        LastRCast = LeagueSharp.Common.Utils.TickCount;
                    }

                    if (GetValue<bool>("AutoRI"))
                    {
                        if (t.IsValidTarget(R.Range) &&
                            (t.HasBuffOfType(BuffType.Stun) || t.HasBuffOfType(BuffType.Snare) ||
                             t.HasBuffOfType(BuffType.Taunt) || t.HasBuff("zhonyasringshield") ||
                             t.HasBuff("Recall")))
                        {
                            R.Cast(t.Position);
                        }
                    }
                }
            }

            if (LaneClearActive && Q.IsReady() && GetValue<bool>("UseQL"))
            {
                var vMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range);
                foreach (var minions in vMinions.Where(minions => minions.Health < Q.GetDamage(minions) - 10))
                {
                    Q.Cast(minions);
                }
            }
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseRC" + Id, "Use R").SetValue(false));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(false));
            config.AddItem(
                new MenuItem("UseQTH" + Id, "Use Q (Toggle)").SetValue(new KeyBind("H".ToCharArray()[0],
                    KeyBindType.Toggle))).Permashow(true, "Marksman | Toggle Q");
            return true;
            }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQM" + Id, "Use Q KS").SetValue(true));
            config.AddItem(new MenuItem("AutoRI" + Id, "R: Stun/Snare/Taunt/Zhonya").SetValue(true));
            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQL" + Id, "Use Q").SetValue(true));
            return true;
        }
        public override bool JungleClearMenu(Menu config)
        {
            return true;
        }
    }
}
