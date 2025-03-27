using ReLogic.Content;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using static ReLogic.Graphics.DynamicSpriteFont;

namespace MoreLocales.Core
{
    /// <summary>
    /// Contains information for a child font of a specific <see cref="Asset{T}"/> of type <see cref="DynamicSpriteFont"/>
    /// </summary>
    /// <param name="Font">The child font</param>
    /// <param name="OverrideParent">Whether or not this font should override the parent font's character if the parent font contains that character</param>
    public readonly record struct ChildFont(Asset<DynamicSpriteFont> Font, Func<bool> OverrideParent = null);
    public readonly struct ChildFontData(ChildFont[] children) : IEnumerable<ChildFont>
    {
        public readonly ChildFont[] Children = children;
        public readonly void Nudge()
        {
            for (int i = 0; i < Children.Length; i++)
            {
                var child = Children[i];

                if (!child.Font.IsLoaded)
                    child.Font.Wait();
            }
        }
        public static ChildFontData Create(string[] fileNames, Func<bool>[] overrideConds)
        {
            if (fileNames.Length != overrideConds.Length)
                throw new ArgumentException("FileNames and OverrideConds params must be the same length");

            ChildFont[] children = new ChildFont[fileNames.Length];

            for (int i = 0; i < children.Length; i++)
            {
                children[i] = new(ModContent.Request<DynamicSpriteFont>($"MoreLocales/Assets/Fonts/{fileNames[i]}"), overrideConds[i]);
            }

            return new ChildFontData(children);
        }

        public IEnumerator<ChildFont> GetEnumerator()
        {
            return ((IEnumerable<ChildFont>)Children).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Children.GetEnumerator();
        }
    }
    public class FontHelperV2
    {
        /// <summary>
        /// I'm DEAD my guy
        /// </summary>
        public static ChildFontData GetChildren(DynamicSpriteFont source)
        {
            if (source == FontAssets.ItemStack.Value)
                return ItemStackChildren;
            else if (source == FontAssets.MouseText.Value)
                return MouseTextChildren;
            else if (source == FontAssets.DeathText.Value)
                return DeathTextChildren;
            else if (source == FontAssets.CombatText[0].Value)
                return CombatTextChildren;
            else if (source == FontAssets.CombatText[1].Value)
                return CritTextChildren;
            return default;
        }

        public static ChildFontData ItemStackChildren;
        public static ChildFontData MouseTextChildren;
        public static ChildFontData DeathTextChildren;
        public static ChildFontData CombatTextChildren;
        public static ChildFontData CritTextChildren;

        private const string itemStackName = "ItemStack-";
        private const string mouseTextName = "MouseText-";
        private const string deathTextName = "DeathText-";
        private const string combatTextName = "CombatText-";
        private const string critTextName = "CritText-";

        public static LocalizedFont ForcedFont { get; set; } = LocalizedFont.None;

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_spriteCharacters")]
        public static extern ref Dictionary<char, SpriteCharacterData> GetSpriteCharacters(DynamicSpriteFont instance);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_defaultCharacterData")]
        public static extern ref SpriteCharacterData GetDefaultCharacterData(DynamicSpriteFont instance);

        public delegate bool IsCharacterSupported_orig(DynamicSpriteFont self, char character);
        public delegate SpriteCharacterData GetCharacterData_orig(DynamicSpriteFont self, char character);

        public static MethodInfo isCharSupported;
        public static MethodInfo getCharData;
        public static MethodInfo internalDraw;

