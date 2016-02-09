using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SCommon;
using SCommon.Database;
using SCommon.PluginBase;
using SCommon.Prediction;
using SCommon.Orbwalking;
using SUtility.Drawings;
using SharpDX;
//typedefs
using TargetSelector = SCommon.TS.TargetSelector;

namespace SAutoCarry.Champions
{
    public class Hecarim : Champion
    {
        public Hecarim()
            : base("Hecarim", "SAutoCarry - Hecarim")
        {
            OnUpdate += BeforeOrbwalk;
            OnCombo += Combo;
            OnHarass += Harass;
            OnLaneClear += LaneClear;
        }

        public override void CreateConfigMenu()
        {
            Menu combo = new Menu("Combo", "SAutoCarry.Hecarim.Combo");
            combo.AddItem(new MenuItem("SAutoCarry.Hecarim.Combo.UseQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Hecarim.Combo.UseW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Hecarim.Combo.UseE", "Use E").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Hecarim.Combo.UseR", "Use R").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Hecarim.Combo.UseRMin", "Use R Min. Hit").SetValue(new Slider(1, 1, 5))).Show(combo.Item("SAutoCarry.Hecarim.Combo.UseR").GetValue<bool>());

            Menu harass = new Menu("Harass", "SAutoCarry.Hecarim.Harass");
            harass.AddItem(new MenuItem("SAutoCarry.Hecarim.Harass.UseQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("SAutoCarry.Hecarim.Harass.UseW", "Use W").SetValue(true));
            harass.AddItem(new MenuItem("SAutoCarry.Hecarim.Harass.UseE", "Use E").SetValue(true));
            harass.AddItem(new MenuItem("SAutoCarry.Hecarim.Harass.MinMana", "Min Mana Percent").SetValue(new Slider(30, 100, 0)));

            Menu laneclear = new Menu("LaneClear/JungleClear", "SAutoCarry.Hecarim.LaneClear");
            laneclear.AddItem(new MenuItem("SAutoCarry.Hecarim.LaneClear.UseQ", "Use Q").SetValue(true));
            laneclear.AddItem(new MenuItem("SAutoCarry.Hecarim.LaneClear.UseW", "Use W").SetValue(true));
            laneclear.AddItem(new MenuItem("SAutoCarry.Hecarim.LaneClear.MinMana", "Min Mana Percent").SetValue(new Slider(50, 100, 0)));

            Menu misc = new Menu("Misc", "SAutoCarry.Hecarim.Misc");
            misc.AddItem(new MenuItem("SAutoCarry.Hecarim.Misc.AutoQ", "Auto Harass Q").SetValue(true));
            misc.AddItem(new MenuItem("SAutoCarry.Hecarim.Misc.RKillSteal", "KS With R").SetValue(true));
            misc.AddItem(new MenuItem("SAutoCarry.Hecarim.Misc.InterruptR", "Interrupt with R").SetValue(true));
            misc.AddItem(new MenuItem("SAutoCarry.Hecarim.Misc.InterruptE", "Interrupt with E").SetValue(true));

                
            ConfigMenu.AddSubMenu(combo);
            ConfigMenu.AddSubMenu(harass);
            ConfigMenu.AddSubMenu(laneclear);
            ConfigMenu.AddSubMenu(misc);
            ConfigMenu.AddToMainMenu();
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q, 350f);

            Spells[W] = new Spell(SpellSlot.W, 525f);

            Spells[E] = new Spell(SpellSlot.E, 325f);

            Spells[R] = new Spell(SpellSlot.R, 1000f);
            Spells[R].SetSkillshot(0.25f, 300f, float.MaxValue, false, SkillshotType.SkillshotCircle);

        }

        public void BeforeOrbwalk()
        {

            if (Spells[Q].IsReady() && AutoQ)
            {
                if (HeroManager.Enemies.Any(p => p.IsValidTarget(Spells[Q].Range)))
                    Spells[Q].Cast();
            }

            if (KillStealR)
                KillSteal();
        }

