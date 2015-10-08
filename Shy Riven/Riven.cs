using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LeagueSharp;
using LeagueSharp.Common;
using ShineCommon;
using SharpDX;

namespace ShyRiven
{
    public class Riven : BaseChamp
    {
        public bool IsDoingFastQ = false;
        public bool IsCrestcentReady
        {
            get { return (Items.HasItem(3077) && Items.CanUseItem(3077)) || (Items.HasItem(3074) && Items.CanUseItem(3074)); }
        }
        /*PBE*/
        /*public int EdgeCount
        {
            get { return 0; }
        }*/
        public SpellSlot SummonerFlash = ObjectManager.Player.GetSpellSlot("summonerflash");

        private Dictionary<string, StringList> ComboMethodBackup = new Dictionary<string, StringList>();
        public Riven()
            : base("Riven")
        {

        }

        public override void CreateConfigMenu()
        {
            combo = new Menu("Combo", "combo");
            combo.AddItem(new MenuItem("CDISABLER", "Disable R Usage").SetValue(false))
                    .ValueChanged += (s, ar) =>
                    {
                        Config.Item("CR1MODE").Show(!ar.GetNewValue<bool>());
                        Config.Item("CR2MODE").Show(!ar.GetNewValue<bool>());
                    };
            combo.AddItem(new MenuItem("CR1MODE", "R1 Mode").SetValue(new StringList(new string[] { "Always", "If Killable With R2", "Smart" }))).Show(!combo.Item("CDISABLER").GetValue<bool>());
            combo.AddItem(new MenuItem("CR2MODE", "R2 Mode").SetValue(new StringList(new string[] { "Always", "If Killable", "If Out of Range" }, 1))).Show(!combo.Item("CDISABLER").GetValue<bool>());
            combo.AddItem(new MenuItem("CEMODE", "E Mode").SetValue(new StringList(new string[] { "E to enemy", "E Cursor Pos", "E to back off", "Dont Use E" }, 0)));
            combo.AddItem(new MenuItem("CUSEF", "Use Flash In Combo").SetValue(new KeyBind('G', KeyBindType.Toggle))).Permashow();

            Menu comboType = new Menu("Combo Methods", "combomethod");
            foreach (var enemy in HeroManager.Enemies)
            {
                ComboMethodBackup.Add(String.Format("CMETHOD{0}", enemy.ChampionName), new StringList(new string[] { "Normal", "Shy Burst", "Flash Combo" }));
                comboType.AddItem(new MenuItem(String.Format("CMETHOD{0}", enemy.ChampionName), enemy.ChampionName).SetValue(new StringList(new string[] { "Normal", "Shy Burst", "Flash Combo" })))
                    .ValueChanged += (s, ar) =>
                        {
                            if(!comboType.Item("CSHYKEY").GetValue<KeyBind>().Active && !comboType.Item("CFLASHKEY").GetValue<KeyBind>().Active)
                                ComboMethodBackup[((MenuItem)s).Name] = ar.GetNewValue<StringList>();
                        };
            }
            comboType.AddItem(new MenuItem("CSHYKEY", "Set All Shy Burst While Pressing Key").SetValue(new KeyBind('T', KeyBindType.Press))).Permashow();
            comboType.AddItem(new MenuItem("CFLASHKEY", "Set All Flash Combo While Pressing Key").SetValue(new KeyBind('Z', KeyBindType.Press))).Permashow();
            combo.AddSubMenu(comboType);
            

            harass = new Menu("Harass", "harass");
            harass.AddItem(new MenuItem("HEMODE", "E Mode").SetValue(new StringList(new string[] { "E to enemy", "E Cursor Pos", "E to back off", "Dont Use E" }, 0)));


            laneclear = new Menu("LaneClear/JungleClear", "laneclear");
            laneclear.AddItem(new MenuItem("LUSEQ", "Use Q").SetValue(true));
            laneclear.AddItem(new MenuItem("LUSEW", "Use W").SetValue(true))
                .ValueChanged += (s, ar) =>
                    {
                        laneclear.Item("LMINW").Show(ar.GetNewValue<bool>());
                    };
            laneclear.AddItem(new MenuItem("LMINW", "Min. Minion To W").SetValue(new Slider(1, 1, 6))).Show(laneclear.Item("LUSEW").GetValue<bool>());
            laneclear.AddItem(new MenuItem("LUSETIAMAT", "Use Tiamat/Hydra").SetValue(true));
            laneclear.AddItem(new MenuItem("LSEMIQJUNG", "Semi-Q Jungle Clear").SetValue(true));
            laneclear.AddItem(new MenuItem("LASTUSETIAMAT", "Use Tiamat/Hydra for Last Hitting").SetValue(true));

            misc = new Menu("Misc", "misc");
            misc.AddItem(new MenuItem("MFLEEKEY", "Flee Key").SetValue(new KeyBind('A', KeyBindType.Press)));
            misc.AddItem(new MenuItem("MFLEEWJ", "Use Wall Jump while flee").SetValue(true)).Permashow();
            misc.AddItem(new MenuItem("MKEEPQ", "Keep Q Alive (To Cursor Pos)").SetValue(false));
            misc.AddItem(new MenuItem("MMINDIST", "Min. Distance to gapclose").SetValue(new Slider(300, 250, 750)));
            misc.AddItem(new MenuItem("MAUTOINTRW", "Interrupt Spells With W").SetValue(true));
            misc.AddItem(new MenuItem("MAUTOINTRQ", "Try Interrupt Spells With Ward & Q3").SetValue(true));
            misc.AddItem(new MenuItem("MANTIGAPW", "Anti Gap Closer With W").SetValue(true));
            misc.AddItem(new MenuItem("MANTIGAPQ", "Try Anti Gap Closer With Ward & Q3").SetValue(true));
            misc.AddItem(new MenuItem("DDRAWCOMBOMODE", "Draw Combo Mode").SetValue(true));
            misc.AddItem(new MenuItem("DDRAWDAMAGEINDC", "Draw Damage Indicator").SetValue(true))
                .ValueChanged += (s, ar) =>
                    {
                        DamageIndicator.Enabled = ar.GetNewValue<bool>();
                    };


            Config.AddSubMenu(combo);
            Config.AddSubMenu(harass);
            Config.AddSubMenu(laneclear);
            Config.AddSubMenu(misc);
            Config.AddToMainMenu();

            ComboInstance.Initialize(this);
            DamageIndicator.DamageToUnit = (t) => (float)CalculateComboDamage(t) + (float)CalculateDamageR2(t);

            BeforeOrbWalking += BeforeOrbwalk;
            BeforeDrawing += BeforeDraw;
            OrbwalkingFunctions[OrbwalkingComboMode] += Combo;
            OrbwalkingFunctions[OrbwalkingHarassMode] += Combo; //same function because harass mode is just same combo w/o flash & r (which already implemented in combo)
            OrbwalkingFunctions[OrbwalkingLaneClearMode] += LaneClear;
            OrbwalkingFunctions[OrbwalkingLastHitMode] += LastHit;

            //Obj_AI_Hero.OnDamage += Animation.OnDamage;
            Obj_AI_Hero.OnDoCast += Animation.OnDoCast;
            Obj_AI_Hero.OnPlayAnimation += Animation.OnPlay;
            Obj_AI_Hero.OnIssueOrder += Animation.OnIssueOrder;
            Animation.OnAnimationCastable += Animation_OnAnimationCastable;
            Game.OnWndProc += Game_OnWndProc;
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q, 260f);
            Spells[W] = new Spell(SpellSlot.W, 250f);
            Spells[E] = new Spell(SpellSlot.E, 270f);
            Spells[R] = new Spell(SpellSlot.R, 900f);
            Spells[R].SetSkillshot(0.25f, 225f, 1600f, false, SkillshotType.SkillshotCone);

