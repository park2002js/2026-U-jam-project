using System;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    // Unity 생명주기만 담당한다. ItemData/Runtime/Effect/Preview는 모두 일반 C# 객체다.
    public sealed class RuntimeTimer : MonoBehaviour
    {
        public static RuntimeTimer Instance { get; private set; }
        public event Action<float> Tick;
        public static RuntimeTimer Ensure()
        {
            if (Instance == null)
            {
                var host = new GameObject("RuntimeTimer");
                Instance = host.AddComponent<RuntimeTimer>();
                DontDestroyOnLoad(host);
            }
            return Instance;
        }
        private void Update()
        {
            TemporaryBuffs.Instance.Update();
            ElementSystem.Instance.Update();
            Tick?.Invoke(Time.deltaTime);
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = null;
    }
}
