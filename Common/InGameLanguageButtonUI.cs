using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace MoreLocales.Common
{
    internal class InGameLanguageButtonUI : UIState
    {
        internal InGameLanguageButton button;
        internal static InGameLanguageButtonUI Instance;
        private static Point _lastDimensions;
        public InGameLanguageButtonUI()
        {
            Append(button = new());
            button.Width.Set(32f, 0f);
            button.Height.Set(32f, 0f);
        }
        public override void Update(GameTime gameTime)
        {
            //
            PlaceButtonCorrectly();
            Recalculate();
            //

            TryRecalculate();
            base.Update(gameTime);
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawChildren(spriteBatch);
        }
        private void TryRecalculate()
        {
            Point dimensions = new(Main.screenWidth, Main.screenHeight);
            if (dimensions != _lastDimensions)
            {
                PlaceButtonCorrectly();
                Recalculate();
            }
            _lastDimensions = dimensions;
        }
        private void PlaceButtonCorrectly()
        {
            float dim = button.Height.Pixels;
            button.Top.Set(Main.screenHeight - dim - 12, 0f);
            button.Left.Set(Main.screenWidth - 200 - dim, 0f);
        }
    }
    internal class InGameLanguageButton : UIElement
    {
        private static Asset<Texture2D> _modIcon;
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            bool hovered = ContainsPoint(Main.MouseScreen);
            _modIcon ??= MoreLocales.Instance.Assets.Request<Texture2D>("icon_small");

            if (hovered)
            {
                UICommon.TooltipMouseText(Lang.menu[103].Value);
                Main.LocalPlayer.mouseInterface = true;
            }

            spriteBatch.Draw(_modIcon.Value, GetDimensions().Center(), null, Color.White, 0f, _modIcon.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

        }
        public override void LeftClick(UIMouseEvent evt)
        {
            SoundEngine.PlaySound(in SoundID.MenuOpen);

            IngameFancyUI.OpenUIState(MoreLocalesSystem.betterLangMenu);
        }
    }
}
