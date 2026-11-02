using System;
using System.Reflection;
using UJam.Runtime.Enemy;
using UJam.Runtime.Enemy.Movement;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UJam.EditorTools
{
    public static class EnemyCoreChecks
    {
        [MenuItem("Tools/Enemies/Run Core Checks")]
        public static void Run()
        {
            Require((int)EnemyStateType.Idle == 1 && (int)EnemyStateType.Move == 2 &&
                    (int)EnemyStateType.Attack == 3 && (int)EnemyStateType.Dead == 4,
                "Existing serialized enemy state values changed.");
            CheckInitialization();
            CheckReflection();
            CheckRangeCrossing();
            Debug.Log("[EnemyCoreChecks] Initialization, Idle, wall reflection and swept range checks passed.");
        }

        private static void CheckInitialization()
        {
            var root = new GameObject("EnemyCoreChecks");
            root.SetActive(false);
            try
            {
                EnemyStatus status = root.AddComponent<EnemyStatus>();
                status.ConfigureSpawnStats(100f, 20f, 10);
                MeleeEnemy enemy = root.AddComponent<MeleeEnemy>();
                typeof(EnemyBase).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(enemy, null);
                Require(enemy.FSM.state == EnemyStateType.Idle && status.HP == 100f,
                    "Init did not initialize the enemy before entering Idle.");
                status.ApplyDamage(25f, true);
                enemy.Idle();
                Require(status.HP == 75f, "Idle healed the enemy.");
                enemy.Init();
                Require(status.HP == 75f, "Repeated initialization healed the enemy.");
                Require(!enemy.FSM.SetState(EnemyStateType.Init) && !enemy.FSM.SetState(EnemyStateType.None) &&
                        enemy.FSM.state == EnemyStateType.Idle,
                    "FSM accepted an invalid initialization transition.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void CheckReflection()
        {
            var direction = new Vector3(.6f, 0f, .8f);
            var position = new Vector3(17f, 2f, 3f);
            RollbugMovement.ReflectPosition(ref position, ref direction, -16f, 16f);
            Require(Near(position.x, 15f) && Near(direction.x, -.6f) && Near(direction.z, .8f) &&
                    Near(position.y, 2f) && Near(position.z, 3f) && Near(direction.magnitude, 1f),
                "Right wall reflection changed descent, height or speed.");

            position.x = -18f;
            RollbugMovement.ReflectPosition(ref position, ref direction, -16f, 16f);
            Require(Near(position.x, -14f) && Near(direction.x, .6f), "Left wall overshoot was lost.");

            position.x = 82f;
            RollbugMovement.ReflectPosition(ref position, ref direction, -16f, 16f);
            Require(Near(position.x, 14f) && Near(direction.x, -.6f), "Multiple wall crossings were not reflected.");

            position.x = -16f;
            RollbugMovement.ReflectPosition(ref position, ref direction, -16f, 16f);
            Require(Near(position.x, -16f) && Near(direction.x, .6f), "Exact wall contact did not turn inward.");
        }

        private static void CheckRangeCrossing()
        {
            Require(RollbugMovement.TryRangeEntry(new Vector2(-2f, 0f), new Vector2(2f, 0f), .5f, out float fraction)
                    && Near(fraction, .375f), "Fast movement skipped a target's attack range.");
            Require(RollbugMovement.TryRangeEntry(Vector2.zero, Vector2.right, .5f, out fraction)
                    && Near(fraction, 0f), "A target already in range was missed.");
            Require(!RollbugMovement.TryRangeEntry(new Vector2(-2f, 0f), new Vector2(-2f, 0f), .5f, out _),
                "Stationary movement outside range counted as a hit.");
            Require(RollbugMovement.TryRangeEntry(new Vector2(-2f, .5f), new Vector2(2f, .5f), .5f, out fraction)
                    && Near(fraction, .5f), "A tangent contact was missed.");
            Require(!RollbugMovement.TryRangeEntry(new Vector2(-2f, .6f), new Vector2(2f, .6f), .5f, out _),
                "A near miss counted as entering attack range.");
            Require(!RollbugMovement.TryRangeEntry(new Vector2(-2f, 0f), new Vector2(-3f, 0f), .5f, out _),
                "Moving away from a target counted as a hit.");
        }

        private static bool Near(float a, float b) => Mathf.Abs(a - b) < .00001f;
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
