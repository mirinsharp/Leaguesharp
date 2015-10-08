using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace ShyRiven
{
    internal class ComboInstance
    {
        public static Action<Obj_AI_Hero>[] MethodsOnUpdate = new Action<Obj_AI_Hero>[3];
        public static Action<Obj_AI_Hero, string>[] MethodsOnAnimation = new Action<Obj_AI_Hero, string>[3];
        public static Action<Obj_AI_Hero>[] GapCloseMethods = new Action<Obj_AI_Hero>[3];

        private const int Q = 0, W = 1, E = 2, R = 3, Q2 = 4, W2 = 5, E2 = 6, R2 = 7;

        public static void Initialize(Riven Me)
        {
            #region Gapclosers
            GapCloseMethods[0] = new Action<Obj_AI_Hero>((t) =>
            {
                if (t.Distance(ObjectManager.Player.ServerPosition) > Me.Config.Item("MMINDIST").GetValue<Slider>().Value)
                {
                    if (Me.Spells[E].IsReady())
                    {
                        int eMode = 3;
                        if (Me.OrbwalkingActiveMode == Me.OrbwalkingComboMode)
                            eMode = Me.Config.Item("CEMODE").GetValue<StringList>().SelectedIndex;
                        else if (Me.OrbwalkingActiveMode == Me.OrbwalkingHarassMode)
                            eMode = Me.Config.Item("HEMODE").GetValue<StringList>().SelectedIndex;

                        if (eMode == 0)
                            Me.Spells[E].Cast(t.ServerPosition);
                        else if (eMode == 1)
                            Me.Spells[E].Cast(Game.CursorPos);
                        else if (eMode == 2)
                            Me.Spells[E].Cast(ObjectManager.Player.ServerPosition + (t.ServerPosition - ObjectManager.Player.ServerPosition).Normalized() * Me.Spells[E].Range);
                    }
                }
            });

            GapCloseMethods[1] = new Action<Obj_AI_Hero>((t) =>
            {
                if (t.Distance(ObjectManager.Player.ServerPosition) > Me.Config.Item("MMINDIST").GetValue<Slider>().Value)
                {
                    if (!Me.Spells[E].IsReady())
                    {
                        if (Me.Spells[Q].IsReady())
                            Me.Spells[Q].Cast(t.ServerPosition);
                    }
                }
            });

            GapCloseMethods[2] = new Action<Obj_AI_Hero>((t) =>
            {
                if (Target.IsTargetFlashed() && Me.Config.Item("CUSEF").GetValue<KeyBind>().Active)
                {
                    if (t.Distance(ObjectManager.Player.ServerPosition) > 300 && t.Distance(ObjectManager.Player.ServerPosition) < 500 && Me.OrbwalkingActiveMode == Me.OrbwalkingComboMode)
                    {
                        int steps = (int)(t.Distance(ObjectManager.Player.ServerPosition) / 10);
                        Vector3 direction = (t.ServerPosition - ObjectManager.Player.ServerPosition).Normalized();
                        for (int i = 0; i < steps - 1; i++)
                        {
                            if (NavMesh.GetCollisionFlags(ObjectManager.Player.ServerPosition + direction * 10 * i).HasFlag(CollisionFlags.Wall))
                                return;
                        }
                        ObjectManager.Player.Spellbook.CastSpell(Me.SummonerFlash, t.ServerPosition);
                    }
                    Target.SetFlashed(false);
                }
            });
            #endregion

            #region Normal Combo
            MethodsOnUpdate[0] = (t) =>
                {
                    if (t != null)
                    {
                        //gapclose
                        for (int i = 0; i < GapCloseMethods.Length; i++)
                            GapCloseMethods[i](t);

                        if (Me.CheckR1(t))
                        {
                            if (Me.Spells[E].IsReady())
                                Me.Spells[E].Cast(t.ServerPosition);
                            Me.Spells[R].Cast();
                        }

                        if (Me.CheckR2(t))
                            Me.Spells[R].Cast(t.ServerPosition);

                        if (Me.Spells[W].IsReady() && t.Distance(ObjectManager.Player.ServerPosition) < Me.Spells[W].Range && !Me.IsDoingFastQ)
                        {
                            Me.CastCrescent();
                            Me.Spells[W].Cast(true);
                        }
                    }

                    //PATCH WARNING
                    if (!Animation.CanAttack() && Animation.CanCastAnimation && !Me.Spells[W].IsReady() && !Me.CheckR1(t))
                        Me.FastQCombo();
                };

            MethodsOnAnimation[0] = (t, animname) =>
                {
                    if (Me.OrbwalkingActiveMode == Me.OrbwalkingComboMode || Me.OrbwalkingActiveMode == Me.OrbwalkingHarassMode)
                    {
                        t = Target.Get(600, true);
                        if (t != null)
                        {
                            if (animname == "Spell3") //e w & e q etc
                            {
                                if (Me.CheckR1(t))
                                {
                                    Me.Spells[R].Cast();
                                    return;
                                }

                                if (Me.Spells[W].IsReady() && t.Distance(ObjectManager.Player.ServerPosition) < Me.Spells[W].Range && !Me.IsDoingFastQ)
                                {
                                    Me.Spells[W].Cast();
                                    return;
                                }

                                //PATCH WARNING
                                if (Me.Spells[Q].IsReady() && !Me.IsDoingFastQ && !Me.CheckR1(t) && t.Distance(ObjectManager.Player.ServerPosition) < Me.Spells[Q].Range)
                                {
                                    Me.Spells[Q].Cast();
                                    Me.FastQCombo();
                                    return;
                                }
                            }
                            else if (animname == "Spell4a")
                            {
                                if (Me.Spells[W].IsReady() && t.Distance(ObjectManager.Player.ServerPosition) < Me.Spells[W].Range)
                                {
                                    Me.Spells[W].Cast();
                                    return;
                                }
                            }
                        }

                        //r2 target
                        t = Target.Get(900);
                        if (t != null && Me.CheckR2(t))
                        {
                            if (animname == "Spell3" || animname == "Spell1c") //e r2 & q3 r2
                                Me.Spells[R].Cast(t.ServerPosition);
                        }
                    }
                };
            #endregion

            #region Shy Burst (E-R-Flash-W-AA-R2-Hydra-Q)
            MethodsOnUpdate[1] = (t) =>
                {
                    if (!ObjectManager.Player.Spellbook.GetSpell(Me.SummonerFlash).IsReady() && !ObjectManager.Player.HasBuff("RivenFengShuiEngine"))
                    {
                        MethodsOnUpdate[0](t);
                        return;
                    }

                    t = Target.Get(1300, true);
                    if (t != null)
                    {
                        if (Me.Spells[E].IsReady() && !ObjectManager.Player.HasBuff("RivenFengShuiEngine"))
                        {
                            Me.Spells[E].Cast(t.ServerPosition);
                            if (!Me.Config.Item("CDISABLER").GetValue<bool>() && Me.Spells[R].IsReady())
                                Me.Spells[R].Cast();
                            return;
                        }

                        if (Me.Spells[W].IsReady() && t.IsValidTarget(Me.Spells[W].Range))
                        {
                            Me.Spells[W].Cast();
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackTo, t);
                            Me.CastCrescent();
                        }

                        if (ObjectManager.Player.HasBuff("RivenFengShuiEngine") && !Me.Config.Item("CDISABLER").GetValue<bool>())
                        {
                            if (Me.Spells[R].IsReady()) //r2
                                Me.Spells[R].Cast(t.ServerPosition);
                            Me.FastQCombo();
                            ShineCommon.Orbwalking.Move2 = false;
                        }
                    }
                };

            MethodsOnAnimation[1] = (t, animname) =>
                {
                    if (!ObjectManager.Player.Spellbook.GetSpell(Me.SummonerFlash).IsReady() && !ObjectManager.Player.HasBuff("RivenFengShuiEngine"))
                    {
                        MethodsOnUpdate[0](t);
                        return;
                    }

                    switch (animname)
                    {
                        case "Spell3": //e r1
                        {
                            if (!ObjectManager.Player.HasBuff("RivenFengShuiEngine") && !Me.Config.Item("CDISABLER").GetValue<bool>() && Me.Spells[R].IsReady())
                                Me.Spells[R].Cast();
                        }
                        break;
                        case "Spell4a": //r flash
                        {
                            if (t.Distance(ObjectManager.Player.ServerPosition) > 300)
                            {
                                ObjectManager.Player.Spellbook.CastSpell(Me.SummonerFlash, t.ServerPosition);
                                ShineCommon.Orbwalking.Move2 = true;
                            }
                        }
                        break;
                    }
                };
            #endregion

            #region Flash Combo (Q1-Q2-E-R1-Flash-Q3-Hydra-W-R2)
            MethodsOnUpdate[2] = (t) =>
                {
                    if (!ObjectManager.Player.Spellbook.GetSpell(Me.SummonerFlash).IsReady() && !ObjectManager.Player.HasBuff("RivenFengShuiEngine"))
                    {
                        MethodsOnUpdate[0](t);
                        return;
                    }

                    t = Target.Get(1000);
                    if (Animation.QStacks == 2)
                    {
                        if (!Me.Spells[E].IsReady() && !ObjectManager.Player.HasBuff("RivenFengShuiEngine"))
                            return;

                        if (t != null)
                        {
                            if (Me.Spells[E].IsReady())
                            {
                                Me.Spells[E].Cast(t.ServerPosition);
                                return;
                            }

                            if (t.IsValidTarget(600))
                            {
                                Me.CastCrescent();
                                if (Me.Spells[W].IsReady())
                                {
                                    if (t.IsValidTarget(Me.Spells[W].Range))
                                        Me.Spells[W].Cast();
                                }
                                else
                                    if (ObjectManager.Player.HasBuff("RivenFengShuiEngine") && !Me.Config.Item("CDISABLER").GetValue<bool>() && Me.Spells[R].IsReady())
                                        Me.Spells[R].Cast(t.ServerPosition);
                            }
                        }
                    }
                    else
                    {
                        if (Me.Spells[Q].IsReady())
                        {
                            if(Utils.TickCount - Animation.LastQTick >= 1000)
                                Me.Spells[Q].Cast(Game.CursorPos);
                        }
                    }
                };

            MethodsOnAnimation[2] = (t, animname) =>
                {
                    {
                        switch (animname)
                        {
                            case "Spell3": //e r1
                            {
                                if (!ObjectManager.Player.HasBuff("RivenFengShuiEngine") && !Me.Config.Item("CDISABLER").GetValue<bool>() && Me.Spells[R].IsReady())
                                    Me.Spells[R].Cast();
                            }
                            break;
                            case "Spell4a": //r1 flash
                            {
                                if (t.Distance(ObjectManager.Player.ServerPosition) > 300 && Me.OrbwalkingActiveMode == Me.OrbwalkingComboMode)
                                {
                                    ObjectManager.Player.Spellbook.CastSpell(Me.SummonerFlash, t.ServerPosition);
                                    Me.Spells[Q].Cast(t.ServerPosition);
                                }
                            }
                            break;
                            case "Spell2": //w r2
                            {
                                if (ObjectManager.Player.HasBuff("RivenFengShuiEngine") && !Me.Config.Item("CDISABLER").GetValue<bool>() && Me.Spells[R].IsReady())
                                    Me.Spells[R].Cast(t.ServerPosition);
                            }
                            break;
                        }
                    }
                };
            #endregion
        }
    }
}
