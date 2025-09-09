using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;
using UniRx;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// マニューバシステム - 事前記録した移動パターンの実行を管理
    /// </summary>
    public class ManeuverSystem : MonoBehaviour
    {
        [Title("コンポーネント参照")]
        [Required, PropertyTooltip("キャラクターコントローラー")]
        [SerializeField] private BattleCharacterController characterController;

        [Required, PropertyTooltip("状態システム")]
        [SerializeField] private StateSystem stateSystem;

        [Required, PropertyTooltip("エネルギーシステム")]
        [SerializeField] private EnergySystem energySystem;

        [Required, PropertyTooltip("移動システム")]
        [SerializeField] private MovementSystem movementSystem;

        [Title("マニューバ設定")]
        [PropertyTooltip("利用可能なマニューバ一覧")]
        [SerializeField] private List<ManeuverData> availableManeuvers = new List<ManeuverData>();

        [PropertyTooltip("最大マニューバ記録数")]
        [Range(1, 10)]
        [SerializeField] private int maxManeuverSlots = 3;

        [Title("現在の状態")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在マニューバ実行中かどうか")]
        public bool IsExecutingManeuver { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在実行中のマニューバインデックス")]
        public int CurrentManeuverIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = -1;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("マニューバ実行進行度")]
        public float ExecutionProgress { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 0f;

        // 内部状態
        private ManeuverData currentExecutingManeuver;
        private int currentStepIndex = 0;
        private float stepStartTime = 0f;
        private bool isRecording = false;
        private ManeuverData recordingManeuver;

        /// <summary>
        /// マニューバデータ
        /// </summary>
        [Serializable]
        public class ManeuverData
        {
            [PropertyTooltip("マニューバ名")]
            public string maneuverName;
            
            [PropertyTooltip("エネルギー消費量")]
            public float energyCost;
            
            [PropertyTooltip("クールタイム")]
            public float cooldownTime;
            
            [PropertyTooltip("移動ステップ一覧")]
            public List<MovementStep> movementSteps = new List<MovementStep>();
            
            [PropertyTooltip("マニューバ後のスキル使用")]
            public int postManeuverSkillIndex = -1;

            /// <summary>
            /// 総実行時間を計算
            /// </summary>
            /// <returns>総実行時間</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float CalculateTotalDuration()
            {
                float totalDuration = 0f;
                foreach (var step in movementSteps)
                {
                    totalDuration += step.duration;
                }
                return totalDuration;
            }
        }

        /// <summary>
        /// 移動ステップ
        /// </summary>
        [Serializable]
        public class MovementStep
        {
            [PropertyTooltip("移動の種類")]
            public MovementType movementType;
            
            [PropertyTooltip("移動方向")]
            public Vector3 direction;
            
            [PropertyTooltip("実行時間")]
            public float duration;
            
            [PropertyTooltip("移動速度倍率")]
            [Range(0.1f, 3f)]
            public float speedMultiplier = 1f;

            [PropertyTooltip("ジャンプ時のチャージ")]
            public bool useChargedJump = false;
        }

        /// <summary>
        /// 移動の種類
        /// </summary>
        public enum MovementType
        {
            Walk,
            Boost,
            Jump,
            Dodge,
            Wait
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Awake()
        {
            if (characterController == null)
                characterController = GetComponent<BattleCharacterController>();
            
            if (stateSystem == null)
                stateSystem = GetComponent<StateSystem>();
                
            if (energySystem == null)
                energySystem = GetComponent<EnergySystem>();
                
            if (movementSystem == null)
                movementSystem = GetComponent<MovementSystem>();
        }

        /// <summary>
        /// 開始処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Start()
        {
            InitializeDefaultManeuvers();
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update()
        {
            if (IsExecutingManeuver)
            {
                UpdateManeuverExecution();
            }

            if (isRecording)
            {
                UpdateManeuverRecording();
            }
        }

        #region Public Maneuver Methods

        /// <summary>
        /// マニューバを実行
        /// </summary>
        /// <param name="maneuverIndex">マニューバインデックス</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteManeuver(int maneuverIndex)
        {
            if (!CanExecuteManeuver(maneuverIndex))
                return;

            var maneuver = availableManeuvers[maneuverIndex];
            
            if (!energySystem.UseEnergy(maneuver.energyCost))
                return;

            StartManeuverExecution(maneuver, maneuverIndex);
        }

        /// <summary>
        /// マニューバ記録を開始
        /// </summary>
        /// <param name="maneuverName">マニューバ名</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartRecording(string maneuverName)
        {
            if (isRecording || IsExecutingManeuver)
                return;

            isRecording = true;
            recordingManeuver = new ManeuverData
            {
                maneuverName = maneuverName,
                movementSteps = new List<MovementStep>()
            };

            Debug.Log($"マニューバ記録開始: {maneuverName}");
        }

        /// <summary>
        /// マニューバ記録を停止
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopRecording()
        {
            if (!isRecording)
                return;

            isRecording = false;
            
            // エネルギー消費量とクールタイムを計算
            CalculateManeuverCosts(recordingManeuver);
            
            // マニューバを追加
            AddManeuver(recordingManeuver);
            
            Debug.Log($"マニューバ記録完了: {recordingManeuver.maneuverName}");
            recordingManeuver = null;
        }

        /// <summary>
        /// マニューバを追加
        /// </summary>
        /// <param name="maneuver">追加するマニューバ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddManeuver(ManeuverData maneuver)
        {
            if (availableManeuvers.Count >= maxManeuverSlots)
            {
                // 最古のマニューバを削除
                availableManeuvers.RemoveAt(0);
            }
            
            availableManeuvers.Add(maneuver);
        }

        /// <summary>
        /// マニューバを削除
        /// </summary>
        /// <param name="index">削除するインデックス</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveManeuver(int index)
        {
            if (index >= 0 && index < availableManeuvers.Count)
            {
                availableManeuvers.RemoveAt(index);
            }
        }

        /// <summary>
        /// マニューバ実行をキャンセル
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CancelManeuver()
        {
            if (IsExecutingManeuver)
            {
                StopManeuverExecution();
            }
        }

        /// <summary>
        /// マニューバが使用可能かどうか
        /// </summary>
        /// <param name="maneuverIndex">マニューバインデックス</param>
        /// <returns>使用可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanExecuteManeuver(int maneuverIndex)
        {
            if (maneuverIndex < 0 || maneuverIndex >= availableManeuvers.Count)
                return false;

            if (IsExecutingManeuver || isRecording)
                return false;

            if (!stateSystem.CanExecuteAction(ActionType.Maneuver))
                return false;

            // クールダウンチェック
            if (stateSystem.AnalysisData.maneuverCooldowns[maneuverIndex] > 0f)
                return false;

            var maneuver = availableManeuvers[maneuverIndex];
            return energySystem.CanUseEnergy(maneuver.energyCost);
        }

        #endregion

        #region Private Execution Methods

        /// <summary>
        /// マニューバ実行を開始
        /// </summary>
        /// <param name="maneuver">実行するマニューバ</param>
        /// <param name="maneuverIndex">マニューバインデックス</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartManeuverExecution(ManeuverData maneuver, int maneuverIndex)
        {
            IsExecutingManeuver = true;
            CurrentManeuverIndex = maneuverIndex;
            currentExecutingManeuver = maneuver;
            currentStepIndex = 0;
            stepStartTime = Time.time;
            ExecutionProgress = 0f;

            stateSystem.ReportActionStateChange(ActionState.UsingManeuver);
            
            // クールダウン設定
            stateSystem.ReportManeuverCooldown(maneuverIndex, maneuver.cooldownTime);

            Debug.Log($"マニューバ実行開始: {maneuver.maneuverName}");
        }

        /// <summary>
        /// マニューバ実行を停止
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StopManeuverExecution()
        {
            IsExecutingManeuver = false;
            CurrentManeuverIndex = -1;
            currentExecutingManeuver = null;
            currentStepIndex = 0;
            ExecutionProgress = 0f;

            stateSystem.ReportActionStateChange(ActionState.Idle);

            Debug.Log("マニューバ実行終了");
        }

        /// <summary>
        /// マニューバ実行の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateManeuverExecution()
        {
            if (currentExecutingManeuver == null || currentStepIndex >= currentExecutingManeuver.movementSteps.Count)
            {
                OnManeuverComplete();
                return;
            }

            var currentStep = currentExecutingManeuver.movementSteps[currentStepIndex];
            float stepElapsedTime = Time.time - stepStartTime;

            // 進行度更新
            UpdateExecutionProgress();

            // ステップ実行
            ExecuteMovementStep(currentStep);

            // ステップ完了判定
            if (stepElapsedTime >= currentStep.duration)
            {
                currentStepIndex++;
                stepStartTime = Time.time;

                if (currentStepIndex >= currentExecutingManeuver.movementSteps.Count)
                {
                    OnManeuverComplete();
                }
            }
        }

        /// <summary>
        /// 移動ステップを実行
        /// </summary>
        /// <param name="step">実行するステップ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteMovementStep(MovementStep step)
        {
            Vector3 adjustedDirection = step.direction * step.speedMultiplier;

            switch (step.movementType)
            {
                case MovementType.Walk:
                    movementSystem.Move(adjustedDirection);
                    break;
                case MovementType.Boost:
                    movementSystem.Boost(adjustedDirection);
                    break;
                case MovementType.Jump:
                    movementSystem.Jump(step.useChargedJump);
                    break;
                case MovementType.Dodge:
                    movementSystem.Dodge(adjustedDirection);
                    break;
                case MovementType.Wait:
                    // 何もしない
                    break;
            }
        }

        /// <summary>
        /// 実行進行度を更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateExecutionProgress()
        {
            if (currentExecutingManeuver == null)
                return;

            float totalDuration = currentExecutingManeuver.CalculateTotalDuration();
            float currentTime = 0f;

            // 完了したステップの時間を計算
            for (int i = 0; i < currentStepIndex; i++)
            {
                currentTime += currentExecutingManeuver.movementSteps[i].duration;
            }

            // 現在のステップの経過時間を追加
            if (currentStepIndex < currentExecutingManeuver.movementSteps.Count)
            {
                currentTime += Time.time - stepStartTime;
            }

            ExecutionProgress = Mathf.Clamp01(currentTime / totalDuration);
        }

        /// <summary>
        /// マニューバ完了時の処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnManeuverComplete()
        {
            // マニューバ後スキル実行
            if (currentExecutingManeuver.postManeuverSkillIndex >= 0)
            {
                var attackSystem = GetComponent<AttackSystem>();
                if (attackSystem != null)
                {
                    attackSystem.ExecuteSkill(currentExecutingManeuver.postManeuverSkillIndex);
                }
            }

            StopManeuverExecution();
        }

        /// <summary>
        /// マニューバ記録の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateManeuverRecording()
        {
            // 実際の記録はプレイヤー入力に基づいて行われる
            // ここでは記録状態の管理のみ
        }

        /// <summary>
        /// 記録中に移動ステップを追加
        /// </summary>
        /// <param name="movementType">移動タイプ</param>
        /// <param name="direction">方向</param>
        /// <param name="duration">継続時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordMovementStep(MovementType movementType, Vector3 direction, float duration)
        {
            if (!isRecording)
                return;

            var step = new MovementStep
            {
                movementType = movementType,
                direction = direction,
                duration = duration,
                speedMultiplier = 1f,
                useChargedJump = false
            };

            recordingManeuver.movementSteps.Add(step);
        }

        /// <summary>
        /// マニューバのコストを計算
        /// </summary>
        /// <param name="maneuver">計算対象のマニューバ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalculateManeuverCosts(ManeuverData maneuver)
        {
            float energyCost = 0f;
            float totalDuration = maneuver.CalculateTotalDuration();

            foreach (var step in maneuver.movementSteps)
            {
                switch (step.movementType)
                {
                    case MovementType.Boost:
                        energyCost += 25f * step.duration; // ブーストのエネルギー消費
                        break;
                    case MovementType.Dodge:
                        energyCost += 15f; // 回避のエネルギー消費
                        break;
                    case MovementType.Jump:
                        energyCost += step.useChargedJump ? 15f : 10f;
                        break;
                }
            }

            maneuver.energyCost = energyCost;
            maneuver.cooldownTime = totalDuration * 2f; // 実行時間の2倍をクールタイムに
        }

        /// <summary>
        /// デフォルトマニューバの初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeDefaultManeuvers()
        {
            if (availableManeuvers.Count == 0)
            {
                // 基本的な背後回り込みマニューバ
                var backAttackManeuver = new ManeuverData
                {
                    maneuverName = "背後回り込み",
                    energyCost = 30f,
                    cooldownTime = 8f,
                    movementSteps = new List<MovementStep>
                    {
                        new MovementStep { movementType = MovementType.Dodge, direction = Vector3.right, duration = 0.3f },
                        new MovementStep { movementType = MovementType.Boost, direction = Vector3.forward, duration = 0.5f },
                        new MovementStep { movementType = MovementType.Wait, direction = Vector3.zero, duration = 0.2f }
                    }
                };

                // 高速離脱マニューバ
                var escapeManeuver = new ManeuverData
                {
                    maneuverName = "高速離脱",
                    energyCost = 20f,
                    cooldownTime = 6f,
                    movementSteps = new List<MovementStep>
                    {
                        new MovementStep { movementType = MovementType.Jump, direction = Vector3.up, duration = 0.3f, useChargedJump = true },
                        new MovementStep { movementType = MovementType.Boost, direction = Vector3.back, duration = 0.8f }
                    }
                };

                availableManeuvers.Add(backAttackManeuver);
                availableManeuvers.Add(escapeManeuver);
            }
        }

        #endregion

        #region Debug Methods

        [Title("デバッグ機能")]
        [Button("マニューバ1実行", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugExecuteManeuver1()
        {
            ExecuteManeuver(0);
        }

        [Button("マニューバ2実行", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugExecuteManeuver2()
        {
            ExecuteManeuver(1);
        }

        [Button("記録開始", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugStartRecording()
        {
            StartRecording("デバッグマニューバ");
        }

        [Button("記録停止", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugStopRecording()
        {
            StopRecording();
        }

        [Button("マニューバキャンセル", ButtonSizes.Medium)]
        [GUIColor(1f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugCancelManeuver()
        {
            CancelManeuver();
        }

        #endregion

        #region SRDebugger Integration

        [System.ComponentModel.Category("SRDebugger - マニューバ")]
        public bool DebugIsExecuting
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsExecutingManeuver;
        }

        [System.ComponentModel.Category("SRDebugger - マニューバ")]
        public float DebugExecutionProgress
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ExecutionProgress;
        }

        [System.ComponentModel.Category("SRDebugger - マニューバ")]
        public int DebugAvailableCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => availableManeuvers.Count;
        }

        [System.ComponentModel.Category("SRDebugger - マニューバ")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugExecuteFirst() => ExecuteManeuver(0);

        [System.ComponentModel.Category("SRDebugger - マニューバ")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugForceCancel() => CancelManeuver();

        #endregion
    }
}
