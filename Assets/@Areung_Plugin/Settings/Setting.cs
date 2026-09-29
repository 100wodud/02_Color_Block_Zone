using Cysharp.Threading.Tasks;
using JSAM;
using UnityEngine;

namespace Areung_Plugin.Settings
{
    public static class Setting
    {
        public static bool IsInitialized = false;
        private static bool _setHaptic;
        private static bool _setBGM;
        private static bool _setSFX;

        public static bool SetHaptic
        {
            get
            {
                if (!IsInitialized)
                {
                    _setHaptic = SettingData.Haptic;
                }

                return _setHaptic;
            }
            set
            {
                _setHaptic = value;
                Haptic.Using = value;
            }
        }
    
        public static bool SetBGM
        {
            get
            {
                if (!IsInitialized)
                {
                    _setBGM = SettingData.BGM;
                }

                return _setBGM;
            }
            set
            {
                _setBGM = value;
                SettingData.BGM = value;
                AudioManager.MusicMuted = !SettingData.BGM;
            }
        }
    
    
        public static bool SetSFX
        {
            get
            {
                if (!IsInitialized)
                {
                    _setSFX = SettingData.SFX;
                }

                return _setSFX;
            }
            set
            {
                _setSFX = value;
                SettingData.SFX = value;
                AudioManager.SoundMuted = !SettingData.SFX;
            }
        }
    
        public static async void Initialized()
        {
            if(IsInitialized) return;
            SettingData.Init();
            await UniTask.WaitUntil(() => SettingData.IsInitialized);
            Haptic.Initialize();
            await UniTask.WaitUntil(() => Haptic.IsInitialized);
            SetHaptic = SettingData.Haptic;
            SetBGM = SettingData.BGM;
            SetSFX = SettingData.SFX;
            IsInitialized = true;
            Debug.Log("[Setting] Initialized");
        }

        #region Haptic

        public static void HapticWeak() => Haptic.Weak();
        public static void HapticSoft() => Haptic.Soft();
        public static void HapticMedium() => Haptic.Medium();
        public static void HapticHard() => Haptic.Hard();

        #endregion

        #region Audio

        // BGM 플레이
        public static void PlayBGM(AudioLibraryMusic clip, float vol = 0.3f)
        {
            if (!_setBGM) return;
            AudioManager.StopAllMusic();
            AudioManager.MusicVolume = vol;
            AudioManager.PlayMusic(clip);
        }
    
        // BGM 플레이
        public static void PlaySFX(AudioLibrarySounds clip, float vol = 0.3f)
        {
            if (!_setSFX) return;
            AudioManager.SoundVolume = vol;
            AudioManager.PlaySound(clip);
        }

        public static void StopBGM()
        {
            AudioManager.StopAllMusic();
        }

        public static void StopSFX()
        {
            AudioManager.StopAllSounds();
        }

        public static void StopAllSound()
        {
            StopBGM();
            StopSFX();
        }

        #endregion
    
    }
}
