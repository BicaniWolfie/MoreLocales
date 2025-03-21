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
using static ReLogic.Graphics.DynamicSpriteFont;

namespace MoreLocales
{
	public class MoreLocales : Mod
	{
        private static ILHook hook;
        private static ILHook thaiFix;
        private static Hook thaiFixTest;
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

            MethodInfo drawDSF = typeof(DynamicSpriteFont).GetMethod("InternalDraw", BindingFlags.Instance | BindingFlags.NonPublic);
            
            /*
            ILHook newHook0 = new(drawDSF, FixThaiDiacritics);
            thaiFix = newHook0;
            thaiFix.Apply();
            */
            
            Hook newHook0 = new(drawDSF, FixThaiTest);
            thaiFixTest = newHook0;
            thaiFixTest.Apply();

            ExtraLocalesSupport.DoLoad();
        }
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetCharacterData")]
        public static extern SpriteCharacterData CallGetCharacterData(DynamicSpriteFont instance, char character);
        public delegate void InternalDraw_orig(DynamicSpriteFont self, string text, SpriteBatch spriteBatch, Vector2 startPosition, Color color, float rotation, Vector2 origin, ref Vector2 scale, SpriteEffects spriteEffects, float depth);
        private static void FixThaiTest(InternalDraw_orig orig, DynamicSpriteFont self, string text, SpriteBatch spriteBatch, Vector2 startPosition, Color color, float rotation, Vector2 origin, ref Vector2 scale, SpriteEffects spriteEffects, float depth)
        {
            // thank you chyattCBT for another flawless deobfuscationem

            // Create a transformation matrix to apply origin offset and rotation.
            Matrix matrix = Matrix.CreateTranslation((0f - origin.X) * scale.X, (0f - origin.Y) * scale.Y, 0f)
                            * Matrix.CreateRotationZ(rotation);

            Vector2 zero = Vector2.Zero; // Tracks current drawing position.
            Vector2 one = Vector2.One;   // Multiplier for flipping direction.
            bool flag = true;            // Indicates start of a new line.
            float x = 0f;                // Holds initial horizontal offset.

            Vector2 lastNonDiacriticZero = zero;

            // Adjust offsets if sprite effects like flipping are applied.
            bool flipHori = (spriteEffects & SpriteEffects.FlipHorizontally) == SpriteEffects.FlipHorizontally;
            bool flipVert = (spriteEffects & SpriteEffects.FlipVertically) == SpriteEffects.FlipVertically;
            if (spriteEffects != SpriteEffects.None)
            {
                Vector2 vector = self.MeasureString(text);
                if (flipHori)
                {
                    x = vector.X * scale.X;
                    one.X = -1f; // Reverse X direction.
                }

                if (flipVert)
                {
                    zero.Y = (vector.Y - (float)self.LineSpacing) * scale.Y;
                    one.Y = -1f; // Reverse Y direction.
                }
            }
            zero.X = x;

            // Process each character.
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\n':
                        zero.X = x; // Reset X to initial offset.
                        zero.Y += (float)self.LineSpacing * scale.Y * one.Y; // Move to next line.
                        flag = true;
                        continue;
                    case '\r':
                        continue;
                }

                // Check if the character is a Thai diacritic.
                bool isDiacritic = FontHelper.IsThaiDiacritic(c);

                // Retrieve character drawing data (kerning, padding, glyph, texture).
                SpriteCharacterData characterData = CallGetCharacterData(self, c);
                Vector3 kerning = characterData.Kerning;
                Rectangle padding = characterData.Padding;

                // Adjust padding if sprite is flipped.
                if (flipHori)
                    padding.X -= padding.Width;

                if (flipVert)
                    padding.Y = self.LineSpacing - characterData.Glyph.Height - padding.Y;

                // Only update the horizontal position for non-diacritic characters.
                if (!isDiacritic)
                {
                    if (flag)
                        // For the first character in a line, ensure non-negative kerning.
                        kerning.X = Math.Max(kerning.X, 0f);
                    else
                        // Apply extra spacing between characters.
                        zero.X += self.CharacterSpacing * scale.X * one.X;

                    // Advance position by the kerning's starting offset.
                    zero.X += kerning.X * scale.X * one.X;

                    //
                    lastNonDiacriticZero = zero;
                }

                // Calculate the drawing position for this character.
                Vector2 position = isDiacritic ? lastNonDiacriticZero : zero;
                position.X += (float)padding.X * scale.X;
                position.Y += (float)padding.Y * scale.Y;
                Vector2.Transform(ref position, ref matrix, out position);
                position += startPosition;

