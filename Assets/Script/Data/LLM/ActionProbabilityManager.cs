using UnityEngine;

namespace LLMDataArchitect
{
    /// <summary>
    /// AIキャラクターの各アクションの実行確率を管理するクラス
    /// プロンプトで使用される確率名に対応
    /// </summary>
    public class ActionProbabilityManager
    {
        /// <summary>
        /// 後ろ回避の実行確率
        /// </summary>
        public float BackwardDodgePercentage { get; set; }

        /// <summary>
        /// 横回避の実行確率（左右統合）
        /// </summary>
        public float HorizontalDodgePercentage { get; set; }

        /// <summary>
        /// 前回避の実行確率
        /// </summary>
        public float ForwardDodgePercentage { get; set; }

        /// <summary>
        /// ガードの実行確率
        /// </summary>
        public float GuardPercentage { get; set; }

        /// <summary>
        /// ブロッキングの実行確率
        /// </summary>
        public float BlockingPercentage { get; set; }

        /// <summary>
        /// 弱攻撃の実行確率
        /// </summary>
        public float LightAttackPercentage { get; set; }

        /// <summary>
        /// 強攻撃の実行確率
        /// </summary>
        public float StrongAttackPercentage { get; set; }

        /// <summary>
        /// 強攻撃キャンセルの実行確率
        /// </summary>
        public float StrongAttackCancelPercentage { get; set; }

        /// <summary>
        /// 横回避攻撃の実行確率
        /// </summary>
        public float HorizontalDodgeAttackPercentage { get; set; }

        /// <summary>
        /// 前回避攻撃の実行確率
        /// </summary>
        public float ForwardDodgeAttackPercentage { get; set; }

        /// <summary>
        /// コンストラクタ。基本確率で初期化
        /// </summary>
        public ActionProbabilityManager()
        {
            InitializeBasicProbabilities();
        }

        /// <summary>
        /// 基本的な確率で初期化
        /// </summary>
        public void InitializeBasicProbabilities()
        {
            BackwardDodgePercentage = 0.05f;
            HorizontalDodgePercentage = 0.05f;
            ForwardDodgePercentage = 0.15f;
            GuardPercentage = 0.05f;
            BlockingPercentage = 0.05f;
            LightAttackPercentage = 0.25f;
            StrongAttackPercentage = 0.20f;
            StrongAttackCancelPercentage = 0.05f;
            HorizontalDodgeAttackPercentage = 0.10f;
            ForwardDodgeAttackPercentage = 0.10f;
        }
    }

}
