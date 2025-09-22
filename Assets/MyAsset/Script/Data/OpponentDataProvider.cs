using LearningAIGame.CombatSystem.Core;
using System.Runtime.CompilerServices;
using UnityEngine;
using NaughtyAttributes;

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
        // 以下の変数はテスト目的のため不要になります。
        // private BattleCharacterController characterController;
        // private StateSystem stateSystem;

        // デバッグ表示用のプライベートフィールドは保持します
        [Header("デバッグ情報")]
        [SerializeField, ReadOnly]
        [Tooltip("現在の体力割合")]
        private float _debugHealthPercentage;

        [SerializeField, ReadOnly]
        [Tooltip("現在のエネルギー割合")]
        private float _debugEnergyPercentage;

        [SerializeField, ReadOnly]
        [Tooltip("現在の状態")]
        private string _debugCurrentState;

        // テスト用の固定値を設定します
        public float HealthPercentage => 0.5f;
        public float EnergyPercentage => 0.8f;
        public Vector3 Position => new Vector3(10, 0, 5);
        public Vector3 Velocity => Vector3.zero;
        public ActionMode CurrentMode => ActionMode.Melee;
        public ActionState CurrentState => ActionState.Idle;
        public AttackDirection CurrentDirection => AttackDirection.Up;
        public bool IsStunned => false;
        public bool IsInvincible => false;
        public float TimeSinceLastAction => 2.5f;
        public bool CanUseSkills => true;
        public bool CanUseManeuvers => false;
        public float[] SkillCooldowns => new float[] { 0f, 15f, 5f, 0f, 0f };
        public float[] ManeuverCooldowns => new float[] { 2f, 0f, 0f };
        public bool IsReloading => false;
        public float AimingAccuracy => 0.9f;
        public Vector3 AimDirection => Vector3.forward;

        // 既存のAwakeとUpdateメソッドは、デバッグ表示のため保持します

        private void Update()
        {
            // デバッグ情報のみを更新します
            this._debugHealthPercentage = HealthPercentage;
            this._debugEnergyPercentage = EnergyPercentage;
            this._debugCurrentState = $"{CurrentMode} / {CurrentState}";
        }

        // テスト用メソッドはそのまま保持します
        public bool IsVulnerable()
        {
            return IsStunned ||
                   CurrentState == ActionState.Attacking ||
                   EnergyPercentage < 0.1f ||
                   CurrentState == ActionState.Dodging;
        }

        public bool IsAggressive()
        {
            return CurrentState == ActionState.Attacking ||
                   CurrentState == ActionState.Boosting ||
                   (CurrentMode == ActionMode.Ranged && AimingAccuracy > 0.7f);
        }

        public bool IsDefensive()
        {
            return CurrentState == ActionState.Guarding ||
                   EnergyPercentage < 0.3f ||
                   HealthPercentage < 0.3f;
        }
    }
}