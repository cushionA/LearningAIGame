using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Systems;
using UnityEngine;

public class DebugHitSystem : HitSystem
{
    /// <summary>
    /// ヒットするかどうか
    /// </summary>
    public bool isHit = true;

    /// <summary>
    /// 攻撃を開始するメソッド
    /// 必中
    /// </summary>
    /// <param name="attackInfo">攻撃情報</param>
    /// <param name="attackDurationFrame">攻撃判定の持続フレーム数</param>
    public override void DamageStart(in AttackInfo attackInfo, int attackDurationFrame)
    {

        // 攻撃結果の初期化
        _info.InitializeDamage(attackInfo);

        // 攻撃情報を保存
        _currentAttack = attackInfo;

        if (isHit)
        {
            AttackHit();
        }
        else
        {
            NotifyObservers(_info);
        }
    }

    /// <summary>
    /// ヒット結果に応じた処理
    /// </summary>
    protected override void ProcessHitResult(HitResultType result)
    {
        AttackResultReport();
    }
}
