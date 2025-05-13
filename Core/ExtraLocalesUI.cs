﻿using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Microsoft.Xna.Framework.Input;
using System;
using MoreLocales.Common;
using System.Reflection;
using Terraria.ID;

namespace MoreLocales.Core
{
    public class ExtraLocalesUI : ModSystem
    {
        //private static bool testOverlap = false;
        public const int betterLangMenuID = 74592; //LANGS
        public static BetterLangMenuUI betterLangMenu = new();
        public override void Load()
        {
            IL_Main.DrawMenu += GoToBetterLangMenuInstead;
            //On_Main.DrawInterface += On_Main_DrawInterface;
        }
        private static void GoToBetterLangMenuInstead(ILContext il)
        {
            Mod mod = MoreLocales.Instance;
            try
            {
                var c = new ILCursor(il);

                if (!c.TryGotoNext(i => i.MatchLdcI4(1213), i => i.MatchStsfld<Main>("menuMode")))
                {
                    mod.Logger.Warn("GoToBetterLangMenuInstead: Couldn't find instruction for attempt to switch to lang menu");
                    return;
                }

                c.Next.Operand = betterLangMenuID;

                Type inter = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.Interface");
                
                if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(inter.GetMethod("ModLoaderMenus", BindingFlags.NonPublic | BindingFlags.Static))))
                {
                    mod.Logger.Warn("GoToBetterLangMenuInstead: Couldn't find instruction for attempt to enter modded menus");
                    return;
                }

                c.EmitDelegate(TryEnterBetterLangMenu);

            }
            catch
            {
                MonoModHooks.DumpIL(mod, il);
            }
        }

        private static void TryEnterBetterLangMenu()
        {
            if (Main.menuMode != betterLangMenuID)
                return;

            Main.MenuUI.SetState(betterLangMenu);
            Main.menuMode = MenuID.FancyUI;
        }
        #region DEBUGGING
        private static void On_Main_DrawInterface(On_Main.orig_DrawInterface orig, Main self, GameTime gameTime)
        {
            orig(self, gameTime);

            string desiredFont = "MoreLocales/Assets/Fonts/MouseText-TH";
            if (!ModContent.HasAsset(desiredFont))
            {
                Main.NewText("Asset not found");
                return;
            }

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            Asset<DynamicSpriteFont> testFont = ModContent.Request<DynamicSpriteFont>(desiredFont, AssetRequestMode.ImmediateLoad);

            Vector2 padding = new(128f);
            float yBetween = 32f;
            float xBetween = 559f;

            SpriteBatch sb = Main.spriteBatch;
            DynamicSpriteFont testVanilla = FontAssets.CombatText[1].Value;

            for (int i = 0; i < 4; i++)
            {
                string testString = i switch
                {
                    0 => "abc01234",
                    1 => "áêç",
                    2 => "бгд",
                    3 => "เกี๊ยว",
                    _ => ""
                };

                for (int j = 0; j < 2; j++)
                {
                    DynamicSpriteFont font = j == 0 ? testVanilla : testFont.Value;
                    sb.DrawString(font, testString, padding + new Vector2(j == 0 ? 0 : false ? 0 : xBetween, i * yBetween), Color.White);
                }
            }

            Main.spriteBatch.End();
        }

        public override void PostUpdateDusts()
        {
            return;

            if (Main.keyState.IsKeyUp(Keys.F) && !Main.oldKeyState.IsKeyUp(Keys.F))
            {
                string target = "pt-PT";
                if (LanguageManager.Instance.ActiveCulture.Name != target)
                    LanguageManager.Instance.SetLanguage(target);
                else
                    LanguageManager.Instance.SetLanguage("en-US");
                Main.NewText(LanguageManager.Instance.ActiveCulture.Name);
            }
        }
        #endregion
    }
}