            m_evader = new Evader(out evade, EvadeMethods.RivenE, Spells[E]);
            Config.AddSubMenu(evade);
        }

        public void BeforeOrbwalk()
        {
            if (!Spells[Q].IsReady(1000))
            {
                Animation.QStacks = 0;
                IsDoingFastQ = false;
            }

            if (!Spells[R].IsReady())
                Animation.UltActive = false;

            if (Config.Item("MFLEEKEY").GetValue<KeyBind>().Active)
                Flee();

            if (Config.Item("CSHYKEY").GetValue<KeyBind>().Active)
            {
                foreach (var enemy in HeroManager.Enemies)
                {
                    var typeVal = Config.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).GetValue<StringList>();
                    if (typeVal.SelectedIndex != 1)
                    {
                        typeVal.SelectedIndex = 1;
                        Config.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).SetValue(typeVal);
                    }
                }
                var target = Orbwalker.GetTarget();
                ShineCommon.Orbwalking.Orbwalk(target, Game.CursorPos);
                Combo();
                return;
            }
            else if (Config.Item("CFLASHKEY").GetValue<KeyBind>().Active)
            {
                foreach (var enemy in HeroManager.Enemies)
                {
                    var typeVal = Config.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).GetValue<StringList>();
                    if (typeVal.SelectedIndex != 2)
                    {
                        typeVal.SelectedIndex = 2;
                        Config.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).SetValue(typeVal);
                    }
                }
                var target = Orbwalker.GetTarget();
                ShineCommon.Orbwalking.Orbwalk(target, Game.CursorPos);
                Combo();
                return;
            }
            else
            {
                if (OrbwalkingActiveMode == OrbwalkingNoneMode)
                    ShineCommon.Orbwalking.Move2 = false;

                foreach (var enemy in HeroManager.Enemies)
                {
                    var typeVal = Config.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).GetValue<StringList>();
                    if (typeVal.SelectedIndex != ComboMethodBackup[String.Format("CMETHOD{0}", enemy.ChampionName)].SelectedIndex)
                    {
                        typeVal.SelectedIndex = ComboMethodBackup[String.Format("CMETHOD{0}", enemy.ChampionName)].SelectedIndex;
                        Config.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).SetValue(typeVal);
                    }
                }
            }

            if (Config.Item("MKEEPQ").GetValue<bool>() && Animation.QStacks != 0 && Utils.TickCount - Animation.LastQTick >= 3500)
                Spells[Q].Cast(Game.CursorPos);
        }

        public void BeforeDraw()
        {
            if (Config.Item("DDRAWCOMBOMODE").GetValue<bool>())
            {
                foreach (var enemy in HeroManager.Enemies)
                {
                    if (!enemy.IsDead && enemy.IsVisible)
                    {
                        var text_pos = Drawing.WorldToScreen(enemy.Position);
                        Drawing.DrawText((int)text_pos.X - 20, (int)text_pos.Y + 35, System.Drawing.Color.Aqua, Config.Item(String.Format("CMETHOD{0}", enemy.ChampionName)).GetValue<StringList>().SelectedValue);
                    }
                }
            }
        }

        public void Combo()
        {
            var t = Target.Get(600, true);
            if (t != null)
                ComboInstance.MethodsOnUpdate[Config.Item(String.Format("CMETHOD{0}", t.ChampionName)).GetValue<StringList>().SelectedIndex](t);
            else
                ShineCommon.Orbwalking.Move2 = false;
        }

        public void LaneClear()
        {
            var minion = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 400, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).FirstOrDefault();
            if (minion != null)
            {
                if (Config.Item("LUSEQ").GetValue<bool>() && Spells[Q].IsReady())
                {
                    Animation.SetAttack(true);
                    if (!IsDoingFastQ && minion.Distance(ObjectManager.Player.ServerPosition) > 150)
                        Spells[Q].Cast(minion.ServerPosition);
                    else
                    {
                        ShineCommon.Orbwalking.Move2 = true;
                        Orbwalker.ForceTarget(minion);
                        ShineCommon.Orbwalking.ResetAutoAttackTimer();
                    }
                    IsDoingFastQ = true;
                }

                if (Config.Item("LUSEW").GetValue<bool>() && Spells[W].IsReady() && (ObjectManager.Get<Obj_AI_Minion>().Count(p => p.Distance(ObjectManager.Player.ServerPosition) <= Spells[W].Range) >= Config.Item("LMINW").GetValue<Slider>().Value || minion.IsJungleMinion()))
                {
                    if (Config.Item("LUSETIAMAT").GetValue<bool>())
                        CastCrescent();
                    Spells[W].Cast();
                }
            }
            else
                ShineCommon.Orbwalking.Move2 = false;
        }

        public void LastHit()
        {
            if (Config.Item("LASTUSETIAMAT").GetValue<bool>() && IsCrestcentReady)
            {
                var minion = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 400, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).FirstOrDefault();
                if (minion != null)
                {
                    float dist = minion.Distance(ObjectManager.Player.ServerPosition);
                    double dmg = (ObjectManager.Player.BaseAttackDamage + ObjectManager.Player.FlatPhysicalDamageMod) * (1 - dist * 0.001);
                    if (minion.Health <= dmg)
                        CastCrescent();
                }
            }
        }

        public void Flee()
        {
            if (Spells[Q].IsReady() && Animation.QStacks != 2)
                Spells[Q].Cast(Game.CursorPos);

            if (Config.Item("MFLEEWJ").GetValue<bool>())
            {
                if (Spells[Q].IsReady())
                {
                    var curSpot = WallJump.GetSpot(ObjectManager.Player.ServerPosition);
                    if (curSpot.Start != Vector3.Zero && Animation.QStacks == 2)
                    {
                        if (Spells[E].IsReady())
                            Spells[E].Cast(curSpot.End);
                        else
                            if (Items.GetWardSlot() != null)
                                Items.UseItem((int)Items.GetWardSlot().Id, curSpot.End);
                        Spells[Q].Cast(curSpot.End);
                        return;
                    }
                    var spot = WallJump.GetNearest(Game.CursorPos);
                    if (spot.Start != Vector3.Zero)
                    {
                        ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, spot.Start);
                        return;
                    }
                    else
                        Spells[E].Cast(Game.CursorPos);
                }
            }
            else
            {
                if (Spells[Q].IsReady() && Animation.QStacks == 2)
                    Spells[Q].Cast(Game.CursorPos);

                if (Spells[E].IsReady())
                    Spells[E].Cast(Game.CursorPos);
            }

            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
        }

        public void FastQCombo()
        {
            if (Spells[Q].IsReady())
            {
                var t = Target.Get(Spells[Q].Range);
                if (t != null)
                {
                    Target.Set(t);
                    Program.Champion.Orbwalker.ForceTarget(t);
                    Animation.SetAttack(true);
                    if (!IsDoingFastQ && t.Distance(ObjectManager.Player.ServerPosition) > 150 + (ObjectManager.Player.HasBuff("RivenFengShuiEngine") ? 75 : 0))
                        Spells[Q].Cast(t.ServerPosition);
                    else
                    {
                        ShineCommon.Orbwalking.Move2 = true;
                        ShineCommon.Orbwalking.ResetAutoAttackTimer();
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, t);
                    }
                    IsDoingFastQ = true;
                }
            }
        }

        public bool CheckR1(Obj_AI_Hero t)
        {
            if (!ObjectManager.Player.HasBuff("RivenFengShuiEngine") && !Config.Item("CDISABLER").GetValue<bool>() && Spells[R].IsReady() && t.Distance(ObjectManager.Player.ServerPosition) < 500 && OrbwalkingActiveMode == OrbwalkingComboMode)
            {
                if (ObjectManager.Player.ServerPosition.CountEnemiesInRange(500) > 1)
                    return true;

                switch (Config.Item("CR1MODE").GetValue<StringList>().SelectedIndex)
                {
                    case 1: if (!(t.Health - CalculateComboDamage(t) - CalculateDamageR2(t) <= 0)) return false;
                        break;
                    case 2: if (!(t.Health - CalculateComboDamage(t) < 1000 && t.Health >= 1000)) return false;
                        break;
                }
                return true;
            }
            return false;
        }

        public bool CheckR2(Obj_AI_Hero t)
        {
            if (ObjectManager.Player.HasBuff("RivenFengShuiEngine") && !Config.Item("CDISABLER").GetValue<bool>() && Spells[R].IsReady() && t.Distance(ObjectManager.Player.ServerPosition) < 900 && OrbwalkingActiveMode == OrbwalkingComboMode)
            {
                switch (Config.Item("CR2MODE").GetValue<StringList>().SelectedIndex)
                {
                    case 1: if (!(t.Health - CalculateDamageR2(t) <= 0) || t.Distance(ObjectManager.Player.ServerPosition) > 600) return false;
                        break;
                    case 2: if (t.Distance(ObjectManager.Player.ServerPosition) < 600) return false;
                        break;
                }
                return true;
            }
            return false;
        }

        public void CastCrescent()
        {
            if (ObjectManager.Player.CountEnemiesInRange(500) > 0 || OrbwalkingActiveMode == OrbwalkingLaneClearMode)
            {
                if (Items.HasItem(3077) && Items.CanUseItem(3077)) //tiamat
                    Items.UseItem(3077);
                else if (Items.HasItem(3074) && Items.CanUseItem(3074)) //hydra
                    Items.UseItem(3074);

                Animation.CanCastAnimation = true;
            }
        }

        /*
         * PBE
        public override double CalculateSpellDamage(Obj_AI_Hero target)
        {
            return (CalculateDamageQ(target) + CalculateDamageW(target) + CalculateDamageE(target) + CalculateDamageR(target)) * (1 + EdgeCount * 0.001);
        }
        */
        public override double CalculateAADamage(Obj_AI_Hero target, int aacount = 3)
        {
            double dmg = base.CalculateAADamage(target, aacount);                                                                                                                                                                                                                                                               /*          PBE            */
            dmg += ObjectManager.Player.CalcDamage(target, Damage.DamageType.Physical, new[] { 0.2, 0.2, 0.25, 0.25, 0.25, 0.3, 0.3, 0.3, 0.35, 0.35, 0.35, 0.4, 0.4, 0.4, 0.45, 0.45, 0.45, 0.5 }[ObjectManager.Player.Level - 1] * (ObjectManager.Player.BaseAttackDamage + ObjectManager.Player.FlatPhysicalDamageMod) * 5) /** (1 + EdgeCount * 0.001)*/;
            return dmg;
        }

        public override double CalculateDamageQ(Obj_AI_Hero target)
        {
            if (!Spells[Q].IsReady())
                return 0.0d;

            return base.CalculateDamageQ(target) * (3 - Animation.QStacks);
        }

        public override double CalculateDamageR(Obj_AI_Hero target)
        {
            if (!Spells[R].IsReady())
                return 0.0d;
            return ObjectManager.Player.CalcDamage(target, Damage.DamageType.Physical, ObjectManager.Player.FlatPhysicalDamageMod * 0.2 * 3);
        }

        public double CalculateDamageR2(Obj_AI_Hero target)
        {
            if (Spells[R].IsReady())
                return ObjectManager.Player.CalcDamage(target, Damage.DamageType.Physical, (new[] { 80, 120, 160 }[Spells[R].Level - 1] + ObjectManager.Player.FlatPhysicalDamageMod * 0.6) * (1 + ((100 - target.HealthPercent) > 75 ? 75 : (100 - target.HealthPercent)) * 0.0267d));
                /*PBE*/
                /*return ObjectManager.Player.CalcDamage(target, Damage.DamageType.Physical, (new[] { 80, 120, 160 }[Spells[R].Level - 1] + ObjectManager.Player.FlatPhysicalDamageMod * 0.6) * (1 + EdgeCount * 0.0267d));*/
            return 0.0d;
        }

        public override void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.IsAutoAttack())
                {
                    Animation.SetLastAATick(Utils.TickCount);
                    if(IsDoingFastQ)
                        ShineCommon.Orbwalking.LastAATick = Environment.TickCount + Game.Ping / 2;
                }
            }
            else if (Target.Get(1000, true) != null)
            {
                if (args.SData.Name == "summonerflash")
                    if (args.End.Distance(ObjectManager.Player.ServerPosition) > 300 && args.End.Distance(ObjectManager.Player.ServerPosition) < 500 && !Spells[E].IsReady())
                        Target.SetFlashed();
            }
        }

        public override void Orbwalking_BeforeAttack(ShineCommon.Orbwalking.BeforeAttackEventArgs args)
        {            
            if (IsDoingFastQ)
            {
                //ShineCommon.Orbwalking.LastAATick = Environment.TickCount + Game.Ping / 2;
                if (!ShineCommon.Utility.DelayAction.Exists("rivenaa"))
                {
                    if (Interlocked.Read(ref Animation.blResetQueued) == 0)
                    {
                        Interlocked.Exchange(ref Animation.blResetQueued, 1);
                        ShineCommon.Utility.DelayAction.Add("rivenaa", (int)(ObjectManager.Player.AttackCastDelay * 1000) + 100, () =>
                        {
                            if (Interlocked.Read(ref Animation.blResetQueued) == 1)
                            {
                                Interlocked.Exchange(ref Animation.blResetQueued, 0);
                                ShineCommon.Orbwalking.Move2 = false;
                                if (OrbwalkingActiveMode == OrbwalkingComboMode || OrbwalkingActiveMode == OrbwalkingHarassMode || Config.Item("CSHYKEY").GetValue<KeyBind>().Active || Config.Item("CFLASHKEY").GetValue<KeyBind>().Active)
                                {
                                    var t = Target.Get(600, true);
                                    if (t != null && Spells[Q].IsReady() && Animation.QStacks != 0)
                                        Spells[Q].Cast(t.ServerPosition);
                                }
                            }
                        });
                    }
                }
                else
                    ShineCommon.Utility.DelayAction.Update("rivenaa", (int)(ObjectManager.Player.AttackCastDelay * 1000) + 100);
            }
        }

        public override void Interrupter_OnPossibleToInterrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Spells[W].IsReady() && sender.IsEnemy && sender.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <= Spells[W].Range && Config.Item("MAUTOINTRW").GetValue<bool>())
                Spells[W].Cast();
        }

        public override void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!gapcloser.Sender.IsEnemy)
                return;

            if (Spells[W].IsReady() && gapcloser.End.Distance(ObjectManager.Player.ServerPosition) <= Spells[W].Range && Config.Item("MANTIGAPW").GetValue<bool>())
                Spells[W].Cast();

            if (Config.Item("MANTIGAPQ").GetValue<bool>() && Animation.QStacks == 2)
                if (gapcloser.Sender.Spellbook.GetSpell(gapcloser.Slot).SData.MissileSpeed != 0)
                {
                    LeagueSharp.Common.Utility.DelayAction.Add((int)(gapcloser.End.Distance(gapcloser.Start) / gapcloser.Sender.Spellbook.GetSpell(gapcloser.Slot).SData.MissileSpeed * 1000f) - Game.Ping, () =>
                        {
                            if (Items.GetWardSlot() != null)
                                Items.UseItem((int)Items.GetWardSlot().Id, ObjectManager.Player.ServerPosition + (gapcloser.End - gapcloser.Start).Normalized() * 40);
                            Spells[Q].Cast(ObjectManager.Player.ServerPosition);
                        });
                }
        }

        public void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowsMessages.WM_LBUTTONDBLCLCK)
            {
                var clickedTarget = HeroManager.Enemies
                    .FindAll(hero => hero.IsValidTarget() && hero.Distance(Game.CursorPos, true) < 40000) // 200 * 200
                    .OrderBy(h => h.Distance(Game.CursorPos, true)).FirstOrDefault();

                if (clickedTarget != null)
                {
                    var typeVal = Config.Item(String.Format("CMETHOD{0}", clickedTarget.ChampionName)).GetValue<StringList>();
                    typeVal.SelectedIndex = (typeVal.SelectedIndex + 1) % 3;
                    Config.Item(String.Format("CMETHOD{0}", clickedTarget.ChampionName)).SetValue(typeVal);
                }
            }

        }

        private void Animation_OnAnimationCastable(string animname)
        {
            if (OrbwalkingActiveMode == OrbwalkingComboMode || OrbwalkingActiveMode == OrbwalkingHarassMode || Config.Item("CSHYKEY").GetValue<KeyBind>().Active || Config.Item("CFLASHKEY").GetValue<KeyBind>().Active)
            {
                var t = Target.Get(1000);
                if(t != null)
                    ComboInstance.MethodsOnAnimation[Config.Item(String.Format("CMETHOD{0}", t.ChampionName)).GetValue<StringList>().SelectedIndex](t, animname);
            }
        }
    }
}
