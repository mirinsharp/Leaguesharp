#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Orb;
using SharpDX;
using Color = System.Drawing.Color;
using Orbwalking = Marksman.Orb.Orbwalking;

#endregion

namespace Marksman.Champions
{
    internal class Graves : Champion
    {
        public Spell Q;
        public Spell W;
        public Spell R;

        public Graves()
        {
            Q = new Spell(SpellSlot.Q, 920f); // Q likes to shoot a bit too far away, so moving the range inward.
            Q.SetSkillshot(0.26f, 10f*2*(float) Math.PI/180, 1950, false, SkillshotType.SkillshotCone);

            W = new Spell(SpellSlot.W, 1100f);
            W.SetSkillshot(0.30f, 250f, 1650f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 1100f);
            R.SetSkillshot(0.22f, 150f, 2100, true, SkillshotType.SkillshotLine);

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            Drawing.OnDraw += DrawingOnOnDraw;

            Utils.Utils.PrintMessage("Graves loaded.");
        }

        private void DrawingOnOnDraw(EventArgs args)
        {
            var t = TargetSelector.GetTarget(Q.Range * 5, TargetSelector.DamageType.Magical);
            if (!t.IsValidTarget())
            {
                return;
            }

            if (Q.IsReady())
            {

                var toPolygon = new Marksman.Common.CommonGeometry.Rectangle(ObjectManager.Player.Position.To2D(), ObjectManager.Player.Position.To2D().Extend(t.Position.To2D(), Q.Range - 200), 50).ToPolygon();
                toPolygon.Draw(System.Drawing.Color.Red, 2);
                
                if (toPolygon.IsInside(t))
                {
                    Render.Circle.DrawCircle(t.Position, t.BoundingRadius, Color.Black);
                    Q.Cast(t);
                }

                var xPos = ObjectManager.Player.Position.To2D().Extend(t.Position.To2D(), Q.Range);

                var toPolygon2 = new Marksman.Common.CommonGeometry.Rectangle(xPos, ObjectManager.Player.Position.To2D().Extend(t.Position.To2D(), Q.Range - 195), 260).ToPolygon();
                toPolygon2.Draw(System.Drawing.Color.Red, 2);

                if (toPolygon2.IsInside(t))
                {
                    Render.Circle.DrawCircle(t.Position, t.BoundingRadius, Color.Black);
                    Q.Cast(t);
                }
            }

        }

        private float GetComboDamage(Obj_AI_Hero t)
        {
            var fComboDamage = 0f;

            if (Q.IsReady())
                fComboDamage += (float) ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q);

            if (W.IsReady())
                fComboDamage += (float) ObjectManager.Player.GetSpellDamage(t, SpellSlot.W);

            if (R.IsReady())
                fComboDamage += (float) ObjectManager.Player.GetSpellDamage(t, SpellSlot.R);

            if (ObjectManager.Player.GetSpellSlot("summonerdot") != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(ObjectManager.Player.GetSpellSlot("summonerdot")) ==
                SpellState.Ready && ObjectManager.Player.Distance(t) < 550)
                fComboDamage += (float) ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);

            if (Items.CanUseItem(3144) && ObjectManager.Player.Distance(t) < 550)
                fComboDamage += (float) ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Bilgewater);

            if (Items.CanUseItem(3153) && ObjectManager.Player.Distance(t) < 550)
                fComboDamage += (float) ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Botrk);

            return fComboDamage;
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (Q.IsReady() && GetValue<KeyBind>("UseQTH").Active)
            {
                if (ObjectManager.Player.HasBuff("Recall"))
                    return;
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t != null)
                    Q.Cast(t, false, true);
            }

            if (GetValue<KeyBind>("CastR").Active && R.IsReady())
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if (ObjectManager.Player.GetSpellDamage(t, SpellSlot.R) > t.Health && !t.IsZombie)
                    {
                        R.CastIfHitchanceEquals(t, HitChance.High, false);
                    }
                }
            }


            if ((!ComboActive && !HarassActive) || !Orbwalking.CanMove(100)) return;
            var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));
            var useR = GetValue<bool>("UseR" + (ComboActive ? "C" : "H"));

            if (Q.IsReady() && useQ)
            {
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t != null)
                    Q.Cast(t, false, true);
            }

            if (W.IsReady() && useW)
            {
                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget(W.Range) &&
                    (t.HasBuffOfType(BuffType.Stun) || t.HasBuffOfType(BuffType.Snare) ||
                     t.HasBuffOfType(BuffType.Taunt) || t.HasBuff("zhonyasringshield") ||
                     t.HasBuff("Recall")))
                    W.Cast(t, false, true);
            }

            if (R.IsReady() && useR)
            {
                foreach (
                    var hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                hero =>
                                    hero.IsValidTarget(R.Range) &&
                                    ObjectManager.Player.GetSpellDamage(hero, SpellSlot.R, 1) - 20 > hero.Health))
                    R.Cast(hero, false, true);
            }
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t != null && (ComboActive || HarassActive) && unit.IsMe)
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));

                if (Q.IsReady() && useQ)
                    Q.Cast(t);

                if (W.IsReady() && useW)
                {
                    if (t.IsValidTarget(W.Range) &&
                        (t.HasBuffOfType(BuffType.Stun) || t.HasBuffOfType(BuffType.Snare) ||
                         t.HasBuffOfType(BuffType.Taunt) || t.HasBuff("zhonyasringshield") ||
                         t.HasBuff("Recall")))
                        W.Cast(t);
                }
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

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            config.AddItem(new MenuItem("UseRC" + Id, "Use R").SetValue(true));
            config.AddItem(
                new MenuItem("CastR" + Id, "Cast R (Manual)").SetValue(new KeyBind("T".ToCharArray()[0],
                    KeyBindType.Press)));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWH" + Id, "Use W").SetValue(false));
            config.AddItem(
                new MenuItem("UseQTH" + Id, "Use Q (Toggle)").SetValue(new KeyBind("H".ToCharArray()[0],
                    KeyBindType.Toggle))).Permashow(true, "Marksman | Toggle Q");

            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true,
                    Color.FromArgb(100, 255, 0, 255))));
            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            return true;
        }
        public override bool JungleClearMenu(Menu config)
        {
            return true;
        }
    }
}
