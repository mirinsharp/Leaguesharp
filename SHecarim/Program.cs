using System;
using SAutoCarry.Champions;
using LeagueSharp;
using LeagueSharp.Common;

namespace SAutoCarry
{
    class Program
    {
        public static SCommon.PluginBase.Champion Champion; 
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != "Hecarim")
                return;

            Champion = new Hecarim();

            if (!Game.Version.StartsWith("6.2"))
                Game.PrintChat("Wrong game version");
        }
    }
}
