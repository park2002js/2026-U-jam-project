using System;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    /// <summary>일반 C# 효과의 코루틴 호스트이며 BuffManager/ElementManager를 매 프레임 갱신한다. 씬 배치가 필요 없다.</summary>
    public sealed class RuntimeTimer : MonoBehaviour
    {
        public static RuntimeTimer Instance { get; private set; }
        /// <summary>지속적으로 갱신할 시스템에서 구독한다. 해제 시 반드시 구독을 제거한다.</summary>
        public event Action<float> Tick;
        /// <summary>코루틴 실행 전 호출한다. 존재하지 않을 때만 중앙 호스트를 만든다.</summary>
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
            BuffManager.Instance.Update();
            ElementManager.Instance.Update();
            Tick?.Invoke(Time.deltaTime);
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = null;
    }
}
