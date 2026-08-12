namespace UJam.Runtime.Enemy
{
    /// <summary>
    /// 근거리 잡몹의 행동을 구체화하는 클래스
    /// 사실상 enemyBase의 행동을 그대로 따라하므로 더이상 구체화 할 것이 없다.
    /// 이것을 상속받는 MeleeEnemy들은 Animation과 Movement 정도만 구체화 하면 될 것 같다.
    /// </summary>
    public class MeleeEnemy : EnemyBase
    {
    }
}
