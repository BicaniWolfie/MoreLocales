using System;
using Terraria.Localization;
using static Terraria.Localization.GameCulture;

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
                CultureNamePlus.BritishEnglish => "en-GB",
                CultureNamePlus.Japanese => "ja-JP",
                CultureNamePlus.Korean => "ko-KR",
                CultureNamePlus.TraditionalChinese => "zh-Hant",
                CultureNamePlus.Turkish => "tr-TR",
                CultureNamePlus.Thai => "th-TH",
                CultureNamePlus.Ukrainian => "uk-UA",
                CultureNamePlus.LatinAmericanSpanish => "es-LA",
                CultureNamePlus.Czech => "cs-CZ",
                CultureNamePlus.Hungarian => "hu-HU",
                CultureNamePlus.PortugalPortuguese => "pt-PT",
                CultureNamePlus.Swedish => "sv-SE",
                CultureNamePlus.Dutch => "nl-NL",
                CultureNamePlus.Danish => "da-DK",
                CultureNamePlus.Vietnamese => "vi-VN",
                CultureNamePlus.Finnish => "fi-FI",
                CultureNamePlus.Romanian => "ro-RO",
                CultureNamePlus.Indonesian => "id-ID",
                _ => null
            };
        }
        public static CultureName FallbackLang(this CultureNamePlus culture)
        {
            return culture switch
            {
                CultureNamePlus.TraditionalChinese => CultureName.Chinese,
                CultureNamePlus.Ukrainian => CultureName.Russian,
                CultureNamePlus.LatinAmericanSpanish => CultureName.Spanish,
                CultureNamePlus.PortugalPortuguese => CultureName.Portuguese,
                _ => CultureName.English
            };
        }
        public static bool IsValid(this CultureNamePlus culture) => culture != CultureNamePlus.Unknown && Enum.IsDefined(culture);
    }
}
