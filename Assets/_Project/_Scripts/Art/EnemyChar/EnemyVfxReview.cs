using System.Collections;
using UJam.Runtime.Enemy;
using UJam.Runtime.Enemy.Projectiles;
using UnityEngine;

public sealed class EnemyVfxReview : MonoBehaviour
{
    public GameObject[] Prefabs;
    public Camera ReviewCamera;
    public EnemyViewerTarget Target;
    public EnemyBase ActiveEnemy { get; private set; }
    public int SelectedIndex { get; private set; }
    public string Action { get; private set; } = "Idle";
    private Transform runtime;
    private Coroutine demo;
    private float actionStarted;
    private float speed = 1;
    private bool topDown;
    private Vector3 defaultCameraPosition;
    private Quaternion defaultCameraRotation;
    private static readonly string[] Descriptions =
    {
        "Smaller walking dust / arm-tip impact / hand trail", "Purple fireball / ember trail / arrival impact",
        "Smaller walking dust / arm-tip impact / crystal glow", "Main current / wrapping branches / concentrated impact",
        "Crystal spell / expanding summon field / two skeleton portals / dissolve", "Ground emergence / steel slash / bone landing dust",
        "Immediate split / upward rounded liquid / sinking parent", "Large polygon detonation / no second death explosion"
    };

    private void Awake()
    {
        Application.runInBackground = true;
        Time.timeScale = 1;
        defaultCameraPosition = ReviewCamera.transform.position;
        defaultCameraRotation = ReviewCamera.transform.rotation;
        runtime = new GameObject("Review Runtime").transform;
        runtime.SetParent(transform, false);
    }
    private void Start() { if (ActiveEnemy == null) Select(0); }

    public void SetTopDown(bool enabled)
    {
        topDown = enabled;
        ReviewCamera.transform.SetPositionAndRotation(enabled ? new Vector3(0, 18, 2) : defaultCameraPosition,
            enabled ? Quaternion.Euler(90, 0, 0) : defaultCameraRotation);
    }

    public void Select(int index)
    {
        if (Prefabs == null || index < 0 || index >= Prefabs.Length || Prefabs[index] == null) return;
        if (demo != null) { StopCoroutine(demo); demo = null; }
        ClearRuntime();
        SelectedIndex = index;
        ActiveEnemy = Instantiate(Prefabs[index], Vector3.zero, Quaternion.identity, runtime).GetComponent<EnemyBase>();
        ActiveEnemy.name = Prefabs[index].name + " Review";
        // The review buttons drive the real FSM while automatic target acquisition is paused.
        ActiveEnemy.enabled = false;
        float distance = Mathf.Min(6.5f, ActiveEnemy.Status.AttackRange * 2f * .94f);
        Target.transform.position = Vector3.forward * distance;
        Target.ResetTarget();
        ActiveEnemy.PrepareForTesting(Target.gameObject);
        Action = "Idle";
        actionStarted = Time.time;
    }

    public void Play(string action)
    {
        int index = SelectedIndex;
        Select(index);
        Action = action;
        actionStarted = Time.time;
        switch (action)
        {
            case "Attack":
                ActiveEnemy.enabled = true;
                ActiveEnemy.FSM.SetState(EnemyStateType.Attack);
                break;
            case "Death": ActiveEnemy.TakeDamage(new UJam.Runtime.Combat.DamageInfo(float.MaxValue)); break;
            case "Summon": ActiveEnemy.FSM.SetState(EnemyStateType.Summon); break;
            case "Spawn": ActiveEnemy.FSM.SetState(EnemyStateType.Spawn); break;
            case "Move":
                Target.transform.position = Vector3.forward * 60;
                ActiveEnemy.PrepareForTesting(Target.gameObject);
                ActiveEnemy.FSM.SetState(EnemyStateType.Move);
                break;
        }
    }

    public void PlayDemo() { Play("Move"); demo = StartCoroutine(Demo()); }
    private IEnumerator Demo()
    {
        yield return new WaitForSeconds(1.4f);
        demo = null; Play("Attack");
        yield return new WaitForSeconds(2.2f);
        if (SelectedIndex == 4) { Play("Summon"); yield return new WaitForSeconds(2f); }
        if (SelectedIndex == 5) { Play("Spawn"); yield return new WaitForSeconds(1.7f); }
        Play("Death");
    }

    private void LateUpdate()
    {
        if (Action == "Move" && ActiveEnemy != null && ActiveEnemy.transform.position.z > 3)
            ActiveEnemy.transform.position = new Vector3(0, 0, -2);
    }

    private void ClearRuntime()
    {
        if (runtime != null)
            foreach (Transform child in runtime) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        foreach (Projectile projectile in FindObjectsByType<Projectile>(FindObjectsSortMode.None)) Destroy(projectile.gameObject);
        foreach (LowpolyVfxLifetime effect in FindObjectsByType<LowpolyVfxLifetime>(FindObjectsSortMode.None)) Destroy(effect.gameObject);
        foreach (UJam.Runtime.Enemy.Vfx.SniperBeam beam in FindObjectsByType<UJam.Runtime.Enemy.Vfx.SniperBeam>(FindObjectsSortMode.None)) Destroy(beam.gameObject);
    }

    private void OnGUI()
    {
        if (Application.isBatchMode) return;
        GUI.skin.label.wordWrap = true;
        GUILayout.BeginArea(new Rect(16, 16, 290, Screen.height - 32), GUI.skin.box);
        GUILayout.Label("LOWPOLY / ENEMY VFX REVIEW");
        GUILayout.Space(8);
        for (int i = 0; i < Prefabs.Length; i++)
            if (GUILayout.Button((i == SelectedIndex ? "● " : "") + Prefabs[i].name, GUILayout.Height(30))) Select(i);
        GUILayout.Space(12);
        if (GUILayout.Button(topDown ? "카메라: 탑뷰 → 사선" : "카메라: 사선 → 탑뷰", GUILayout.Height(30))) SetTopDown(!topDown);
        if (GUILayout.Button("전체 동작 순서대로 재생", GUILayout.Height(34))) PlayDemo();
        foreach (string action in new[] { "Idle", "Move", "Attack", "Death", "Summon", "Spawn" })
        {
            GUI.enabled = action != "Summon" && action != "Spawn" || SelectedIndex == (action == "Summon" ? 4 : 5);
            if (GUILayout.Button(action, GUILayout.Height(27))) Play(action);
        }
        GUI.enabled = true;
        GUILayout.BeginHorizontal();
        foreach (float value in new[] { .25f, .5f, 1f })
            if (GUILayout.Button(value + "x")) { speed = value; Time.timeScale = value; }
        GUILayout.EndHorizontal();
        if (GUILayout.Button(Time.timeScale > 0 ? "일시 정지" : "계속 재생")) Time.timeScale = Time.timeScale > 0 ? 0 : speed;
        GUILayout.Space(10);
        GUILayout.Label(Action + " / " + (Time.time - actionStarted).ToString("F2") + "s");
        GUILayout.Label("실제 대상 피해: " + Target.DamageTaken.ToString("F0") + " / " + Target.HitCount + "회");
        GUILayout.Label(Descriptions[SelectedIndex]);
        GUILayout.Label("Death로 피격 사망을, Rollbug의 Attack으로 자폭을 확인합니다.");
        GUILayout.EndArea();
    }

    private void OnDisable() { Time.timeScale = 1; }
}
