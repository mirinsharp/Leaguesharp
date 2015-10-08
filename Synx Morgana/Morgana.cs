﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using LeagueSharp;
using LeagueSharp.Common;
using ShineCommon;
using SharpDX;
using SPrediction;

namespace SynxMorgana
{
    public class Morgana : BaseChamp
    {
        public Morgana()
            : base ("Morgana")
        {

        }

        public override void CreateConfigMenu()
        {
            combo = new Menu("Combo", "Combo");
            combo.AddItem(new MenuItem("CUSEQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("CUSEW", "Use W").SetValue(false));
            combo.AddItem(new MenuItem("CUSERHIT", "Use R If Enemies >=").SetValue(new Slider(2, 2, 5)));

            harass = new Menu("Harass", "Harass");
            harass.AddItem(new MenuItem("HUSEQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("HUSEW", "Use W").SetValue(true));
            harass.AddItem(new MenuItem("HMANA", "Min. Mana Percent").SetValue(new Slider(50, 0, 100)));

            misc = new Menu("Misc", "Misc");
            misc.AddItem(new MenuItem("MAUTOQ", "Auto Harass Q").SetValue(new KeyBind('T', KeyBindType.Toggle)));
            misc.AddItem(new MenuItem("MAUTOQRANGE", "Auto Q Max Range").SetValue(new Slider(1000, 0, 1300)));
            misc.AddItem(new MenuItem("MAUTOQIMMO", "Auto Q Immobile Target").SetValue(true));
            misc.AddItem(new MenuItem("MAUTOWIMMO", "Auto W Immobile Target").SetValue(true));
            misc.AddItem(new MenuItem("MAUTOR", "Auto Use R If Enemies >=").SetValue(new Slider(2, 4, 5)));


            m_evader = new Evader(out evade, EvadeMethods.MorganaE);

            Config.AddSubMenu(combo);
            Config.AddSubMenu(harass);
            Config.AddSubMenu(evade);
            Config.AddSubMenu(misc);
            Config.AddToMainMenu();
            
            base.OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Combo] += Combo;
            base.OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Mixed] += Harass;
            base.BeforeOrbWalking += BeforeOrbwalk;
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q, 1300f);
            Spells[Q].SetSkillshot(0.25f, 75f, 1200f, true, SkillshotType.SkillshotLine);

            Spells[W] = new Spell(SpellSlot.W, 800f);
            Spells[W].SetSkillshot(0.5f, 400f, 2200f, false, SkillshotType.SkillshotCircle);

            Spells[E] = new Spell(SpellSlot.E, 750f);

            Spells[R] = new Spell(SpellSlot.R, 600f);

            m_evader.SetEvadeSpell(Spells[E]);
        }


        public void BeforeOrbwalk()
        {
            #region Auto Harass
            if (Spells[Q].IsReady() && Config.Item("MAUTOQ").GetValue<KeyBind>().Active && !ObjectManager.Player.UnderTurret())
            {
                var t = TargetSelector.GetTarget(Config.Item("MAUTOQRANGE").GetValue<Slider>().Value, TargetSelector.DamageType.Magical);
                if (t != null)
                    Spells[Q].SPredictionCast(t, HitChance.High);
            }
            #endregion

            #region Auto R
            if (Spells[R].IsReady())
            {
                if (ObjectManager.Player.CountEnemiesInRange(Spells[R].Range - 40) >= combo.Item("MAUTOR").GetValue<Slider>().Value)
                    Spells[R].Cast();
            }
            #endregion
        }

        public void Combo()
        {
            if (Spells[Q].IsReady() && Config.Item("CUSEQ").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(Spells[Q].Range, TargetSelector.DamageType.Magical);
                if (t != null)
                    Spells[Q].SPredictionCast(t, HitChance.High);
            }

            if (Spells[W].IsReady() && Config.Item("CUSEW").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(Spells[W].Range, TargetSelector.DamageType.Magical);
                if (t != null)
                    Spells[W].SPredictionCast(t, HitChance.High);
            }

            if (Spells[R].IsReady())
            {
                if (ObjectManager.Player.CountEnemiesInRange(Spells[R].Range - 40) >= Config.Item("CUSERHIT").GetValue<Slider>().Value)
                    Spells[R].Cast();
            }
        }

        public void Harass()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("HMANA").GetValue<Slider>().Value)
                return;

            if (Spells[Q].IsReady() && Config.Item("HUSEQ").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(Spells[Q].Range, TargetSelector.DamageType.Magical);
                if (target != null)
                    Spells[Q].SPredictionCast(target, HitChance.High);
            }

            if (Spells[W].IsReady() && Config.Item("HUSEW").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(Spells[W].Range, TargetSelector.DamageType.Magical);
                if (target != null)
                    Spells[W].SPredictionCast(target, HitChance.High);
            }

        }

        public override void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            if (sender.IsEnemy && sender.IsChampion() && ShineCommon.Utility.IsImmobileTarget(sender as Obj_AI_Hero))
            {
                if (Spells[Q].IsReady() && sender.IsValidTarget(Spells[Q].Range) && Config.Item("MAUTOQIMMO").GetValue<bool>())
                    Spells[Q].Cast(sender.ServerPosition);

                if (Spells[W].IsReady() && sender.IsValidTarget(Spells[W].Range) && Config.Item("MAUTOWIMMO").GetValue<bool>())
                    Spells[W].Cast(sender.ServerPosition);
            }
        }
    }
}
    


