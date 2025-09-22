using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UniRx;
using NaughtyAttributes;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// AIコントローラー - AI判断に基づいてキャラクターを制御
    /// </summary>
    public class AIController : BattleCharacterController
    {
        [Header("AI設定")]
        [Tooltip("AI個性設定")]
        [SerializeField] private AIPersonality _personality;

        [Tooltip("反応時間（秒）")]
        [Range(0f, 1f)]
        [SerializeField] private float _reactionTime = 0.1f;

        [Tooltip("判断更新頻度（秒）")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _decisionUpdateRate = 0.05f;

        [Header("難易度設定")]
        [Tooltip("完璧なブロッキングを行うかどうか")]
        [SerializeField] private bool _enablePerfectBlocking = false;

        [Tooltip("ジャスト回避を狙うかどうか")]
        [SerializeField]
        private bool _enableJust = true;

        [Tooltip("高度な戦術を使用するかどうか")]
        [SerializeField] private bool _useAdvancedTactics = true;

        [Header("現在の状態")]
        [Tooltip("現在の戦術状態")]
        public AITacticalState CurrentTacticalState { get; private set; } = AITacticalState.Neutral;

        [Tooltip("最後の判断時間")]
        public float LastDecisionTime { get; private set; } = 0f;

        [Tooltip("現在の計画されたアクション")]
        public AIAction PlannedAction { get; private set; }

        public OpponentDataProvider OpponentData { get; private set; }

        // 内部状態
        private BattleSituation _lastSituation;
        private float _lastOpponentActionTime = 0f;
        private ActionState _lastOpponentState = ActionState.Idle;
        private Queue<AIAction> _actionQueue = new Queue<AIAction>();

        /// <summary>
        /// AI戦術状態
        /// </summary>
        public enum AITacticalState : byte
        {
            Aggressive,    // 攻撃的
            Defensive,     // 防御的
            Neutral,       // 中立
            Retreat,       // 撤退
            Opportunity    // 機会待ち
        }

        /// <summary>
        /// 戦闘状況データ
        /// </summary>
        public struct BattleSituation
        {
            // 距離情報
            public float distanceToOpponent;
            public bool isInMeleeRange;
            public bool isInSafeRange;

            // 相手の状態
            public float opponentHealth;
            public float opponentEnergy;
            public ActionState opponentState;
            public AttackDirection opponentDirection;
            public bool opponentVulnerable;

            // 自分の状態
            public float myHealth;
            public float myEnergy;
            public ActionMode myMode;
            public bool canUseSkills;

            // 戦況判断
            public AdvantageState advantageState;
            public ThreatLevel threatLevel;
        }

        /// <summary>
        /// 優劣状態
        /// </summary>
        public enum AdvantageState : byte
        {
            Disadvantage,
            Even,
            Advantage
        }

        /// <summary>
        /// 脅威レベル
        /// </summary>
        public enum ThreatLevel : byte
        {
            Low,
            Medium,
            High,
            Critical
        }

        /// <summary>
        /// 次の行動を決定（AI判断ベース）
        /// </summary>
        protected override void DecideNextAction()
        {


            // 状況分析と判断
            var situation = AnalyzeSituation();
            PlannedAction = MakeDecision(situation);

            // 学習・適応処理
            UpdateLearning(situation);
        }

        /// <summary>
        /// 状況を分析
        /// </summary>
        /// <returns>戦闘状況</returns>
        private BattleSituation AnalyzeSituation()
        {
            var situation = new BattleSituation
            {
                // 距離情報
                distanceToOpponent = GetDistanceToOpponent(),
                isInMeleeRange = IsInRange(5f), // Settings.attack.meleeRange の代替
                isInSafeRange = GetDistanceToOpponent() > 8f, // Settings.movement.safeDistance の代替

                // 相手の状態
                opponentHealth = OpponentData.HealthPercentage,
                opponentEnergy = OpponentData.EnergyPercentage,
                opponentState = OpponentData.CurrentState,
                opponentDirection = OpponentData.CurrentDirection,
                opponentVulnerable = IsOpponentVulnerable(),

                // 自分の状態
                myHealth = CurrentHealthPercentage,
                myEnergy = CurrentEnergyPercentage,

                // 戦況判断
                advantageState = CalculateAdvantageState(),
                threatLevel = CalculateThreatLevel()
            };

            return situation;
        }

        /// <summary>
        /// AIの判断を行う
        /// </summary>
        /// <param name="situation">戦闘状況</param>
        /// <returns>AIアクション</returns>
        private AIAction MakeDecision(BattleSituation situation)
        {
            // 戦術状態の更新
            UpdateTacticalState(situation);

            // 緊急行動の判定
            var emergencyAction = CheckEmergencyActions(situation);
            if (emergencyAction != null)
                return emergencyAction;

            // 戦術状態に基づく判断
            switch (CurrentTacticalState)
            {
                case AITacticalState.Aggressive:
                    return DecideAggressiveAction(situation);
                case AITacticalState.Defensive:
                    return DecideDefensiveAction(situation);
                case AITacticalState.Retreat:
                    return DecideRetreatAction(situation);
                case AITacticalState.Opportunity:
                    return DecideOpportunityAction(situation);
                default:
                    return DecideNeutralAction(situation);
            }
        }

        /// <summary>
        /// 攻撃的な行動を決定
        /// </summary>
        /// <param name="situation">戦闘状況</param>
        /// <returns>AIアクション</returns>
        private AIAction DecideAggressiveAction(BattleSituation situation)
        {
            if (situation.opponentVulnerable)
            {
                // 相手が脆弱な状態 - 最大火力攻撃
                return AIAction.Create(AIActionType.Attack, 0.9f);
            }

            if (situation.isInMeleeRange)
            {
                // 近距離での連続攻撃
                var attackWeight = _personality.GetBehaviorWeight(ActionType.WeakAttack) * _personality.aggressiveness;
                return AIAction.Create(AIActionType.Attack, attackWeight);
            }

            // 接近行動
            return AIAction.Create(AIActionType.Approach, _personality.aggressiveness);
        }

        /// <summary>
        /// 防御的な行動を決定
        /// </summary>
        /// <param name="situation">戦闘状況</param>
        /// <returns>AIアクション</returns>
        private AIAction DecideDefensiveAction(BattleSituation situation)
        {
            if (situation.opponentState == ActionState.Attacking)
            {
                // 相手が攻撃中 - ブロッキングまたはガード
                if (_enablePerfectBlocking && _personality.preferCounterAttacks)
                {
                    return AIAction.Create(AIActionType.Defend, 0.8f);
                }
                else
                {
                    return AIAction.Create(AIActionType.Defend, 0.7f);
                }
            }

            if (!situation.isInSafeRange)
            {
                // 安全距離まで後退
                return AIAction.Create(AIActionType.Retreat, _personality.defensiveness);
            }

            // 距離を保って様子見
            return AIAction.Create(AIActionType.Wait, 0.5f);
        }

        /// <summary>
        /// 撤退行動を決定
        /// </summary>
        /// <param name="situation">戦闘状況</param>
        /// <returns>AIアクション</returns>
        private AIAction DecideRetreatAction(BattleSituation situation)
        {
            if (situation.myEnergy < 0.3f)
            {
                // エネルギー不足 - 距離を取ってエネルギー回復
                return AIAction.Create(AIActionType.Retreat, 0.9f);
            }

            if (situation.isInSafeRange && situation.myHealth > 0.5f)
            {
                // 回復完了 - 中立状態に復帰
                return AIAction.Create(AIActionType.Wait, 0.3f);
            }

            // 継続撤退
            return AIAction.Create(AIActionType.Retreat, 0.8f);
        }

        /// <summary>
        /// 機会待ち行動を決定
        /// </summary>
        /// <param name="situation">戦闘状況</param>
        /// <returns>AIアクション</returns>
        private AIAction DecideOpportunityAction(BattleSituation situation)
        {
            if (situation.opponentVulnerable)
            {
                // 機会到来 - カウンター攻撃
                return AIAction.Create(AIActionType.Attack, 1.0f);
            }

            if (situation.opponentEnergy < 0.2f)
            {
                // 相手のエネルギー不足を狙う
                return AIAction.Create(AIActionType.Approach, 0.7f);
            }

            // 継続待機
            return AIAction.Create(AIActionType.Wait, 0.4f);
        }

        /// <summary>
        /// 中立的な行動を決定
        /// </summary>
        /// <param name="situation">戦闘状況</param>
        /// <returns>AIアクション</returns>
        private AIAction DecideNeutralAction(BattleSituation situation)
        {
            // 距離に応じた基本行動
            if (situation.isInMeleeRange)
            {
                // 近距離 - 攻撃か後退
                float attackPriority = _personality.CalculateActionPriority(ActionType.WeakAttack);
                float retreatPriority = _personality.CalculateActionPriority(ActionType.Dodge);

                if (attackPriority > retreatPriority)
                {
                    return AIAction.Create(AIActionType.Attack, attackPriority);
                }
                else
                {
                    return AIAction.Create(AIActionType.Retreat, retreatPriority);
                }
            }
            else if (situation.distanceToOpponent > _personality.preferredCombatDistance)
            {
                // 遠距離 - 接近
                return AIAction.Create(AIActionType.Approach, 0.6f);
            }
            else
            {
                // 適正距離 - 様子見または攻撃
                return AIAction.Create(AIActionType.Wait, 0.5f);
            }
        }

        /// <summary>
        /// 緊急行動のチェック
        /// </summary>
        /// <param name="situation">戦闘状況</param>
        /// <returns>緊急アクション（なければnull）</returns>
        private AIAction CheckEmergencyActions(BattleSituation situation)
        {
            // 体力危険域
            if (situation.myHealth < 0.2f && situation.threatLevel == ThreatLevel.Critical)
            {
                return AIAction.Create(AIActionType.Retreat, 1.0f);
            }

            //// エネルギー枯渇時の特殊対応
            //if (situation.myEnergy <= 0f && stateSystem.CurrentActionMode == ActionMode.EnergyBarrier)
            //{
            //    return AIAction.Create(AIActionType.Defend, 0.9f);
            //}

            return null;
        }

        /// <summary>
        /// 戦術状態を更新
        /// </summary>
        /// <param name="situation">戦闘状況</param>
        private void UpdateTacticalState(BattleSituation situation)
        {
            var newState = CurrentTacticalState;

            // 体力・エネルギー状況による判定
            if (situation.myHealth < 0.3f || situation.myEnergy < 0.2f)
            {
                newState = AITacticalState.Retreat;
            }
            else if (situation.advantageState == AdvantageState.Advantage && _personality.aggressiveness > 0.6f)
            {
                newState = AITacticalState.Aggressive;
            }
            else if (situation.advantageState == AdvantageState.Disadvantage || situation.threatLevel == ThreatLevel.High)
            {
                newState = AITacticalState.Defensive;
            }
            else if (_personality.preferCounterAttacks && situation.opponentState == ActionState.Attacking)
            {
                newState = AITacticalState.Opportunity;
            }
            else
            {
                newState = AITacticalState.Neutral;
            }

            CurrentTacticalState = newState;
        }

        /// <summary>
        /// 優劣状態を計算
        /// </summary>
        /// <returns>優劣状態</returns>
        private AdvantageState CalculateAdvantageState()
        {
            float myScore = CurrentHealthPercentage + CurrentEnergyPercentage;
            float opponentScore = OpponentData.HealthPercentage + OpponentData.EnergyPercentage;

            float difference = myScore - opponentScore;

            if (difference > 0.3f)
                return AdvantageState.Advantage;
            else if (difference < -0.3f)
                return AdvantageState.Disadvantage;
            else
                return AdvantageState.Even;
        }

        /// <summary>
        /// 脅威レベルを計算
        /// </summary>
        /// <returns>脅威レベル</returns>
        private ThreatLevel CalculateThreatLevel()
        {
            float threatScore = 0f;

            // 距離による脅威
            if (GetDistanceToOpponent() < _personality.dangerDistance)
                threatScore += 0.3f;

            // 相手の状態による脅威
            if (OpponentData.CurrentState == ActionState.Attacking)
                threatScore += 0.4f;

            if (OpponentData.EnergyPercentage > 0.7f)
                threatScore += 0.2f;

            // 自分の状態による脅威増加
            if (CurrentHealthPercentage < 0.3f)
                threatScore += 0.3f;

            if (threatScore > 0.8f)
                return ThreatLevel.Critical;
            else if (threatScore > 0.6f)
                return ThreatLevel.High;
            else if (threatScore > 0.3f)
                return ThreatLevel.Medium;
            else
                return ThreatLevel.Low;
        }


        /// <summary>
        /// アクションに遅延が必要かどうか
        /// </summary>
        /// <param name="action">アクション</param>
        /// <returns>遅延が必要かどうか</returns>
        private bool ShouldDelayAction(AIAction action)
        {
            // 防御アクションは遅延なし
            if (action.type == AIActionType.Defend)
                return false;

            // 反応速度の個性による
            return UnityEngine.Random.value > _personality.reactionSpeed;
        }


        /// <summary>
        /// 学習・適応処理
        /// </summary>
        /// <param name="situation">戦闘状況</param>
        private void UpdateLearning(BattleSituation situation)
        {
            // 相手の行動変化を記録
            if (situation.opponentState != _lastOpponentState)
            {
                _lastOpponentActionTime = Time.time;
                _lastOpponentState = situation.opponentState;
            }

            _lastSituation = situation;
        }

        /// <summary>
        /// アクション成功時の学習
        /// </summary>
        /// <param name="actionType">成功したアクション</param>
        protected override void OnActionSucceeded(ActionType actionType)
        {
            _personality.ReinforceBehavior(actionType, 1.0f);
        }

        /// <summary>
        /// アクション失敗時の学習
        /// </summary>
        /// <param name="actionType">失敗したアクション</param>
        protected override void OnActionFailed(ActionType actionType)
        {
            _personality.ReinforceBehavior(actionType, -0.5f);
        }

        /// <summary>
        /// 対戦相手状態変化時の反応
        /// </summary>
        /// <param name="newState">新しい状態</param>
        protected override void OnOpponentStateChanged(ActionState newState)
        {
            if (newState == ActionState.Attacking && _enablePerfectBlocking)
            {
                // 完璧なブロッキングを試行
                var blockDirection = OpponentData.CurrentDirection;
                ExecuteBlock(blockDirection);
            }
        }
    }
}
