using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using NaughtyAttributes;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// AI判断の種類
    /// </summary>
    public enum AIActionType : byte
    {
        /// <summary>
        /// 接近行動
        /// </summary>
        Approach,

        /// <summary>
        /// 後退行動
        /// </summary>
        Retreat,

        /// <summary>
        /// 攻撃行動
        /// </summary>
        Attack,

        /// <summary>
        /// 防御行動
        /// </summary>
        Defend,

        /// <summary>
        /// 待機行動
        /// </summary>
        Wait,

        /// <summary>
        /// 特殊行動
        /// </summary>
        Special
    }

    /// <summary>
    /// AI行動データ
    /// </summary>
    [Serializable]
    public class AIAction
    {
        [Tooltip("行動の種類")]
        public AIActionType type;

        [Tooltip("攻撃の種類（攻撃行動時のみ）")]
        public AttackType attackType;

        [Tooltip("行動方向")]
        public AttackDirection direction;

        [Tooltip("特殊行動の種類")]
        public int specialType;

        [Tooltip("行動の優先度")]
        [Range(0f, 1f)]
        public float priority;

        /// <summary>
        /// AIActionを作成
        /// </summary>
        /// <param name="actionType">行動タイプ</param>
        /// <param name="priority">優先度</param>
        /// <returns>AI行動</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AIAction Create(AIActionType actionType, float priority = 0.5f)
        {
            return new AIAction
            {
                type = actionType,
                attackType = AttackType.WeakMelee,
                direction = AttackDirection.Up,
                specialType = 0,
                priority = priority
            };
        }
    }

    /// <summary>
    /// AIの個性・性格を定義するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "AI_Personality", menuName = "LearningAIGame/AI Personality")]
    public class AIPersonality : ScriptableObject
    {
        [Header("戦闘スタイル")]
        [Range(0f, 1f)]
        [Tooltip("攻撃性 - 高いほど積極的に攻撃を行う")]
        public float aggressiveness = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("守備性 - 高いほど防御を重視する")]
        public float defensiveness = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("機動性 - 高いほど移動・回避を多用する")]
        public float mobility = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("リスク許容度 - 高いほど危険な行動を取る")]
        public float riskTaking = 0.5f;

        [Header("判断傾向")]
        [Range(0f, 1f)]
        [Tooltip("反応速度 - 高いほど素早く行動する")]
        public float reactionSpeed = 0.8f;

        [Range(0f, 1f)]
        [Tooltip("行動パターンの多様性 - 高いほど予測困難")]
        public float patternVariation = 0.6f;

        [Range(0f, 1f)]
        [Tooltip("適応性 - 高いほど学習・適応が早い")]
        public float adaptability = 0.7f;

        [Header("特殊行動傾向")]
        [Tooltip("連続攻撃を好むかどうか")]
        public bool preferComboAttacks = false;

        [Tooltip("カウンター攻撃を好むかどうか")]
        public bool preferCounterAttacks = true;

        [Tooltip("高度なマニューバを使用するかどうか")]
        public bool useAdvancedManeuvers = false;

        [Tooltip("射撃戦を好むかどうか")]
        public bool preferRangedCombat = false;

        [Header("距離感設定")]
        [Tooltip("好む戦闘距離")]
        [Range(1f, 15f)]
        public float preferredCombatDistance = 5f;

        [Tooltip("危険と判断する距離")]
        [Range(0.5f, 3f)]
        public float dangerDistance = 2f;

        [Header("学習機能")]
        [SerializeField, ReadOnly]
        [Tooltip("各行動の重み（学習により変化）")]
        public Dictionary<ActionType, float> behaviorWeights = new Dictionary<ActionType, float>();

        [SerializeField, ReadOnly]
        [Tooltip("経験値ポイント")]
        public float experiencePoints = 0f;

        [SerializeField, ReadOnly]
        [Tooltip("適応レベル")]
        public int adaptationLevel = 1;

        /// <summary>
        /// 初期化処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnEnable()
        {
            InitializeBehaviorWeights();
        }

        /// <summary>
        /// 行動の重み付けを初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeBehaviorWeights()
        {
            if (this.behaviorWeights == null)
            {
                this.behaviorWeights = new Dictionary<ActionType, float>();
            }

            // 初期重み設定
            foreach (ActionType actionType in Enum.GetValues(typeof(ActionType)))
            {
                if (!this.behaviorWeights.ContainsKey(actionType))
                {
                    this.behaviorWeights[actionType] = 1.0f;
                }
            }
        }

        /// <summary>
        /// 行動を学習・強化する
        /// </summary>
        /// <param name="actionType">強化する行動</param>
        /// <param name="reinforcement">強化値（正で強化、負で弱化）</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReinforceBehavior(ActionType actionType, float reinforcement)
        {
            InitializeBehaviorWeights();

            if (!this.behaviorWeights.ContainsKey(actionType))
            {
                this.behaviorWeights[actionType] = 1.0f;
            }

            this.behaviorWeights[actionType] = Mathf.Clamp(
                this.behaviorWeights[actionType] + (reinforcement * this.adaptability),
                0.1f, 3.0f
            );

            // 経験値の蓄積
            this.experiencePoints += Mathf.Abs(reinforcement);

            // 適応レベルの更新
            this.adaptationLevel = Mathf.FloorToInt(this.experiencePoints / 100f) + 1;
        }

        /// <summary>
        /// 行動の重みを取得
        /// </summary>
        /// <param name="actionType">行動タイプ</param>
        /// <returns>重み</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetBehaviorWeight(ActionType actionType)
        {
            InitializeBehaviorWeights();
            return this.behaviorWeights.GetValueOrDefault(actionType, 1.0f);
        }

        /// <summary>
        /// 行動の優先度を計算
        /// </summary>
        /// <param name="actionType">行動タイプ</param>
        /// <param name="situationModifier">状況による修正値</param>
        /// <returns>優先度</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float CalculateActionPriority(ActionType actionType, float situationModifier = 1f)
        {
            var baseWeight = GetBehaviorWeight(actionType);
            var personalityModifier = GetPersonalityModifier(actionType);
            var randomVariation = UnityEngine.Random.Range(1f - this.patternVariation, 1f + this.patternVariation);

            return baseWeight * personalityModifier * situationModifier * randomVariation;
        }

        /// <summary>
        /// 個性による行動修正値を取得
        /// </summary>
        /// <param name="actionType">行動タイプ</param>
        /// <returns>修正値</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetPersonalityModifier(ActionType actionType)
        {
            return actionType switch
            {
                ActionType.WeakAttack or ActionType.StrongAttack or ActionType.SkillAttack =>
                    0.5f + this.aggressiveness,
                ActionType.Guard or ActionType.Block =>
                    0.5f + this.defensiveness,
                ActionType.Dodge or ActionType.Boost =>
                    0.5f + this.mobility,
                ActionType.Maneuver =>
                    this.useAdvancedManeuvers ? (0.8f + this.riskTaking * 0.4f) : 0.2f,
                _ => 1f
            };
        }
    }
}
