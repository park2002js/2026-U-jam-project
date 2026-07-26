using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace EnemySystem
{
    public class EnemyPathfinding : MonoBehaviour
    {
        public static EnemyPathfinding Instance { get; private set; }

        [System.Serializable]
        public class RouteConfig
        {
            [Tooltip("EnemyAgent의 enemyId와 매칭 (예: goblin)")]
            public string enemyId = "goblin";
            [Tooltip("시작 위치(스포너) — 참고용")]
            public Transform start;
            [Tooltip("도착 위치(베이스)")]
            public Transform goal;
        }

        [Header("적 종류별 경로 설정")]
        public List<RouteConfig> routes = new List<RouteConfig>();

        [Header("혼잡 회피")]
        [Tooltip("한 칸을 1마리가 예약할 때 붙는 추가 통행 비용 (칸 이동 기본비용≈1000)")]
        public uint penaltyPerEnemy = 8000;
        [Tooltip("경로 끝점이 목표에 이 거리 안이면 도달로 판단")]
        public float reachThreshold = 2f;

        public bool Ready { get; private set; }

        private readonly Dictionary<GraphNode, int> occupancy = new Dictionary<GraphNode, int>();
        private readonly Dictionary<EnemyAgent, List<GraphNode>> reservations
            = new Dictionary<EnemyAgent, List<GraphNode>>();
        private readonly List<EnemyAgent> agents = new List<EnemyAgent>();
        private bool reassignQueued;

        public struct RouteResult
        {
            public bool ok;
            public Path path;            // 따라갈 경로 (null이면 제자리)
            public Barricade barricade;  // 부술 대상 (없으면 null)
            public Vector3 attackPoint;  // 벽 칸 좌표 (공격 거리 기준)
        }

        // 바리케이드 통과 금지 + 혼잡 비용
        private class AvoidProvider : ITraversalProvider
        {
            public EnemyPathfinding o;
            public bool CanTraverse(Path p, GraphNode n)
                => n.Walkable && n.Tag != Barricade.BarricadeTag;
            public uint GetTraversalCost(Path p, GraphNode n) => o.CostOf(n);
        }
        // 바리케이드 통과 허용 + 혼잡 비용
        private class ThroughProvider : ITraversalProvider
        {
            public EnemyPathfinding o;
            public bool CanTraverse(Path p, GraphNode n) => n.Walkable;
            public uint GetTraversalCost(Path p, GraphNode n) => o.CostOf(n);
        }
        private AvoidProvider avoid;
        private ThroughProvider through;

        private uint CostOf(GraphNode n)
            => occupancy.TryGetValue(n, out int c) && c > 0 ? (uint)c * penaltyPerEnemy : 0;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            avoid = new AvoidProvider { o = this };
            through = new ThroughProvider { o = this };
        }

        void OnEnable()  { Barricade.OnAnyBarricadeDestroyed += QueueReassign; }
        void OnDisable() { Barricade.OnAnyBarricadeDestroyed -= QueueReassign; }

        // MapSetup이 Scan+등록 후 호출
        public void Initialize()
        {
            GridGraph g = AstarPath.active != null ? AstarPath.active.data.gridGraph : null;
            if (g != null)
                Debug.Log($"[Pathfinding] 격자 수신 — {g.width}x{g.depth}, nodeSize={g.nodeSize}");
            Ready = true;
        }

        // ---------- 등록 ----------
        public void RegisterAgent(EnemyAgent a) { if (!agents.Contains(a)) agents.Add(a); }
        public void UnregisterAgent(EnemyAgent a) { ReleaseReservation(a); agents.Remove(a); }

        public Transform GetGoal(string enemyId)
        {
            foreach (var r in routes)
                if (r.goal != null && string.Equals(r.enemyId, enemyId, System.StringComparison.OrdinalIgnoreCase))
                    return r.goal;
            foreach (var r in routes) if (r.goal != null) return r.goal;
            GameObject b = GameObject.FindGameObjectWithTag("Base");
            return b != null ? b.transform : null;
        }

        // ---------- 경로 배정 ----------
        public RouteResult BuildRoute(EnemyAgent agent, Vector3 start, Vector3 goal)
        {
            var res = new RouteResult();
            if (AstarPath.active == null) return res;

            ReleaseReservation(agent);   // 내 예약을 내가 피하는 버그 방지

            // 1차: 바리케이드 우회 + 혼잡 회피
            ABPath p1 = Calc(start, goal, avoid);
            if (p1 != null && Reached(p1, goal))
            {
                Reserve(agent, p1);
                res.ok = true; res.path = p1;
                return res;
            }

            // 2차: 바리케이드 통과 허용 → 최단경로상 첫 바리케이드 찾기
            ABPath p2 = Calc(start, goal, through);
            if (p2 == null) return res;

            GraphNode prev = null, wall = null;
            Barricade target = null;
            foreach (GraphNode n in p2.path)
            {
                if (n.Tag == Barricade.BarricadeTag &&
                    Barricade.NodeLookup.TryGetValue(n, out Barricade b) && b != null)
                { wall = n; target = b; break; }
                prev = n;
            }

            if (target == null) { Reserve(agent, p2); res.ok = true; res.path = p2; return res; }

            res.barricade = target;
            res.attackPoint = (Vector3)wall.position;

            if (prev == null) { res.ok = true; return res; }   // 이미 코앞

            // 3차: 벽 앞 칸까지 가는 경로
            ABPath p3 = Calc(start, (Vector3)prev.position, avoid);
            if (p3 == null) { res.ok = true; return res; }

            Reserve(agent, p3);
            res.ok = true; res.path = p3;
            return res;
        }

        private ABPath Calc(Vector3 a, Vector3 b, ITraversalProvider prov)
        {
            ABPath p = ABPath.Construct(a, b);
            p.traversalProvider = prov;
            AstarPath.StartPath(p);
            p.BlockUntilCalculated();   // 콜백 밖에서만 호출됨
            if (p.error || p.path == null || p.path.Count == 0) return null;
            return p;
        }

        private bool Reached(Path p, Vector3 goal)
        {
            if (p.vectorPath == null || p.vectorPath.Count == 0) return false;
            return Vector3.Distance(p.vectorPath[p.vectorPath.Count - 1], goal) <= reachThreshold;
        }

        // ---------- 점유 ----------
        private void Reserve(EnemyAgent agent, Path p)
        {
            var list = new List<GraphNode>();
            foreach (GraphNode n in p.path)
            {
                list.Add(n);
                occupancy.TryGetValue(n, out int c);
                occupancy[n] = c + 1;
            }
            reservations[agent] = list;
        }

        public void ReleaseReservation(EnemyAgent agent)
        {
            if (!reservations.TryGetValue(agent, out var list)) return;
            foreach (GraphNode n in list)
            {
                if (occupancy.TryGetValue(n, out int c))
                {
                    c = Mathf.Max(0, c - 1);
                    if (c == 0) occupancy.Remove(n); else occupancy[n] = c;
                }
            }
            reservations.Remove(agent);
        }

        // ---------- 재배정 (바리케이드 파괴 시) ----------
        private void QueueReassign() { reassignQueued = true; }

        void Update()
        {
            if (!reassignQueued) return;
            reassignQueued = false;

            // 한 마리씩 순서대로 → 앞 적의 점유를 뒤 적이 봄
            foreach (EnemyAgent a in agents.ToArray())
                if (a != null && a.isActiveAndEnabled) a.RequestRoute();

            Debug.Log($"[Pathfinding] 바리케이드 파괴 → {agents.Count}마리 재배정");
        }
    }
}