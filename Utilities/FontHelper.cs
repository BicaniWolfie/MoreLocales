using MoreLocales.Core;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ID;

namespace MoreLocales.Utilities
{
    /// <summary>
    /// For CombatText, 0 is regular combat text (small), and 1 is crit combat text (larger)
    /// </summary>
    public readonly struct GameFonts(Asset<DynamicSpriteFont> itemStack, Asset<DynamicSpriteFont> mouseText, Asset<DynamicSpriteFont> deathText, Asset<DynamicSpriteFont>[] combatText)
    {
        public readonly Asset<DynamicSpriteFont> ItemStack = itemStack;
        public readonly Asset<DynamicSpriteFont> MouseText = mouseText;
        public readonly Asset<DynamicSpriteFont> DeathText = deathText;
        public readonly Asset<DynamicSpriteFont>[] CombatText = combatText;
        public readonly bool IsLoaded => ItemStack.IsLoaded && MouseText.IsLoaded && DeathText.IsLoaded && CombatText[0].IsLoaded && CombatText[1].IsLoaded;
        public static Asset<DynamicSpriteFont> GetFont(string name, AssetRequestMode mode = AssetRequestMode.ImmediateLoad) => ModContent.Request<DynamicSpriteFont>($"MoreLocales/Assets/Fonts/{name}", mode);
        public static GameFonts Create(string id, AssetRequestMode mode = AssetRequestMode.AsyncLoad)
        {
            return new
            (
                GetFont($"ItemStack-{id}", mode), GetFont($"MouseText-{id}", mode), GetFont($"DeathText-{id}", mode), [GetFont($"CombatText-{id}", mode), GetFont($"CritText-{id}", mode)]
            );
        }
        /// <summary>
        /// Forces the already loading font assets to load as if using <see cref="AssetRequestMode.ImmediateLoad"/>
        /// </summary>
        public void Nudge()
        {
            if (ItemStack.State == AssetState.Loading)
                ItemStack.Wait();
            if (MouseText.State == AssetState.Loading)
                MouseText.Wait();
            if (DeathText.State == AssetState.Loading)
                DeathText.Wait();
            for (int i = 0; i < CombatText.Length; i++)
            {
                var combatText = CombatText[i];
                if (combatText.State == AssetState.Loading)
                    combatText.Wait();
            }
        }
    }
    public static class FontHelper
    {
        private static readonly GameFonts defaultFonts;
        private static GameFonts japaneseFonts;
        private static GameFonts koreanFonts;
        private static GameFonts thaiFonts;
        private static GameFonts vietnameseFonts;
        private static bool forcedFont = false;
        public static bool usingLocalizedFont = false;
        public static LocalizedFont currentLocalizedFont = LocalizedFont.None;
        static FontHelper()
        {
            if (Main.dedServ)
                return;

            defaultFonts = new
            (
                FontAssets.ItemStack, FontAssets.MouseText, FontAssets.DeathText, FontAssets.CombatText
            );
        }
        public static void InitLocalizedFonts()
        {
            if (Main.dedServ)
                return;

            japaneseFonts = GameFonts.Create("JP");
            koreanFonts = GameFonts.Create("KR");
            thaiFonts = GameFonts.Create("TH");
            vietnameseFonts = GameFonts.Create("VN");
        }
        public static void ResetFont(bool bypassForced = false)
        {
            if (Main.dedServ)
                return;
            if (forcedFont && !bypassForced)
                return;

            usingLocalizedFont = false;

            SwitchFontInner(defaultFonts);
        }
        public static void SwitchFont(LocalizedFont font, bool setAsForcedFont = false)
        {
            if (Main.dedServ)
                return;
            if (font == LocalizedFont.None)
            {
                forcedFont = false;
                return;
            }
            if (forcedFont)
                return;

            if (!usingLocalizedFont && font == LocalizedFont.Default)
                return;

            GameFonts target = font switch
            {
                LocalizedFont.Japanese => japaneseFonts,
                LocalizedFont.Korean => koreanFonts,
                LocalizedFont.Thai => thaiFonts,
                LocalizedFont.Vietnamese => vietnameseFonts,
                _ => defaultFonts
            };

            usingLocalizedFont = font > LocalizedFont.Default;

            if (usingLocalizedFont)
                currentLocalizedFont = font;

            SwitchFontInner(target);

            forcedFont = setAsForcedFont;
        }
        private static void SwitchFontInner(GameFonts target)
        {
            target.Nudge();

            FontAssets.ItemStack = target.ItemStack;
            FontAssets.MouseText = target.MouseText;
            FontAssets.DeathText = target.DeathText;
            FontAssets.CombatText = target.CombatText;
        }
        public static bool? LocalizedFontAvailable(this CultureNamePlus culture)
        {
            LocalizedFont font = culture.GetLocalizedFont();
            if (font == LocalizedFont.None)
                return null;
            return true;
        }
        public static bool IsUsingAppropriateFont(GameCulture culture)
        {
            if (culture.TryGetLocalizedFont(out LocalizedFont font))
                return IsUsingFont(font);
            return false;
        }
        public static bool IsUsingFont(LocalizedFont font)
        {
            if (currentLocalizedFont == font)
                return usingLocalizedFont;
            return false;
        }
        public static LocalizedFont GetLocalizedFont(this CultureNamePlus culture)
        {
            return culture switch
            {
                CultureNamePlus.Japanese => LocalizedFont.Japanese,
                CultureNamePlus.Korean => LocalizedFont.Korean,
                CultureNamePlus.Thai => LocalizedFont.Thai,
                CultureNamePlus.Vietnamese => LocalizedFont.Vietnamese,
                _ => LocalizedFont.None
            };
        }
        public static bool IsThaiDiacritic(char c) => c == '\u0E31' || (c >= '\u0E34' && c <= '\u0E3A') || (c >= '\u0E47' && c <= '\u0E4E');
    }
}
