using LearningAIGame.CombatSystem.AI;
using LearningAIGame.CombatSystem.Data;
using LLMDataArchitect;
using UnityEngine;

public class CombatTestAI : RuleBaseInjection
{
    /// <summary>
    /// 戦術の実行結果
    /// </summary>
    private StrategyResult _strategyResult;

    public AIParameterContainer parameterContainer;

    public LLMInputData LLMData { get => _llmData; }

    /// <summary>
    /// 戦術データの初期化
    /// </summary>
    public override void InjectionData(LLMInputData data)
    {
        _llmData = data;
        // 行動の結果を記録するためにLLMからインスタンスを受け取る
        _strategyResult = _llmData.StrategyResult;
        Debug.Log($"[{nameof(StrategyAI)}] 戦術データを注入しました");

        // 戦術結果の初期化
        UpdateStrategy();
    }

    /// <summary>
    /// 戦術更新時の処理
    /// </summary>
    public override void UpdateStrategy()
    {
        if (_llmData.CurrentStrategy == null)
        {
            Debug.LogWarning($"[{nameof(StrategyAI)}] 戦術データが注入されていません。デフォルトデータを使用します。");
            _llmData.CurrentStrategy = parameterContainer.defaultStrategyData;
        }
    }

}
