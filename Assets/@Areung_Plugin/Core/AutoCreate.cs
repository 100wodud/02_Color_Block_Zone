// using System;
// using Cysharp.Threading.Tasks;
// using UnityEngine;
// using UnityEngine.AddressableAssets;
// using UnityEngine.ResourceManagement.AsyncOperations;
// using Object = UnityEngine.Object;
//
// namespace Areung_Plugin.Core
// {
//     public class AutoCreate : MonoBehaviour
//     {
//         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
//         [Obsolete("Obsolete")]
//         private static void AutoCreatePrefab()
//         {
//             LoadInitializer().Forget();
//         }
//         [Obsolete("Obsolete")]
//         private static async UniTaskVoid LoadInitializer()
//         {
//             
//             if (FindObjectOfType<Initializer>(true) != null)
//             {
//                 Debug.Log("[Initializer] 이미 존재함 - 생성하지 않음");
//                 return;
//             }
//             
//             AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>("Initializer");
//             await handle.Task;
//
//             if (handle.Status == AsyncOperationStatus.Succeeded)
//             {
//                 Object.Instantiate(handle.Result);
//             }
//             else Debug.LogError("[Initializer] 프리팹 Addressable 로드 실패!");
//         }
//     }
// }
