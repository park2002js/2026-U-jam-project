using System.Collections.Generic;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UJam.Runtime.Phase;
using UJam.Runtime.Player;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-10000)]
public class EnemyAnimationViewer : MonoBehaviour
{
    [SerializeField, Tooltip("EnemyBase가 있는 프리팹 또는 씬 적을 연결합니다.")] private GameObject model;
    [SerializeField] private EnemyBase[] _enemyPrefabs = new EnemyBase[0];
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private GameObject _target;
    [SerializeField, Tooltip("재생 중 게임 진행·기존 카메라·게임 UI를 잠시 멈추고 적 테스트 화면을 표시합니다.")]
    private bool _testMode = true;
    [SerializeField] private Camera _previewCamera;
    [SerializeField] private Light _previewLight;

    private readonly List<Behaviour> _paused = new List<Behaviour>();
    private readonly List<GameObject> _hiddenTemplates = new List<GameObject>();
    private Transform _runtimeRoot;
    private GameObject _source;
    private int _selectedIndex;
    private Vector2 _scroll;
    private string _message;
    private bool _isolated;
    private float _previousTimeScale;
    private CursorLockMode _previousLock;
    private bool _previousCursor;
    private bool _previousRunInBackground;

    public EnemyBase ActiveEnemy { get; private set; }
    public GameObject Target => _target;

