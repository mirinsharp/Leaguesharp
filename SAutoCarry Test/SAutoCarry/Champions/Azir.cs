using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SCommon;
using SCommon.PluginBase;
using SCommon.Prediction;
using SCommon.Orbwalking;
using SAutoCarry.Champions.Helpers;
using SharpDX;
//typedefs
//using TargetSelector = SCommon.TS.TargetSelector;

namespace SAutoCarry.Champions
{
    public class Azir : Champion
    {
        private int lastLaneClearTick = 0;
        private int CastET, CastQT;
        private Vector2 CastQLocation, CastELocation, InsecLocation, InsecTo;
        public Azir()
            : base("Azir", "SAutoCarry - Azir")
        {
            SoldierMgr.Initialize(this);
            Orbwalker.RegisterShouldWait(ShouldWait);
            Orbwalker.RegisterCanAttack(CanAttack);
            Orbwalker.RegisterCanOrbwalkTarget(CanOrbwalkTarget);
            OnCombo += Combo;
            OnHarass += Harass;
            OnLaneClear += LaneClear;
            OnUpdate += BeforeOrbwalk;
            OnDraw += BeforeDraw;

            Game.OnWndProc += Game_OnWndProc;
        }

        public override void CreateConfigMenu()
        {
            Menu combo = new Menu("Combo", "SAutoCarry.Azir.Combo");
            combo.AddItem(new MenuItem("SAutoCarry.Azir.Combo.UseQ", "Use Q").SetValue(true)).ValueChanged += (s, ar) =>
            {
                combo.Item("SAutoCarry.Azir.Combo.UseQOnlyOutOfAA").Show(ar.GetNewValue<bool>());
                combo.Item("SAutoCarry.Azir.Combo.UseQAlwaysMaxRange").Show(ar.GetNewValue<bool>());
                combo.Item("SAutoCarry.Azir.Combo.UseQWhenNoWAmmo").Show(ar.GetNewValue<bool>());
            };
            combo.AddItem(new MenuItem("SAutoCarry.Azir.Combo.UseQOnlyOutOfAA", "Use Q Only When Enemy out of range").SetValue(true)).Show(combo.Item("SAutoCarry.Azir.Combo.UseQ").GetValue<bool>());
            combo.AddItem(new MenuItem("SAutoCarry.Azir.Combo.UseQAlwaysMaxRange", "Always Cast Q To Max Range").SetValue(false)).Show(combo.Item("SAutoCarry.Azir.Combo.UseQ").GetValue<bool>());
            combo.AddItem(new MenuItem("SAutoCarry.Azir.Combo.UseQWhenNoWAmmo", "Use Q When Out of W Ammo").SetValue(false)).Show(combo.Item("SAutoCarry.Azir.Combo.UseQ").GetValue<bool>());
            combo.AddItem(new MenuItem("SAutoCarry.Azir.Combo.UseW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Azir.Combo.UseE", "Use E If target is killable").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Azir.Combo.UseR", "Use R").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Azir.Combo.RMinHit", "Min R Hit").SetValue(new Slider(1, 1, 5)));
            combo.AddItem(new MenuItem("SAutoCarry.Azir.Combo.RMinHP", "Use R whenever my health < ").SetValue(new Slider(20, 0, 100)));

