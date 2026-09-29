using TMPro;
using UnityEngine;

namespace Areung_Plugin.Localize
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizeTMP : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private int[] values;
        private TMP_Text _localeText;
        
        private void Awake() => _localeText = GetComponent<TMP_Text>();
        
        private void OnEnable()
        {
            LocalizeProvider.OnLanguageChanged += ChangeText;
            ChangeText();
        }
        
        private void OnDisable()
        {
            LocalizeProvider.OnLanguageChanged -= ChangeText;
        }
        
        private void ChangeText()
        {
            _localeText.text = values.Length switch
            {
                0 => LocalizeProvider.GetString(key),
                1 => LocalizeProvider.GetString(key, values[0]),
                2 => LocalizeProvider.GetString(key, values[0], values[1]),
                _ => "Value Is Over"
            };
        }
    }
}