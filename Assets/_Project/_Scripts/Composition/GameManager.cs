using System;
using UJam.Runtime.Defense;
using UJam.Runtime.Grid;
using UJam.Runtime.Phase;
using UJam.Runtime.Shop;
using UnityEngine;

/// <summary>
/// 게임의 주요 흐름을 담당한다. 전역에서 사용될 수 있도록 싱글톤을 이용하였다.
/// <para>담당하는 흐름은 다음과 같다.</para>
/// <para>1. 게임 시작</para>
/// <para>2. 게임 오버</para>
/// <para>3. 게임 클리어</para>
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action OnGameOver;
    public event Action<PhaseState> OnPhaseChanged;

    public bool IsGameOver { get; private set; }
    public bool IsInitialized { get; private set; }
    public PhaseState CurrentPhase => _phaseSystem != null ? _phaseSystem.CurrentState : PhaseState.None;

    private PhaseSystem _phaseSystem;

    /// <summary>
    /// UIManager와 UICursorController 등이 Inspector 연결 없이 사용할 싱글톤을 등록합니다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 각 싱글톤의 Awake 초기화를 확인하고 연결을 마친 뒤 최초 정비 Phase를 시작합니다.
    /// </summary>
    private void Start()
    {
        if (Instance != this || IsInitialized || IsGameOver) return;
        if (GridSystem.Instance == null || !GridSystem.Instance.IsInitialized)
        {
            Debug.LogError("[GameManager] GridSystem의 Awake 초기화가 완료되지 않았습니다.", this);
            return;
        }

        if (ShopManager.Instance == null || !ShopManager.Instance.IsInitialized)
        {
            Debug.LogError("[GameManager] ShopManager의 Awake 초기화가 완료되지 않았습니다.", this);
            return;
        }

        _phaseSystem = FindFirstObjectByType<PhaseSystem>();
        BaseCore baseCore = FindFirstObjectByType<BaseCore>();
        WaveController waveController = WaveController.Instance;
        if (_phaseSystem == null || baseCore == null || waveController == null)
        {
            Debug.LogError("[GameManager] 활성화된 PhaseSystem, BaseCore, WaveController가 필요합니다.", this);
            return;
        }

        waveController.ConfigureDefaultTarget(baseCore.gameObject);
        if (!_phaseSystem.Initialize(waveController)) return;
        IsInitialized = true;
        _phaseSystem.StartPreparationPhase();
    }

    /// <summary>
    /// UIManager의 전투 시작 요청을 PhaseSystem에 전달하며, 전환 가능 여부는 PhaseSystem이 판단합니다.
    /// </summary>
    public void StartCombatPhase()
    {
        if (IsInitialized && !IsGameOver) _phaseSystem.StartCombatPhase();
    }

    /// <summary>
    /// PhaseSystem이 확정한 Phase를 UIManager, UICursorController와 다른 구독자에게 알립니다.
    /// </summary>
    public void HandlePhaseChanged(PhaseState phase)
    {
        if (!IsInitialized || IsGameOver) return;
        if (phase == PhaseState.Preparation) ShopManager.Instance?.BeginPreparation();
        OnPhaseChanged?.Invoke(phase);
    }

    /// <summary>
    /// PlayerStatus의 사망 보고를 받아 UIManager 등에 게임 오버를 한 번만 알립니다.
    /// </summary>
    public void GameOver()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        Debug.Log("[GameManager] Game Over 확정: OnGameOver 통지", this);
        OnGameOver?.Invoke();
    }

    /// <summary>
    /// 자신이 등록한 싱글톤과 초기화 상태를 해제합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        IsInitialized = false;
    }
}
