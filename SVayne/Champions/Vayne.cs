using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SCommon;
using SCommon.PluginBase;
using SCommon.Prediction;
using SAutoCarry.Champions.Helpers;
using SharpDX;

namespace SAutoCarry.Champions
{
    public class Vayne : Champion
    {
        public Vayne()
            : base("Vayne", "SAutoCarry - Vayne")
        {
            Tumble.Initialize(this);
            Condemn.Initialize(this);
            SCommon.Prediction.Prediction.predMenu.Item("SPREDDRAWINGS").SetValue(false);
            OnDraw += BeforeDraw;
            OnCombo += Combo;
            OnHarass += Harass;
        }

        public override void CreateConfigMenu()
        {
            Menu combo = new Menu("Combo", "SAutoCarry.Vayne.Combo");
            combo.AddItem(new MenuItem("SAutoCarry.Vayne.Combo.UseQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Vayne.Combo.UseE", "Use E").SetValue(true));

            Menu harass = new Menu("Harass", "SAutoCarry.Vayne.Harass");
            harass.AddItem(new MenuItem("SAutoCarry.Vayne.Harass.UseQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("SAutoCarry.Vayne.Harass.UseE", "Use E").SetValue(true)).ValueChanged += (s, ar) => harass.Item("SAutoCarry.Vayne.Harass.UseE3RDW").Show(ar.GetNewValue<bool>());
            harass.AddItem(new MenuItem("SAutoCarry.Vayne.Harass.UseE3RDW", "Use E for 3rd W stack").SetValue(false)).Show(harass.Item("SAutoCarry.Vayne.Harass.UseE").GetValue<bool>());

            Menu misc = new Menu("Misc", "SAutoCarry.Vayne.Misc");
            misc.AddItem(new MenuItem("SAutoCarry.Vayne.Misc.AAIndicator", "Draw AA Indicator").SetValue(true));
            misc.AddItem(new MenuItem("SAutoCarry.Vayne.Misc.DontAAInvisible", "Dont AA While Stealth").SetValue(true)).ValueChanged += (s, ar) => misc.Item("SAutoCarry.Vayne.Misc.DontAAInvisibleCount").Show(ar.GetNewValue<bool>());
            misc.AddItem(new MenuItem("SAutoCarry.Vayne.Misc.DontAAInvisibleCount", "Dont AA While Stealth if around enemy count >").SetValue(new Slider(1, 0, 5))).Show(misc.Item("SAutoCarry.Vayne.Misc.DontAAInvisible").GetValue<bool>());
            misc.AddItem(new MenuItem("SAutoCarry.Vayne.Misc.LaneClearQ", "Use Q LaneClear").SetValue(true));

            ConfigMenu.AddSubMenu(combo);
            ConfigMenu.AddSubMenu(harass);
            ConfigMenu.AddSubMenu(misc);

            ConfigMenu.AddToMainMenu();
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q, 300f);
            Spells[W] = new Spell(SpellSlot.W, 0f);
            Spells[E] = new Spell(SpellSlot.E, 650f);
            Spells[E].SetSkillshot(0.25f, 70f, 1200f, false, SkillshotType.SkillshotLine);
            Spells[R] = new Spell(SpellSlot.R, 0f);
        }

        public void Combo()
        {
            if (ComboUseE)
            {
                var t = TargetSelector.GetTarget(Spells[E].Range + 300f, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (t != null && Spells[E].IsReady() && Condemn.IsValidTarget(t))
                    Spells[E].CastOnUnit(t);
            }
        }

        public void Harass()
        {
            if (HarassUseE)
            {
                var t = TargetSelector.GetTarget(Spells[E].Range + 300, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (t != null && Spells[E].IsReady())
                {
                    if (HarassUseE3RdW && t.IsValidTarget(Spells[E].Range) && t.GetBuffCount("vaynesilvereddebuff") == 2)
                        Spells[E].CastOnUnit(t);

                    if (Condemn.IsValidTarget(t))
                        Spells[E].CastOnUnit(t);
                }
            }
        }

        public void BeforeDraw()
        {
            if (DrawAAIndicator)
            {
                foreach (var enemy in HeroManager.Enemies)
                {
                    if (enemy.IsValidTarget(1200))
                    {
                        float autoAttackDamage = SCommon.Damage.AutoAttack.GetDamage(enemy);
                        int aaCount = (int)Math.Ceiling(Math.Max(1, enemy.Health - Spells[Q].GetDamage(enemy)) / autoAttackDamage);
                        int wCount = (aaCount - aaCount % 3) / 3;
                        float wDmg = Math.Max(40 + 20 * (ObjectManager.Player.GetSpell(SpellSlot.W).Level - 1), enemy.MaxHealth * (0.06f + 0.015f * (ObjectManager.Player.GetSpell(SpellSlot.W).Level - 1))) * wCount;
                        int x = (int)Math.Floor(wDmg / autoAttackDamage); //1 * w = x * aa
                        int neededAA = aaCount;
                        if (wCount > 0)
                        {
                            for (int i = aaCount; i > 1; i++)
                            {
                                neededAA--;
                                if (neededAA + (neededAA - neededAA % 3) / 3 < aaCount)
                                {
                                    neededAA++;
                                    break;
                                }
                            }
                        }
                        if (neededAA > 1)
                            neededAA--;
                        Text.DrawText(null, neededAA.ToString() + " x AA", (int)enemy.HPBarPosition.X, (int)enemy.HPBarPosition.Y, Color.Gold);
                    }
                }
            }
        }

        protected override void Orbwalking_BeforeAttack(SCommon.Orbwalking.BeforeAttackArgs args)
        {
            if (DontAAStealth && ObjectManager.Player.HasBuff("vaynetumblefade"))
            {
                if (ObjectManager.Player.ServerPosition.CountEnemiesInRange(1000) > DontAAStealthCount)
                {
                    if (args.Target is Obj_AI_Hero && args.Target.Health <= SCommon.Damage.AutoAttack.GetDamage(args.Target as Obj_AI_Base, true) * 2 && ObjectManager.Player.Health > (args.Target as Obj_AI_Hero).GetAutoAttackDamage(ObjectManager.Player, true)) //can killable
                        return;

                    args.Process = false;
                }
            }
        }

        protected override void Orbwalking_AfterAttack(SCommon.Orbwalking.AfterAttackArgs args)
        {
            if (args.Target is Obj_AI_Hero)
            {
                if (Spells[Q].IsReady() && ((Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo && ComboUseQ) || (Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Mixed && HarassUseQ)))
                {
                    Vector3 pos = Tumble.FindTumblePosition(args.Target as Obj_AI_Hero);

                    if (pos.IsValid())
                        Spells[Q].Cast(pos);
                }
            }
            else if (Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.LaneClear)
            {
                if (Spells[Q].IsReady())
                {
                    var jungleMob = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();
                    if (jungleMob != null)
                        Spells[Q].Cast(Game.CursorPos);
                    else
                    {
                        if(LaneClearQ)
                        {
                            var minion = MinionManager.GetMinions(ObjectManager.Player.AttackRange + 100).Where(p => p.Health <= SCommon.Damage.AutoAttack.GetDamage(p) + ObjectManager.Player.GetSpellDamage(p, SpellSlot.Q)).FirstOrDefault();
                            if(minion != null)
                            {
                                Orbwalker.ForcedTarget = minion;
                                Spells[Q].Cast(Game.CursorPos);
                            }
                        }
                    }
                }
            }
        }

        public bool ComboUseQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Vayne.Combo.UseQ").GetValue<bool>(); }
        }

        public bool ComboUseE
        {
            get { return ConfigMenu.Item("SAutoCarry.Vayne.Combo.UseE").GetValue<bool>(); }
        }

        public bool HarassUseQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Vayne.Harass.UseQ").GetValue<bool>(); }
        }

        public bool HarassUseE
        {
            get { return ConfigMenu.Item("SAutoCarry.Vayne.Harass.UseE").GetValue<bool>(); }
        }

        public bool HarassUseE3RdW
        {
            get { return ConfigMenu.Item("SAutoCarry.Vayne.Harass.UseE3RDW").GetValue<bool>(); }
        }

        public bool DrawAAIndicator
        {
            get { return ConfigMenu.Item("SAutoCarry.Vayne.Misc.AAIndicator").GetValue<bool>(); }
        }

        public bool DontAAStealth
        {
            get { return ConfigMenu.Item("SAutoCarry.Vayne.Misc.DontAAInvisible").GetValue<bool>(); }
        }

        public int DontAAStealthCount
        {
            get { return ConfigMenu.Item("SAutoCarry.Vayne.Misc.DontAAInvisibleCount").GetValue<Slider>().Value; }
        }

        public bool LaneClearQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Vayne.Misc.LaneClearQ").GetValue<bool>(); }
        }
    }
}
