using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Champions;

namespace Marksman.Common
{
    internal static class CommonSettings
    {
        public static Menu MenuLocal { get; private set; }
        private static Menu MenuCastSettings { get; set; }
        private static Menu MenuHitchanceSettings { get; set; }
        public static void Init(Menu nParentMenu)
        {
            MenuLocal = new Menu("Settings", "Settings");
            nParentMenu.AddSubMenu(MenuLocal);

            MenuCastSettings = new Menu("Spell Cast:", "MenuSettings.CastDelay");
            {
                string[] strQ = new string[1000/250];
                for (float i = 250; i <= 1000; i += 250)
                {
                    strQ[(int) (i/250 - 1)] = (i/1000) + " sec. ";
                }
                MenuCastSettings.AddItem(new MenuItem("Settings.SpellCast.VisibleDelay", "Cast Delay: Instatly Visible Enemy").SetValue(new StringList(strQ, 2))).SetTooltip("Exp: Rengar / Shaco / Wukong / Kha'Zix / Vayne / Enemy Ganker from the bush");
                MenuCastSettings.AddItem(new MenuItem("Settings.SpellCast.Default", "Load Recommended Settings").SetValue(true)).SetFontStyle(FontStyle.Bold, SharpDX.Color.Wheat)
                    .ValueChanged += (sender, args) =>
                    {
                        if (args.GetNewValue<bool>() == true)
                        {
                            LoadDefaultCastDelaySettings();
                        }
                    };
                MenuLocal.AddSubMenu(MenuCastSettings);
            }

            MenuHitchanceSettings = new Menu("Hitchance:", "MenuSettings.Hitchance");
            {
                string[] nHitchanceList = new[] { "Medium", "High", "VeryHigh" }; 

                MenuItem qHitchanceSettings = new MenuItem("MenuSettings.Hitchance.Q", "Q Hitchance:").SetValue(new StringList(nHitchanceList, 1));
                MenuHitchanceSettings.AddItem(qHitchanceSettings);

                MenuItem wHitchanceSettings = new MenuItem("MenuSettings.Hitchance.W", "W Hitchance:").SetValue(new StringList(nHitchanceList, 1));
                MenuHitchanceSettings.AddItem(wHitchanceSettings);

                MenuItem eHitchanceSettings = new MenuItem("MenuSettings.Hitchance.E", "E Hitchance:").SetValue(new StringList(nHitchanceList, 1));
                MenuHitchanceSettings.AddItem(eHitchanceSettings);

                MenuItem rHitchanceSettings = new MenuItem("MenuSettings.Hitchance.R", "R Hitchance:").SetValue(new StringList(nHitchanceList, 1));
                MenuHitchanceSettings.AddItem(rHitchanceSettings);

                MenuLocal.AddSubMenu(MenuHitchanceSettings);
            }
        }

        static void LoadDefaultCastDelaySettings()
        {
            string[] strQ = new string[1000 / 250];
            //for (var i = 250; i <= 1000; i += 250)
            //{
            //    str[i / 250 - 1] = i + " ms. ";
            //}
            for (float i = 250; i <= 1000; i += 250)
            {
                strQ[(int)(i / 250 - 1)] = (i / 100) + " sec. ";
            }
            MenuCastSettings.Item("Settings.SpellCast.VisibleDelay").SetValue(new StringList(strQ, 2));
            //MenuSettingQ.Item("Settings.SpellCast.Clone").SetValue(new StringList(new[] {"Off", "Cast Q", "Cast W", "Cast E"}, 3));
        }

        public static HitChance GetHitchance(this Spell nSpell)
        {
            HitChance[] hitChances = new[] { HitChance.Medium, HitChance.High, HitChance.VeryHigh};
            return hitChances[MenuHitchanceSettings.Item("MenuSettings.Hitchance." + nSpell.Slot).GetValue<StringList>().SelectedIndex];
        }
    }
}
