/*
 * Copyright (C) 2025 qAngel
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, see <https://www.gnu.org/licenses/>.
 */

global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using Mono.Cecil.Cil;
global using MonoMod.Cil;
global using MonoMod.RuntimeDetour;
global using MoreLocales.Core;
global using MoreLocales.Utilities;
global using ReLogic.Graphics;
global using Terraria.ModLoader;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Terraria.Localization;

namespace MoreLocales
{
	public class MoreLocales : Mod
	{
        private static ILHook hook;
        public override void PostSetupContent()
        {
            ExtraLocalesSupport.cachedVanillaCulture = LanguageManager.Instance.ActiveCulture.LegacyId;
            ExtraLocalesSupport.LoadCustomCultureData();

            if (FontHelperV2.CharDataInlined)
                MessageBox.Show(Language.GetTextValue("Mods.MoreLocales.Misc.Error.FontPatchingError"), Language.GetTextValue("Error.Error"));
        }
        public override void Load()
        {
            FontHelperV2.DoLoad();

            Type[] mParams =
            [
                typeof(Mod),
                typeof(string),
                typeof(GameCulture)
            ];
            MethodInfo peskyLegacyMarker = typeof(LocalizationLoader).GetMethod("UpdateLocalizationFilesForMod", BindingFlags.Static | BindingFlags.NonPublic, mParams);
            ILHook newHook = new(peskyLegacyMarker, FixPeskyLegacyMarking);
            hook = newHook;
            hook.Apply();

            ExtraLocalesSupport.DoLoad();
        }
        private static void FixPeskyLegacyMarking(ILContext il)
        {
            Mod mod = ModContent.GetInstance<MoreLocales>();
            try
            {
                var c = new ILCursor(il);

                MethodInfo move = typeof(File).GetMethod("Move", [typeof(string), typeof(string)]);
                PropertyInfo getTMLprop = typeof(Logging).GetProperty("tML", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo getTML = getTMLprop.GetGetMethod(true);

                if (!c.TryGotoNext(i => i.MatchCall(getTML)))
                {
                    mod.Logger.Warn("FixPeskyLegacyMarking: Couldn't find start of legacy marking");
                    return;
                }

                var skipLabel = il.DefineLabel();

                c.EmitLdarg0();

                c.EmitDelegate<Func<Mod, bool>>(m =>
                {
                    return m.Name == ModContent.GetInstance<MoreLocales>().Name;
                });

                c.EmitBrtrue(skipLabel);

                if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(move)))
                {
                    mod.Logger.Warn("FixPeskyLegacyMarking: Couldn't find branch target");
                    return;
                }

                c.MarkLabel(skipLabel);
            }
            catch
            {
                MonoModHooks.DumpIL(mod, il);
            }
        }
        public override void Unload()
        {
            ExtraLocalesSupport.DoUnload();
            hook?.Dispose();
        }
    }
}
