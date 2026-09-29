using Areung_Plugin.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Areung_Plugin.UI
{
    [RequireComponent(typeof(Button))]
    public class SoundButton : MonoBehaviour
    {
        private void Awake()
        {
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    Setting.PlaySFX(AudioLibrarySounds.Button, 0.1f);
                });
            }
        }
    }
}