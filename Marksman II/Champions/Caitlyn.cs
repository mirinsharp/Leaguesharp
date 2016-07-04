#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Policy;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Common;
using Marksman.Utils;
using SharpDX;
using Color = System.Drawing.Color;
using Orbwalking = Marksman.Orb.Orbwalking;

#endregion

namespace Marksman.Champions
{
    using System.Linq;

    using Utils = LeagueSharp.Common.Utils;

    internal class Caitlyn : Champion
    {
        public static Spell R;

        public Spell E;

        public static Spell Q;

        public bool ShowUlt;

        public string UltTarget;

        public static Spell W;

        private bool canCastR = true;

        private static int LastCastWTick = 0;

       // private static bool headshotReady = ObjectManager.Player.Buffs.Any(buff => buff.DisplayName == "CaitlynHeadshotReady");

        private string[] dangerousEnemies = new[]
        {
            "Alistar", "Garen", "Zed", "Fizz", "Rengar", "JarvanIV", "Irelia", "Amumu", "DrMundo", "Ryze", "Fiora", "KhaZix", "LeeSin", "Riven",
            "Lissandra", "Vayne", "Lucian", "Zyra"
        };

        public Caitlyn()
        {
            Q = new Spell(SpellSlot.Q, 1240);
            W = new Spell(SpellSlot.W, 820);
            E = new Spell(SpellSlot.E, 800);
            R = new Spell(SpellSlot.R, 2000);

            Q.SetSkillshot(0.50f, 50f, 2000f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 60f, 1600f, true, SkillshotType.SkillshotLine);

            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnEndScene += DrawingOnOnEndScene;

            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (W.IsReady())
                    {
                        BuffInstance aBuff =
                            (from fBuffs in
                                 sender.Buffs.Where(
                                     s =>
                                     sender.Team != ObjectManager.Player.Team
                                     && sender.Distance(ObjectManager.Player.Position) < W.Range)
                             from b in new[]
                                           {
                                               "teleport", /* Teleport */ "pantheon_grandskyfall_jump", /* Pantheon */ 
                                               "crowstorm", /* FiddleScitck */
                                               "zhonya", "katarinar", /* Katarita */
                                               "MissFortuneBulletTime", /* MissFortune */
                                               "gate", /* Twisted Fate */
                                               "chronorevive" /* Zilean */
                                           }
                             where args.Buff.Name.ToLower().Contains(b)
                             select fBuffs).FirstOrDefault();

                        if (aBuff != null)
                        {
                            CastW(sender.Position);
                            //W.Cast(sender.Position);
                        }
                    }
                };

            Marksman.Utils.Utils.PrintMessage("Caitlyn loaded.");
        }