                // Draw the character.
                spriteBatch.Draw(characterData.Texture, position, characterData.Glyph, color, rotation, Vector2.Zero, scale, spriteEffects, depth);

                // For non-diacritic characters, advance the position by the trailing kerning.
                if (!isDiacritic)
                    zero.X += (kerning.Y + kerning.Z) * scale.X * one.X;

                // Diacritics do not advance the position so they are drawn over the previous character.
                flag = false;
            }
        }
        // TODO: Why doesn't this do anything I'm gonna cry
        private static void FixThaiDiacritics(ILContext il)
        {
            // this is so utterly fucked,,,
            Mod mod = ModContent.GetInstance<MoreLocales>();
            try
            {
                var isThaiDiacritic = new VariableDefinition(il.Import(typeof(bool)));
                var lastNonDiacriticZero = new VariableDefinition(il.Import(typeof(Vector2)));

                il.Body.Variables.Add(isThaiDiacritic);
                il.Body.Variables.Add(lastNonDiacriticZero);

                var c = new ILCursor(il);

                int charVariable = 0;

                if (!c.TryGotoNext(i => i.MatchLdarg0(), i => i.MatchLdloc(out charVariable), i => i.MatchCall<DynamicSpriteFont>("GetCharacterData")))
                {
                    mod.Logger.Warn("FixThaiDiacritics: Couldn't find start of individual character drawing");
                    return;
                }

                c.EmitLdloc(charVariable);

                c.EmitDelegate(FontHelper.IsThaiDiacritic);

                c.EmitStloc(isThaiDiacritic.Index);

                if (!c.TryGotoNext(i => i.MatchLdloc3()))
                {
                    mod.Logger.Warn("FixThaiDiacritics: Couldn't find start of character final horizontal position calculation for branching");
                    return;
                }

                var skipLabel0 = il.DefineLabel();

                c.EmitLdloc(isThaiDiacritic.Index);

                c.EmitBrtrue(skipLabel0);

                // before searching for the branch target, let's find the index for the zero variable first.
                int zero = 0;
                if (!c.TryGotoNext(i => i.MatchLdloca(out zero), i => i.MatchLdflda<Vector2>("X")))
                {
                    mod.Logger.Warn("FixThaiDiacritics: Couldn't find index for variable 'zero'");
                    return;
                }

                if (!c.TryGotoNext(i => i.MatchLdloc1()))
                {
                    mod.Logger.Warn("FixThaiDiacritics: Couldn't find branch 0 target");
                    return;
                }

                // in addition to branching, we can set our variable here.

                c.EmitLdloca(zero);
                c.EmitStloc(lastNonDiacriticZero.Index);

                c.MarkLabel(skipLabel0);

                // alright, now we can set the position conditionally based on whether or not the current character is a diacritic.

                int position = 0;
                if (!c.TryGotoNext(MoveType.After, i => i.MatchStloc(out position)))
                {
                    mod.Logger.Warn("FixThaiDiacritics: Couldn't find setting of position for branching");
                    return;
                }

                var skipLabel2 = il.DefineLabel();

                c.EmitLdloc(isThaiDiacritic.Index);

                c.EmitBrfalse(skipLabel2);

                c.EmitLdloc(lastNonDiacriticZero.Index);
                c.EmitStloc(position);

                if (!c.TryGotoNext(i => i.MatchLdloca(out _)))
                {
                    mod.Logger.Warn("FixThaiDiacritics: Couldn't find branch 2 target");
                    return;
                }

                c.MarkLabel(skipLabel2);

                // now, diacritics will draw in the correct place. however, now we have another problem. the current position will still advance unless we also intercept that. so let's do it.

                if (!c.TryGotoNext(i => i.MatchCallvirt(out _)))
                {
                    mod.Logger.Warn("FixThaiDiacritics: Couldn't find draw call");
                    return;
                }
                if (!c.TryGotoNext(i => i.MatchLdloca(out _)))
                {
                    mod.Logger.Warn("FixThaiDiacritics: Couldn't find start of position advancement for branching");
                    return;
                }

                var skipLabel1 = il.DefineLabel();

                c.EmitLdloc(isThaiDiacritic.Index);

                c.EmitBrtrue(skipLabel1);

                if (!c.TryGotoNext(i => i.MatchLdcI4(0)))
                {
                    mod.Logger.Warn("FixThaiDiacritics: Couldn't find branch 1 target");
                    return;
                }

                c.MarkLabel(skipLabel1);
            }
            catch
            {
                MonoModHooks.DumpIL(mod, il);
            }
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
            thaiFix?.Dispose();
            thaiFixTest?.Dispose();
        }
    }
}
