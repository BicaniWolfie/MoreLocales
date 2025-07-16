using Terraria.ID;

namespace MoreLocales.Core
{
    /// <summary>
    /// sorry nothing
    /// </summary>
    public static class MoreLocalesSets
    {
        internal static bool _contentReady = false;
        internal static bool _commentsAdded = false;
        internal static readonly InflectionData[] CachedInflectionData = ItemID.Sets.Factory.CreateCustomSet(InflectionData.Default);
        internal static void ReloadedLocalizations()
        {
            // AddComment actually causes files to be reloaded so i need to take that into account
            if (!_contentReady)
                return;
            for (int i = 1; i < CachedInflectionData.Length; i++)
            {
                CachedInflectionData[i] = LangFeaturesPlus.GetItemInflection(i, !_commentsAdded);
            }
            // auto add prefix keys too
            for (int i = 1; i < PrefixLoader.PrefixCount; i++) // start from 1 cuz 0 means no prefix
            {
                LangFeaturesPlus.EnsureKeysForPrefixExist(i, !_commentsAdded);
            }
            if (!_commentsAdded)
                _commentsAdded = true;
        }
    }
}
