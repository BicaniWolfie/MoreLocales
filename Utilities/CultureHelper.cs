using System;
using Terraria.Localization;
using static Terraria.Localization.GameCulture;
using static MoreLocales.Core.CultureNamePlus;

namespace MoreLocales.Utilities
{
    public static class CultureHelper
    {
        public static bool CustomCultureActive(CultureNamePlus customCulture) => LanguageManager.Instance.ActiveCulture.LegacyId == (int)customCulture;
        public static bool NeedsLocalizedTitle(string cultureKey) => Language.Exists($"{cultureKey}.LocalizedFont");
        public static string FullName(this GameCulture culture) => culture.IsCustom() ? ((CultureNamePlus)culture.LegacyId).ToString() : ((CultureName)culture.LegacyId).ToString();
        public static bool IsCustom(this GameCulture culture) => ExtraLocalesSupport.extraCultures.ContainsValue(culture);
        public static bool HasSubtitle(this GameCulture culture)
        {
            if (!Enum.IsDefined((CultureName)culture.LegacyId))
            {
                CultureNamePlus name = (CultureNamePlus)culture.LegacyId;
                return name switch
                {
                    CultureNamePlus.Vietnamese => false,
                    _ => true
                };
            }
            return true;

        }
        public static bool HasDescription(this GameCulture culture)
        {
            if (culture.IsCustom())
            {
                CultureNamePlus name0 = (CultureNamePlus)culture.LegacyId;
                return name0 switch
                {
                    _ => false
                };
            }
            CultureName name1 = (CultureName)culture.LegacyId;
            return name1 switch
            {
                _ => false
            };
        }
        public static string LangCode(this CultureNamePlus culture)
        {
            return culture switch
            {
                BritishEnglish => "en-GB",
                Japanese => "ja-JP",
                Korean => "ko-KR",
                TraditionalChinese => "zh-Hant",
                Turkish => "tr-TR",
                Thai => "th-TH",
                Ukrainian => "uk-UA",
                LatinAmericanSpanish => "es-LA",
                Czech => "cs-CZ",
                Hungarian => "hu-HU",
                PortugalPortuguese => "pt-PT",
                Swedish => "sv-SE",
                Dutch => "nl-NL",
                Danish => "da-DK",
                Vietnamese => "vi-VN",
                Finnish => "fi-FI",
                Romanian => "ro-RO",
                Indonesian => "id-ID",
                _ => null
            };
        }
        public static CultureName FallbackLang(this CultureNamePlus culture)
        {
            return culture switch
            {
                TraditionalChinese => CultureName.Chinese,
                Ukrainian => CultureName.Russian,
                LatinAmericanSpanish => CultureName.Spanish,
                PortugalPortuguese => CultureName.Portuguese,
                _ => CultureName.English
            };
        }
        public static bool IsValid(this CultureNamePlus culture) => Enum.IsDefined(culture) && culture != Unknown;
        public static PluralizationType Pluralization(this CultureNamePlus culture)
        {
            return culture switch
            {
                BritishEnglish or LatinAmericanSpanish or PortugalPortuguese
                or Hungarian or Swedish or Dutch or Danish or Finnish => PluralizationType.Simple,

                Japanese or Korean or TraditionalChinese or Thai or Vietnamese or Indonesian => PluralizationType.None, // for completion's sake

                Ukrainian => PluralizationType.RussianThreeway,

                Czech or Turkish or Romanian => PluralizationType.Custom,

                _ => PluralizationType.None,
            };
        }
        public static int CustomPluralization(this CultureNamePlus culture, int mod10, int mod100, int count)
        {
            return 0;
        }
    }
}
