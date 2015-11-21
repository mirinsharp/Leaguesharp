using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SCommon;
using SCommon.PluginBase;
using SCommon.Prediction;
//typedefs
//using TargetSelector = SCommon.TS.TargetSelector;

namespace SAutoCarry.Champions
{
    public class Rengar : Champion
    {
        private Obj_AI_Hero leapTarget = null;
        public Rengar()
            : base ("Rengar", "SAutoCarry - Rengar")
        {
            OnCombo += Combo;
            SCommon.Orbwalking.Events.OnAttack += Events_OnAttack;
            OnUpdate += BeforeOrbwalk;
        }

        public override void CreateConfigMenu()
        {
            Menu combo = new Menu("Combo", "SAutoCarry.Rengar.Combo");
            combo.AddItem(new MenuItem("SAutoCarry.Rengar.Combo.UseQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Rengar.Combo.UseW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Rengar.Combo.UseE", "Use E").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Rengar.Combo.OneShot", "Active One Shot Combo").SetValue(new KeyBind('T', KeyBindType.Toggle)));

            Menu misc = new Menu("Misc", "SAutoCarry.Rengar.Misc");
            misc.AddItem(new MenuItem("SAutoCarry.Rengar.Misc.AutoHeal", "Auto Heal %").SetValue(new Slider(20, 0, 100)));
            misc.AddItem(new MenuItem("SAutoCarry.Rengar.Misc.LaneClearQ", "Use Q On LaneClear").SetValue(true));

            ConfigMenu.AddSubMenu(combo);
            ConfigMenu.AddSubMenu(misc);
            ConfigMenu.AddToMainMenu();
        }
        
        public void BeforeOrbwalk()
        {
            if (HaveFullFerocity && ObjectManager.Player.HealthPercent < AutoHealPercent && Spells[W].IsReady())
                Spells[W].Cast();
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q);

            Spells[W] = new Spell(SpellSlot.W, 450f);

            Spells[E] = new Spell(SpellSlot.E, 900f);
            Spells[E].SetSkillshot(0.25f, 70, 1500f, true, SkillshotType.SkillshotLine);

            Spells[R] = new Spell(SpellSlot.R);
        }

        public void Combo()
        {
            if (!ObjectManager.Player.HasBuff("RengarR"))
            {
                var t = TargetSelector.GetTarget(Spells[E].Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (t != null && t.IsValidTarget(Spells[E].Range) && Spells[E].IsReady())
                {
                    if (t.IsValidTarget(SCommon.Orbwalking.Utility.GetRealAARange(t) * 2) && HaveFullFerocity)
                        return;
                    Spells[E].SPredictionCast(t, HitChance.High);
                }

                t = TargetSelector.GetTarget(Spells[W].Range, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                if (Spells[W].IsReady() && ComboUseW && t != null && t.IsValidTarget(Spells[W].Range) && !HaveFullFerocity)
                    Spells[W].Cast();
            }
        }

        protected override void Orbwalking_BeforeAttack(SCommon.Orbwalking.BeforeAttackArgs args)
        {
            if (Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo && args.Target is Obj_AI_Hero && ComboUseQ)
            {
                leapTarget = args.Target as Obj_AI_Hero;
                if (WillLeap)
                {
                    if (OneShotComboActive && Spells[Q].IsReady())
                        Spells[Q].Cast();
                }

                if (Spells[Q].IsReady() && CalculateDamageQ(leapTarget) >= leapTarget.Health)
                    Spells[Q].Cast();
            }
        }

        private void Events_OnAttack(SCommon.Orbwalking.OnAttackArgs args)
        {
            if (Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo && WillLeap && args.Target is Obj_AI_Hero)
            {
                LeagueSharp.Common.Utility.DelayAction.Add(50, () =>
                {
                    if (ComboUseE && Spells[E].IsReady())
                        Spells[E].Cast(args.Target.Position);
                });
            }
        }

        protected override void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (sender.IsMe && Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo && leapTarget != null)
            {
                LeagueSharp.Common.Utility.DelayAction.Add(args.Duration - 50, () =>
                {
                    Spells[W].Cast();
                });
            }
        }

        protected override void Orbwalking_AfterAttack(SCommon.Orbwalking.AfterAttackArgs args)
        {
            if (Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo)
            {
                if (Items.HasItem(3077) && Items.CanUseItem(3077))
                    Items.UseItem(3077);

                if (Items.HasItem(3074) && Items.CanUseItem(3074))
                    Items.UseItem(3074);

                if (Spells[Q].IsReady())
                    Spells[Q].Cast();
            }
            else if(Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.LaneClear)
            {
                if (!args.Target.IsDead && !HaveFullFerocity && LaneClearQ)
                    Spells[Q].Cast();
            }
        }

        public bool ComboUseQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Rengar.Combo.UseQ").GetValue<bool>(); }
        }

        public bool ComboUseW
        {
            get { return ConfigMenu.Item("SAutoCarry.Rengar.Combo.UseW").GetValue<bool>(); }
        }

        public bool ComboUseE
        {
            get { return ConfigMenu.Item("SAutoCarry.Rengar.Combo.UseE").GetValue<bool>(); }
        }

        public bool OneShotComboActive
        {
            get { return ConfigMenu.Item("SAutoCarry.Rengar.Combo.OneShot").GetValue<KeyBind>().Active; }
        }

        public int AutoHealPercent
        {
            get { return ConfigMenu.Item("SAutoCarry.Rengar.Misc.AutoHeal").GetValue<Slider>().Value; }
        }

        public bool LaneClearQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Rengar.Misc.LaneClearQ").GetValue<bool>(); }
        }

        public bool HaveFullFerocity
        {
            get { return ObjectManager.Player.Mana == 5; }
        }

        public bool WillLeap
        {
            get { return ObjectManager.Player.AttackRange > 125; }
        }
    }
}
