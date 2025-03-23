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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreLocales.Core;
using MoreLocales.Utilities;
using ReLogic.Graphics;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using static ReLogic.Graphics.DynamicSpriteFont;

namespace MoreLocales
{
	public class MoreLocales : Mod
	{
        private static ILHook hook;
        public override void PostSetupContent()
        {
            FontHelper.InitLocalizedFonts();
            ExtraLocalesSupport.cachedVanillaCulture = LanguageManager.Instance.ActiveCulture.LegacyId;
            ExtraLocalesSupport.LoadCustomCultureData();
        }
        public override void Load()
        {
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

                if (!c.TryGotoNext
                (
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdloc(out _),
                    i => i.MatchCall(move)
                ))
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
            FontHelper.ResetFont(true);
            ExtraLocalesSupport.DoUnload();
            hook?.Dispose();
        }
    }
}
