using UnityEngine;
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
        [Header("基本設定")]
        [Tooltip("エネルギー消費量")]
        public float energyCost = 0f;

        [Tooltip("最大連続使用回数")]
        [Range(1, 999)]
        public int maxConsecutiveUses = 1;

        /// <summary>
        /// 一度そのアクションを使用した後、再度使用可能になるまでの待機時間（秒）
        /// </summary>
        [Tooltip("クールダウン時間")]
        [Range(0f, 60f)]
        public float cooldownTime = 0f;

        [Header("専用データ")]
        [Tooltip("アクション固有のデータ")]
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
            if (Time.time - lastUsedTime >= cooldownTime)
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
    public class MovementSetting
    {
        [Header("移動設定")]
        [Tooltip("移動速度")]
        public float speed = 5f;
    }

    [System.Serializable]
    public class JumpSetting
    {
        [Header("ジャンプ設定")]
        [Tooltip("ジャンプ力")]
        public float jumpForce = 10f;

        [Tooltip("ジャンプ継続時間")]
        public float jumpTime = 1f;
    }

    /// <summary>
    /// 回避アクションのデータ
    /// 通常回避とエネルギー切れ時の回避で二つ作る
    /// </summary>
    [System.Serializable]
    public class DodgeSetting
    {
        [Header("回避設定")]
        [Tooltip("回避距離")]
        public float dodgeDistance = 8f;

        [Tooltip("回避エネルギー消費")]
        public float dodgeEnergyCost = 20f;
    }

    /// <summary>
    /// 攻撃アクションデータ
    /// バリアの耐久削りの値はダメージをベースに行う
    /// </summary>
    [System.Serializable]
    public class AttackSetting
    {
        [Header("基本設定")]
        /// <summary>
        /// エネルギー枯渇時のバリアに対する蓄積ダメージ値でもある
        /// </summary>
        [Tooltip("基本ダメージ")]
        public float baseDamage = 25f;

        [Tooltip("スタン蓄積値")]
        public float stunAccumulation = 10f;

        /// <summary>
        /// スキルや強攻撃の場合、この秒数以降はキャンセル可能になる
        /// 0の場合はキャンセル不可
        /// </summary>
        [Tooltip("キャンセル可能タイミング")]
        public float cancelTiming = 0f;

        [Header("コンボ設定")]
        /// <summary>
        /// 攻撃実行後、次の攻撃の入力を受け付ける猶予時間（秒）
        /// 
        /// </summary>
        [Tooltip("入力待機時間")]
        public float comboContinueTime = 1f;

        [Tooltip("最大コンボ数")]
        public int maxComboCount = 3;
    }

    /// <summary>
    /// 射撃アクションデータ
    /// </summary>
    [System.Serializable]
    public class RangedActionData
    {
        [Header("弾薬設定")]
        [Tooltip("弾薬数")]
        public int ammoCount = 10;

        [Tooltip("リロード時間")]
        public float reloadTime = 2f;

        [Header("射撃精度")]
        [Tooltip("精度最大化時間")]
        public float accuracyTime = 1.5f;

        [Tooltip("最大精度時ガード貫通")]
        public bool pierceGuardAtMaxAccuracy = true;

        [Header("射撃パターン")]
        [Tooltip("同時発射数")]
        public int simultaneousShots = 1;

        [Tooltip("拡散角度")]
        public float spreadAngle = 5f;
    }

    /// <summary>
    /// 防御アクションデータ
    /// </summary>
    [System.Serializable]
    public class DefenseActionData
    {
        [Header("防御設定")]
        [Tooltip("防御可能方向数")]
        public int defensiveDirections = 3;

        [Tooltip("エネルギー回復量")]
        public float energyRecovery = 10f;

        [Tooltip("エネルギーボーナス時間")]
        public float energyBonusTime = 3f;

        [Tooltip("エネルギーボーナス倍率")]
        public float energyBonusMultiplier = 1.5f;

        [Header("ブロッキング設定")]
        [Tooltip("成功判定時間")]
        public float successWindow = 0.2f;

        [Tooltip("失敗時ダメージ倍率")]
        public float failureDamageMultiplier = 1.2f;

        [Tooltip("成功時移動距離")]
        public float successMoveDistance = 3f;

        [Tooltip("移動中ガード可能")]
        public bool canGuardWhileMoving = true;
    }

    /// <summary>
    /// キャラクター設定のメインクラス
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterSettings", menuName = "Battle/Character Settings")]
    public class CharacterSettings : ScriptableObject
    {
        #region === 基本ステータス ===

        [Header("基本ステータス")]
        [Tooltip("最大体力")]
        public float maxHealth = 500f;

        [Tooltip("最大エネルギー")]
        public float maxEnergy = 100f;

        [Tooltip("通常エネルギー回復速度")]
        public float normalEnergyRecoveryRate = 25f;

        [Tooltip("高速エネルギー回復速度")]
        public float fastEnergyRecoveryRate = 50f;

        [Tooltip("最大スタンゲージ")]
        public float maxStunGauge = 100f;

        [Tooltip("スタンゲージ回復速度")]
        public float stunGaugeRecoveryRate = 20f;

        private bool ValidateHealth(float health) => health >= 100f;
        private bool ValidateEnergy(float energy) => energy >= 50f;

        #endregion

        #region === ActionDataシステム ===

        //[Header("攻撃アクション")]
        //[SerializeField] private ActionData<AttackActionData> weakAttackAction;
        //[Header("攻撃アクション")]
        //[SerializeField] private ActionData<AttackActionData> strongAttackAction;
        //[Header("攻撃アクション")]
        //[SerializeField] private ActionData<AttackActionData> skillAttackAction;

        [Header("射撃アクション")]
        [SerializeField] private ActionData<RangedActionData> _weakRangedAction;
        [Header("射撃アクション")]
        [SerializeField] private ActionData<RangedActionData> _strongRangedAction;

        [Header("防御アクション")]
        [SerializeField] private ActionData<DefenseActionData> _guardAction;

        #endregion

        #region === 従来のシステム互換性 ===

        [Header("従来システム互換性")]
        [Header("エネルギー設定")]
        [Tooltip("エネルギー設定（従来システム用）")]
        public EnergySettings energy = new EnergySettings();

        [Header("移動設定")]
        [Tooltip("移動設定（従来システム用）")]
        public MovementSettings movement = new MovementSettings();

        [Header("攻撃設定")]
        [Tooltip("攻撃設定（従来システム用）")]
        public AttackSettings attack = new AttackSettings();

        [Header("防御設定")]
        [Tooltip("防御設定（従来システム用）")]
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

        #endregion

        #region === Unity初期化 ===

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
    }
}

/// <summary>
/// テスト用の設定データ。
/// </summary>
public class TestSetting
{
    /// <summary>
    /// ブロッキングする確率
    /// </summary>
    public int blockingRate = 33;

    /// <summary>
    /// 回避する確率
    /// </summary>
    public int avoidRate = 33;

    /// <summary>
    /// バックステップする確率
    /// </summary>
    public int backStepRate = 33;
}

/// <summary>
/// テスト用のログデータ
/// 最小規模の入出力テスト用
/// テストではログデータをもとに以下二つが行われるかを確認する
/// ・被弾要因の割り出し
/// ・設定データの書き換え
/// </summary>
public class TestLog
{
    /// <summary>
    /// 被弾時の状況を示す列挙体
    /// </summary>
    public enum TestActState : byte
    {
        Blocking,// ブロッキング
        Avoiding, // 回避
        backStepping,// バックステップ
    }

    /// <summary>
    /// 被弾時の状況
    /// </summary>
    public TestActState actState;

    /// <summary>
    /// 受けたダメージ
    /// </summary>
    public int damage;
}

