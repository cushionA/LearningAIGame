using LLMDataArchitect;
using UnityEngine;

namespace LearningAIGame.CombatSystem.AI
{
    /// <summary>
    /// LLMが作成したルールデータの注入用基底クラス
    /// </summary>
    public abstract class RuleBaseInjection : MonoBehaviour
    {
        /// <summary>
        /// 現在の戦術データを含むLLMデータ
        /// </summary>
        protected LLMInputData _llmData;

        /// <summary>
        /// 戦術データの初期化
        /// </summary>
        public abstract void InjectionData(LLMInputData data);

        /// <summary>
        /// 戦術が更新された際の処理
        /// </summary>
        public abstract void UpdateStrategy();
    }
}
