using System;
using UnityEngine;

namespace Areung_Plugin.SDK.Ads
{
    public class AdExtension 
    {
        public static void UpdateLastCooldownTime(string key)
        {
            double currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            PlayerPrefs.SetString(key, currentTime.ToString());
            PlayerPrefs.Save();
        }

        public static bool InterCondition(string key, int cooldown)
        {
            double currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string saved = PlayerPrefs.GetString(key, "0");
            double lastCooldownTime = double.TryParse(saved, out double val) ? val : 0;
            return currentTime - lastCooldownTime >= cooldown;
        }
    }
}