        public void Combo()
        {

            if (Spells[E].IsReady() && ComboUseE)
            {
                var t = TargetSelector.GetTarget(Spells[E].Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (t != null)
                    Spells[E].CastOnUnit(t);
            }

            if (Spells[W].IsReady() && ComboUseW)
            {
                var t = TargetSelector.GetTarget(Spells[W].Range, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                if (t != null)
                    Spells[W].Cast();
            }

            if (Spells[Q].IsReady() && ComboUseQ)
            {
                var t = TargetSelector.GetTarget(Spells[W].Range, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                if (t != null)
                    Spells[Q].Cast(t);
            }

            if (Spells[R].IsReady() && ComboUseR)
            {
                if (ComboUseRMin == 1)
                {
                    var t = HeroManager.Enemies.Where(p => p.IsValidTarget(Spells[R].Range) && p.Health < CalculateDamageR(p)).OrderBy(q => q.GetPriority()).FirstOrDefault();
                    if (t != null)
                        Spells[R].SPredictionCast(t, HitChance.High);
                }
                else
                Spells[R].SPredictionCastAoe(ComboUseRMin);

            }
        }

        public void Harass()
        {
            if (ObjectManager.Player.ManaPercent < HarassMinMana)
                return;

            if (Spells[Q].IsReady() && HarassUseQ)
            {
                var t = TargetSelector.GetTarget(Spells[Q].Range, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                if (t != null)
                    Spells[Q].Cast(t);
            }

            if (Spells[W].IsReady() && HarassUseW)
            {
                var t = TargetSelector.GetTarget(Spells[W].Range, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                if (t != null)
                    Spells[W].Cast(t);
            }

            if (Spells[E].IsReady() && HarassUseE)
            {
                var t = TargetSelector.GetTarget(Spells[E].Range, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                if (t != null)
                    Spells[E].CastOnUnit(t);
            }
        }

        public void LaneClear()
        {
            if (ObjectManager.Player.ManaPercent < LaneClearMinMana)
                return;

            var minion = MinionManager.GetMinions(Spells[Q].Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).FirstOrDefault();
            if (minion != null)

                if (Spells[Q].IsReady() && LaneClearQ)
                {
                    if (MinionManager.GetMinions(Spells[Q].Range + 100, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth).Count() > 3)
                        Spells[Q].Cast(minion);
                }

            if (Spells[W].IsReady() && LaneClearW)
            {
                if (MinionManager.GetMinions(Spells[Q].Range + 100, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth).Count() > 3)
                    Spells[W].Cast();
            }
        }

        public void KillSteal()
        {
            if (!Spells[R].IsReady())
                return;

            foreach (Obj_AI_Hero target in HeroManager.Enemies.Where(x => x.IsValidTarget(Spells[R].Range) && !x.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (CalculateDamageR(target) > target.Health + 20)
                    Spells[R].SPredictionCast(target, HitChance.High);
            }
        }

        protected override void OrbwalkingEvents_AfterAttack(SCommon.Orbwalking.AfterAttackArgs args)
        {
        }

        protected override void Interrupter_OnPossibleToInterrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (InterruptE && Spells[E].IsReady() && sender.IsValidTarget(Spells[E].Range))
                Spells[E].CastOnUnit(sender);

            if (InterruptR && Spells[R].IsReady() && sender.IsValidTarget(Spells[R].Range))
                Spells[R].SPredictionCast(sender, HitChance.High);
        }

        public override double CalculateDamageR(Obj_AI_Hero target)
        {
            if (Spells[R].IsReady())
                return ObjectManager.Player.GetSpellDamage(target, SpellSlot.R);

            return 0.0d;
        }

        public bool ComboUseQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Combo.UseQ").GetValue<bool>(); }
        }

        public bool ComboUseE
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Combo.UseE").GetValue<bool>(); }
        }

        public bool ComboUseW
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Combo.UseW").GetValue<bool>(); }
        }

        public bool ComboUseR
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Combo.UseR").GetValue<bool>(); }
        }

        public int ComboUseRMin
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Combo.UseRMin").GetValue<Slider>().Value; }
        }

        public bool HarassUseQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Harass.UseQ").GetValue<bool>(); }
        }

        public bool HarassUseW
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Harass.UseW").GetValue<bool>(); }
        }

        public bool AutoQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Misc.AutoQ").GetValue<bool>(); }
        }

        public bool HarassUseE
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Harass.UseE").GetValue<bool>(); }
        }

        public bool HarassUseR
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Harass.UseR").GetValue<bool>(); }
        }

        public int HarassRStack
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Harass.RStacks").GetValue<Slider>().Value; }
        }

        public int HarassMinMana
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Harass.MinMana").GetValue<Slider>().Value; }
        }

        public bool LaneClearQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.LaneClear.UseQ").GetValue<bool>(); }
        }

        public bool LaneClearW
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.LaneClear.UseW").GetValue<bool>(); }
        }

        public int LaneClearMinMana
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.LaneClear.MinMana").GetValue<Slider>().Value; }
        }

        public bool KillStealR
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Misc.RKillSteal").GetValue<bool>(); }
        }

        public bool InterruptR
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Combo.InterruptR").GetValue<bool>(); }
        }

        public bool InterruptE
        {
            get { return ConfigMenu.Item("SAutoCarry.Hecarim.Combo.InterruptE").GetValue<bool>(); }
        }
    }
}