        public static void DoLoad()
        {
            static bool japaneseActive() => CultureHelper.CustomCultureActive(CultureNamePlus.Japanese);
            static bool koreanActive() => CultureHelper.CustomCultureActive(CultureNamePlus.Korean);

            Func<bool>[] commonOverrideConds =
            [
                japaneseActive,
                koreanActive,
                null,
                null
            ];

            string[] itemStackChildrenNames =
            [
                $"{itemStackName}JP",
                $"{itemStackName}KR",
                $"{itemStackName}TH",
                $"{itemStackName}VN"
            ];
            string[] mouseTextChildrenNames =
            [
                $"{mouseTextName}JP",
                $"{mouseTextName}KR",
                $"{mouseTextName}TH",
                $"{mouseTextName}VN"
            ];
            string[] deathTextChildrenNames =
            [
                $"{deathTextName}JP",
                $"{deathTextName}KR",
                $"{deathTextName}TH",
                $"{deathTextName}VN"
            ];
            string[] combatTextChildrenNames =
            [
                $"{combatTextName}JP",
                $"{combatTextName}KR",
                $"{combatTextName}TH",
                $"{mouseTextName}VN" // yes this is on purpose
            ];
            string[] critTextChildrenNames =
            [
                $"{critTextName}JP",
                $"{critTextName}KR",
                $"{critTextName}TH",
                $"{critTextName}VN"
            ];

            ItemStackChildren = ChildFontData.Create(itemStackChildrenNames, commonOverrideConds);
            MouseTextChildren = ChildFontData.Create(mouseTextChildrenNames, commonOverrideConds);
            DeathTextChildren = ChildFontData.Create(deathTextChildrenNames, commonOverrideConds);
            CombatTextChildren = ChildFontData.Create(combatTextChildrenNames, commonOverrideConds);
            CritTextChildren = ChildFontData.Create(critTextChildrenNames, commonOverrideConds);

            Type dsf = typeof(DynamicSpriteFont);

            isCharSupported = dsf.GetMethod("IsCharacterSupported");
            getCharData = dsf.GetMethod("GetCharacterData", BindingFlags.Instance | BindingFlags.NonPublic);
            internalDraw = dsf.GetMethod("InternalDraw", BindingFlags.Instance | BindingFlags.NonPublic);

            MonoModHooks.Add(isCharSupported, OnIsCharacterSupported);
            MonoModHooks.Add(getCharData, OnGetCharacterData);
            MonoModHooks.Modify(internalDraw, EditInternalDraw);
        }

        private static bool OnIsCharacterSupported(IsCharacterSupported_orig orig, DynamicSpriteFont self, char character)
        {
            if (character != '\n' && character != '\r')
                return GetSpriteCharacters(self).ContainsKey(character) || GetChildren(self).Any(c => GetSpriteCharacters(c.Font.Value).ContainsKey(character));
            return true;
        }
        private static SpriteCharacterData OnGetCharacterData(GetCharacterData_orig orig, DynamicSpriteFont self, char character)
        {
            ChildFont[] children = GetChildren(self).Children;

            if (GetSpriteCharacters(self).TryGetValue(character, out var value))
            {
                for (int i = 0; i < children.Length; i++)
                {
                    ChildFont c = children[i];

                    if (c.OverrideParent is null || !c.OverrideParent())
                        continue;

                    Asset<DynamicSpriteFont> font = c.Font;

                    if (!font.IsLoaded)
                        font.Wait();

                    if (GetSpriteCharacters(font.Value).TryGetValue(character, out var overriddenValue))
                        return overriddenValue;
                }
                return value;
            }
            else
            {
                for (int i = 0; i < children.Length; i++)
                {
                    ChildFont c = children[i];

                    Asset<DynamicSpriteFont> font = c.Font;

                    if (!font.IsLoaded)
                        font.Wait();

                    if (GetSpriteCharacters(font.Value).TryGetValue(character, out var extendedValue))
                        return extendedValue;
                }
            }

            return GetDefaultCharacterData(self);
        }
        private static void EditInternalDraw(ILContext il)
        {
            Main.NewText("asf3d");

            var c = new ILCursor(il);

            if (!c.TryFindNext(out _, i => i.MatchCall(getCharData)))
            {
                System.Windows.Forms.MessageBox.Show("Method was inlined on this compilation", "Yep");
                return;
            }
        }
    }
}
