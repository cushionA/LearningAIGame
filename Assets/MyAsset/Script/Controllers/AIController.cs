using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;
using UniRx;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// AIコントローラー - AI判断に基づいてキャラクターを制御
    /// </summary>
    public class AIController : BattleCharacterController
    {
        [Title("AI設定")]
        [Required, PropertyTooltip("AI個性設定")]
        [SerializeField] private AIPersonality personality;

        [PropertyTooltip("反応時間（秒）")]
        [Range(0f, 1f)]
        [SerializeField] private float reactionTime = 0.1f;

        [PropertyTooltip("判断更新頻度（秒）")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float decisionUpdateRate = 0.05f;

        [Title("難易度設定")]
        [PropertyTooltip("完璧なブロッキングを行うかどうか")]
        [SerializeField] private bool enablePerfectBlocking = false;

        [PropertyTooltip("ジャスト回避を狙うかどうか")]
        [SerializeField] private bool enableJustDodge = true;

        [PropertyTooltip("高度な戦術を使用するかどうか")]
        [SerializeField] private bool useAdvancedTactics = true;

        [Title("現在の状態")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の戦術状態")]
        public AITacticalState CurrentTacticalState { get; private set; } = AITacticalState.Neutral;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("最後の判断時間")]
        public float LastDecisionTime { get; private set; } = 0f;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の計画されたアクション")]
        public AIAction PlannedAction { get; private set; }

        // 内部状態
        private BattleSituation lastSituation;
        private float lastOpponentActionTime = 0f;
        private ActionState lastOpponentState = ActionState.Idle;
        private Queue<AIAction> actionQueue = new Queue<AIAction>();

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
            // 判断更新頻度制御
            if (Time.time - LastDecisionTime < decisionUpdateRate)
            {
                ExecutePlannedAction();
                return;
            }

            // 状況分析と判断
            var situation = AnalyzeSituation();
            PlannedAction = MakeDecision(situation);
            
            // リアクションタイム考慮してアクション実行
            HandleActionExecution(PlannedAction);
            LastDecisionTime = Time.time;

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
                myMode = stateSystem.CurrentActionMode,
                canUseSkills = stateSystem.AnalysisData.canUseSkills,
                
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
                var attackWeight = personality.GetBehaviorWeight(ActionType.WeakAttack) * personality.aggressiveness;
                return AIAction.Create(AIActionType.Attack, attackWeight);
            }

            // 接近行動
            return AIAction.Create(AIActionType.Approach, personality.aggressiveness);
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
                if (enablePerfectBlocking && personality.preferCounterAttacks)
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
                return AIAction.Create(AIActionType.Retreat, personality.defensiveness);
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
                float attackPriority = personality.CalculateActionPriority(ActionType.WeakAttack);
                float retreatPriority = personality.CalculateActionPriority(ActionType.Dodge);

                if (attackPriority > retreatPriority)
                {
                    return AIAction.Create(AIActionType.Attack, attackPriority);
                }
                else
                {
                    return AIAction.Create(AIActionType.Retreat, retreatPriority);
                }
            }
            else if (situation.distanceToOpponent > personality.preferredCombatDistance)
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

            // エネルギー枯渇時の特殊対応
            if (situation.myEnergy <= 0f && stateSystem.CurrentActionMode == ActionMode.EnergyBarrier)
            {
                return AIAction.Create(AIActionType.Defend, 0.9f);
            }

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
            else if (situation.advantageState == AdvantageState.Advantage && personality.aggressiveness > 0.6f)
            {
                newState = AITacticalState.Aggressive;
            }
            else if (situation.advantageState == AdvantageState.Disadvantage || situation.threatLevel == ThreatLevel.High)
            {
                newState = AITacticalState.Defensive;
            }
            else if (personality.preferCounterAttacks && situation.opponentState == ActionState.Attacking)
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
            if (GetDistanceToOpponent() < personality.dangerDistance)
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
        /// アクション実行の処理
        /// </summary>
        /// <param name="action">実行するアクション</param>
        private void HandleActionExecution(AIAction action)
        {
            if (reactionTime > 0f && ShouldDelayAction(action))
            {
                StartCoroutine(DelayedActionExecution(action, reactionTime));
            }
            else
            {
                ExecuteAIAction(action);
            }
        }

        /// <summary>
        /// 遅延実行のコルーチン
        /// </summary>
        /// <param name="action">実行するアクション</param>
        /// <param name="delay">遅延時間</param>
        /// <returns>コルーチン</returns>
        private IEnumerator DelayedActionExecution(AIAction action, float delay)
        {
            yield return new WaitForSeconds(delay);
            ExecuteAIAction(action);
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
            return UnityEngine.Random.value > personality.reactionSpeed;
        }

        /// <summary>
        /// 計画されたアクションを実行
        /// </summary>
        private void ExecutePlannedAction()
        {
            if (PlannedAction != null)
            {
                ExecuteAIAction(PlannedAction);
            }
        }

        /// <summary>
        /// AIアクションを実行
        /// </summary>
        /// <param name="action">実行するアクション</param>
        private void ExecuteAIAction(AIAction action)
        {
            switch (action.type)
            {
                case AIActionType.Approach:
                    ExecuteMovementToward(OpponentData.Position);
                    break;
                    
                case AIActionType.Retreat:
                    ExecuteEvasiveAction();
                    break;
                    
                case AIActionType.Attack:
                    ExecuteAttackAction(action);
                    break;
                    
                case AIActionType.Defend:
                    ExecuteDefenseAction(action);
                    break;
                    
                case AIActionType.Wait:
                    // 待機（何もしない）
                    break;
                    
                case AIActionType.Special:
                    ExecuteSpecialAction(action);
                    break;
            }
        }

        /// <summary>
        /// 攻撃アクションを実行
        /// </summary>
        /// <param name="action">攻撃アクション</param>
        private void ExecuteAttackAction(AIAction action)
        {
            var attackDirection = GetOptimalAttackDirection();
            
            // デフォルトは弱攻撃
            switch (action.attackType)
            {
                case AttackType.WeakMelee:
                    ExecuteWeakAttack(attackDirection);
                    break;
                case AttackType.StrongMelee:
                    ExecuteStrongAttack(attackDirection);
                    break;
                case AttackType.MeleeSkill:
                    ExecuteSkill(0);
                    break;
                case AttackType.WeakRanged:
                    // 射撃系はswitchCombatModeで射撃モードに変更後実行
                    if (stateSystem.CurrentActionMode != ActionMode.Ranged)
                        SwitchCombatMode();
                    break;
                case AttackType.StrongRanged:
                    if (stateSystem.CurrentActionMode != ActionMode.Ranged)
                        SwitchCombatMode();
                    break;
                default:
                    ExecuteWeakAttack(attackDirection);
                    break;
            }
        }

        /// <summary>
        /// 防御アクションを実行
        /// </summary>
        /// <param name="action">防御アクション</param>
        private void ExecuteDefenseAction(AIAction action)
        {
            var defenseDirection = OpponentData.CurrentDirection;
            
            switch (action.defenseType)
            {
                case DefenseType.Guard:
                    ExecuteGuard(defenseDirection);
                    break;
                case DefenseType.Block:
                    ExecuteBlock(defenseDirection);
                    break;
                case DefenseType.Dodge:
                    ExecuteDodge(-GetDirectionToOpponent());
                    break;
                default:
                    ExecuteGuard(defenseDirection);
                    break;
            }
        }

        /// <summary>
        /// 特殊アクションを実行
        /// </summary>
        /// <param name="action">特殊アクション</param>
        private void ExecuteSpecialAction(AIAction action)
        {
            switch (action.specialType)
            {
                case 0: // マニューバ
                    ExecuteManeuver(0);
                    break;
                case 1: // モード切替
                    SwitchCombatMode();
                    break;
                case 2: // クイックターン
                    ExecuteQuickTurn();
                    break;
            }
        }

        /// <summary>
        /// 学習・適応処理
        /// </summary>
        /// <param name="situation">戦闘状況</param>
        private void UpdateLearning(BattleSituation situation)
        {
            // 相手の行動変化を記録
            if (situation.opponentState != lastOpponentState)
            {
                lastOpponentActionTime = Time.time;
                lastOpponentState = situation.opponentState;
            }

            lastSituation = situation;
        }

        /// <summary>
        /// アクション成功時の学習
        /// </summary>
        /// <param name="actionType">成功したアクション</param>
        protected override void OnActionSucceeded(ActionType actionType)
        {
            personality.ReinforceBehavior(actionType, 1.0f);
        }

        /// <summary>
        /// アクション失敗時の学習
        /// </summary>
        /// <param name="actionType">失敗したアクション</param>
        protected override void OnActionFailed(ActionType actionType)
        {
            personality.ReinforceBehavior(actionType, -0.5f);
        }

        /// <summary>
        /// 対戦相手状態変化時の反応
        /// </summary>
        /// <param name="newState">新しい状態</param>
        protected override void OnOpponentStateChanged(ActionState newState)
        {
            if (newState == ActionState.Attacking && enablePerfectBlocking)
            {
                // 完璧なブロッキングを試行
                var blockDirection = OpponentData.CurrentDirection;
                ExecuteBlock(blockDirection);
            }
        }

        #region Debug Methods

        [Title("AIデバッグ機能")]
        [Button("戦術状態: 攻撃的", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.8f)]
        private void DebugSetAggressive()
        {
            CurrentTacticalState = AITacticalState.Aggressive;
            Debug.Log("AI戦術状態: 攻撃的");
        }

        [Button("戦術状態: 防御的", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 1f)]
        private void DebugSetDefensive()
        {
            CurrentTacticalState = AITacticalState.Defensive;
            Debug.Log("AI戦術状態: 防御的");
        }

        [Button("完璧ブロッキング切替", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        private void DebugTogglePerfectBlocking()
        {
            enablePerfectBlocking = !enablePerfectBlocking;
            Debug.Log($"完璧ブロッキング: {(enablePerfectBlocking ? "ON" : "OFF")}");
        }

        [Button("状況分析結果出力", ButtonSizes.Medium)]
        [GUIColor(1f, 1f, 0.8f)]
        private void DebugLogSituation()
        {
            var situation = AnalyzeSituation();
            Debug.Log($"AI状況分析: 距離{situation.distanceToOpponent:F1}, 戦術{CurrentTacticalState}, 優劣{situation.advantageState}, 脅威{situation.threatLevel}");
        }

        #endregion

        #region SRDebugger Integration

        [System.ComponentModel.Category("SRDebugger - AI")]
        public string DebugTacticalState
        {
            get => CurrentTacticalState.ToString();
        }

        [System.ComponentModel.Category("SRDebugger - AI")]
        public float DebugAggressiveness
        {
            get => personality?.aggressiveness ?? 0f;
            set { if (personality != null) personality.aggressiveness = value; }
        }

        [System.ComponentModel.Category("SRDebugger - AI")]
        public bool DebugPerfectBlocking
        {
            get => enablePerfectBlocking;
            set => enablePerfectBlocking = value;
        }

        [System.ComponentModel.Category("SRDebugger - AI")]
        public void DebugForceAggressive() => DebugSetAggressive();

        [System.ComponentModel.Category("SRDebugger - AI")]
        public void DebugForceDefensive() => DebugSetDefensive();

        #endregion
    }
}