            Menu harass = new Menu("Harass", "SAutoCarry.Azir.Harass");
            harass.AddItem(new MenuItem("SAutoCarry.Azir.Harass.UseQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("SAutoCarry.Azir.Harass.UseW", "Use W").SetValue(true));
            harass.AddItem(new MenuItem("SAutoCarry.Azir.Harass.ManaPercent", "Min. Mana Percent").SetValue(new Slider(40, 0, 100)));

            Menu laneclear = new Menu("LaneClear", "SAutoCarry.Azir.LaneClear");
            laneclear.AddItem(new MenuItem("SAutoCarry.Azir.LaneClear.UseQ", "Use Q").SetValue(true));
            laneclear.AddItem(new MenuItem("SAutoCarry.Azir.LaneClear.MinQMinion", "Q Min. Minions").SetValue(new Slider(3, 1, 5)));
            laneclear.AddItem(new MenuItem("SAutoCarry.Azir.LaneClear.UseW", "Use W").SetValue(true));
            laneclear.AddItem(new MenuItem("SAutoCarry.Azir.LaneClear.ManaPercent", "Min. Mana Percent").SetValue(new Slider(40, 0, 100)));

            Menu misc = new Menu("Misc", "SAutoCarry.Azir.Misc");
            misc.AddItem(new MenuItem("SAutoCarry.Azir.Misc.Jump", "Jump To Cursor").SetValue(new KeyBind('G', KeyBindType.Press)));
            misc.AddItem(new MenuItem("SAutoCarry.Azir.Misc.Insec", "Insec Selected Target").SetValue(new KeyBind('T', KeyBindType.Press)));
            misc.AddItem(new MenuItem("SAutoCarry.Azir.Misc.WQKillSteal", "Use W->Q to KillSteal").SetValue(true));

            ConfigMenu.AddSubMenu(combo);
            ConfigMenu.AddSubMenu(harass);
            ConfigMenu.AddSubMenu(laneclear);
            ConfigMenu.AddSubMenu(misc);
            ConfigMenu.AddToMainMenu();
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q, 825f);
            Spells[Q].SetSkillshot(0.25f, 70f, 1600f, false, SkillshotType.SkillshotLine);

            Spells[W] = new Spell(SpellSlot.W, 450f);
            Spells[W].SetSkillshot(0.25f, 70f, 0f, false, SkillshotType.SkillshotCircle);

            Spells[E] = new Spell(SpellSlot.E, 1250f);
            Spells[E].SetSkillshot(0.25f, 100, 1700f, false, SkillshotType.SkillshotLine);

            Spells[R] = new Spell(SpellSlot.R, 450f);
            Spells[R].SetSkillshot(0.5f, 70f, 1400f, false, SkillshotType.SkillshotLine);
        }

        public void Combo()
        {
            var t = TargetSelector.GetTarget(Spells[Q].Range, LeagueSharp.Common.TargetSelector.DamageType.Magical);
            var extendedTarget = TargetSelector.GetTarget(Spells[Q].Range + 400, LeagueSharp.Common.TargetSelector.DamageType.Magical);

            if (t != null)
            {
                if (ComboUseR && Spells[R].IsReady() && t.IsValidTarget(Spells[R].Range) && ShouldCast(SpellSlot.R, t))
                    Spells[R].SPredictionCast(t, HitChance.High);

                if (ComboUseW && Spells[W].IsReady() && ShouldCast(SpellSlot.W, t))
                    Spells[W].Cast(ObjectManager.Player.Position.To2D().Extend(t.Position.To2D(), 450));

                if (ComboUseQ && Spells[Q].IsReady() && ShouldCast(SpellSlot.Q, t))
                {
                    foreach (var soldier in SoldierMgr.ActiveSoldiers)
                    {
                        if (ObjectManager.Player.ServerPosition.Distance(t.ServerPosition) < Spells[Q].Range)
                        {
                            Spells[Q].UpdateSourcePosition(soldier.Position, ObjectManager.Player.ServerPosition);
                            var predRes = Spells[Q].GetSPrediction(t);
                            if (predRes.HitChance >= HitChance.High)
                            {
                                var pos = predRes.CastPosition;
                                if (ComboUseQAlwaysMaxRange)
                                    pos = ObjectManager.Player.ServerPosition.To2D().Extend(pos, Spells[Q].Range);
                                Spells[Q].Cast(pos);
                                return;
                            }
                        }
                    }
                }
            }

            if (extendedTarget != null)
            {
                if (ComboUseE && Spells[E].IsReady() && ShouldCast(SpellSlot.E, extendedTarget))
                {
                    foreach (var soldier in SoldierMgr.ActiveSoldiers)
                    {
                        if (Spells[E].WillHit(extendedTarget, soldier.Position))
                        {
                            Spells[E].Cast(soldier.Position);
                            return;
                        }
                    }
                }
            }
        }

