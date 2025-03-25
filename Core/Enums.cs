namespace MoreLocales.Core
{
    /// <summary>
    /// The new added cultures. Enums can be freely cast into other enums without any errors. The enum underneath will keep the value.
    /// </summary>
    public enum CultureNamePlus
    {
        BritishEnglish = 10,
        Japanese,
        Korean,
        TraditionalChinese,
        Turkish,
        Thai,
        Ukrainian,
        LatinAmericanSpanish,
        Czech,
        Hungarian,
        PortugalPortuguese,
        Swedish,
        Dutch,
        Danish,
        Vietnamese, // omg is this a mirrorman reference
        Finnish,
        Romanian,
        Indonesian,
        Unknown = 9999,
    }
    /// <summary>
    /// List of fonts that are needed to support different languages, especially Asian languages.
    /// </summary>
    public enum LocalizedFont
    {
        /// <summary>
        /// Does not change the font. Additionally, sets <see cref="FontHelper.forcedFont"/> to false.
        /// </summary>
        None,
        Default,
        Japanese,
        Korean,
    }
}