        public void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Program.Config.Item("Misc.AntiGapCloser").GetValue<bool>())
            {
                return;
            }

            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range))
            {
                E.Cast(gapcloser.Sender.Position);
            }
        }

        public override void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            //if (args.Slot == SpellSlot.W && LastCastWTick + 2000 > Utils.TickCount)
            //{
            //    args.Process = false;
            //}
            //else
            //{
            //    args.Process = true;
            //}

            //if (args.Slot == SpellSlot.Q)
            //{
            //    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && GetValue<bool>("UseQC"))
            //    {
            //        var t = TargetSelector.GetTarget(Q.Range - 20, TargetSelector.DamageType.Physical);
            //        if (!t.IsValidTarget())
            //        {
            //            args.Process = false;
            //        }
            //        else
            //        {
            //            args.Process = true;
            //            //CastQ(t);
            //        }
            //    }
            //}
        }
        public override void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

            if (args.Slot == SpellSlot.W)
            {
                LastCastWTick = Utils.TickCount;
            }

            if (sender.IsEnemy && sender is Obj_AI_Turret && args.Target.IsMe)
            {
                canCastR = false;
            }
            else
            {
                canCastR = true;
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {

            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                Render.Circle.DrawCircle(t.Position, 105f, Color.GreenYellow);

                var wcCenter = ObjectManager.Player.Position.Extend(t.Position,
                    ObjectManager.Player.Distance(t.Position)/2);

                Vector2 wcLeft = ObjectManager.Player.Position.To2D() +
                                 Vector2.Normalize(t.Position.To2D() - ObjectManager.Player.Position.To2D())
                                     .Rotated(ObjectManager.Player.Distance(t.Position) < 300
                                         ? 45
                                         : 37*(float) Math.PI/180)*ObjectManager.Player.Distance(t.Position)/2;

                Vector2 wcRight = ObjectManager.Player.Position.To2D() +
                                  Vector2.Normalize(t.Position.To2D() - ObjectManager.Player.Position.To2D())
                                      .Rotated(ObjectManager.Player.Distance(t.Position) < 300
                                          ? -45
                                          : -37*(float) Math.PI/180)*ObjectManager.Player.Distance(t.Position)/2;

                Render.Circle.DrawCircle(wcCenter, 50f, Color.Red);
                Render.Circle.DrawCircle(wcLeft.To3D(), 50f, Color.Green);
                Render.Circle.DrawCircle(wcRight.To3D(), 50f, Color.Yellow);
            }
            //var bx = HeroManager.Enemies.Where(e => e.IsValidTarget(E.Range * 3));
            //foreach (var n in bx)
            //{
            //    if (n.IsValidTarget(800) && ObjectManager.Player.Distance(n) < 450)
            //    {
            //        Vector3[] x = new[] { ObjectManager.Player.Position, n.Position };
            //        Vector2 aX =
            //            Drawing.WorldToScreen(new Vector3(CommonGeometry.CenterOfVectors(x).X,
            //                CommonGeometry.CenterOfVectors(x).Y, CommonGeometry.CenterOfVectors(x).Z));

            //        Render.Circle.DrawCircle(CommonGeometry.CenterOfVectors(x), 85f, Color.White );
            //        Drawing.DrawText(aX.X - 15, aX.Y - 15, Color.GreenYellow, n.ChampionName);
                    
            //    }
            //}

            //var enemies = HeroManager.Enemies.Where(e => e.IsValidTarget(1500));
            //var objAiHeroes = enemies as Obj_AI_Hero[] ?? enemies.ToArray();
            //IEnumerable<Obj_AI_Hero> nResult =
            //    (from e in objAiHeroes join d in dangerousEnemies on e.ChampionName equals d select e)
            //        .Distinct();

            //foreach (var n in nResult)
            //{
            //    var x = E.GetPrediction(n).CollisionObjects.Count;
            //    Render.Circle.DrawCircle(n.Position, (Orbwalking.GetRealAutoAttackRange(null) + 65) - 300, Color.GreenYellow);
            //}

            var nResult = HeroManager.Enemies.Where(e => e.IsValidTarget(E.Range - 200));
            foreach (var n in nResult.Where(n => n.IsFacing(ObjectManager.Player)))
            {
                if (n.IsValidTarget())
                {
                    Render.Circle.DrawCircle(n.Position, E.Range - 200, Color.GreenYellow, 1);
                }
            }
            
            Spell[] spellList = { Q, W, E, R };
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
                }
            }
        }

        static void CastQ(Obj_AI_Base t)
        {
            if (Q.CanCast(t))
            {
                var qPrediction = Q.GetPrediction(t);
                var hithere = qPrediction.CastPosition.Extend(ObjectManager.Player.Position, -100);

                if (qPrediction.Hitchance >= Q.GetHitchance())
                {
                    Q.Cast(hithere);
                }
            }
        }

        static void CastW(Vector3 pos, bool delayControl = true)
        {
            if (!W.IsReady())
            {
                return;
            }

            //if (headshotReady)
            //{
            //    return;
            //}

            if (delayControl && LastCastWTick + 2000 > Utils.TickCount)
            {
                return;
            }

            W.Cast(pos);
        }

        static void CastW2(Obj_AI_Base t)
        {
            if (t.IsValidTarget(W.Range))
            {
                BuffType[] buffList =
                {
                    BuffType.Fear,
                    BuffType.Taunt,
                    BuffType.Stun,
                    BuffType.Slow,
                    BuffType.Snare
                };

                foreach (var b in buffList.Where(t.HasBuffOfType))
                {
                    CastW(t.Position);
                }
            }
        }

        private static void DrawingOnOnEndScene(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
            {
                return;
            }

            var x = 0;
            foreach (var b in ObjectManager.Player.Buffs.Where(buff => buff.DisplayName == "CaitlynHeadshotCount"))
            {
                x = b.Count;
            }

            for (int i = 1; i < 7; i++)
            {
                CommonGeometry.DrawBox(new Vector2(ObjectManager.Player.HPBarPosition.X + 23 + (i * 17), ObjectManager.Player.HPBarPosition.Y + 25), 15, 4, Color.Transparent, 1, Color.Black);
            }
            var headshotReady = ObjectManager.Player.Buffs.Any(buff => buff.DisplayName == "CaitlynHeadshotReady");
            for (int i = 1; i < (headshotReady ? 7 : x + 1); i++)
            {
                CommonGeometry.DrawBox(new Vector2(ObjectManager.Player.HPBarPosition.X + 24 + (i * 17), ObjectManager.Player.HPBarPosition.Y + 26), 13, 3, headshotReady ? Color.Red : Color.LightGreen, 0, Color.Black);
            }

            var rCircle2 = Program.Config.Item("Draw.UltiMiniMap").GetValue<Circle>();
            if (rCircle2.Active)
            {
                #pragma warning disable 618
                Utility.DrawCircle(ObjectManager.Player.Position, R.Range, rCircle2.Color, 1, 23, true);
                #pragma warning restore 618
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            R.Range = 500 * (R.Level == 0 ? 1 : R.Level) + 1500;

            Obj_AI_Hero t;

            if (W.IsReady() && (GetValue<StringList>("AutoWI").SelectedIndex == 1 || (GetValue<StringList>("AutoWI").SelectedIndex == 2 && ComboActive)))
            {
                t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget(W.Range))
                {
                    if (t.HasBuffOfType(BuffType.Stun) || t.HasBuffOfType(BuffType.Snare) ||
                        t.HasBuffOfType(BuffType.Taunt) || t.HasBuffOfType(BuffType.Knockup) ||
                        t.HasBuff("zhonyasringshield") || t.HasBuff("Recall"))
                    {
                        CastW(t.Position);
                    }

                    if (t.HasBuffOfType(BuffType.Slow))
                    {
                        var hit = t.IsFacing(ObjectManager.Player)
                            ? t.Position.Extend(ObjectManager.Player.Position, +140)
                            : t.Position.Extend(ObjectManager.Player.Position, -140);
                        CastW(hit);
                    }
                }
            }

            if (Q.IsReady() && (GetValue<StringList>("AutoQI").SelectedIndex == 1 || (GetValue<StringList>("AutoQI").SelectedIndex == 2 && ComboActive)))
            {
                t = TargetSelector.GetTarget(Q.Range - 30, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget(Q.Range)
                    && (t.HasBuffOfType(BuffType.Stun) || t.HasBuffOfType(BuffType.Snare) || t.HasBuffOfType(BuffType.Taunt) || (t.Health <= ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q) && !Orb.Orbwalking.InAutoAttackRange(t))))
                {
                    CastQ(t);
                }
            }

            if (GetValue<KeyBind>("UseQMC").Active)
            {
                t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                CastQ(t);
            }

            //if (GetValue<KeyBind>("UseEMC").Active)
            //{
            //    t = TargetSelector.GetTarget(E.Range - 50, TargetSelector.DamageType.Physical);
            //    E.Cast(t);
            //}

            if (GetValue<KeyBind>("UseRMC").Active && R.IsReady())
            {
                foreach (var e in HeroManager.Enemies.Where(e => e.IsValidTarget(R.Range)).OrderBy(e => e.Health))
                {
                    R.CastOnUnit(e);
                }
            }

            if (GetValue<KeyBind>("UseEQC").Active && E.IsReady() && Q.IsReady())
            {
                t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget(E.Range)
                    && t.Health
                    < ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q)
                    + ObjectManager.Player.GetSpellDamage(t, SpellSlot.E) + 20 && E.CanCast(t))
                {
                    E.Cast(t);
                    CastQ(t);
                }
            }

            if ((!ComboActive && !HarassActive) || !Orb.Orbwalking.CanMove(100))
            {
                return;
            }

            //var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            var useW = GetValue<bool>("UseWC");
            var useE = GetValue<bool>("UseEC");
            var useR = GetValue<bool>("UseRC");

            //if (Q.IsReady() && useQ)
            //{
            //    t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            //    if (t != null)
            //    {
            //        CastQ(t);
            //    }
            //}

            if (useE && E.IsReady())
            {
                //var enemies = HeroManager.Enemies.Where(e => e.IsValidTarget(E.Range));
                //var objAiHeroes = enemies as Obj_AI_Hero[] ?? enemies.ToArray();
                //IEnumerable<Obj_AI_Hero> nResult =
                //    (from e in objAiHeroes join d in dangerousEnemies on e.ChampionName equals d select e)
                //        .Distinct();

                //foreach (var n in nResult.Where(n => n.IsFacing(ObjectManager.Player)))
                //{
                //    if (n.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65 - 300) && E.GetPrediction(n).CollisionObjects.Count == 0)
                //    {
                //        E.Cast(n.Position);
                //        if (W.IsReady())
                //            W.Cast(n.Position);
                //    }
                //}

                var nResult = HeroManager.Enemies.Where(e => e.IsValidTarget(E.Range));
                foreach (var n in nResult)
                {
                    if (n.IsValidTarget(n.IsFacing(ObjectManager.Player) ? E.Range - 200 : E.Range - 300) && E.GetPrediction(n).CollisionObjects.Count == 0)
                    {
                        E.Cast(n.Position);
                    }
                }
            }

            if (useW && W.IsReady())
            {
                var nResult = HeroManager.Enemies.Where(e => e.IsValidTarget(W.Range));
                foreach (var n in nResult)
                {
                    if (ObjectManager.Player.Distance(n) < 450 && n.IsFacing(ObjectManager.Player))
                    {
                        CastW(CommonGeometry.CenterOfVectors(new[] { ObjectManager.Player.Position, n.Position }));
                    }
                }
            }

            if (R.IsReady() && useR)
            {
                foreach (var e in HeroManager.Enemies.Where(e => e.IsValidTarget(R.Range) && e.Health <= R.GetDamage(e) && !Orb.Orbwalking.InAutoAttackRange(e) && canCastR))
                {
                    R.CastOnUnit(e);
                }
            }
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t == null || (!ComboActive && !HarassActive) || unit.IsMe) return;

            //var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            //if (useQ) Q.Cast(t, false, true);

            base.Orbwalking_AfterAttack(unit, target);
        }

        public override bool MainMenu(Menu config)
        {
            return base.MainMenu(config);
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Q:").SetValue(true)).SetFontStyle(FontStyle.Regular, Q.MenuColor());
            config.AddItem(new MenuItem("UseQMC" + Id, "Q: Semi-Manual").SetValue(new KeyBind("G".ToCharArray()[0],KeyBindType.Press))).SetFontStyle(FontStyle.Regular, Q.MenuColor()).Permashow();
            config.AddItem(new MenuItem("UseWC" + Id, "W:").SetValue(true)).SetFontStyle(FontStyle.Regular, W.MenuColor());
            config.AddItem(new MenuItem("UseEC" + Id, "E:").SetValue(true)).SetFontStyle(FontStyle.Regular, E.MenuColor());
            //config.AddItem(new MenuItem("UseEMC" + Id, "E: Semi-Manual").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Press))).SetFontStyle(FontStyle.Regular, E.MenuColor()).Permashow();
            config.AddItem(new MenuItem("UseRC" + Id, "R:").SetValue(true)).SetFontStyle(FontStyle.Regular, R.MenuColor());
            config.AddItem(new MenuItem("UseRMC" + Id, "R: Semi-Manual").SetValue(new KeyBind("R".ToCharArray()[0], KeyBindType.Press))).SetFontStyle(FontStyle.Regular, R.MenuColor()).Permashow();


            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(true));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(new MenuItem("Champion.Drawings", ObjectManager.Player.ChampionName + " Draw Options"));
            config.AddItem(new MenuItem("DrawQ" + Id, Marksman.Utils.Utils.Tab + "Q:").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(new MenuItem("DrawW" + Id, Marksman.Utils.Utils.Tab + "W:").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(new MenuItem("DrawE" + Id, Marksman.Utils.Utils.Tab + "E:").SetValue(new Circle(false, Color.FromArgb(100, 255, 255, 255))));
            config.AddItem(new MenuItem("DrawR" + Id, Marksman.Utils.Utils.Tab + "R:").SetValue(new Circle(false, Color.FromArgb(100, 255, 255, 255))));
            config.AddItem(new MenuItem("Draw.UltiMiniMap", Marksman.Utils.Utils.Tab + "Draw Ulti Minimap").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("Misc.AntiGapCloser", "E Anti Gap Closer").SetValue(true));
            config.AddItem(new MenuItem("UseEQC" + Id, "Use E-Q Combo").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            config.AddItem(new MenuItem("Dash" + Id, "Dash to Mouse").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
            config.AddItem(new MenuItem("AutoQI" + Id, "Auto Q (Stun/Snare/Taunt/Slow)").SetValue(new StringList(new []{"Off", "On: Everytime", "On: Combo Mode"}, 2)));
            config.AddItem(new MenuItem("AutoWI" + Id, "Auto W (Stun/Snare/Taunt)").SetValue(new StringList(new[] { "Off", "On: Everytime", "On: Combo Mode" }, 2)));

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

        public override void ExecuteFlee()
        {
            if (E.IsReady())
            {
                var pos = Vector3.Zero;
                var enemy =
                    HeroManager.Enemies.FirstOrDefault(
                        e =>
                            e.IsValidTarget(E.Range +
                                            (ObjectManager.Player.MoveSpeed > e.MoveSpeed
                                                ? ObjectManager.Player.MoveSpeed - e.MoveSpeed
                                                : e.MoveSpeed - ObjectManager.Player.MoveSpeed)) && E.CanCast(e));

                pos = enemy?.Position ??
                      ObjectManager.Player.ServerPosition.To2D().Extend(Game.CursorPos.To2D(), -300).To3D();
                //E.Cast(pos);
            }

            base.PermaActive();
        }
    }
}
