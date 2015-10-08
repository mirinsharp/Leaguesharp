using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using ShineCommon;

namespace ShineCommon
{
    public static class Utility
    {
        #region Champion Priority Arrays
        public static string[] lowPriority =
            {
                "Alistar", "Amumu", "Bard", "Blitzcrank", "Braum", "Cho'Gath", "Dr. Mundo", "Garen", "Gnar",
                "Hecarim", "Janna", "Jarvan IV", "Leona", "Lulu", "Malphite", "Nami", "Nasus", "Nautilus", "Nunu",
                "Olaf", "Rammus", "Renekton", "Sejuani", "Shen", "Shyvana", "Singed", "Sion", "Skarner", "Sona",
                "Soraka", "Tahm", "Taric", "Thresh", "Volibear", "Warwick", "MonkeyKing", "Yorick", "Zac", "Zyra"
            };

        public static string[] mediumPriority =
            {
                "Aatrox", "Akali", "Darius", "Diana", "Ekko", "Elise", "Evelynn", "Fiddlesticks", "Fiora", "Fizz",
                "Galio", "Gangplank", "Gragas", "Heimerdinger", "Irelia", "Jax", "Jayce", "Kassadin", "Kayle", "Kha'Zix",
                "Lee Sin", "Lissandra", "Maokai", "Mordekaiser", "Morgana", "Nocturne", "Nidalee", "Pantheon", "Poppy",
                "RekSai", "Rengar", "Riven", "Rumble", "Ryze", "Shaco", "Swain", "Trundle", "Tryndamere", "Udyr",
                "Urgot", "Vladimir", "Vi", "XinZhao", "Yasuo", "Zilean"
            };

        public static string[] highPriority =
            {
                "Ahri", "Anivia", "Annie", "Ashe", "Azir", "Brand", "Caitlyn", "Cassiopeia", "Corki", "Draven",
                "Ezreal", "Graves", "Jinx", "Kalista", "Karma", "Karthus", "Katarina", "Kennen", "KogMaw", "Leblanc",
                "Lucian", "Lux", "Malzahar", "MasterYi", "MissFortune", "Orianna", "Quinn", "Sivir", "Syndra", "Talon",
                "Teemo", "Tristana", "TwistedFate", "Twitch", "Varus", "Vayne", "Veigar", "VelKoz", "Viktor", "Xerath",
                "Zed", "Ziggs"
            };
        #endregion

        public static string[] HitchanceNameArray = { "Low", "Medium", "High", "Very High", "Only Immobile" };
        public static HitChance[] HitchanceArray = { HitChance.Low, HitChance.Medium, HitChance.High, HitChance.VeryHigh, HitChance.Immobile };

        private static string[] JungleMinions;

        static Utility()
        {
            if (LeagueSharp.Common.Utility.Map.GetMap().Type.Equals(LeagueSharp.Common.Utility.Map.MapType.TwistedTreeline))
            {
                JungleMinions = new string[] { "TT_Spiderboss", "TT_NWraith", "TT_NGolem", "TT_NWolf" };
            }
            else
            {
                JungleMinions = new string[]
                    {
                        "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Red", "SRU_Krug", "SRU_Dragon",
                        "SRU_Baron", "Sru_Crab"
                    };
            }
        }

        public static int GetPriority(string championName)
        {
            if (lowPriority.Contains(championName))
                return 1;

            if (mediumPriority.Contains(championName))
                return 2;

            if (highPriority.Contains(championName))
                return 3;

            return 2;
        }

        public static bool IsJungleMinion(this AttackableUnit unit)
        {
            return JungleMinions.Contains(unit.Name);
        }

        public static bool IsImmobilizeBuff(BuffType type)
        {
            return type == BuffType.Snare || type == BuffType.Stun || type == BuffType.Charm || type == BuffType.Knockup || type == BuffType.Suppression;
        }

        public static bool IsImmobileTarget(Obj_AI_Hero target)
        {
            return target.Buffs.Count(p => IsImmobilizeBuff(p.Type)) > 0 || target.IsChannelingImportantSpell();
        }

        public static bool IsActive(this Spell s)
        {
            return ObjectManager.Player.Spellbook.GetSpell(s.Slot).ToggleState == 2;
        }

        public static bool IsKillable(this Obj_AI_Hero t, double dmg)
        {
            return t.Health <= dmg;
        }

        public static async Task CastWithDelay(this Spell s, int delay)
        {
            System.Threading.Thread.Sleep(delay);
            s.Cast();
        }

        public static class DelayAction
        {
            public delegate void Callback();
                
            public static ConcurrentDictionary<string, Action> ActionList = new ConcurrentDictionary<string, Action>();

            static DelayAction()
            {
                Game.OnUpdate += GameOnOnGameUpdate;
            }

            private static void GameOnOnGameUpdate(EventArgs args)
            {
                for (var i = ActionList.Count - 1; i >= 0; i--)
                {
                    if (ActionList.ElementAt(i).Value.Time <= Utils.GameTimeTickCount)
                    {
                        try
                        {
                            if (ActionList.ElementAt(i).Value.CallbackObject != null)
                            {
                                ActionList.ElementAt(i).Value.CallbackObject();
                                //Will somehow result in calling ALL non-internal marked classes of the called assembly and causes NullReferenceExceptions.
                            }
                        }
                        catch (Exception)
                        {
                            // ignored
                        }

                        Action act;
                        ActionList.TryRemove(ActionList.Keys.ElementAt(i), out act); //remove
                    }
                }
            }

            public static void Add(string name, int time, Callback func)
            {
                var action = new Action(time, func);
                ActionList.TryAdd(name, action);
            }

            public static void Update(string name, int time)
            {
                Action act, act2;
                if (ActionList.TryGetValue(name, out act))
                {
                    act2 = new Action(time, act.CallbackObject);
                    ActionList.TryUpdate(name, act2, act);
                }
            }

            public static bool Exists(string name)
            {
                Action act;
                return ActionList.TryGetValue(name, out act);
            }

            public struct Action
            {
                public Callback CallbackObject;
                public int Time;

                public Action(int time, Callback callback)
                {
                    Time = time + Utils.GameTimeTickCount;
                    CallbackObject = callback;
                }
            }
        }

    }
}
