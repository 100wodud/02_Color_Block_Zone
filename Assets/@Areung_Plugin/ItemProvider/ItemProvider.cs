using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Areung_Plugin.ItemProvider
{
    public static class ItemProvider
    {
        
        public static void GetGolds(int value)
        {
            int gold = value / 10;
            GameManager.Instance.Provider.ProvideItem(ProviderType.Gold, 10, () =>
            {
                PlayerData.Gold += gold;
            });
        }

        public static void GetItemBundle(int value)
        {
        }
    }
}
