using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;

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
        [PropertyTooltip("行動の種類")]
        public AIActionType type;

        [PropertyTooltip("攻撃の種類（攻撃行動時のみ）")]
        public AttackType attackType;

        [PropertyTooltip("防御の種類（防御行動時のみ）")]
        public DefenseType defenseType;

        [PropertyTooltip("行動方向")]
        public AttackDirection direction;

        [PropertyTooltip("特殊行動の種類")]
        public int specialType;

        [PropertyTooltip("行動の優先度")]
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
                defenseType = DefenseType.Guard,
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
        [Title("戦闘スタイル")]
        [Range(0f, 1f)]
        [PropertyTooltip("攻撃性 - 高いほど積極的に攻撃を行う")]
        public float aggressiveness = 0.5f;

        [Range(0f, 1f)]
        [PropertyTooltip("守備性 - 高いほど防御を重視する")]
        public float defensiveness = 0.5f;

        [Range(0f, 1f)]
        [PropertyTooltip("機動性 - 高いほど移動・回避を多用する")]
        public float mobility = 0.5f;

        [Range(0f, 1f)]
        [PropertyTooltip("リスク許容度 - 高いほど危険な行動を取る")]
        public float riskTaking = 0.5f;

        [Title("判断傾向")]
        [Range(0f, 1f)]
        [PropertyTooltip("反応速度 - 高いほど素早く行動する")]
        public float reactionSpeed = 0.8f;

        [Range(0f, 1f)]
        [PropertyTooltip("行動パターンの多様性 - 高いほど予測困難")]
        public float patternVariation = 0.6f;

        [Range(0f, 1f)]
        [PropertyTooltip("適応性 - 高いほど学習・適応が早い")]
        public float adaptability = 0.7f;

        [Title("特殊行動傾向")]
        [PropertyTooltip("連続攻撃を好むかどうか")]
        public bool preferComboAttacks = false;

        [PropertyTooltip("カウンター攻撃を好むかどうか")]
        public bool preferCounterAttacks = true;

        [PropertyTooltip("高度なマニューバを使用するかどうか")]
        public bool useAdvancedManeuvers = false;

        [PropertyTooltip("射撃戦を好むかどうか")]
        public bool preferRangedCombat = false;

        [Title("距離感設定")]
        [PropertyTooltip("好む戦闘距離")]
        [Range(1f, 15f)]
        public float preferredCombatDistance = 5f;

        [PropertyTooltip("危険と判断する距離")]
        [Range(0.5f, 3f)]
        public float dangerDistance = 2f;

        [Title("学習機能")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("各行動の重み（学習により変化）")]
        public Dictionary<ActionType, float> behaviorWeights = new Dictionary<ActionType, float>();

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("経験値ポイント")]
        public float experiencePoints = 0f;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("適応レベル")]
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
            if ( this.behaviorWeights == null )
            {
                this.behaviorWeights = new Dictionary<ActionType, float>();
            }

            // 初期重み設定
            foreach ( ActionType actionType in Enum.GetValues(typeof(ActionType)) )
            {
                if ( !this.behaviorWeights.ContainsKey(actionType) )
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

            if ( !this.behaviorWeights.ContainsKey(actionType) )
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
                ActionType.WeakShoot or ActionType.StrongShoot =>
                    this.preferRangedCombat ? (0.8f + this.aggressiveness * 0.4f) : 0.4f,
                _ => 1f
            };
        }

        [Title("個性管理")]
        [Button("学習データリセット", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetLearningData()
        {
            this.behaviorWeights.Clear();
            this.experiencePoints = 0f;
            this.adaptationLevel = 1;
            InitializeBehaviorWeights();
            Debug.Log($"{this.name}の学習データをリセットしました");
        }

        [Button("個性ランダム生成", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RandomizePersonality()
        {
            this.aggressiveness = UnityEngine.Random.Range(0.2f, 0.9f);
            this.defensiveness = UnityEngine.Random.Range(0.2f, 0.9f);
            this.mobility = UnityEngine.Random.Range(0.2f, 0.9f);
            this.riskTaking = UnityEngine.Random.Range(0.1f, 0.8f);
            this.reactionSpeed = UnityEngine.Random.Range(0.5f, 1f);
            this.patternVariation = UnityEngine.Random.Range(0.3f, 0.8f);
            this.adaptability = UnityEngine.Random.Range(0.4f, 0.9f);

            this.preferComboAttacks = UnityEngine.Random.value > 0.5f;
            this.preferCounterAttacks = UnityEngine.Random.value > 0.4f;
            this.useAdvancedManeuvers = UnityEngine.Random.value > 0.7f;
            this.preferRangedCombat = UnityEngine.Random.value > 0.6f;

            this.preferredCombatDistance = UnityEngine.Random.Range(3f, 12f);
            this.dangerDistance = UnityEngine.Random.Range(1f, 3f);

            Debug.Log($"{this.name}の個性をランダム生成しました");
        }

        [Title("個性プリセット")]
        [HorizontalGroup("プリセット")]
        [Button("攻撃型")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetAggressivePreset()
        {
            this.aggressiveness = 0.9f;
            this.defensiveness = 0.3f;
            this.mobility = 0.6f;
            this.riskTaking = 0.8f;
            this.preferComboAttacks = true;
            this.preferCounterAttacks = false;
            this.preferredCombatDistance = 3f;
        }

        [HorizontalGroup("プリセット")]
        [Button("防御型")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetDefensivePreset()
        {
            this.aggressiveness = 0.3f;
            this.defensiveness = 0.9f;
            this.mobility = 0.4f;
            this.riskTaking = 0.2f;
            this.preferComboAttacks = false;
            this.preferCounterAttacks = true;
            this.preferredCombatDistance = 6f;
        }

        [HorizontalGroup("プリセット")]
        [Button("機動型")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetMobilePreset()
        {
            this.aggressiveness = 0.6f;
            this.defensiveness = 0.4f;
            this.mobility = 0.9f;
            this.riskTaking = 0.7f;
            this.useAdvancedManeuvers = true;
            this.preferredCombatDistance = 8f;
        }
    }
}