    private void Awake()
    {
        if (!_testMode) return;
        _isolated = true;
        _previousTimeScale = Time.timeScale;
        _previousLock = Cursor.lockState;
        _previousCursor = Cursor.visible;
        _previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        foreach (GameManager manager in FindObjectsByType<GameManager>(FindObjectsSortMode.None)) Pause(manager);
        foreach (WaveController waves in FindObjectsByType<WaveController>(FindObjectsSortMode.None)) Pause(waves);
        foreach (PlayerInputManager input in FindObjectsByType<PlayerInputManager>(FindObjectsSortMode.None)) Pause(input);
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) Pause(canvas);
        if (_previewCamera != null)
        {
            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (camera != _previewCamera) Pause(camera);
            _previewCamera.tag = "MainCamera";
            _previewCamera.enabled = true;
        }
        if (_previewLight != null) _previewLight.enabled = true;
    }

    private void Start()
    {
        if (model != null && model.GetComponent<EnemyBase>() != null)
        {
            _source = model;
            for (int index = 0; index < _enemyPrefabs.Length; index++)
                if (_enemyPrefabs[index] != null && _enemyPrefabs[index].gameObject == model) _selectedIndex = index;
            ResetSelected();
        }
        else SelectEnemy(0);
    }

    private void Pause(Behaviour behaviour)
    {
        if (!behaviour.enabled) return;
        _paused.Add(behaviour);
        behaviour.enabled = false;
    }

    public bool SelectEnemy(int index)
    {
        if (!Application.isPlaying || index < 0 || index >= _enemyPrefabs.Length || _enemyPrefabs[index] == null)
        {
            _message = "EnemyBase가 있는 적 프리팹을 Inspector에 연결해 주세요.";
            return false;
        }
        _selectedIndex = index;
        _source = _enemyPrefabs[index].gameObject;
        ResetSelected();
        return ActiveEnemy != null;
    }

    public void ResetSelected()
    {
        if (!Application.isPlaying) return;
        if (_source == null) _source = model;
        if (_source == null || _source.GetComponent<EnemyBase>() == null)
        {
            _message = "모델 FBX 대신 EnemyBase가 있는 게임용 적을 연결해 주세요.";
            return;
        }
        if (_runtimeRoot == null)
        {
            _runtimeRoot = new GameObject("Runtime Enemies").transform;
            _runtimeRoot.SetParent(transform, false);
        }
        for (int index = _runtimeRoot.childCount - 1; index >= 0; index--)
        {
            GameObject previous = _runtimeRoot.GetChild(index).gameObject;
            previous.SetActive(false);
            Destroy(previous);
        }
        if (_source.scene.IsValid() && _source.activeSelf)
        {
            _hiddenTemplates.Add(_source);
            _source.SetActive(false);
        }
        Transform spawn = _spawnPoint != null ? _spawnPoint : transform;
        if (!ValidateTarget(spawn.position)) return;
        GameObject instance = Instantiate(_source, spawn.position, spawn.rotation, _runtimeRoot);
        instance.name = _source.name + " (FSM Test)";
        instance.SetActive(true);
        ActiveEnemy = instance.GetComponent<EnemyBase>();
        ActiveEnemy.PrepareForTesting(_target);
        if (_target.TryGetComponent(out EnemyViewerTarget viewerTarget)) viewerTarget.ResetTarget();
        _message = "Init 완료 · 상태 버튼은 실제 FSM으로 전달됩니다.";
    }

    public bool SetTarget(GameObject target)
    {
        if (target == null || target.GetComponent<IDamageable>() == null) return false;
        _target = target;
        if (ActiveEnemy != null) ActiveEnemy.PrepareForTesting(target);
        return true;
    }

    private bool ValidateTarget(Vector3 spawn)
    {
        if (_target == null)
        {
            var target = new GameObject("Viewer Target");
            target.transform.SetParent(transform, false);
            target.transform.position = spawn + new Vector3(6, 0, 12);
            target.AddComponent<EnemyViewerTarget>();
            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.center = Vector3.up * .7f;
            collider.size = new Vector3(1.2f, 1.4f, 1.2f);
            _target = target;
        }
        if (_target.GetComponent<IDamageable>() != null) return true;
        _message = "공격 대상에 IDamageable 컴포넌트가 필요합니다.";
        return false;
    }

    public bool CommandState(EnemyStateType state)
    {
        if (state == EnemyStateType.Init) { ResetSelected(); return ActiveEnemy != null; }
        if (ActiveEnemy == null)
        {
            _message = "Init / 다시 생성으로 적을 먼저 준비하세요.";
            return false;
        }
        if ((state == EnemyStateType.Spawn || state == EnemyStateType.Summon) &&
            (ActiveEnemy.Animation == null || !ActiveEnemy.Animation.HasState(state.ToString()))) return false;
        bool accepted = ActiveEnemy.FSM.SetState(state);
        _message = accepted ? ActiveEnemy.FSM.PendingState != EnemyStateType.None
            ? "공격 동작이 끝나면 " + state + " 상태로 전환합니다." : state + " 상태를 요청했습니다."
            : "현재 상태에서는 전환할 수 없습니다. 사망 후에는 다시 생성하세요.";
        return accepted;
    }

    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        GUILayout.BeginArea(new Rect(12, 12, 280, Screen.height - 24), GUI.skin.box);
        _scroll = GUILayout.BeginScrollView(_scroll);
        GUILayout.Label("적 FSM / 애니메이션 테스트");
        for (int index = 0; index < _enemyPrefabs.Length; index++)
        {
            EnemyBase prefab = _enemyPrefabs[index];
            if (prefab != null && GUILayout.Button((index == _selectedIndex ? "● " : "") + prefab.name, GUILayout.Height(27)))
                SelectEnemy(index);
        }
        GUILayout.Space(8);
        DrawStateButtons();
        if (GUILayout.Button("다시 생성 · Init", GUILayout.Height(30))) ResetSelected();
        GUILayout.Space(8);
        GUILayout.Label(Status());
        if (_target != null && _target.TryGetComponent(out EnemyViewerTarget target))
            GUILayout.Label("대상 HP: " + target.HP.ToString("F0") + "\n누적 피해: " + target.DamageTaken.ToString("F0") + " / " + target.HitCount + "회");
        GUILayout.Label(_message ?? "");
        GUILayout.Label("공격 도중 요청한 상태는 동작 완료 후 적용됩니다.\nDead는 즉시 전환됩니다.");
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawStateButtons()
    {
        EnemyStateType[] states = { EnemyStateType.Idle, EnemyStateType.Move, EnemyStateType.Attack, EnemyStateType.Spawn, EnemyStateType.Summon, EnemyStateType.Dead };
        for (int index = 0; index < states.Length; index += 2)
        {
            GUILayout.BeginHorizontal();
            for (int column = 0; column < 2; column++)
            {
                EnemyStateType state = states[index + column];
                bool available = ActiveEnemy != null && (state != EnemyStateType.Spawn && state != EnemyStateType.Summon
                    || ActiveEnemy.Animation != null && ActiveEnemy.Animation.HasState(state.ToString()));
                GUI.enabled = available;
                if (GUILayout.Button(state.ToString(), GUILayout.Height(30))) CommandState(state);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }
    }

    private string Status()
    {
        if (ActiveEnemy == null) return "적이 제거되었습니다.";
        string status = "FSM: " + ActiveEnemy.FSM.state + "\n예약: " + ActiveEnemy.FSM.PendingState + "\nHP: " + ActiveEnemy.Status.HP.ToString("F0");
        Animator animator = ActiveEnemy.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
            if (clips.Length > 0) status += "\n동작: " + clips[0].clip.name + "\n진행: " + animator.GetCurrentAnimatorStateInfo(0).normalizedTime.ToString("F2");
        }
        return status;
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        if (_runtimeRoot != null) Destroy(_runtimeRoot.gameObject);
        foreach (GameObject template in _hiddenTemplates) if (template != null) template.SetActive(true);
        _hiddenTemplates.Clear();
        if (!_isolated) return;
        foreach (Behaviour behaviour in _paused) if (behaviour != null) behaviour.enabled = true;
        _paused.Clear();
        if (_previewCamera != null) _previewCamera.enabled = false;
        if (_previewLight != null) _previewLight.enabled = false;
        Time.timeScale = _previousTimeScale;
        Cursor.lockState = _previousLock;
        Cursor.visible = _previousCursor;
        Application.runInBackground = _previousRunInBackground;
        _isolated = false;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(EnemyAnimationViewer))]
    private class ViewerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var viewer = (EnemyAnimationViewer)target;
            GameObject previousModel = viewer.model;
            DrawDefaultInspector();
            if (Application.isPlaying && viewer.model != previousModel && viewer.model != null)
            {
                viewer._source = viewer.model;
                viewer.ResetSelected();
            }
            if (viewer.model != null && viewer.model.GetComponent<EnemyBase>() == null)
                EditorGUILayout.HelpBox("Model에는 FBX 원본 대신 EnemyBase가 붙은 적 프리팹을 연결하세요.", MessageType.Warning);
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            for (int index = 0; index < viewer._enemyPrefabs.Length; index++)
                if (viewer._enemyPrefabs[index] != null && GUILayout.Button(viewer._enemyPrefabs[index].name)) viewer.SelectEnemy(index);
            viewer.DrawStateButtons();
            if (GUILayout.Button("다시 생성 · Init")) viewer.ResetSelected();
            EditorGUI.EndDisabledGroup();
            if (Application.isPlaying) EditorGUILayout.HelpBox(viewer.Status(), MessageType.Info);
        }
    }
#endif
}
