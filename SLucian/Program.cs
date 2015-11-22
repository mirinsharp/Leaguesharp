﻿using System;
using SAutoCarry.Champions;
using LeagueSharp;
using LeagueSharp.Common;

namespace SAutoCarry
{
    class Program
    {
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.CharData.BaseSkinName != "Lucian")
                return;

            new Lucian();
        }
    }
}
