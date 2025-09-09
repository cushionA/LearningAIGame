using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// 対戦相手の情報を外部に提供するインターフェース
    /// </summary>
    public interface IOpponentData
    {
        // 基本情報
        /// <summary>
        /// 体力の割合（0.0-1.0）
        /// </summary>
        float HealthPercentage { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// エネルギーの割合（0.0-1.0）
        /// </summary>
        float EnergyPercentage { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// 現在位置
        /// </summary>
        Vector3 Position { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// 移動ベクトル
        /// </summary>
        Vector3 Velocity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        // 状態情報
        /// <summary>
        /// 現在の行動モード
        /// </summary>
        ActionMode CurrentMode { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// 現在の行動状態
        /// </summary>
        ActionState CurrentState { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// 現在の攻撃・防御方向
        /// </summary>
        AttackDirection CurrentDirection { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// スタン状態かどうか
        /// </summary>
        bool IsStunned { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// 無敵状態かどうか
        /// </summary>
        bool IsInvincible { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        // 行動予測情報
        /// <summary>
        /// 最後のアクションからの経過時間
        /// </summary>
        float TimeSinceLastAction { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// スキルが使用可能かどうか
        /// </summary>
        bool CanUseSkills { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// マニューバが使用可能かどうか
        /// </summary>
        bool CanUseManeuvers { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// 各スキルのクールタイム残り時間
        /// </summary>
        float[] SkillCooldowns { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// 各マニューバのクールタイム残り時間
        /// </summary>
        float[] ManeuverCooldowns { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        // 射撃関連
        /// <summary>
        /// 現在リロード中かどうか
        /// </summary>
        bool IsReloading { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// 射撃精度（0.0-1.0）
        /// </summary>
        float AimingAccuracy { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// 狙い方向
        /// </summary>
        Vector3 AimDirection { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }
    }

    /// <summary>
    /// 対戦相手のデータを提供するコンポーネント
    /// </summary>
    public class OpponentDataProvider : MonoBehaviour, IOpponentData
    {
        [Title("参照コンポーネント")]
        [Required]
        [PropertyTooltip("対象のキャラクターコントローラー")]
        [SerializeField] private BattleCharacterController characterController;

        [Required]
        [PropertyTooltip("対象の状態システム")]
        [SerializeField] private StateSystem stateSystem;

        [Title("デバッグ情報")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の体力割合")]
        private float debugHealthPercentage;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在のエネルギー割合")]
        private float debugEnergyPercentage;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の状態")]
        private string debugCurrentState;

        /// <summary>
        /// 初期化処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Awake()
        {
            if ( this.characterController == null )
            {
                this.characterController = GetComponent<BattleCharacterController>();
            }

            if ( this.stateSystem == null )
            {
                this.stateSystem = GetComponent<StateSystem>();
            }
        }

        /// <summary>
        /// デバッグ情報の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update()
        {
            if ( this.characterController != null && this.stateSystem != null )
            {
                this.debugHealthPercentage = HealthPercentage;
                this.debugEnergyPercentage = EnergyPercentage;
                this.debugCurrentState = $"{CurrentMode} / {CurrentState}";
            }
        }

        // IOpponentDataの実装
        public float HealthPercentage
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.characterController?.CurrentHealth / this.characterController?.MaxHealth ?? 0f;
        }

        public float EnergyPercentage
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.EnergyPercentage ?? 0f;
        }

        public Vector3 Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.characterController?.Position ?? Vector3.zero;
        }

        public Vector3 Velocity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.AnalysisData.currentVelocity ?? Vector3.zero;
        }

        public ActionMode CurrentMode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.CurrentActionMode ?? ActionMode.Melee;
        }

        public ActionState CurrentState
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.CurrentActionState ?? ActionState.Idle;
        }

        public AttackDirection CurrentDirection
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.CurrentDirection ?? AttackDirection.Up;
        }

        public bool IsStunned
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.HealthData.isStunned ?? false;
        }

        public bool IsInvincible
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.HealthData.isInvincible ?? false;
        }

        public float TimeSinceLastAction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.AnalysisData.timeSinceLastAction ?? 0f;
        }

        public bool CanUseSkills
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.AnalysisData.canUseSkills ?? false;
        }

        public bool CanUseManeuvers
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.AnalysisData.canUseManeuvers ?? false;
        }

        public float[] SkillCooldowns
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.AnalysisData.skillCooldowns ?? new float[5];
        }

        public float[] ManeuverCooldowns
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.AnalysisData.maneuverCooldowns ?? new float[3];
        }

        public bool IsReloading
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.AnalysisData.isReloading ?? false;
        }

        public float AimingAccuracy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.AnalysisData.aimingAccuracy ?? 0f;
        }

        public Vector3 AimDirection
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.stateSystem?.AnalysisData.aimDirection ?? Vector3.forward;
        }

        /// <summary>
        /// 相手が脆弱な状態かどうかを判定
        /// </summary>
        /// <returns>脆弱状態かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsVulnerable()
        {
            return IsStunned ||
                   CurrentState == ActionState.Attacking ||
                   EnergyPercentage < 0.1f ||
                   CurrentState == ActionState.Dodging;
        }

        /// <summary>
        /// 攻撃的な状態かどうかを判定
        /// </summary>
        /// <returns>攻撃的状態かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAggressive()
        {
            return CurrentState == ActionState.Attacking ||
                   CurrentState == ActionState.Boosting ||
                   (CurrentMode == ActionMode.Ranged && AimingAccuracy > 0.7f);
        }

        /// <summary>
        /// 防御的な状態かどうかを判定
        /// </summary>
        /// <returns>防御的状態かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDefensive()
        {
            return CurrentState == ActionState.Guarding ||
                   EnergyPercentage < 0.3f ||
                   HealthPercentage < 0.3f;
        }

        [Title("開発者ツール")]
        [Button("対戦相手情報を出力", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 1f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogOpponentInfo()
        {
            var info = $"=== 対戦相手情報 ===\n" +
                      $"体力: {HealthPercentage:P1}\n" +
                      $"エネルギー: {EnergyPercentage:P1}\n" +
                      $"モード: {CurrentMode}\n" +
                      $"状態: {CurrentState}\n" +
                      $"方向: {CurrentDirection}\n" +
                      $"脆弱: {IsVulnerable()}\n" +
                      $"攻撃的: {IsAggressive()}\n" +
                      $"防御的: {IsDefensive()}";

            Debug.Log(info);
        }
    }
}
