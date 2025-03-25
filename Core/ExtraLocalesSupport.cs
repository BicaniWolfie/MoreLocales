using MonoMod.RuntimeDetour;
using MoreLocales.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Threading;

namespace MoreLocales.Core
{
    public class ExtraLocalesSupport
    {
        private const string customCultureDataName = "LocalizationPlusData.dat";
        private static CultureNamePlus loadedCulture = CultureNamePlus.Unknown;
        internal static int cachedVanillaCulture = 1; // english by default
        public static readonly Dictionary<CultureNamePlus, GameCulture> extraCultures = [];

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "_NamedCultures")]
        public static extern ref Dictionary<GameCulture.CultureName, GameCulture> GetNamedCultures(GameCulture type = null);

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "_legacyCultures")]
        public static extern ref Dictionary<int, GameCulture> GetLegacyCultures(GameCulture type = null);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SetTitle")]
        public static extern void CallSetTitle(Main instance);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ActiveCulture")]
        public static extern void SetActiveCulture(LanguageManager instance, GameCulture culture);

        internal static void DoLoad()
        {
            IL_LanguageManager.ReloadLanguage += AddFallbacks;
            On_Main.SaveSettings += Save;

            var vanillaNamedCultures = GetNamedCultures();

            CultureNamePlus[] values = Enum.GetValues<CultureNamePlus>();
            for (int i = 0; i < values.Length; i++)
            {
                CultureNamePlus newCulture = values[i];

                if (newCulture == CultureNamePlus.Unknown)
                    continue;

                GameCulture generatedCulture = new(newCulture.LangCode(), (int)newCulture);

                extraCultures.Add(newCulture, generatedCulture);
                vanillaNamedCultures.Add((GameCulture.CultureName)newCulture, generatedCulture);
            }
        }

        private static bool Save(On_Main.orig_SaveSettings orig)
        {
            // So, why do we need this?
            // The game will actually save our custom culture by default, using GameCulture.Name, but it won't recognize it when loading, and revert back to English.
            // First, we can save our custom culture data in our file.
            SaveCustomCultureData();
            // Second, we can revert the culture by ourselves before the game has the chance to save it.
            RevertCustomCulture(false, out var customCulture);
            bool result = orig();
            // Then, bring it back (if settings are saved outside of game exit, this is necessary)
            LanguageManager.Instance?.SetLanguage(customCulture);
            return result;
        }

        private static void AddFallbacks(ILContext il)
        {
            Mod mod = ModContent.GetInstance<MoreLocales>();
            try
            {
                // first we need to add a local var for our custom GameCulture
                var localGameCulture = new VariableDefinition(il.Import(typeof(GameCulture)));
                il.Body.Variables.Add(localGameCulture);

                var c = new ILCursor(il);

                // this is inside the if statement, so we already know that the active culture isn't english
                if (!c.TryGotoNext(i => i.MatchLdarg0(), i => i.MatchLdarg0(), i => i.MatchCall<LanguageManager>("get_ActiveCulture")))
                {
                    mod.Logger.Warn("AddFallbacks: Couldn't find in-between step insertion position");
                    return;
                }

                // load this in order to consume it for our delegate
                c.EmitLdarg0();

                // figure out if the current lang has a fallback defined
                c.EmitDelegate<Func<LanguageManager, GameCulture>>(l =>
                {
                    CultureNamePlus possibleCustomCulture = (CultureNamePlus)l.ActiveCulture.LegacyId;
                    if (extraCultures.ContainsKey(possibleCustomCulture))
                    {
                        GameCulture.CultureName possibleFallback = possibleCustomCulture.FallbackLang();
                        if (possibleFallback != GameCulture.CultureName.English)
                        {
                            return GetNamedCultures()[possibleFallback];
                        }
                    }
                    return null;
                });

                // store that value in the variable
                c.EmitStloc(localGameCulture.Index);

                var skipLabel = il.DefineLabel();

                // load the variable to check if it's null
                c.EmitLdloc(localGameCulture.Index);

                // if it's null, skip the call
                c.EmitBrfalse(skipLabel);

                // otherwise, load arguments
                c.EmitLdarg0();
                c.EmitLdloc(localGameCulture.Index);

                // then call the method
                c.EmitCall(typeof(LanguageManager).GetMethod("LoadFilesForCulture", BindingFlags.Instance | BindingFlags.NonPublic));

                // it should skip to after the call
                c.MarkLabel(skipLabel);
            }
            catch
            {
                MonoModHooks.DumpIL(mod, il);
            }
        }
        /// <summary>
        /// Sets the game's language without calling <see cref="LanguageManager.SetLanguage(GameCulture)"/>
        /// </summary>
        /// <param name="culture"></param>
        public static void SetLanguageSoft(GameCulture culture)
        {
            var lang = LanguageManager.Instance;
            SetActiveCulture(lang, culture);
            Thread.CurrentThread.CurrentCulture = culture.CultureInfo;
            Thread.CurrentThread.CurrentUICulture = culture.CultureInfo;
        }
        public static void LoadCustomCultureData()
        {
            string pathToCustomCultureData = Path.Combine(Main.SavePath, customCultureDataName);

            if (!File.Exists(pathToCustomCultureData))
                return;

            using var reader = new BinaryReader(File.Open(pathToCustomCultureData, FileMode.Open));
            CultureNamePlus culture = (CultureNamePlus)reader.ReadByte();

            if (!culture.IsValid())
                return;

            loadedCulture = culture;

            LanguageManager.Instance.SetLanguage(extraCultures[loadedCulture]);
            CallSetTitle(Main.instance);
        }
        private static void SaveCustomCultureData()
        {
            string pathToCustomCultureData = Path.Combine(Main.SavePath, customCultureDataName);

            void WriteFile()
            {
                using var writer = new BinaryWriter(File.Open(pathToCustomCultureData, FileMode.OpenOrCreate));
                byte id = (byte)LanguageManager.Instance.ActiveCulture.LegacyId;
                writer.Write(id);
            }

            if (!File.Exists(pathToCustomCultureData))
            {
                WriteFile();
            }
            else
            {
                File.WriteAllText(pathToCustomCultureData, "");
                WriteFile();
            }
        }
        internal static void DoUnload()
        {
            SaveCustomCultureData();
            UnregisterCultures();
        }
        private static void RevertCustomCulture(bool setTitle, out GameCulture customCulture, bool soft = false)
        {
            customCulture = LanguageManager.Instance.ActiveCulture;
            if (!customCulture.IsCustom())
                return;

            if (soft)
                SetLanguageSoft(GameCulture.FromLegacyId(cachedVanillaCulture));
            else
                LanguageManager.Instance.SetLanguage(cachedVanillaCulture);

            if (setTitle)
                CallSetTitle(Main.instance);
        }
        private static void UnregisterCultures()
        {
            RevertCustomCulture(true, out _, true);

            extraCultures.Clear();

            var vanillaLegacyCultures = GetLegacyCultures();
            var vanillaNamedCultures = GetNamedCultures();

            CultureNamePlus[] values = Enum.GetValues<CultureNamePlus>();
            for (int i = 0; i < values.Length; i++)
            {
                CultureNamePlus newCulture = values[i];

                if (newCulture == CultureNamePlus.Unknown)
                    continue;

                vanillaLegacyCultures.Remove((int)newCulture);
                vanillaNamedCultures.Remove((GameCulture.CultureName)newCulture);
            }
        }
    }
}