        public void Harass()
        {
            if (ObjectManager.Player.ManaPercent < HarassMinMana)
                return;

            var t = TargetSelector.GetTarget(Spells[Q].Range, LeagueSharp.Common.TargetSelector.DamageType.Magical);
            if (t == null)
                return;

            if (HarassUseW && Spells[W].IsReady() && ShouldCast(SpellSlot.W, t))
                Spells[W].Cast(ObjectManager.Player.Position.To2D().Extend(t.Position.To2D(), 450));

            if (HarassUseQ && Spells[Q].IsReady() && ShouldCast(SpellSlot.Q, t))
            {
                foreach (var soldier in SoldierMgr.ActiveSoldiers)
                {
                    if (ObjectManager.Player.ServerPosition.Distance(t.ServerPosition) < Spells[Q].Range)
                    {
                        Spells[Q].UpdateSourcePosition(soldier.Position, ObjectManager.Player.ServerPosition);
                        if (Spells[Q].SPredictionCast(t, HitChance.High))
                            return;
                    }
                }
            }
        }

        public void LaneClear()
        {
            if (ObjectManager.Player.ManaPercent < LaneClearMinMana)
                return;

            if (Utils.TickCount - lastLaneClearTick > 100 && !Orbwalker.ShouldWait())
            {
                if (LaneClearUseW && Spells[W].IsReady() && Spells[W].Instance.Ammo != 0)
                {
                    var minions = MinionManager.GetMinions(Spells[W].Range + SoldierMgr.SoldierAttackRange / 2f);
                    if (minions.Count > 1)
                    {
                        var loc = MinionManager.GetBestCircularFarmLocation(minions.Select(p => p.ServerPosition.To2D()).ToList(), SoldierMgr.SoldierAttackRange, Spells[W].Range);
                        if (loc.MinionsHit > 2)
                            Spells[W].Cast(loc.Position);
                    }
                }

                if (LaneClearUseQ && Spells[Q].IsReady())
                {
                    MinionManager.FarmLocation bestfarm = MinionManager.GetBestCircularFarmLocation(MinionManager.GetMinions(Spells[Q].Range + 100).Select(p => p.ServerPosition.To2D()).ToList(), SoldierMgr.SoldierAttackRange, Spells[Q].Range + 100);
                    if (bestfarm.MinionsHit >= LaneClearQMinMinion)
                        Spells[Q].Cast(bestfarm.Position);
                }
                lastLaneClearTick = Utils.TickCount;
            }
        }

        public void BeforeOrbwalk()
        {
            if (Spells[R].IsReady())
                Spells[R].Width = 133 * (3 + Spells[R].Level);

            if (JumpActive)
                Jump(Game.CursorPos);

            if (InsecActive)
                Insec();

            if (WQKillSteal)
                KillSteal();
        }

        public void BeforeDraw()
        {
            if (InsecTo.IsValid() && InsecTo.Distance(ObjectManager.Player.ServerPosition.To2D()) <= 2000)
                Render.Circle.DrawCircle(InsecTo.To3D2(), 200, System.Drawing.Color.DarkBlue);
        }

