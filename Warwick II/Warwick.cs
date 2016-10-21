#region
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Warwick.Common;
using SharpDX;
using Color = System.Drawing.Color;
using Geometry = Warwick.Common.CommonGeometry;

#endregion

namespace Warwick
{
    internal class Warwick
    {
        public static string ChampionName => "Warwick";
        public static void Init()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.CharData.BaseSkinName != ChampionName)
            {
                return;
            }

            Champion.PlayerSpells.Init();
            Modes.ModeConfig.Init();
            Common.CommonItems.Init();

            Game.PrintChat(
                "<font color='#ff3232'>Successfully Loaded: </font><font color='#d4d4d4'><font color='#FFFFFF'>" +
                ChampionName + "</font>");

            Console.Clear();
        }
    }
}