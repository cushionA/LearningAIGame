using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// アクションタイプの列挙
    /// </summary>
    public enum ActionType : byte
    {
        Walk,
        Jump,
        Boost,
        Dodge,
        WeakAttack,
        StrongAttack,
        SkillAttack,
        Guard,
        Block,
        Extension,
        ModeSwitch,
        Maneuver
    }

    /// <summary>
    /// アクションモードの列挙
    /// </summary>
    public enum ActionMode : byte
    {
        Melee,
        Ranged,
        EnergyBarrier
    }

    /// <summary>
    /// アクション状態の列挙
    /// </summary>
    public enum ActionState : byte
    {
        Idle,
        Moving,
        Boosting,
        Falling,
        Jumping,
        Dodging,
        DoubleDodging,
        Attacking,
        Guarding,
        UsingManeuver,
        Stunned,
        Flinching,
        EnergyShielding,
        AirCharge,
        QuickTurn
    }

    /// <summary>
    /// 攻撃方向の列挙
    /// </summary>
    public enum AttackDirection : byte
    {
        Up,
        Left,
        Right
    }

    /// <summary>
    /// 攻撃タイプの列挙
    /// </summary>
    public enum AttackType : byte
    {
        None,
        WeakMelee,
        StrongMelee,
        WeakShoot,
        StrongShoot,
    }

    /// <summary>
    /// 基本アクションデータ - 全アクションの共通データ構造
    /// 
    /// 実装メモ:
    /// - ジェネリック型Tにより、各アクション固有のデータを型安全に管理
    /// - クールダウン、エネルギー消費、連続使用制限を統一的に処理
    /// - リフレクションを避け、パフォーマンスを重視した設計
    /// 
    /// 使用例:
    /// - ActionData<MovementActionData> walkAction
    /// - ActionData<AttackActionData> attackAction
    /// </summary>
    [System.Serializable]
    public class ActionData<T> where T : class, new()
    {
        [Title("基本設定")]
        [PropertyTooltip("エネルギー消費量")]
        public float energyCost = 0f;

        [PropertyTooltip("最大連続使用回数")]
        [Range(1, 999)]
        public int maxConsecutiveUses = 1;

        [PropertyTooltip("クールダウン時間")]
        [Range(0f, 60f)]
        public float cooldownTime = 0f;

        [Title("専用データ")]
        [PropertyTooltip("アクション固有のデータ")]
        public T data = new T();

        // 内部管理用
        [HideInInspector]
        public float lastUsedTime = 0f;
        [HideInInspector]
        public int consecutiveUseCount = 0;

        /// <summary>
        /// アクションが使用可能かどうかを判定
        /// 
        /// 判定条件:
        /// 1. クールダウンが終了している
        /// 2. 連続使用回数が上限に達していない
        /// 3. 必要なエネルギーが足りている
        /// 
        /// 注意: この判定はStateSystemの状態チェックとは独立して動作する
        /// </summary>
        /// <param name="currentEnergy">現在のエネルギー量</param>
        /// <returns>使用可能な場合true</returns>
        public bool CanUse(float currentEnergy)
        {
            return Time.time - lastUsedTime >= cooldownTime &&
                   consecutiveUseCount < maxConsecutiveUses &&
                   currentEnergy >= energyCost;
        }

        /// <summary>
        /// アクションを実行し、内部状態を更新
        /// 
        /// 実行内容:
        /// - 最終使用時刻を現在時刻に更新
        /// - 連続使用回数をインクリメント
        /// 
        /// 注意: エネルギー消費は別途EnergySystemで処理される
        /// </summary>
        public void Execute()
        {
            lastUsedTime = Time.time;
            consecutiveUseCount++;
        }

        /// <summary>
        /// クールダウン状態を更新し、必要に応じて連続使用回数をリセット
        /// 
        /// 更新ロジック:
        /// - クールダウン時間が経過していれば連続使用回数を0にリセット
        /// - BattleCharacterController.FixedUpdate()から毎フレーム呼び出される
        /// 
        /// パフォーマンス: 軽量な処理なので毎フレーム実行しても問題なし
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        public void UpdateCooldown(float deltaTime)
        {
            if ( Time.time - lastUsedTime >= cooldownTime )
            {
                consecutiveUseCount = 0;
            }
        }

        /// <summary>
        /// アクションの使用状態を完全にリセット
        /// 
        /// リセット内容:
        /// - 最終使用時刻を0に戻す
        /// - 連続使用回数を0に戻す
        /// 
        /// 使用場面:
        /// - デバッグ時の状態リセット
        /// - キャラクター復活時の初期化
        /// - 戦闘開始時の状態クリア
        /// </summary>
        public void Reset()
        {
            lastUsedTime = 0f;
            consecutiveUseCount = 0;
        }
    }

    /// <summary>
    /// 移動アクションデータ - 歩行、ジャンプ、ブースト、回避の設定を統合管理
    /// 
    /// 設計方針:
    /// - 全ての移動アクションで共通のデータ構造を使用
    /// - アクション種別による設定値の使い分けは各Systemで実装
    /// - 回避インターバル機能により戦術性を向上
    /// 
    /// 注意:
    /// - 従来のMovementSettingsとの互換性を保持
    /// - エネルギー切れ時の特殊ルールに対応
    /// </summary>
    [System.Serializable]
    public class MovementActionData
    {
        [Title("移動設定")]
        [PropertyTooltip("移動速度")]
        public float speed = 5f;

        [PropertyTooltip("加速時間")]
        public float acceleration = 0.2f;

        [Title("ジャンプ設定")]
        [PropertyTooltip("ジャンプ力")]
        public float jumpForce = 10f;

        [PropertyTooltip("チャージ時間")]
        public float chargeTime = 1f;

        [PropertyTooltip("チャージジャンプ力")]
        public float chargedJumpForce = 15f;

        [Title("回避設定")]
        [PropertyTooltip("回避距離")]
        public float dodgeDistance = 8f;

        [PropertyTooltip("通常時回避インターバル")]
        public float normalDodgeInterval = 0.3f;

        [PropertyTooltip("エネルギー切れ時回避インターバル")]
        public float energyDepletedDodgeInterval = 1f;

        [PropertyTooltip("二段回避エネルギー消費")]
        public float doubleDodgeEnergyCost = 20f;
    }

    /// <summary>
    /// 攻撃アクションデータ
    /// </summary>
    [System.Serializable]
    public class AttackActionData
    {
        [Title("基本攻撃設定")]
        [PropertyTooltip("基本ダメージ")]
        public float baseDamage = 25f;

        [PropertyTooltip("攻撃範囲")]
        public float range = 2f;

        [PropertyTooltip("攻撃速度")]
        public float speed = 1f;

        [PropertyTooltip("スタン蓄積値")]
        public float stunAccumulation = 10f;

        [Title("キャンセル設定")]
        [PropertyTooltip("キャンセル可能かどうか")]
        public bool canCancel = false;

        [PropertyTooltip("キャンセル可能タイミング")]
        public float cancelWindow = 0.3f;

        [Title("コンボ設定")]
        [PropertyTooltip("コンボ継続時間")]
        public float comboContinueTime = 1f;

        [PropertyTooltip("最大コンボ数")]
        public int maxComboCount = 3;
    }

    /// <summary>
    /// 射撃アクションデータ
    /// </summary>
    [System.Serializable]
    public class RangedActionData
    {
        [Title("弾薬設定")]
        [PropertyTooltip("弾薬数")]
        public int ammoCount = 10;

        [PropertyTooltip("リロード時間")]
        public float reloadTime = 2f;

        [Title("射撃精度")]
        [PropertyTooltip("精度最大化時間")]
        public float accuracyTime = 1.5f;

        [PropertyTooltip("最大精度時ガード貫通")]
        public bool pierceGuardAtMaxAccuracy = true;

        [Title("射撃パターン")]
        [PropertyTooltip("同時発射数")]
        public int simultaneousShots = 1;

        [PropertyTooltip("拡散角度")]
        public float spreadAngle = 5f;
    }

    /// <summary>
    /// 防御アクションデータ
    /// </summary>
    [System.Serializable]
    public class DefenseActionData
    {
        [Title("防御設定")]
        [PropertyTooltip("防御可能方向数")]
        public int defensiveDirections = 3;

        [PropertyTooltip("エネルギー回復量")]
        public float energyRecovery = 10f;

        [PropertyTooltip("エネルギーボーナス時間")]
        public float energyBonusTime = 3f;

        [PropertyTooltip("エネルギーボーナス倍率")]
        public float energyBonusMultiplier = 1.5f;

        [Title("ブロッキング設定")]
        [PropertyTooltip("成功判定時間")]
        public float successWindow = 0.2f;

        [PropertyTooltip("失敗時ダメージ倍率")]
        public float failureDamageMultiplier = 1.2f;

        [PropertyTooltip("成功時移動距離")]
        public float successMoveDistance = 3f;

        [PropertyTooltip("移動中ガード可能")]
        public bool canGuardWhileMoving = true;
    }

    /// <summary>
    /// エクステンション効果タイプ
    /// </summary>
    public enum ExtensionEffectType
    {
        Damage,
        Support,
        Defensive,
        Environmental
    }

    /// <summary>
    /// エクステンションアクションデータ
    /// </summary>
    [System.Serializable]
    public class ExtensionActionData
    {
        [Title("エクステンション設定")]
        [PropertyTooltip("効果タイプ")]
        public ExtensionEffectType effectType = ExtensionEffectType.Support;

        [PropertyTooltip("効果値")]
        public float effectValue = 10f;

        [PropertyTooltip("効果範囲")]
        public float effectRange = 5f;

        [PropertyTooltip("効果持続時間")]
        public float effectDuration = 3f;

        [PropertyTooltip("効果名")]
        public string effectName = "";

        [Title("配置設定")]
        [PropertyTooltip("設置型かどうか")]
        public bool isPlaceable = false;

        [PropertyTooltip("自動発動するかどうか")]
        public bool isAutoActivate = false;
    }

    /// <summary>
    /// モード切り替え条件
    /// </summary>
    public enum ModeSwitchCondition
    {
        Always,
        OnGround,
        InAir,
        HealthAbove50,
        EnergyAbove30
    }

    /// <summary>
    /// モード切り替えアクションデータ
    /// </summary>
    [System.Serializable]
    public class ModeSwitchActionData
    {
        [Title("切り替え設定")]
        [PropertyTooltip("切り替え先モード")]
        public ActionMode targetMode = ActionMode.Ranged;

        [PropertyTooltip("切り替え時間")]
        public float switchTime = 0.3f;

        [PropertyTooltip("切り替え中無敵時間")]
        public float invincibilityDuration = 0.1f;

        [PropertyTooltip("切り替え条件")]
        public ModeSwitchCondition switchCondition = ModeSwitchCondition.Always;
    }

    /// <summary>
    /// マニューバアクションデータ
    /// </summary>
    [System.Serializable]
    public class ManeuverActionData
    {
        [Title("マニューバ設定")]
        [PropertyTooltip("記録済みパターン")]
        [TextArea(3, 5)]
        public string recordedPattern = "";

        [PropertyTooltip("実行時間")]
        public float executionTime = 2f;

        [PropertyTooltip("速度倍率")]
        public float speedMultiplier = 1.5f;

        [PropertyTooltip("実行中無敵時間")]
        public float invincibilityDuration = 0.2f;

        [Title("キャンセル設定")]
        [PropertyTooltip("途中キャンセル可能")]
        public bool canCancelMidway = false;

        [PropertyTooltip("実行後自動スキル")]
        public ActionType autoSkillAfterExecution = ActionType.Walk;

        [PropertyTooltip("早期使用エネルギー倍率")]
        public float earlyUseEnergyMultiplier = 1.5f;
    }

    /// <summary>
    /// キャラクター設定のメインクラス
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterSettings", menuName = "Battle/Character Settings")]
    public class CharacterSettings : ScriptableObject
    {
        #region === 基本ステータス ===

        [Title("基本ステータス")]
        [ValidateInput("ValidateHealth", "体力は100以上である必要があります")]
        [PropertyTooltip("最大体力")]
        public float maxHealth = 500f;

        [ValidateInput("ValidateEnergy", "エネルギーは50以上である必要があります")]
        [PropertyTooltip("最大エネルギー")]
        public float maxEnergy = 100f;

        [PropertyTooltip("通常エネルギー回復速度")]
        public float normalEnergyRecoveryRate = 25f;

        [PropertyTooltip("高速エネルギー回復速度")]
        public float fastEnergyRecoveryRate = 50f;

        [PropertyTooltip("最大スタンゲージ")]
        public float maxStunGauge = 100f;

        [PropertyTooltip("スタンゲージ回復速度")]
        public float stunGaugeRecoveryRate = 20f;

        private bool ValidateHealth(float health) => health >= 100f;
        private bool ValidateEnergy(float energy) => energy >= 50f;

        #endregion

        #region === ActionDataシステム ===

        [Title("アクションデータベース")]
        [PropertyTooltip("全アクションのデータベース")]
        [ShowInInspector, ReadOnly]
        private Dictionary<ActionType, object> actionDatabase = new Dictionary<ActionType, object>();

        // 各アクションデータ
        [FoldoutGroup("移動アクション")]
        [SerializeField] private ActionData<MovementActionData> walkAction;
        [FoldoutGroup("移動アクション")]
        [SerializeField] private ActionData<MovementActionData> jumpAction;
        [FoldoutGroup("移動アクション")]
        [SerializeField] private ActionData<MovementActionData> boostAction;
        [FoldoutGroup("移動アクション")]
        [SerializeField] private ActionData<MovementActionData> dodgeAction;

        [FoldoutGroup("攻撃アクション")]
        [SerializeField] private ActionData<AttackActionData> weakAttackAction;
        [FoldoutGroup("攻撃アクション")]
        [SerializeField] private ActionData<AttackActionData> strongAttackAction;
        [FoldoutGroup("攻撃アクション")]
        [SerializeField] private ActionData<AttackActionData> skillAttackAction;

        [FoldoutGroup("射撃アクション")]
        [SerializeField] private ActionData<RangedActionData> weakRangedAction;
        [FoldoutGroup("射撃アクション")]
        [SerializeField] private ActionData<RangedActionData> strongRangedAction;

        [FoldoutGroup("防御アクション")]
        [SerializeField] private ActionData<DefenseActionData> guardAction;
        [FoldoutGroup("防御アクション")]
        [SerializeField] private ActionData<DefenseActionData> blockAction;

        [FoldoutGroup("特殊アクション")]
        [SerializeField] private ActionData<ExtensionActionData> extensionAction;
        [FoldoutGroup("特殊アクション")]
        [SerializeField] private ActionData<ModeSwitchActionData> modeSwitchAction;
        [FoldoutGroup("特殊アクション")]
        [SerializeField] private ActionData<ManeuverActionData> maneuverAction;

        /// <summary>
        /// アクションデータを取得
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ActionData<T> GetActionData<T>(ActionType actionType) where T : class, new()
        {
            if ( actionDatabase.TryGetValue(actionType, out var data) )
            {
                return data as ActionData<T>;
            }
            return null;
        }

        /// <summary>
        /// アクションデータを取得（型指定なし）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetActionData(ActionType actionType)
        {
            actionDatabase.TryGetValue(actionType, out var data);
            return data;
        }

        /// <summary>
        /// アクションが実行可能かどうか
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanExecuteAction(ActionType actionType, float currentEnergy)
        {
            if ( !actionDatabase.TryGetValue(actionType, out var data) )
                return false;

            // リフレクションを避けるためのswitch文
            switch ( actionType )
            {
                case ActionType.Walk:
                    return ((ActionData<MovementActionData>)data).CanUse(currentEnergy);
                case ActionType.Jump:
                    return ((ActionData<MovementActionData>)data).CanUse(currentEnergy);
                case ActionType.Boost:
                    return ((ActionData<MovementActionData>)data).CanUse(currentEnergy);
                case ActionType.Dodge:
                    return ((ActionData<MovementActionData>)data).CanUse(currentEnergy);
                case ActionType.WeakAttack:
                    return ((ActionData<AttackActionData>)data).CanUse(currentEnergy);
                case ActionType.StrongAttack:
                    return ((ActionData<AttackActionData>)data).CanUse(currentEnergy);
                case ActionType.SkillAttack:
                    return ((ActionData<AttackActionData>)data).CanUse(currentEnergy);
                case ActionType.Guard:
                    return ((ActionData<DefenseActionData>)data).CanUse(currentEnergy);
                case ActionType.Block:
                    return ((ActionData<DefenseActionData>)data).CanUse(currentEnergy);
                case ActionType.Extension:
                    return ((ActionData<ExtensionActionData>)data).CanUse(currentEnergy);
                case ActionType.ModeSwitch:
                    return ((ActionData<ModeSwitchActionData>)data).CanUse(currentEnergy);
                case ActionType.Maneuver:
                    return ((ActionData<ManeuverActionData>)data).CanUse(currentEnergy);
                default:
                    return false;
            }
        }

        /// <summary>
        /// アクションを実行
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteAction(ActionType actionType)
        {
            if ( !actionDatabase.TryGetValue(actionType, out var data) )
                return;

            // リフレクションを避けるためのswitch文
            switch ( actionType )
            {
                case ActionType.Walk:
                    ((ActionData<MovementActionData>)data).Execute();
                    break;
                case ActionType.Jump:
                    ((ActionData<MovementActionData>)data).Execute();
                    break;
                case ActionType.Boost:
                    ((ActionData<MovementActionData>)data).Execute();
                    break;
                case ActionType.Dodge:
                    ((ActionData<MovementActionData>)data).Execute();
                    break;
                case ActionType.WeakAttack:
                    ((ActionData<AttackActionData>)data).Execute();
                    break;
                case ActionType.StrongAttack:
                    ((ActionData<AttackActionData>)data).Execute();
                    break;
                case ActionType.SkillAttack:
                    ((ActionData<AttackActionData>)data).Execute();
                    break;
                case ActionType.Guard:
                    ((ActionData<DefenseActionData>)data).Execute();
                    break;
                case ActionType.Block:
                    ((ActionData<DefenseActionData>)data).Execute();
                    break;
                case ActionType.Extension:
                    ((ActionData<ExtensionActionData>)data).Execute();
                    break;
                case ActionType.ModeSwitch:
                    ((ActionData<ModeSwitchActionData>)data).Execute();
                    break;
                case ActionType.Maneuver:
                    ((ActionData<ManeuverActionData>)data).Execute();
                    break;
            }
        }

        /// <summary>
        /// 全アクションのクールダウンを更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateAllCooldowns(float deltaTime)
        {
            foreach ( var kvp in actionDatabase )
            {
                var actionType = kvp.Key;
                var data = kvp.Value;

                // リフレクションを避けるためのswitch文
                switch ( actionType )
                {
                    case ActionType.Walk:
                        ((ActionData<MovementActionData>)data).UpdateCooldown(deltaTime);
                        break;
                    case ActionType.Jump:
                        ((ActionData<MovementActionData>)data).UpdateCooldown(deltaTime);
                        break;
                    case ActionType.Boost:
                        ((ActionData<MovementActionData>)data).UpdateCooldown(deltaTime);
                        break;
                    case ActionType.Dodge:
                        ((ActionData<MovementActionData>)data).UpdateCooldown(deltaTime);
                        break;
                    case ActionType.WeakAttack:
                        ((ActionData<AttackActionData>)data).UpdateCooldown(deltaTime);
                        break;
                    case ActionType.StrongAttack:
                        ((ActionData<AttackActionData>)data).UpdateCooldown(deltaTime);
                        break;
                    case ActionType.SkillAttack:
                        ((ActionData<AttackActionData>)data).UpdateCooldown(deltaTime);
                        break;
                    case ActionType.Guard:
                        ((ActionData<DefenseActionData>)data).UpdateCooldown(deltaTime);
                        break;
                    case ActionType.Block:
                        ((ActionData<DefenseActionData>)data).UpdateCooldown(deltaTime);
                        break;
                    case ActionType.Extension:
                        ((ActionData<ExtensionActionData>)data).UpdateCooldown(deltaTime);
                        break;
                    case ActionType.ModeSwitch:
                        ((ActionData<ModeSwitchActionData>)data).UpdateCooldown(deltaTime);
                        break;
                    case ActionType.Maneuver:
                        ((ActionData<ManeuverActionData>)data).UpdateCooldown(deltaTime);
                        break;
                }
            }
        }

        /// <summary>
        /// 全アクションのクールダウンをリセット
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetAllCooldowns()
        {
            foreach ( var kvp in actionDatabase )
            {
                var actionType = kvp.Key;
                var data = kvp.Value;

                // リフレクションを避けるためのswitch文
                switch ( actionType )
                {
                    case ActionType.Walk:
                        ((ActionData<MovementActionData>)data).Reset();
                        break;
                    case ActionType.Jump:
                        ((ActionData<MovementActionData>)data).Reset();
                        break;
                    case ActionType.Boost:
                        ((ActionData<MovementActionData>)data).Reset();
                        break;
                    case ActionType.Dodge:
                        ((ActionData<MovementActionData>)data).Reset();
                        break;
                    case ActionType.WeakAttack:
                        ((ActionData<AttackActionData>)data).Reset();
                        break;
                    case ActionType.StrongAttack:
                        ((ActionData<AttackActionData>)data).Reset();
                        break;
                    case ActionType.SkillAttack:
                        ((ActionData<AttackActionData>)data).Reset();
                        break;
                    case ActionType.Guard:
                        ((ActionData<DefenseActionData>)data).Reset();
                        break;
                    case ActionType.Block:
                        ((ActionData<DefenseActionData>)data).Reset();
                        break;
                    case ActionType.Extension:
                        ((ActionData<ExtensionActionData>)data).Reset();
                        break;
                    case ActionType.ModeSwitch:
                        ((ActionData<ModeSwitchActionData>)data).Reset();
                        break;
                    case ActionType.Maneuver:
                        ((ActionData<ManeuverActionData>)data).Reset();
                        break;
                }
            }
        }

        #endregion

        #region === 従来のシステム互換性 ===

        [Title("従来システム互換性")]
        [FoldoutGroup("エネルギー設定")]
        [PropertyTooltip("エネルギー設定（従来システム用）")]
        public EnergySettings energy = new EnergySettings();

        [FoldoutGroup("移動設定")]
        [PropertyTooltip("移動設定（従来システム用）")]
        public MovementSettings movement = new MovementSettings();

        [FoldoutGroup("攻撃設定")]
        [PropertyTooltip("攻撃設定（従来システム用）")]
        public AttackSettings attack = new AttackSettings();

        [FoldoutGroup("防御設定")]
        [PropertyTooltip("防御設定（従来システム用）")]
        public DefenseSettings defense = new DefenseSettings();

        [System.Serializable]
        public class EnergySettings
        {
            public float maxEnergy = 100f;
            public float normalRecoveryRate = 25f;
            public float fastRecoveryRate = 50f;
            public float boostConsumption = 30f;
            public float dodgeEnergyCost = 15f;
            public float airJumpEnergyCost = 10f;
        }

        [System.Serializable]
        public class MovementSettings
        {
            [Header("基本移動")]
            [Tooltip("通常の歩行速度")]
            public float moveSpeed = 10f;

            [Tooltip("空中での移動速度")]
            public float airMoveSpeed = 5f;

            [Tooltip("ブースト時の移動速度")]
            public float boostSpeed = 12f;

            [Header("ジャンプ")]
            [Tooltip("通常ジャンプの力")]
            public float jumpForce = 10f;

            [Tooltip("ジャンプ時間")]
            public float jumpTime = 1f;

            [Header("回避")]
            [Tooltip("回避速度")]
            public float dodgeSpeed = 8f;

            [Tooltip("回避継続時間")]
            public float dodgeTime = 1f;

            [Tooltip("回避のエネルギー消費量")]
            public float dodgeEnergyCost = 15f;

            [Tooltip("回避インターバル")]
            public float normalDodgeInterval = 0.3f;

            [Tooltip("エネルギー切れ時の回避インターバル")]
            public float energyDepletedDodgeInterval = 1f;

            [Header("空中制御")]
            [Tooltip("空中での移動速度倍率")]
            public float airMobilityMultiplier = 0.7f;

            [Header("エネルギー")]
            [Tooltip("ブーストの毎秒エネルギー消費量")]
            public float boostEnergyConsumption = 30f;

            [Header("AI用設定")]
            [Tooltip("AIが安全と判断する距離")]
            public float safeDistance = 10f;

            /// <summary>
            /// エネルギー状態に応じた回避インターバルを取得
            /// </summary>
            /// <param name="isEnergyDepleted">エネルギー切れ状態かどうか</param>
            /// <returns>適用すべき回避インターバル時間</returns>
            public float GetDodgeInterval(bool isEnergyDepleted)
            {
                return isEnergyDepleted ? energyDepletedDodgeInterval : normalDodgeInterval;
            }
        }

        [System.Serializable]
        public class AttackSettings
        {
            public float meleeRange = 2;
            public float weakAttackDamage = 25;
            public float strongAttackDamage = 50;
            public float attackSpeed = 1;
        }

        [System.Serializable]
        public class DefenseSettings
        {
            public float blockEnergyRecovery = 20f;
            public float blockFailDamageMultiplier = 1.5f;
            public float blockFailEnergyCost = 10f;
            public float guardEnergyBonusTime = 3f;
            public float guardEnergyBonusMultiplier = 2f;
            public float blockMoveDistance = 6f;
        }

        #endregion

        #region === 初期化 ===

        /// <summary>
        /// デフォルト値の初期化
        /// </summary>
        private void InitializeDefaultValues()
        {
            // 基本ステータス
            maxHealth = 500f;
            maxEnergy = 100f;
            normalEnergyRecoveryRate = 25f;
            fastEnergyRecoveryRate = 50f;
            maxStunGauge = 100f;
            stunGaugeRecoveryRate = 20f;

            // 従来システム設定
            energy = new EnergySettings();
            movement = new MovementSettings();
            attack = new AttackSettings();
            defense = new DefenseSettings();
        }

        /// <summary>
        /// アクションデータベースの初期化
        /// </summary>
        private void InitializeActionDatabase()
        {
            actionDatabase = new Dictionary<ActionType, object>();

            InitializeMovementActions();
            InitializeAttackActions();
            InitializeRangedActions();
            InitializeDefenseActions();
            InitializeSpecialActions();

            // データベースに登録
            actionDatabase[ActionType.Walk] = walkAction;
            actionDatabase[ActionType.Jump] = jumpAction;
            actionDatabase[ActionType.Boost] = boostAction;
            actionDatabase[ActionType.Dodge] = dodgeAction;
            actionDatabase[ActionType.WeakAttack] = weakAttackAction;
            actionDatabase[ActionType.StrongAttack] = strongAttackAction;
            actionDatabase[ActionType.SkillAttack] = skillAttackAction;
            actionDatabase[ActionType.Guard] = guardAction;
            actionDatabase[ActionType.Block] = blockAction;
            actionDatabase[ActionType.Extension] = extensionAction;
            actionDatabase[ActionType.ModeSwitch] = modeSwitchAction;
            actionDatabase[ActionType.Maneuver] = maneuverAction;
        }

        private void InitializeMovementActions()
        {
            // 歩行
            walkAction = new ActionData<MovementActionData>
            {
                energyCost = 0f,
                maxConsecutiveUses = 999,
                cooldownTime = 0f,
                data = new MovementActionData
                {
                    speed = 5f,
                    acceleration = 0.2f,
                    jumpForce = 10f,
                    chargeTime = 1f,
                    chargedJumpForce = 15f,
                    dodgeDistance = 8f,
                    normalDodgeInterval = 0.3f,
                    energyDepletedDodgeInterval = 1f,
                    doubleDodgeEnergyCost = 30f
                }
            };

            // ジャンプ
            jumpAction = new ActionData<MovementActionData>
            {
                energyCost = 0f,
                maxConsecutiveUses = 999,
                cooldownTime = 0f,
                data = new MovementActionData
                {
                    speed = 5f,
                    acceleration = 0.2f,
                    jumpForce = 10f,
                    chargeTime = 1f,
                    chargedJumpForce = 15f,
                    dodgeDistance = 8f,
                    normalDodgeInterval = 0.3f,
                    energyDepletedDodgeInterval = 1f,
                    doubleDodgeEnergyCost = 30f
                }
            };

            // ブースト
            boostAction = new ActionData<MovementActionData>
            {
                energyCost = 30f, // 持続消費
                maxConsecutiveUses = 999,
                cooldownTime = 0f,
                data = new MovementActionData
                {
                    speed = 12f,
                    acceleration = 0.1f,
                    jumpForce = 10f,
                    chargeTime = 1f,
                    chargedJumpForce = 15f,
                    dodgeDistance = 8f,
                    normalDodgeInterval = 0.3f,
                    energyDepletedDodgeInterval = 1f,
                    doubleDodgeEnergyCost = 30f
                }
            };

            // 回避
            dodgeAction = new ActionData<MovementActionData>
            {
                energyCost = 15f,
                maxConsecutiveUses = 999,
                cooldownTime = 0.3f, // 通常インターバル
                data = new MovementActionData
                {
                    speed = 5f,
                    acceleration = 0.2f,
                    jumpForce = 10f,
                    chargeTime = 1f,
                    chargedJumpForce = 15f,
                    dodgeDistance = 8f,
                    normalDodgeInterval = 0.3f,
                    energyDepletedDodgeInterval = 1f,
                    doubleDodgeEnergyCost = 30f
                }
            };
        }

        private void InitializeAttackActions()
        {
            // 弱攻撃
            weakAttackAction = new ActionData<AttackActionData>
            {
                energyCost = 5f,
                maxConsecutiveUses = 999,
                cooldownTime = 0f,
                data = new AttackActionData
                {
                    baseDamage = 25f,
                    range = 2f,
                    speed = 1.2f,
                    stunAccumulation = 10f,
                    canCancel = false,
                    cancelWindow = 0f,
                    comboContinueTime = 1f,
                    maxComboCount = 5
                }
            };

            // 強攻撃
            strongAttackAction = new ActionData<AttackActionData>
            {
                energyCost = 15f,
                maxConsecutiveUses = 999,
                cooldownTime = 0f,
                data = new AttackActionData
                {
                    baseDamage = 50f,
                    range = 2.5f,
                    speed = 0.8f,
                    stunAccumulation = 25f,
                    canCancel = true,
                    cancelWindow = 0.3f,
                    comboContinueTime = 1.5f,
                    maxComboCount = 3
                }
            };

            // スキル攻撃
            skillAttackAction = new ActionData<AttackActionData>
            {
                energyCost = 25f,
                maxConsecutiveUses = 1,
                cooldownTime = 10f,
                data = new AttackActionData
                {
                    baseDamage = 75f,
                    range = 3f,
                    speed = 1f,
                    stunAccumulation = 40f,
                    canCancel = false,
                    cancelWindow = 0f,
                    comboContinueTime = 0f,
                    maxComboCount = 1
                }
            };
        }

        private void InitializeRangedActions()
        {
            // 弱射撃
            weakRangedAction = new ActionData<RangedActionData>
            {
                energyCost = 0f,
                maxConsecutiveUses = 10,
                cooldownTime = 0f,
                data = new RangedActionData
                {
                    ammoCount = 10,
                    reloadTime = 2f,
                    accuracyTime = 1.5f,
                    pierceGuardAtMaxAccuracy = true,
                    simultaneousShots = 1,
                    spreadAngle = 2f
                }
            };

            // 強射撃
            strongRangedAction = new ActionData<RangedActionData>
            {
                energyCost = 0f,
                maxConsecutiveUses = 5,
                cooldownTime = 1f,
                data = new RangedActionData
                {
                    ammoCount = 5,
                    reloadTime = 3f,
                    accuracyTime = 2f,
                    pierceGuardAtMaxAccuracy = true,
                    simultaneousShots = 1,
                    spreadAngle = 0f
                }
            };
        }

        private void InitializeDefenseActions()
        {
            // ガード
            guardAction = new ActionData<DefenseActionData>
            {
                energyCost = 0f,
                maxConsecutiveUses = 999,
                cooldownTime = 0f,
                data = new DefenseActionData
                {
                    defensiveDirections = 3,
                    energyRecovery = 0f,
                    energyBonusTime = 3f,
                    energyBonusMultiplier = 2f,
                    successWindow = 999f, // ガードは常時有効
                    failureDamageMultiplier = 1f,
                    successMoveDistance = 0f,
                    canGuardWhileMoving = true
                }
            };

            // ブロッキング
            blockAction = new ActionData<DefenseActionData>
            {
                energyCost = 5f, // 失敗時のペナルティ
                maxConsecutiveUses = 999,
                cooldownTime = 0f,
                data = new DefenseActionData
                {
                    defensiveDirections = 3,
                    energyRecovery = 20f,
                    energyBonusTime = 0f,
                    energyBonusMultiplier = 1f,
                    successWindow = 0.15f,
                    failureDamageMultiplier = 1.5f,
                    successMoveDistance = 6f,
                    canGuardWhileMoving = false
                }
            };
        }

        private void InitializeSpecialActions()
        {
            // エクステンション
            extensionAction = new ActionData<ExtensionActionData>
            {
                energyCost = 0f,
                maxConsecutiveUses = 3,
                cooldownTime = 8f,
                data = new ExtensionActionData
                {
                    effectType = ExtensionEffectType.Support,
                    effectValue = 20f,
                    effectRange = 10f,
                    effectDuration = 5f,
                    effectName = "DefaultExtension",
                    isPlaceable = false,
                    isAutoActivate = false
                }
            };

            // モード切り替え
            modeSwitchAction = new ActionData<ModeSwitchActionData>
            {
                energyCost = 0f,
                maxConsecutiveUses = 999,
                cooldownTime = 0f,
                data = new ModeSwitchActionData
                {
                    targetMode = ActionMode.Ranged, // デフォルトで射撃モードに切り替え
                    switchTime = 0.3f,
                    invincibilityDuration = 0f,
                    switchCondition = ModeSwitchCondition.Always
                }
            };

            // マニューバ
            maneuverAction = new ActionData<ManeuverActionData>
            {
                energyCost = 30f,
                maxConsecutiveUses = 1,
                cooldownTime = 15f,
                data = new ManeuverActionData
                {
                    recordedPattern = "",
                    executionTime = 2f,
                    speedMultiplier = 1f,
                    invincibilityDuration = 0f,
                    canCancelMidway = false,
                    autoSkillAfterExecution = ActionType.Walk, // 既存ActionTypeのデフォルト値
                    earlyUseEnergyMultiplier = 2f
                }
            };
        }

        #endregion

        #region === Unity初期化 ===

        /// <summary>
        /// ScriptableObjectの初期化時に呼ばれる
        /// </summary>
        private void OnEnable()
        {
            if ( actionDatabase == null )
            {
                InitializeDefaultValues();
                InitializeActionDatabase();
            }
        }

        /// <summary>
        /// エディタでの値変更時に呼ばれる
        /// </summary>
        private void OnValidate()
        {
            // 値の範囲チェック
            maxHealth = Mathf.Max(100f, maxHealth);
            maxEnergy = Mathf.Max(50f, maxEnergy);
            normalEnergyRecoveryRate = Mathf.Max(10f, normalEnergyRecoveryRate);
            fastEnergyRecoveryRate = Mathf.Max(normalEnergyRecoveryRate, fastEnergyRecoveryRate);
            maxStunGauge = Mathf.Max(50f, maxStunGauge);
            stunGaugeRecoveryRate = Mathf.Max(10f, stunGaugeRecoveryRate);
        }

        #endregion

        #region === デバッグ・ツール ===

        [Title("デバッグ機能")]
        [Button("全アクションリセット", ButtonSizes.Large)]
        [GUIColor(1f, 0.8f, 0.8f)]
        private void DebugResetAllActions()
        {
            ResetAllCooldowns();
            Debug.Log("全アクションのクールダウンをリセットしました");
        }

        [Button("ActionDataベース再構築", ButtonSizes.Large)]
        [GUIColor(0.8f, 1f, 0.8f)]
        private void DebugRebuildActionDatabase()
        {
            InitializeActionDatabase();
            Debug.Log("ActionDataベースを再構築しました");
        }

        [Button("設定値検証", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.8f, 1f)]
        private void DebugValidateSettings()
        {
            bool isValid = true;

            if ( !ValidateHealth(maxHealth) )
            {
                Debug.LogError("体力設定が無効です");
                isValid = false;
            }

            if ( !ValidateEnergy(maxEnergy) )
            {
                Debug.LogError("エネルギー設定が無効です");
                isValid = false;
            }

            if ( actionDatabase == null || actionDatabase.Count == 0 )
            {
                Debug.LogError("ActionDataベースが初期化されていません");
                isValid = false;
            }

            if ( isValid )
            {
                Debug.Log("全ての設定が有効です");
            }
        }

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在のActionDataベースサイズ")]
        private int ActionDatabaseSize => actionDatabase?.Count ?? 0;

        #endregion
    }
}