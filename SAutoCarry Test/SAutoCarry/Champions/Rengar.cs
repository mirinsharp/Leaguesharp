﻿using System;
using System.Linq;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using SCommon;
using SCommon.Database;
using SCommon.PluginBase;
using SCommon.Prediction;
using SharpDX;
//typedefs
//using TargetSelector = SCommon.TS.TargetSelector;

namespace SAutoCarry.Champions
{
    public class Rengar : Champion
    {
        private Obj_AI_Hero leapTarget = null;
        private int lastLeap = 0;
        public Rengar()
            : base("Rengar", "SAutoCarry - Rengar")
        {
            OnCombo += Combo;
            OnLaneClear += LaneClear;
            SCommon.Orbwalking.Events.OnAttack += Events_OnAttack;
            OnUpdate += BeforeOrbwalk;
            OnDraw += BeforeDraw;
        }

        public override void CreateConfigMenu()
        {
            Menu combo = new Menu("Combo", "SAutoCarry.Rengar.Combo");
            combo.AddItem(new MenuItem("SAutoCarry.Rengar.Combo.UseQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Rengar.Combo.UseW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Rengar.Combo.UseE", "Use E").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Rengar.Combo.OneShot", "Active One Shot Combo").SetValue(new KeyBind('T', KeyBindType.Toggle)));

            Menu laneclear = new Menu("LaneClear/JungleClear", "SAutoCarry.Rengar.LaneClear");
            laneclear.AddItem(new MenuItem("SAutoCarry.Rengar.LaneClear.UseQ", "Use Q").SetValue(true));
            laneclear.AddItem(new MenuItem("SAutoCarry.Rengar.LaneClear.UseW", "Use W").SetValue(true));
            laneclear.AddItem(new MenuItem("SAutoCarry.Rengar.LaneClear.UseE", "Use E").SetValue(true));
            laneclear.AddItem(new MenuItem("SAutoCarry.Rengar.LaneClear.SaveFerocity", "Save Ferocity").SetValue(true));

            Menu misc = new Menu("Misc", "SAutoCarry.Rengar.Misc");
            misc.AddItem(new MenuItem("SAutoCarry.Rengar.Misc.AutoHeal", "Auto Heal %").SetValue(new Slider(20, 0, 100)));
            misc.AddItem(new MenuItem("SAutoCarry.Rengar.Misc.DrawComboMode", "Draw Combo Mode").SetValue(true));

            ConfigMenu.AddSubMenu(combo);
            ConfigMenu.AddSubMenu(laneclear);
            ConfigMenu.AddSubMenu(misc);
            ConfigMenu.AddToMainMenu();
        }

        public void BeforeOrbwalk()
        {
            if (HaveFullFerocity && ObjectManager.Player.HealthPercent <= AutoHealPercent && Spells[W].IsReady())
                Spells[W].Cast();
        }

        public void BeforeDraw()
        {
            if(DrawComboMode)
            {
                var text_pos = Drawing.WorldToScreen(ObjectManager.Player.Position);
                Drawing.DrawText((int)text_pos.X - 20, (int)text_pos.Y + 35, System.Drawing.Color.Aqua, OneShotComboActive ? "Mode: One Shot (Q First)" : "Mode: Root (E First)");
            }
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
            if (!WillLeap)
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

        public void LaneClear()
        {
            if (!LaneClearSaveFerocity || !HaveFullFerocity)
            {
                if (Spells[W].IsReady() && LaneClearUseW)
                {
                    var minion = MinionManager.GetMinions(400, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).FirstOrDefault();
                    if (minion != null)
                    {
                        if (minion.IsJungleMinion())
                        {
                            Spells[W].Cast();
                            return;
                        }
                        else
                        {
                            if (Spells[W].GetDamage(minion) > minion.Health)
                            {
                                Spells[W].Cast();
                                return;
                            }
                        }
                    }
                }

                if (Spells[E].IsReady() && LaneClearUseE)
                {
                    var minion = MinionManager.GetMinions(950, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).FirstOrDefault();
                    if (minion != null)
                    {
                        if (minion.IsJungleMinion())
                            Spells[E].Cast(minion.ServerPosition);
                        else
                        {
                            if (Spells[E].GetDamage(minion) > minion.Health)
                                    Spells[E].Cast(minion.ServerPosition);
                        }
                    }
                }
            }
        }

        protected override void Orbwalking_BeforeAttack(SCommon.Orbwalking.BeforeAttackArgs args)
        {
            if (Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo && args.Target is Obj_AI_Hero && ComboUseQ)
            {
                leapTarget = args.Target as Obj_AI_Hero;
                if(Utils.TickCount - lastLeap < 100 && !HaveFullFerocity)
                {
                    if (Spells[Q].IsReady())
                        Spells[Q].Cast();
                    else
                        args.Process = false;

                    lastLeap = 0;
                }

                if (Spells[Q].IsReady() || HaveFullFerocity)
                {
                    float dmg = 0f;
                    if (HaveFullFerocity)
                        dmg = (float)ObjectManager.Player.CalcDamage(leapTarget, LeagueSharp.Common.Damage.DamageType.Physical, new int[] { 30, 45, 60, 75, 90, 105, 120, 135, 150, 160, 170, 180, 190, 200, 210, 220, 230, 240 }[ObjectManager.Player.Level - 1] + (ObjectManager.Player.BaseAttackDamage + ObjectManager.Player.FlatPhysicalDamageMod) * 0.5f);
                    else
                        dmg = (float)ObjectManager.Player.CalcDamage(leapTarget, LeagueSharp.Common.Damage.DamageType.Physical, new int[] { 30, 60, 90, 120, 150 }[Spells[Q].Level - 1] + (ObjectManager.Player.BaseAttackDamage + ObjectManager.Player.FlatPhysicalDamageMod) * new int[] { 0, 5, 10, 15, 20 }[ObjectManager.Player.GetSpell(SpellSlot.Q).Level - 1] / 100f);

                    if (dmg >= leapTarget.Health || (WillLeap && OneShotComboActive))
                    {
                        Spells[Q].Cast();
                        if (!WillLeap)
                            args.Process = false;
                    }
                }

                if (ObjectManager.Player.HasBuff("RengarR"))
                    Orbwalker.Configuration.DontMoveInRange = true;
            }
        }

        private void Events_OnAttack(SCommon.Orbwalking.OnAttackArgs args)
        {
            if (Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo && WillLeap && args.Target is Obj_AI_Hero)
            {
                LeagueSharp.Common.Utility.DelayAction.Add(100, () =>
                {
                    if (ComboUseE && Spells[E].IsReady() && (!HaveFullFerocity || !OneShotComboActive || ObjectManager.Player.HasBuff("rengarqbase") || ObjectManager.Player.HasBuff("rengarqemp")))
                    {
                        var pred = Spells[E].GetSPrediction(args.Target as Obj_AI_Hero);
                        if (pred.HitChance > HitChance.Impossible)
                            Spells[E].Cast(pred.CastPosition);
                        else
                            Spells[E].Cast((args.Target as Obj_AI_Hero).ServerPosition);
                    }
                });
            }
        }

        protected override void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (Orbwalker.Configuration.DontMoveInRange)
                LeagueSharp.Common.Utility.DelayAction.Add(args.Duration + 100, () => Orbwalker.Configuration.DontMoveInRange = false);

            if (sender.IsMe && Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo && leapTarget != null)
            {
                LeagueSharp.Common.Utility.DelayAction.Add(Math.Max(1, args.Duration - 200), () =>
                {
                    if (Items.HasItem(3077) && Items.CanUseItem(3077))
                        Items.UseItem(3077);

                    if (Items.HasItem(3074) && Items.CanUseItem(3074))
                        Items.UseItem(3074);

                    Spells[W].Cast();
                });
            }
        }

        protected override void Orbwalking_AfterAttack(SCommon.Orbwalking.AfterAttackArgs args)
        {
            if (Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo)
            {
                if (Spells[Q].IsReady() && ComboUseQ)
                    Spells[Q].Cast();
            }
            else if (Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.LaneClear)
            {
                if (!args.Target.IsDead && LaneClearUseQ && (!LaneClearSaveFerocity || !HaveFullFerocity))
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

        public bool LaneClearUseQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Rengar.LaneClear.UseQ").GetValue<bool>(); }
        }

        public bool LaneClearUseW
        {
            get { return ConfigMenu.Item("SAutoCarry.Rengar.LaneClear.UseW").GetValue<bool>(); }
        }

        public bool LaneClearUseE
        {
            get { return ConfigMenu.Item("SAutoCarry.Rengar.LaneClear.UseE").GetValue<bool>(); }
        }

        public bool LaneClearSaveFerocity
        {
            get { return ConfigMenu.Item("SAutoCarry.Rengar.LaneClear.SaveFerocity").GetValue<bool>(); }
        }

        public int AutoHealPercent
        {
            get { return ConfigMenu.Item("SAutoCarry.Rengar.Misc.AutoHeal").GetValue<Slider>().Value; }
        }

        public bool DrawComboMode
        {
            get { return ConfigMenu.Item("SAutoCarry.Rengar.Misc.DrawComboMode").GetValue<bool>(); }
        }

        public bool HaveFullFerocity
        {
            get { return ObjectManager.Player.Mana == 5; }
        }

        public bool WillLeap
        {
            get { return ObjectManager.Player.AttackRange > 125 || ObjectManager.Player.HasBuff("RengarR") || ObjectManager.Player.IsDashing(); }
        }
    }
}