        public void KillSteal()
        {
            if (!Spells[Q].IsReady() || (SoldierMgr.ActiveSoldiers.Count == 0 && !Spells[W].IsReady()))
                return;

            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(Spells[Q].Range + 100) && !x.HasBuffOfType(BuffType.Invulnerability)).OrderByDescending(p => CalculateComboDamage(p)))
            {
                if ((ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q)) > target.Health + 20)
                {
                    if (SoldierMgr.ActiveSoldiers.Count == 0)
                        Spells[W].Cast(ObjectManager.Player.Position.To2D().Extend(target.Position.To2D(), 450));
                    else
                        Spells[Q].SPredictionCast(target, HitChance.High);
                }
            }
        }
        //Kortatu's azir jump code
        public void Jump(Vector3 pos)
        {
            Orbwalker.Orbwalk(null, pos);
            if (Math.Abs(Spells[E].Cooldown) < 0.00001)
            {
                var extended = ObjectManager.Player.ServerPosition.To2D().Extend(pos.To2D(), 800f);
                if (Spells[W].IsReady() && (SoldierMgr.ActiveSoldiers.Count == 0 || Spells[Q].Instance.State == SpellState.Cooldown && SoldierMgr.ActiveSoldiers.Min(s => s.Position.To2D().Distance(extended, true)) >= ObjectManager.Player.ServerPosition.To2D().Distance(extended, true)))
                {
                    Spells[W].Cast(extended);

                    if (Spells[Q].Instance.State != SpellState.Cooldown)
                    {
                        var extended2 = ObjectManager.Player.ServerPosition.To2D().Extend(pos.To2D(), 450f);
                        if (extended2.IsWall())
                        {
                            LeagueSharp.Common.Utility.DelayAction.Add(250, () => Spells[Q].Cast(extended, true));
                            CastET = Utils.TickCount + 250;
                            CastELocation = extended;
                        }
                        else
                        {
                            LeagueSharp.Common.Utility.DelayAction.Add(250, () => Spells[E].Cast(extended, true));
                            CastQT = Utils.TickCount + 250;
                            CastQLocation = extended;
                        }
                    }
                    else
                    {
                        LeagueSharp.Common.Utility.DelayAction.Add(100, () => Spells[E].Cast(extended, true));
                    }
                    return;
                }

                if (SoldierMgr.ActiveSoldiers.Count > 0 && Spells[Q].IsReady())
                {
                    var closestSoldier = SoldierMgr.ActiveSoldiers.MinOrDefault(s => s.Position.To2D().Distance(extended, true));
                    if (closestSoldier.Position.To2D().Distance(extended, true) < ObjectManager.Player.Distance(extended, true) && ObjectManager.Player.Distance(closestSoldier.Position, true) > 400f)
                    {
                        LeagueSharp.Common.Utility.DelayAction.Add(250, () => Spells[E].Cast(extended, true));
                        CastQT = Utils.TickCount + 250;
                        CastQLocation = extended;
                    }
                    else
                    {
                        LeagueSharp.Common.Utility.DelayAction.Add(250, () => Spells[Q].Cast(extended, true));
                        LeagueSharp.Common.Utility.DelayAction.Add(500 + Game.Ping, () => Spells[E].Cast(extended, true));
                    }
                }
            }
        }

        public void Insec()
        {
            if (TargetSelector.SelectedTarget != null)
            {
                if (TargetSelector.SelectedTarget.IsValidTarget(825))
                {
                    if (Spells[Q].IsReady())
                    {
                        if (Spells[R].IsReady())
                        {
                            var direction = (TargetSelector.SelectedTarget.ServerPosition - ObjectManager.Player.ServerPosition).To2D().Normalized();
                            var insecPos = TargetSelector.SelectedTarget.ServerPosition.To2D() + (direction * 200f);
                            if (!InsecLocation.IsValid())
                                InsecLocation = ObjectManager.Player.ServerPosition.To2D();
                            Jump(insecPos.To3D());
                        }
                    }
                    else if (ObjectManager.Player.ServerPosition.Distance(TargetSelector.SelectedTarget.ServerPosition) < 200 && InsecLocation.IsValid())
                    {
                        if (InsecTo.IsValid() && InsecTo.Distance(ObjectManager.Player.ServerPosition.To2D()) < 1500)
                            Spells[R].Cast(InsecTo);
                        else
                            Spells[R].Cast(InsecLocation);
                        if (!Spells[R].IsReady())
                            InsecLocation = Vector2.Zero;
                    }
                }
                else
                {
                    Orbwalker.Orbwalk(null, Game.CursorPos);
                }
            }
            else
            {
                Orbwalker.Orbwalk(null, Game.CursorPos);
            }
        }

        public bool ShouldCast(SpellSlot slot, Obj_AI_Hero target)
        {
            switch (slot)
            {
                case SpellSlot.Q:
                    {
                        if (ComboUseQOnlyOutOfRange && SoldierMgr.InAARange(target))
                            return false;

                        if (SoldierMgr.ActiveSoldiers.Count == 0)
                            return false;

                        if (ComboUseQWhenNoWAmmo && Spells[W].Instance.Ammo != 0)
                            return false;

                        return true;
                    }

                case SpellSlot.W:
                    {
                        if (Spells[W].Instance.Ammo == 0)
                            return false;

                        return true;
                    }

                case SpellSlot.E:
                    {
                        if (CalculateDamageE(target) + SCommon.Damage.AutoAttack.GetDamage(target) >= target.Health)
                            return true;

                        return false;
                    }

                case SpellSlot.R:
                    {
                        if (CalculateDamageR(target) >= target.Health - 150)
                            return true;

                        if (ObjectManager.Player.HealthPercent < ComboRMinHP)
                            return true;

                        if (IsWallStunable(target.ServerPosition.To2D(), ObjectManager.Player.ServerPosition.To2D().Extend(Spells[R].GetSPrediction(target).UnitPosition, 200 - target.BoundingRadius)) && CalculateDamageR(target) >= target.Health / 2f)
                            return true;

                        if (ComboRMinHit > 1 && Spells[R].GetAoeSPrediction().HitCount > ComboRMinHit)
                            return true;

                        return false;
                    }
            }

            return false;
        }

        private bool IsWallStunable(Vector2 from, Vector2 to)
        {
            float count = from.Distance(to);
            for (uint i = 0; i <= count; i += 25)
            {
                Vector2 pos = from.Extend(ObjectManager.Player.ServerPosition.To2D(), -i);
                var colFlags = NavMesh.GetCollisionFlags(pos.X, pos.Y);
                if (colFlags.HasFlag(CollisionFlags.Wall) || colFlags.HasFlag(CollisionFlags.Building))
                    return true;
            }
            return false;
        }

        private bool ShouldWait()
        {
            return
                ObjectManager.Get<Obj_AI_Minion>()
                    .Any(
                        minion =>
                            (minion.IsValidTarget() && minion.Team != GameObjectTeam.Neutral &&
                             (SoldierMgr.InAARange(minion) || SCommon.Orbwalking.Utility.InAARange(minion)) && MinionManager.IsMinion(minion, false) &&
                             (minion.Health - SCommon.Damage.Prediction.GetPrediction(minion, ObjectManager.Player.AttackDelay * 1000f * 2f, true) <= SCommon.Damage.AutoAttack.GetDamage(minion, true) * (int)(Math.Ceiling(SCommon.Damage.Prediction.AggroCount(minion) / 2f)))));
        }
        private bool CanAttack()
        {
            if (SoldierMgr.SoldierAttacking)
                return false;

            return Utils.TickCount + Game.Ping / 2 - Orbwalker.LastAATick >= 1000 / (ObjectManager.Player.GetAttackSpeed() * Orbwalker.BaseAttackSpeed);
        }

        private bool CanOrbwalkTarget(AttackableUnit target)
        {
            if (target.IsValidTarget())
            {
                if (target is Obj_AI_Base)
                {
                    foreach (var soldier in SoldierMgr.ActiveSoldiers)
                    {
                        if (ObjectManager.Player.Distance(soldier.Position) < 950 && target.Position.Distance(soldier.Position) < SoldierMgr.SoldierAttackRange)
                            return true;
                    }
                }

                if (target.Type == GameObjectType.obj_AI_Hero)
                {
                    Obj_AI_Hero hero = target as Obj_AI_Hero;
                    return ObjectManager.Player.Distance(hero.ServerPosition) - hero.BoundingRadius - hero.GetScalingRange() + 10 < ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius;
                }
                else
                    return ObjectManager.Player.Distance(target.Position) - target.BoundingRadius + 10 < ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius;
            }
            return false;
        }

        protected override void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "azirq")
                    Orbwalker.ResetAATimer();

                if (JumpActive || InsecActive)
                {
                    if (args.SData.Name == "azire" && Utils.TickCount - CastQT < 500 + Game.Ping)
                    {
                        Spells[Q].Cast(CastQLocation, true);
                        CastQT = 0;
                    }

                    if (args.SData.Name == "azirq" && Utils.TickCount - CastET < 500 + Game.Ping)
                    {
                        Spells[E].Cast(CastELocation, true);
                        CastET = 0;
                    }
                }
            }
        }

        public override double CalculateDamageW(Obj_AI_Hero target)
        {
            return SoldierMgr.GetAADamage(target);
        }


        private void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowsMessages.WM_LBUTTONDBLCLCK)
            {
                var clickedObject = ObjectManager.Get<GameObject>().Where(p => p.Position.Distance(Game.CursorPos, true) < 40000).OrderBy(q => q.Position.Distance(Game.CursorPos, true)).FirstOrDefault();

                if (clickedObject != null)
                {
                    InsecTo = clickedObject.Position.To2D();
                    if (clickedObject.IsMe)
                        InsecTo = Vector2.Zero;
                }
            }

        }

        public bool ComboUseQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Combo.UseQ").GetValue<bool>(); }
        }

        public bool ComboUseQOnlyOutOfRange
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Combo.UseQOnlyOutOfAA").GetValue<bool>(); }
        }

        public bool ComboUseQAlwaysMaxRange
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Combo.UseQAlwaysMaxRange").GetValue<bool>(); }
        }

        public bool ComboUseQWhenNoWAmmo
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Combo.UseQWhenNoWAmmo").GetValue<bool>(); }
        }

        public bool ComboUseW
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Combo.UseW").GetValue<bool>(); }
        }

        public bool ComboUseE
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Combo.UseE").GetValue<bool>(); }
        }

        public bool ComboUseR
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Combo.UseR").GetValue<bool>(); }
        }

        public int ComboRMinHit
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Combo.RMinHit").GetValue<Slider>().Value; }
        }

        public int ComboRMinHP
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Combo.RMinHP").GetValue<Slider>().Value; }
        }

        public bool HarassUseQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Harass.UseQ").GetValue<bool>(); }
        }

        public bool HarassUseW
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Harass.UseW").GetValue<bool>(); }
        }

        public bool LaneClearUseQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.LaneClear.UseQ").GetValue<bool>(); }
        }

        public bool LaneClearUseW
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.LaneClear.UseW").GetValue<bool>(); }
        }

        public int LaneClearQMinMinion
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.LaneClear.MinQMinion").GetValue<Slider>().Value; }
        }

        public bool JumpActive
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Misc.Jump").GetValue<KeyBind>().Active; }
        }

        public bool InsecActive
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Misc.Insec").GetValue<KeyBind>().Active; }
        }

        public bool WQKillSteal
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Misc.WQKillSteal").GetValue<bool>(); }
        }

        public int LaneClearMinMana
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.LaneClear.ManaPercent").GetValue<Slider>().Value; }
        }

        public int HarassMinMana
        {
            get { return ConfigMenu.Item("SAutoCarry.Azir.Harass.ManaPercent").GetValue<Slider>().Value; }
        }
    }
}
