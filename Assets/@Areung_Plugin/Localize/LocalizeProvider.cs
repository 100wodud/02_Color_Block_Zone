using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Areung_Plugin.Localize
{
    [CreateAssetMenu(fileName = "LocalizeProvider", menuName = "Localize")]
    public class LocalizeProvider: ScriptableObject
    {
        private static Locale CurrentLanguage => LocalizationSettings.SelectedLocale;
        private const string TableRef = "LocalizeTable";
        private const string LangPrefKey = "LocalizeLang";
        private static int _currentLanguageIndex;
        public static int CurrentLangIndex => _currentLanguageIndex;
        public static event Action OnLanguageChanged;

        public static string ResolveLanguageCode()
        {
            string saved = PlayerPrefs.GetString(LangPrefKey, string.Empty);
            if (!string.IsNullOrEmpty(saved)) return saved;

            return UnityEngine.Device.Application.systemLanguage switch
            {
                SystemLanguage.Korean => "ko",
                SystemLanguage.Japanese => "ja",
                SystemLanguage.English => "en",
                _ => "en"
            };
        }

        public async UniTask InitProvider()
        {
            await LocalizationSettings.InitializationOperation.Task;

            var provider = LocalizationSettings.AvailableLocales;

            Locale target = provider.GetLocale(ResolveLanguageCode())
                            ?? provider.GetLocale(new LocaleIdentifier("en"));

            if (target == null && provider.Locales.Count > 0) target = provider.Locales[0];
            if (target == null) return;

            ApplyLocale(target);
        }

        private static void ApplyLocale(Locale locale)
        {
            LocalizationSettings.SelectedLocale = locale;
            _currentLanguageIndex = LocalizationSettings.AvailableLocales.Locales.IndexOf(locale);
            PlayerPrefs.SetString(LangPrefKey, locale.Identifier.Code);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }

        public static void ChangeLocale(int selectedLocale)
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (selectedLocale < 0 || selectedLocale >= locales.Count) return;
            ApplyLocale(locales[selectedLocale]);
        }

        public static string GetString(string key)
        {
            if(string.IsNullOrEmpty(key)) return string.Empty; 
            string s = LocalizationSettings.StringDatabase.GetLocalizedString(TableRef,key, CurrentLanguage);
            if (string.IsNullOrEmpty(s)) return $"Doesn't have Key : {key}";
            return s.Contains(@"\\") ? s.Replace(@"\\", "\n") : s;
        }

        public static string GetString(string key, int value) => string.Format(GetString(key),value);
        public static string GetString(string key, int value1, int value2) => string.Format(GetString(key),value1,value2);
    }
}