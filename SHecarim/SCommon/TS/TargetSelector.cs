﻿using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SCommon.Database;
using SharpDX;

namespace SCommon.TS
{
    public static class TargetSelector
    {
        public enum Mode
        {
            Auto,
            LowHP,
            MostAD,
            MostAP,
            Closest,
            NearMouse,
            LessAttack,
            LessCast,
            MostStack,
        }

        private static string[] StackNames =
            {
                "kalistaexpungemarker",
                "vaynesilvereddebuff",
                "twitchdeadlyvenom",
                "ekkostacks",
                "dariushemo",
                "gnarwproc",
                "tahmkenchpdebuffcounter",
                "varuswdebuff",
            };

        private static Obj_AI_Hero s_SelectedTarget = null;
        private static Obj_AI_Hero s_LastTarget = null;
        private static int s_LastTargetSent;
        private static Func<Obj_AI_Hero, float> s_fnCustomMultipler;

        public static Obj_AI_Hero SelectedTarget
        {
            get { return s_SelectedTarget; }
        }

        static TargetSelector()
        {
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        public static void Initialize(Menu menuToAttach)
        {
            ConfigMenu.Create(menuToAttach);
        }

        public static Obj_AI_Hero GetTarget(float range, LeagueSharp.Common.TargetSelector.DamageType dmgType = LeagueSharp.Common.TargetSelector.DamageType.Physical, Vector3? _from = null)
        {
            Vector3 from = _from.HasValue ? _from.Value : ObjectManager.Player.ServerPosition;
            //if (s_LastTarget == null || !s_LastTarget.IsValidTarget(range) || Utils.TickCount - s_LastTargetSent > 250)
            //{
            //    var t = GetNewTarget(range, dmgType, from);
            //    s_LastTarget = t;
            //    s_LastTargetSent = Utils.TickCount;
            //}
            return GetNewTarget(range, dmgType, from);
        }

        public static void RegisterCustomMultipler(Func<Obj_AI_Hero, float> fn)
        {
            s_fnCustomMultipler = fn;
        }

        public static void UnregisterCustomMultipler()
        {
            s_fnCustomMultipler = null;
        }

        private static Obj_AI_Hero GetNewTarget(float range, LeagueSharp.Common.TargetSelector.DamageType dmgType = LeagueSharp.Common.TargetSelector.DamageType.Physical, Vector3? _from = null)
        {
            if (range == -1)
                range = Orbwalking.Utility.GetAARange();

            if (ConfigMenu.OnlyAttackSelected)
            {
                if (s_SelectedTarget != null)
                {
                    if (s_SelectedTarget.IsValidTarget(range))
                        return s_SelectedTarget;
                    else if (s_SelectedTarget.IsValidTarget())  
                        return null;
                }
            }
           
            if(ConfigMenu.FocusSelected)
            {
                if(s_SelectedTarget != null)
                {
                    if (s_SelectedTarget.IsValidTarget(range))
                        return s_SelectedTarget;
                    else if (ConfigMenu.FocusExtraRange > 0 && s_SelectedTarget.IsValidTarget(range + ConfigMenu.FocusExtraRange))
                        return null;
                }
            }
            Vector3 from = _from.HasValue ? _from.Value : ObjectManager.Player.ServerPosition;

            var enemies = HeroManager.Enemies.Where(p => p.IsValidTarget(range + p.BoundingRadius, true, from) && !LeagueSharp.Common.TargetSelector.IsInvulnerable(p, dmgType));
            if (enemies.Count() == 0)
                return null;

            switch ((Mode)ConfigMenu.TargettingMode)
            {
                case Mode.LowHP:
                    return enemies.MinOrDefault(hero => hero.Health);

                case Mode.MostAD:
                    return enemies.MaxOrDefault(hero => hero.BaseAttackDamage + hero.FlatPhysicalDamageMod);

                case Mode.MostAP:
                    return enemies.MaxOrDefault(hero => hero.BaseAbilityDamage + hero.FlatMagicDamageMod);

                case Mode.Closest:
                    return
                        enemies.MinOrDefault(
                            hero =>
                                (_from.HasValue ? _from.Value : ObjectManager.Player.ServerPosition).Distance(
                                    hero.ServerPosition, true));

                case Mode.NearMouse:
                    return enemies.Find(hero => hero.Distance(Game.CursorPos, true) < 22500); // 150 * 150

                case Mode.LessAttack:
                    return
                        enemies.MaxOrDefault(
                            hero =>
                                ObjectManager.Player.CalcDamage(hero, LeagueSharp.Common.Damage.DamageType.Physical, 100) / (1 + hero.Health) *
                                GetPriority(hero));

                case Mode.LessCast:
                    return
                        enemies.MaxOrDefault(
                            hero =>
                                ObjectManager.Player.CalcDamage(hero, LeagueSharp.Common.Damage.DamageType.Magical, 100) / (1 + hero.Health) *
                                GetPriority(hero));

                case Mode.Auto:
                {
                    var killableWithAA = enemies.Where(p => p.Health <= Damage.AutoAttack.GetDamage(p, true)).FirstOrDefault();
                    if (killableWithAA != null)
                        return killableWithAA;
                
                    var possibleTargets = enemies.OrderByDescending(q => GetPriority(q));
                    if (possibleTargets.Count() == 1)
                        return possibleTargets.First();
                    else if (possibleTargets.Count() > 1)
                    {
                        var killableTarget = possibleTargets.OrderByDescending(p => GetTotalADAPMultipler(p)).FirstOrDefault(q => GetHealthMultipler(q) >= 10);
                        if (killableTarget != null)
                            return killableTarget;
                
                        var targets = possibleTargets.OrderBy(p => ObjectManager.Player.Distance(p.ServerPosition));
                        Obj_AI_Hero mostImportant = null;
                        double mostImportantsDamage = 0;
                        foreach (var target in targets)
                        {
                            double dmg = target.CalcDamage(ObjectManager.Player, LeagueSharp.Common.Damage.DamageType.Physical, 100) + target.CalcDamage(ObjectManager.Player, LeagueSharp.Common.Damage.DamageType.Magical, 100);
                            if (mostImportant == null)
                            {
                                mostImportant = target;
                                mostImportantsDamage = dmg;
                            }
                            else
                            {
                                if (Orbwalking.Utility.InAARange(ObjectManager.Player, target) && !Orbwalking.Utility.InAARange(ObjectManager.Player, mostImportant))
                                {
                                    mostImportant = target;
                                    mostImportantsDamage = dmg;
                                    continue;
                                }
                                else if ((Orbwalking.Utility.InAARange(ObjectManager.Player, target) && Orbwalking.Utility.InAARange(ObjectManager.Player, mostImportant)) || (!Orbwalking.Utility.InAARange(ObjectManager.Player, target) && !Orbwalking.Utility.InAARange(ObjectManager.Player, mostImportant)))
                                {
                                    if (mostImportantsDamage < dmg / 2f)
                                    {
                                        mostImportant = target;
                                        mostImportantsDamage = dmg;
                                        continue;
                                    }
                
                                    if ((mostImportant.IsMelee && !target.IsMelee) || (!mostImportant.IsMelee && target.IsMelee))
                                    {
                                        float targetMultp = GetHealthMultipler(target);
                                        float mostImportantsMultp = GetHealthMultipler(mostImportant);
                                        if (mostImportantsMultp < targetMultp)
                                        {
                                            mostImportant = target;
                                            mostImportantsDamage = dmg;
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                        return mostImportant;
                    }
                    return null;
                }
                case Mode.MostStack:
                    return enemies.OrderByDescending(hero =>
                        hero.Buffs.Where(x => StackNames.Contains(x.Name.ToLower()))
                            .Sum(buff => buff.Count)).ThenByDescending(hero2 => ObjectManager.Player.CalcDamage(hero2, LeagueSharp.Common.Damage.DamageType.Physical, 100) / (1 + hero2.Health) * GetPriority(hero2)).FirstOrDefault();

            }

            return null;
        }

        private static float GetPriority(Obj_AI_Hero target)
        {
            return GetTotalMultipler(target) * (GetRoleMultipler(target) + GetCustomMultipler(target) + (s_fnCustomMultipler != null ? s_fnCustomMultipler(target) : 0));
        }
        private static float GetTotalMultipler(Obj_AI_Hero target)
        {
            return GetHealthMultipler(target) + GetTotalADAPMultipler(target) + (s_SelectedTarget == target && ConfigMenu.FocusSelected ? 10 : 0);
        }

        private static float GetTotalADAPMultipler(Obj_AI_Hero target)
        {
            return HeroManager.Enemies.OrderByDescending(p => p.TotalMagicalDamage + p.TotalAttackDamage).ToList().FindIndex(q => q.NetworkId == target.NetworkId) * 2;
        }

        private static float GetCustomMultipler(Obj_AI_Hero target)
        {
            return ConfigMenu.GetChampionPriority(target) * 2;
        }

        private static float GetRoleMultipler(Obj_AI_Hero target)
        {
            return (5 - target.GetPriority());
        }

        private static float GetHealthMultipler(Obj_AI_Hero target)
        {
            if (target.Health <= ObjectManager.Player.GetAutoAttackDamage(target) * 2f)
                return 20;

            if (target.HealthPercent <= 50 && target.GetRole() != ChampionRole.Tank)
                return 10 / (target.HealthPercent + 1);

            return 0;
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN)
            {
                return;
            }
            s_SelectedTarget =
                HeroManager.Enemies
                    .FindAll(hero => hero.IsValidTarget() && hero.Distance(Game.CursorPos, true) < 40000) // 200 * 200
                    .OrderBy(h => h.Distance(Game.CursorPos, true)).FirstOrDefault();

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (ConfigMenu.FocusSelected && ConfigMenu.SelectedTargetColor.Active)
            {
                if (s_SelectedTarget != null && s_SelectedTarget.IsValidTarget())
                    Render.Circle.DrawCircle(s_SelectedTarget.Position, 150, ConfigMenu.SelectedTargetColor.Color, 7, true);
            }
        }
    }
}
