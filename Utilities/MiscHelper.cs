using System;

namespace MoreLocales.Utilities
{
    public class MiscHelper
    {
        // lol. xd, even.
        public static Color LerpMany(float amount, ReadOnlySpan<Color> values)
        {
            float p = MathHelper.Clamp(amount * (values.Length - 1), 0, values.Length - 1);
            int start = (int)p;
            int end = Math.Min(start + 1, values.Length - 1);
            return Color.Lerp(values[start], values[end], p - start);
        }
    }
}
