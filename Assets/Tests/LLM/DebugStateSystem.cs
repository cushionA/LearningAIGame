using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using UnityEngine;

public class DebugStateSystem : StateSystem
{

    /// <summary>
    /// デバッグ用のダメージ処理
    /// </summary>
    public void DebugDamage(int amount)
    {
        _characterData.TakeDamage(amount);
    }

    public void DebugRecoverEnergy(float amount)
    {
        _characterData.RecoverEnergyByRate(amount);
    }

    public void DebugOnHit(HitReportInfo hitReportInfo)
    {
        OnHit(hitReportInfo);
    }
    public void DebagOnDamage(DamageReportInfo damageReportInfo)
    {
        OnDamage(damageReportInfo);
    }

    public CharacterData GetCharacterData()
    {
        return _characterData;
    }
}
