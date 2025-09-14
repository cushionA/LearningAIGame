using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Mathematics;

namespace LearningAIGame.CombatSystem
{
    #region === ActionType定義 ===

    /// <summary>
    /// 共通の行動データ（汎用化）
    /// すべての行動（移動・攻撃・スキルなど）に適用可能。
    /// </summary>
    [Serializable]
    public class ActionDataBase
    {
        [Title("消費設定")]
        [PropertyTooltip("行動に必要なエネルギー量")]
        [Range(0f, 100f)]
        public float energyCost = 10f;
        /// <summary>
        /// 銃撃系のアクションであれば弾数にも応用できる
        /// </summary>
        [Title("実行回数設定")]
        [PropertyTooltip("連続して実行できる最大回数")]
        [Range(1, 10)]
        public int maxConsecutiveUses = 1;
        /// <summary>
        /// 連続実行回数とは異なり、一度の実行で追加入力できる回数
        /// この範囲内なら何度入力しても一回の実行としてまとめられる
        /// </summary>
        [Title("連続入力数")]
        [PropertyTooltip("一度の実行で追加入力できる回数")]
        [Range(1, 10)]
        public int additionalCount = 1;
        [PropertyTooltip("現在の残り実行可能回数")]
        public int currentUses;
        [Title("クールタイム設定")]
        [PropertyTooltip("連続実行回数を使い果たした際のクールタイム（秒）")]
        [Range(0f, 150f)]
        public float cooldownTime = 5f;
        [Title("アクションの継続実行可能時間")]
        [PropertyTooltip("アクションの継続実行可能時間")]
        [Range(0f, 30f)]
        public float continueTime = 5f;
        /// <summary>
        /// この行動がキャンセルされなかった場合、自動でつながるアクション
        /// これいらないかも
        /// チャージ完了とかは別にイベント飛ばせるし
        /// </summary>
        [Title("この行動がキャンセルされなかった場合、自動でつながるアクション")]
        [PropertyTooltip("この行動がキャンセルされなかった場合、自動でつながるアクション")]
        public ActionType nextAction;
        [HideInInspector]
        [PropertyTooltip("現在のクールタイム残り時間（秒）")]
        public float currentCooldown;
        /// <summary>
        /// 行動を実行し、回数とクールタイムを更新
        /// </summary>
        public virtual void Execute()
        {
            currentUses--;
            if ( currentUses <= 0 )
            {
                // 使用回数を使い果たしたらクールタイム開始
                StartCooldown();
            }
        }
        /// <summary>
        /// クールタイムを開始する
        /// </summary>
        public void StartCooldown()
        {
            currentCooldown = cooldownTime;
        }
        /// <summary>
        /// 毎フレーム呼び出してクールタイムを進行させる
        /// </summary>
        public void UpdateCooldown(float deltaTime)
        {
            if ( currentCooldown > 0f )
            {
                currentCooldown -= deltaTime;
                if ( currentCooldown <= 0f )
                {
                    // クールタイム終了で使用回数をリセット
                    currentUses = maxConsecutiveUses;
                    currentCooldown = 0f;
                }
            }
        }
    }
    /// <summary>
    /// ジェネリック対応の行動データ
    /// Tの型で追加データを持たせることができる。
    /// ActionTypeをキーにして管理を行う
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ActionData<T> : ActionDataBase
    {
        /// <summary>
        /// 行動に関連するデータ
        /// 攻撃系の行動なら攻撃力y踏み込み距離など
        /// アクションに必要なデータを持つ
        /// </summary>
        public T data;
    }

    #endregion

    #region === 専用ActionDataクラス群 ===

    /// <summary>
    /// 移動系行動のデータ
    /// </summary>
    [Serializable]
    public class MovementActionData
    {
        [PropertyTooltip("移動速度")]
        [Range(1f, 30f)]
        public float speed = 5f;

        [PropertyTooltip("移動距離（ジャンプの高さなど）")]
        [Range(0f, 20f)]
        public float distance = 0f;

        [PropertyTooltip("無敵時間（開始時間, 継続時間）")]
        public float2 invincibilityFrame = new float2(0f, 0f);

        [PropertyTooltip("継続消費エネルギー（秒あたり）")]
        [Range(0f, 50f)]
        public float continuousEnergyCost = 0f;

        [PropertyTooltip("チャージ可能な行動か")]
        public bool canCharge = false;

        [PropertyTooltip("最大チャージ時間")]
        [Range(0f, 5f)]
        public float maxChargeTime = 1f;

        [PropertyTooltip("チャージ時の効果倍率")]
        [Range(1f, 3f)]
        public float chargeMultiplier = 1.5f;

        [PropertyTooltip("行動後の特殊状態継続時間")]
        [Range(0f, 2f)]
        public float postActionDuration = 0f;
    }

    /// <summary>
    /// 攻撃系行動のデータ
    /// </summary>
    [Serializable]
    public class AttackActionData
    {
        [PropertyTooltip("基本ダメージ")]
        [Range(5f, 200f)]
        public float damage = 25f;

        [PropertyTooltip("発生フレーム（攻撃開始までの時間）")]
        [Range(0.05f, 2f)]
        public float startup = 0.2f;

        [PropertyTooltip("持続フレーム（攻撃判定の継続時間）")]
        [Range(0.05f, 1f)]
        public float duration = 0.1f;

        [PropertyTooltip("硬直フレーム（攻撃後の隙）")]
        [Range(0.1f, 2f)]
        public float recovery = 0.3f;

        [PropertyTooltip("踏み込み距離")]
        [Range(0f, 10f)]
        public float lungeDistance = 2f;

        [PropertyTooltip("攻撃範囲")]
        [Range(0.5f, 10f)]
        public float range = 3f;

        [PropertyTooltip("キャンセル可能か")]
        public bool canCancel = false;

        [PropertyTooltip("キャンセル時の追加エネルギー消費")]
        [Range(0f, 30f)]
        public float cancelEnergyCost = 10f;

        [PropertyTooltip("ガード可能な攻撃か")]
        public bool canBeGuarded = true;

        [PropertyTooltip("ガード時の相手への影響")]
        [Range(0f, 2f)]
        public float guardImpact = 0.5f;

        [PropertyTooltip("コンボ受付時間")]
        [Range(0f, 2f)]
        public float comboWindow = 0.8f;

        [PropertyTooltip("空中攻撃時の滞空時間")]
        [Range(0f, 2f)]
        public float floatTime = 0f;
    }

    /// <summary>
    /// 射撃系行動のデータ
    /// </summary>
    [Serializable]
    public class ShootActionData
    {
        [PropertyTooltip("基本ダメージ")]
        [Range(5f, 150f)]
        public float damage = 15f;

        [PropertyTooltip("発射速度")]
        [Range(10f, 100f)]
        public float projectileSpeed = 50f;

        [PropertyTooltip("射程距離")]
        [Range(10f, 100f)]
        public float range = 50f;

        [PropertyTooltip("発射レート（秒間発射数）")]
        [Range(1f, 20f)]
        public float fireRate = 5f;

        [PropertyTooltip("弾数（0で無限）")]
        [Range(0, 100)]
        public int ammoCount = 0;

        [PropertyTooltip("リロード時間")]
        [Range(0f, 5f)]
        public float reloadTime = 2f;

        [PropertyTooltip("精度向上に必要な時間")]
        [Range(0.1f, 3f)]
        public float accuracyTime = 1.5f;

        [PropertyTooltip("最大精度時のガード貫通")]
        public bool pierceGuardAtMaxAccuracy = true;

        [PropertyTooltip("同時発射数")]
        [Range(1, 10)]
        public int simultaneousShots = 1;

        [PropertyTooltip("拡散角度")]
        [Range(0f, 45f)]
        public float spreadAngle = 0f;
    }

    /// <summary>
    /// 防御系行動のデータ
    /// </summary>
    [Serializable]
    public class DefenseActionData
    {
        [PropertyTooltip("防御可能な方向数")]
        [Range(1, 3)]
        public int defensiveDirections = 3;

        [PropertyTooltip("成功時のエネルギー回復量")]
        [Range(0f, 50f)]
        public float energyRecovery = 20f;

        [PropertyTooltip("成功時のエネルギー回復ボーナス時間")]
        [Range(0f, 5f)]
        public float energyBonusTime = 3f;

        [PropertyTooltip("成功時のエネルギー回復倍率")]
        [Range(1f, 3f)]
        public float energyBonusMultiplier = 2f;

        [PropertyTooltip("成功判定ウィンドウ")]
        [Range(0.05f, 0.5f)]
        public float successWindow = 0.15f;

        [PropertyTooltip("失敗時のダメージ増加率")]
        [Range(1f, 2f)]
        public float failureDamageMultiplier = 1.5f;

        [PropertyTooltip("成功時の移動距離（ブロッキング用）")]
        [Range(0f, 10f)]
        public float successMoveDistance = 6f;

        [PropertyTooltip("移動中のガード可能性")]
        public bool canGuardWhileMoving = true;
    }

    /// <summary>
    /// スキル効果タイプ
    /// </summary>
    public enum SkillEffectType
    {
        Damage,         // ダメージ
        Heal,           // 回復
        EnergyRestore,  // エネルギー回復
        Buff,           // バフ効果
        Debuff,         // デバフ効果
        Movement,       // 移動効果
        Control,        // 制御効果
        Shield,         // シールド効果
        Special         // 特殊効果
    }

    /// <summary>
    /// マニューバ行動のデータ
    /// </summary>
    [Serializable]
    public class ManeuverActionData
    {
        [PropertyTooltip("記録された移動パターン")]
        public string recordedPattern = "";

        [PropertyTooltip("実行時間")]
        [Range(0.5f, 10f)]
        public float executionTime = 2f;

        [PropertyTooltip("実行速度倍率")]
        [Range(0.5f, 3f)]
        public float speedMultiplier = 1f;

        [PropertyTooltip("実行中の無敵時間")]
        [Range(0f, 1f)]
        public float invincibilityDuration = 0f;

        [PropertyTooltip("途中キャンセル可能")]
        public bool canCancelMidway = false;

        [PropertyTooltip("終了時に自動実行するスキル")]
        public ActionType autoSkillAfterExecution = ActionType.None;

        [PropertyTooltip("早期使用時の追加エネルギー消費倍率")]
        [Range(1f, 3f)]
        public float earlyUseEnergyMultiplier = 2f;
    }
    #endregion

    #region === CharacterSettings本体 ===
    /// <summary>
    /// 行動データベース統合型のキャラクター設定
    /// 全ての行動をActionDataで統一管理
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterSettings", menuName = "LearningAIGame/Character Settings")]
    public class CharacterSettings : ScriptableObject
    {
        [Title("基本パラメータ")]
        [ValidateInput("ValidateHealth", "体力は0より大きい必要があります")]
        [Range(100f, 1000f)]
        [PropertyTooltip("最大体力")]
        public float maxHealth = 500f;

        [PropertyTooltip("最大エネルギー")]
        [Range(50f, 200f)]
        public float maxEnergy = 100f;

        [PropertyTooltip("通常時のエネルギー回復速度")]
        [Range(10f, 40f)]
        public float normalEnergyRecoveryRate = 25f;

        [PropertyTooltip("エネルギー切れ時の高速回復速度")]
        [Range(30f, 80f)]
        public float fastEnergyRecoveryRate = 50f;

        [PropertyTooltip("スタンゲージの最大値")]
        [Range(50f, 150f)]
        public float maxStunGauge = 100f;

        [PropertyTooltip("スタンゲージの回復速度")]
        [Range(10f, 40f)]
        public float stunGaugeRecoveryRate = 25f;

        [Title("武器設定")]
        [PropertyTooltip("装備する武器の設定")]
        [InlineEditor(InlineEditorModes.LargePreview)]
        public WeaponSettings weaponSettings;

        #region === 行動データベース ===

        [Title("移動系行動")]
        [PropertyTooltip("歩行")]
        [InlineProperty, HideLabel]
        public ActionData<MovementActionData> walkAction = new ActionData<MovementActionData>();

        [PropertyTooltip("ブースト")]
        [InlineProperty, HideLabel]
        public ActionData<MovementActionData> boostAction = new ActionData<MovementActionData>();

        [PropertyTooltip("ジャンプ")]
        [InlineProperty, HideLabel]
        public ActionData<MovementActionData> jumpAction = new ActionData<MovementActionData>();

        [PropertyTooltip("空中ジャンプ")]
        [InlineProperty, HideLabel]
        public ActionData<MovementActionData> airJumpAction = new ActionData<MovementActionData>();

        [PropertyTooltip("回避")]
        [InlineProperty, HideLabel]
        public ActionData<MovementActionData> dodgeAction = new ActionData<MovementActionData>();

        [PropertyTooltip("二段回避")]
        [InlineProperty, HideLabel]
        public ActionData<MovementActionData> doubleDodgeAction = new ActionData<MovementActionData>();

        [PropertyTooltip("クイックターン")]
        [InlineProperty, HideLabel]
        public ActionData<MovementActionData> quickTurnAction = new ActionData<MovementActionData>();

        [Title("近接攻撃系行動")]
        [PropertyTooltip("弱攻撃")]
        [InlineProperty, HideLabel]
        public ActionData<AttackActionData> weakMeleeAction = new ActionData<AttackActionData>();

        [PropertyTooltip("強攻撃")]
        [InlineProperty, HideLabel]
        public ActionData<AttackActionData> strongMeleeAction = new ActionData<AttackActionData>();

        [PropertyTooltip("空中攻撃")]
        [InlineProperty, HideLabel]
        public ActionData<AttackActionData> aerialAttackAction = new ActionData<AttackActionData>();

        [PropertyTooltip("回避攻撃")]
        [InlineProperty, HideLabel]
        public ActionData<AttackActionData> dodgeAttackAction = new ActionData<AttackActionData>();

        [Title("射撃系行動")]
        [PropertyTooltip("弱射撃")]
        [InlineProperty, HideLabel]
        public ActionData<ShootActionData> weakShootAction = new ActionData<ShootActionData>();

        [PropertyTooltip("強射撃")]
        [InlineProperty, HideLabel]
        public ActionData<ShootActionData> strongShootAction = new ActionData<ShootActionData>();

        [PropertyTooltip("チャージ射撃")]
        [InlineProperty, HideLabel]
        public ActionData<ShootActionData> chargedShootAction = new ActionData<ShootActionData>();

        [Title("防御系行動")]
        [PropertyTooltip("ガード")]
        [InlineProperty, HideLabel]
        public ActionData<DefenseActionData> guardAction = new ActionData<DefenseActionData>();

        [PropertyTooltip("ブロッキング")]
        [InlineProperty, HideLabel]
        public ActionData<DefenseActionData> blockAction = new ActionData<DefenseActionData>();

        [Title("スキル系行動")]
        [PropertyTooltip("スキル1")]
        [InlineProperty, HideLabel]
        public ActionData<SkillActionData> skill1Action = new ActionData<SkillActionData>();

        [PropertyTooltip("スキル2")]
        [InlineProperty, HideLabel]
        public ActionData<SkillActionData> skill2Action = new ActionData<SkillActionData>();

        [PropertyTooltip("スキル3")]
        [InlineProperty, HideLabel]
        public ActionData<SkillActionData> skill3Action = new ActionData<SkillActionData>();

        [PropertyTooltip("スキル4")]
        [InlineProperty, HideLabel]
        public ActionData<SkillActionData> skill4Action = new ActionData<SkillActionData>();

        [PropertyTooltip("スキル5")]
        [InlineProperty, HideLabel]
        public ActionData<SkillActionData> skill5Action = new ActionData<SkillActionData>();

        [Title("マニューバ系行動")]
        [PropertyTooltip("マニューバ1")]
        [InlineProperty, HideLabel]
        public ActionData<ManeuverActionData> maneuver1Action = new ActionData<ManeuverActionData>();

        [PropertyTooltip("マニューバ2")]
        [InlineProperty, HideLabel]
        public ActionData<ManeuverActionData> maneuver2Action = new ActionData<ManeuverActionData>();

        [PropertyTooltip("マニューバ3")]
        [InlineProperty, HideLabel]
        public ActionData<ManeuverActionData> maneuver3Action = new ActionData<ManeuverActionData>();

        [PropertyTooltip("マニューバ4")]
        [InlineProperty, HideLabel]
        public ActionData<ManeuverActionData> maneuver4Action = new ActionData<ManeuverActionData>();

        [PropertyTooltip("マニューバ5")]
        [InlineProperty, HideLabel]
        public ActionData<ManeuverActionData> maneuver5Action = new ActionData<ManeuverActionData>();

        #endregion

        #region === 行動データアクセス用Dictionary ===

        [HideInInspector]
        private Dictionary<ActionType, ActionDataBase> actionDatabase;

        /// <summary>
        /// 行動データベースを初期化（起動時に一度だけ実行）
        /// </summary>
        public void InitializeActionDatabase()
        {
            actionDatabase = new Dictionary<ActionType, ActionDataBase>
            {
                // 移動系
                { ActionType.Walk, walkAction },
                { ActionType.Boost, boostAction },
                { ActionType.Jump, jumpAction },
                { ActionType.AirJump, airJumpAction },
                { ActionType.Dodge, dodgeAction },
                { ActionType.DoubleDodge, doubleDodgeAction },
                { ActionType.QuickTurn, quickTurnAction },
                
                // 近接攻撃系
                { ActionType.WeakMelee, weakMeleeAction },
                { ActionType.StrongMelee, strongMeleeAction },
                { ActionType.AerialAttack, aerialAttackAction },
                { ActionType.DodgeAttack, dodgeAttackAction },
                
                // 射撃系
                { ActionType.WeakShoot, weakShootAction },
                { ActionType.StrongShoot, strongShootAction },
                { ActionType.ChargedShoot, chargedShootAction },
                
                // 防御系
                { ActionType.Guard, guardAction },
                { ActionType.Block, blockAction },
                
                // スキル系
                { ActionType.Skill1, skill1Action },
                { ActionType.Skill2, skill2Action },
                { ActionType.Skill3, skill3Action },
                { ActionType.Skill4, skill4Action },
                { ActionType.Skill5, skill5Action },
                
                // マニューバ系
                { ActionType.Maneuver1, maneuver1Action },
                { ActionType.Maneuver2, maneuver2Action },
                { ActionType.Maneuver3, maneuver3Action },
                { ActionType.Maneuver4, maneuver4Action },
                { ActionType.Maneuver5, maneuver5Action }
            };

            // 各行動データの初期化
            foreach ( var kvp in actionDatabase )
            {
                if ( kvp.Value != null )
                {
                    kvp.Value.currentUses = kvp.Value.maxConsecutiveUses;
                }
            }
        }

        #endregion

        #region === 行動データアクセスメソッド ===

        /// <summary>
        /// 指定した行動タイプの基本データを取得
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ActionDataBase GetActionData(ActionType actionType)
        {
            if ( actionDatabase == null )
                InitializeActionDatabase();

            return actionDatabase.TryGetValue(actionType, out var data) ? data : null;
        }

        /// <summary>
        /// 指定した行動タイプの詳細データを取得（ジェネリック版）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ActionData<T> GetActionData<T>(ActionType actionType)
        {
            return GetActionData(actionType) as ActionData<T>;
        }

        /// <summary>
        /// 行動が実行可能かチェック
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanExecuteAction(ActionType actionType, float currentEnergy)
        {
            var actionData = GetActionData(actionType);
            if ( actionData == null )
                return false;

            // クールタイム中でないかチェック
            if ( actionData.currentCooldown > 0f )
                return false;

            // 使用回数が残っているかチェック
            if ( actionData.currentUses <= 0 )
                return false;

            // エネルギーが足りているかチェック
            if ( currentEnergy < actionData.energyCost )
                return false;

            return true;
        }

        /// <summary>
        /// 行動を実行し、データを更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ExecuteAction(ActionType actionType)
        {
            var actionData = GetActionData(actionType);
            if ( actionData == null )
                return false;

            actionData.Execute();
            return true;
        }

        /// <summary>
        /// 全ての行動データのクールタイムを更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateAllCooldowns(float deltaTime)
        {
            if ( actionDatabase == null )
                return;

            foreach ( var actionData in actionDatabase.Values )
            {
                actionData?.UpdateCooldown(deltaTime);
            }
        }

        /// <summary>
        /// 指定した行動タイプのクールタイムをリセット
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetActionCooldown(ActionType actionType)
        {
            var actionData = GetActionData(actionType);
            if ( actionData != null )
            {
                actionData.currentCooldown = 0f;
                actionData.currentUses = actionData.maxConsecutiveUses;
            }
        }

        /// <summary>
        /// 全ての行動のクールタイムをリセット
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetAllCooldowns()
        {
            if ( actionDatabase == null )
                return;

            foreach ( var actionData in actionDatabase.Values )
            {
                if ( actionData != null )
                {
                    actionData.currentCooldown = 0f;
                    actionData.currentUses = actionData.maxConsecutiveUses;
                }
            }
        }

        #endregion

        #region === 後方互換性用メソッド ===

        /// <summary>
        /// 攻撃力を計算（後方互換性）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float CalculateFinalDamage(ActionType attackType, int comboIndex = 0, bool isAerial = false)
        {
            var attackData = GetActionData<AttackActionData>(attackType);
            if ( attackData?.data == null )
                return 0f;

            float baseDamage = attackData.data.damage;

            // 武器設定からのダメージ補正
            if ( weaponSettings?.comboSettings != null )
            {
                var weaponAttackData = weaponSettings.comboSettings.GetAttackData(comboIndex, isAerial);
                if ( weaponAttackData != null )
                {
                    baseDamage = weaponAttackData.damage;
                }
            }

            return baseDamage;
        }

        /// <summary>
        /// 踏み込み距離を計算（後方互換性）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float CalculateLungeDistance(ActionType attackType, int comboIndex = 0, bool isAerial = false)
        {
            var attackData = GetActionData<AttackActionData>(attackType);
            if ( attackData?.data == null )
                return 0f;

            return attackData.data.lungeDistance;
        }

        /// <summary>
        /// 回避インターバルを取得（後方互換性）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetDodgeInterval(bool isEnergyDepleted)
        {
            var dodgeData = GetActionData(ActionType.Dodge);
            if ( dodgeData == null )
                return 0.3f;

            // エネルギー切れ時はクールタイムを延長
            return isEnergyDepleted ? dodgeData.cooldownTime * 2f : dodgeData.cooldownTime;
        }

        #endregion

        #region === 検証・デバッグ機能 ===

        /// <summary>
        /// 最大体力の妥当性を検証
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ValidateHealth(float health) => health > 0;

        /// <summary>
        /// 設定の妥当性を検証
        /// </summary>
        [Button("設定検証実行", ButtonSizes.Large)]
        [GUIColor(0.7f, 1f, 0.7f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ValidateSettings()
        {
            var isValid = true;
            var report = new System.Text.StringBuilder();

            // 基本設定の検証
            if ( this.maxHealth <= 0 )
            {
                report.AppendLine("❌ 最大体力が0以下です");
                isValid = false;
            }

            if ( this.maxEnergy < 50f )
            {
                report.AppendLine("❌ 最大エネルギーが50未満です");
                isValid = false;
            }

            if ( this.fastEnergyRecoveryRate <= this.normalEnergyRecoveryRate )
            {
                report.AppendLine("⚠️ 高速回復速度が通常回復速度以下です");
            }

            // 行動データの検証
            if ( actionDatabase == null )
                InitializeActionDatabase();

            foreach ( var kvp in actionDatabase )
            {
                var actionData = kvp.Value;
                if ( actionData == null )
                    continue;

                if ( actionData.energyCost < 0 )
                {
                    report.AppendLine($"❌ {kvp.Key}のエネルギー消費が負の値です");
                    isValid = false;
                }

                if ( actionData.maxConsecutiveUses < 1 )
                {
                    report.AppendLine($"❌ {kvp.Key}の最大使用回数が1未満です");
                    isValid = false;
                }

                if ( actionData.cooldownTime < 0 )
                {
                    report.AppendLine($"❌ {kvp.Key}のクールタイムが負の値です");
                    isValid = false;
                }
            }

            if ( isValid )
            {
                report.AppendLine("✅ 全ての設定が妥当です");
            }

            Debug.Log($"設定検証結果:\n{report}");
            return isValid;
        }

        /// <summary>
        /// 行動データベースの状態をログ出力（デバッグ用）
        /// </summary>
        [Button("行動データベース状態表示", ButtonSizes.Medium)]
        [GUIColor(0.7f, 0.7f, 1f)]
        public void LogActionDatabaseStatus()
        {
            if ( actionDatabase == null )
                InitializeActionDatabase();

            var report = new System.Text.StringBuilder();
            report.AppendLine("=== 行動データベース状態 ===");

            foreach ( var kvp in actionDatabase )
            {
                var actionData = kvp.Value;
                if ( actionData == null )
                    continue;

                report.AppendLine($"【{kvp.Key}】");
                report.AppendLine($"  エネルギー消費: {actionData.energyCost}");
                report.AppendLine($"  残り使用回数: {actionData.currentUses}/{actionData.maxConsecutiveUses}");
                report.AppendLine($"  クールタイム: {actionData.currentCooldown:F2}秒");
                report.AppendLine($"  状態: {(actionData.currentCooldown > 0 ? "クールタイム中" : "使用可能")}");
                report.AppendLine();
            }

            Debug.Log(report.ToString());
        }

        #endregion

        #region === プリセット機能 ===

        [Title("プリセット機能")]
        [HorizontalGroup("プリセット")]
        [Button("攻撃特化")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetAttackPreset()
        {
            if ( actionDatabase == null )
                InitializeActionDatabase();

            // 攻撃系のダメージとエネルギー効率を向上
            var weakMelee = GetActionData<AttackActionData>(ActionType.WeakMelee);
            if ( weakMelee?.data != null )
            {
                weakMelee.data.damage *= 1.3f;
                weakMelee.data.startup *= 0.8f;
                weakMelee.energyCost *= 0.9f;
            }

            var strongMelee = GetActionData<AttackActionData>(ActionType.StrongMelee);
            if ( strongMelee?.data != null )
            {
                strongMelee.data.damage *= 1.3f;
                strongMelee.data.startup *= 0.8f;
                strongMelee.energyCost *= 0.9f;
            }

            // 回避性能を向上（攻撃的スタイル）
            var dodge = GetActionData<MovementActionData>(ActionType.Dodge);
            if ( dodge?.data != null )
            {
                dodge.cooldownTime *= 0.8f;
                dodge.energyCost *= 0.9f;
            }

            // エネルギー総量を少し減少
            this.maxEnergy *= 0.9f;

            Debug.Log("攻撃特化プリセットを適用しました");
        }

        [HorizontalGroup("プリセット")]
        [Button("防御特化")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetDefensePreset()
        {
            if ( actionDatabase == null )
                InitializeActionDatabase();

            // 体力とエネルギー回復を向上
            this.maxHealth *= 1.3f;
            this.normalEnergyRecoveryRate *= 1.2f;

            // 防御系の性能向上
            var guard = GetActionData<DefenseActionData>(ActionType.Guard);
            if ( guard?.data != null )
            {
                guard.data.energyRecovery *= 1.3f;
                guard.data.energyBonusMultiplier *= 1.5f;
            }

            var block = GetActionData<DefenseActionData>(ActionType.Block);
            if ( block?.data != null )
            {
                block.data.energyRecovery *= 1.3f;
                block.data.successWindow *= 1.2f;
            }

            // 回避インターバルを延長（慎重なスタイル）
            var dodge = GetActionData(ActionType.Dodge);
            if ( dodge != null )
            {
                dodge.cooldownTime *= 1.2f;
            }

            Debug.Log("防御特化プリセットを適用しました");
        }

        [HorizontalGroup("プリセット")]
        [Button("機動特化")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetMobilityPreset()
        {
            if ( actionDatabase == null )
                InitializeActionDatabase();

            // 移動系の性能を大幅向上
            var boost = GetActionData<MovementActionData>(ActionType.Boost);
            if ( boost?.data != null )
            {
                boost.data.speed *= 1.3f;
                boost.data.continuousEnergyCost *= 0.8f;
            }

            var dodge = GetActionData<MovementActionData>(ActionType.Dodge);
            if ( dodge?.data != null )
            {
                dodge.data.distance *= 1.2f;
                dodge.energyCost *= 0.8f;
                dodge.cooldownTime *= 0.5f; // 大幅短縮
            }

            var doubleDodge = GetActionData<MovementActionData>(ActionType.DoubleDodge);
            if ( doubleDodge?.data != null )
            {
                doubleDodge.energyCost *= 0.7f;
            }

            var airJump = GetActionData<MovementActionData>(ActionType.AirJump);
            if ( airJump?.data != null )
            {
                airJump.energyCost *= 0.7f;
            }

            // エネルギー総量を増加
            this.maxEnergy *= 1.2f;

            Debug.Log("機動特化プリセットを適用しました");
        }

        [HorizontalGroup("プリセット")]
        [Button("空中特化")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetAerialPreset()
        {
            if ( actionDatabase == null )
                InitializeActionDatabase();

            // 空中攻撃の性能向上
            var aerialAttack = GetActionData<AttackActionData>(ActionType.AerialAttack);
            if ( aerialAttack?.data != null )
            {
                aerialAttack.data.damage *= 1.3f;
                aerialAttack.data.lungeDistance *= 1.2f;
                aerialAttack.data.floatTime *= 1.5f;
            }

            // 空中移動の性能向上
            var airJump = GetActionData<MovementActionData>(ActionType.AirJump);
            if ( airJump?.data != null )
            {
                airJump.energyCost *= 0.7f;
                airJump.data.distance *= 1.2f;
            }

            var jump = GetActionData<MovementActionData>(ActionType.Jump);
            if ( jump?.data != null )
            {
                jump.data.distance *= 1.2f;
                jump.data.chargeMultiplier *= 1.2f;
            }

            // 地上での回避性能を微調整
            var dodge = GetActionData(ActionType.Dodge);
            if ( dodge != null )
            {
                dodge.cooldownTime *= 0.9f;
            }

            Debug.Log("空中特化プリセットを適用しました");
        }

        [HorizontalGroup("プリセット")]
        [Button("スキル特化")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetSkillPreset()
        {
            if ( actionDatabase == null )
                InitializeActionDatabase();

            // 全スキルのエネルギー効率向上
            for ( int i = 1; i <= 5; i++ )
            {
                var skillType = (ActionType)System.Enum.Parse(typeof(ActionType), $"Skill{i}");
                var skill = GetActionData<SkillActionData>(skillType);
                if ( skill?.data != null )
                {
                    skill.energyCost *= 0.8f;
                    skill.cooldownTime *= 0.8f;
                    skill.data.effectValue *= 1.2f;
                    skill.maxConsecutiveUses += 1;
                }
            }

            // エネルギー回復速度向上
            this.normalEnergyRecoveryRate *= 1.3f;
            this.fastEnergyRecoveryRate *= 1.2f;

            Debug.Log("スキル特化プリセットを適用しました");
        }

        [HorizontalGroup("プリセット")]
        [Button("バランス")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetBalancedPreset()
        {
            // 基本値にリセット
            InitializeDefaultValues();
            InitializeActionDatabase();
            Debug.Log("バランスプリセットを適用しました");
        }

        [Title("回避インターバル調整ツール")]
        [HorizontalGroup("回避調整")]
        [Button("素早い回避")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFastDodge()
        {
            if ( actionDatabase == null )
                InitializeActionDatabase();

            var dodge = GetActionData(ActionType.Dodge);
            if ( dodge != null )
            {
                dodge.cooldownTime = 0.2f;
            }

            var doubleDodge = GetActionData(ActionType.DoubleDodge);
            if ( doubleDodge != null )
            {
                doubleDodge.cooldownTime = 0.6f;
            }

            Debug.Log("素早い回避設定を適用しました");
        }

        [HorizontalGroup("回避調整")]
        [Button("標準的な回避")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetNormalDodge()
        {
            if ( actionDatabase == null )
                InitializeActionDatabase();

            var dodge = GetActionData(ActionType.Dodge);
            if ( dodge != null )
            {
                dodge.cooldownTime = 0.3f;
            }

            var doubleDodge = GetActionData(ActionType.DoubleDodge);
            if ( doubleDodge != null )
            {
                doubleDodge.cooldownTime = 1f;
            }

            Debug.Log("標準的な回避設定を適用しました");
        }

        [HorizontalGroup("回避調整")]
        [Button("慎重な回避")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetCautiousDodge()
        {
            if ( actionDatabase == null )
                InitializeActionDatabase();

            var dodge = GetActionData(ActionType.Dodge);
            if ( dodge != null )
            {
                dodge.cooldownTime = 0.5f;
            }

            var doubleDodge = GetActionData(ActionType.DoubleDodge);
            if ( doubleDodge != null )
            {
                doubleDodge.cooldownTime = 1.5f;
            }

            Debug.Log("慎重な回避設定を適用しました");
        }

        #endregion

        #region === 初期化処理 ===

        /// <summary>
        /// デフォルト値で初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeDefaultValues()
        {
            // 基本パラメータのリセット
            maxHealth = 500f;
            maxEnergy = 100f;
            normalEnergyRecoveryRate = 25f;
            fastEnergyRecoveryRate = 50f;
            maxStunGauge = 100f;
            stunGaugeRecoveryRate = 25f;

            // 行動データの初期化
            InitializeMovementActions();
            InitializeAttackActions();
            InitializeShootActions();
            InitializeDefenseActions();
            InitializeSkillActions();
            InitializeManeuverActions();
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
                    continuousEnergyCost = 0f
                }
            };

            // ブースト
            boostAction = new ActionData<MovementActionData>
            {
                energyCost = 0f,
                maxConsecutiveUses = 999,
                cooldownTime = 0f,
                data = new MovementActionData
                {
                    speed = 20f,
                    continuousEnergyCost = 25f
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
                    distance = 8f,
                    canCharge = true,
                    maxChargeTime = 1.5f,
                    chargeMultiplier = 1.5f
                }
            };

            // 空中ジャンプ
            airJumpAction = new ActionData<MovementActionData>
            {
                energyCost = 10f,
                maxConsecutiveUses = 1,
                cooldownTime = 0f,
                data = new MovementActionData
                {
                    distance = 6f,
                    canCharge = true,
                    maxChargeTime = 1f,
                    chargeMultiplier = 1.3f
                }
            };

            // 回避
            dodgeAction = new ActionData<MovementActionData>
            {
                energyCost = 15f,
                maxConsecutiveUses = 1,
                cooldownTime = 0.3f,
                data = new MovementActionData
                {
                    distance = 5f,
                    speed = 15f,
                    invincibilityFrame = new float2(0.05f, 0.2f),
                    postActionDuration = 0.5f // ガード不可時間
                }
            };

            // 二段回避
            doubleDodgeAction = new ActionData<MovementActionData>
            {
                energyCost = 30f,
                maxConsecutiveUses = 1,
                cooldownTime = 1f,
                data = new MovementActionData
                {
                    distance = 8f,
                    speed = 20f,
                    invincibilityFrame = new float2(0.05f, 0.3f),
                    postActionDuration = 0.5f
                }
            };

            // クイックターン
            quickTurnAction = new ActionData<MovementActionData>
            {
                energyCost = 5f,
                maxConsecutiveUses = 3,
                cooldownTime = 2f,
                data = new MovementActionData
                {
                    speed = 0f // 瞬間移動
                }
            };
        }

        private void InitializeAttackActions()
        {
            // 弱攻撃
            weakMeleeAction = new ActionData<AttackActionData>
            {
                energyCost = 5f,
                maxConsecutiveUses = 5,
                cooldownTime = 2f,
                data = new AttackActionData
                {
                    damage = 25f,
                    startup = 0.2f,
                    duration = 0.1f,
                    recovery = 0.3f,
                    lungeDistance = 2f,
                    range = 3f,
                    canCancel = false,
                    canBeGuarded = true,
                    guardImpact = 0.2f,
                    comboWindow = 0.8f
                }
            };

            // 強攻撃
            strongMeleeAction = new ActionData<AttackActionData>
            {
                energyCost = 20f,
                maxConsecutiveUses = 3,
                cooldownTime = 5f,
                data = new AttackActionData
                {
                    damage = 60f,
                    startup = 0.5f,
                    duration = 0.2f,
                    recovery = 0.6f,
                    lungeDistance = 3f,
                    range = 4f,
                    canCancel = true,
                    cancelEnergyCost = 10f,
                    canBeGuarded = true,
                    guardImpact = 0.8f,
                    comboWindow = 0.8f
                }
            };

            // 空中攻撃
            aerialAttackAction = new ActionData<AttackActionData>
            {
                energyCost = 15f,
                maxConsecutiveUses = 3,
                cooldownTime = 3f,
                data = new AttackActionData
                {
                    damage = 50f,
                    startup = 0.3f,
                    duration = 0.15f,
                    recovery = 0.4f,
                    lungeDistance = 4f,
                    range = 3.5f,
                    canCancel = false,
                    canBeGuarded = true,
                    guardImpact = 0.6f,
                    floatTime = 0.8f
                }
            };

            // 回避攻撃
            dodgeAttackAction = new ActionData<AttackActionData>
            {
                energyCost = 20f,
                maxConsecutiveUses = 2,
                cooldownTime = 4f,
                data = new AttackActionData
                {
                    damage = 40f,
                    startup = 0.15f,
                    duration = 0.12f,
                    recovery = 0.35f,
                    lungeDistance = 5f,
                    range = 3f,
                    canCancel = false,
                    canBeGuarded = true,
                    guardImpact = 0.5f
                }
            };
        }

        private void InitializeShootActions()
        {
            // 弱射撃
            weakShootAction = new ActionData<ShootActionData>
            {
                energyCost = 0f,
                maxConsecutiveUses = 999,
                cooldownTime = 0f,
                data = new ShootActionData
                {
                    damage = 15f,
                    projectileSpeed = 50f,
                    range = 50f,
                    fireRate = 5f,
                    ammoCount = 30,
                    reloadTime = 2f,
                    accuracyTime = 1.5f,
                    pierceGuardAtMaxAccuracy = true,
                    simultaneousShots = 1,
                    spreadAngle = 0f
                }
            };

            // 強射撃
            strongShootAction = new ActionData<ShootActionData>
            {
                energyCost = 0f,
                maxConsecutiveUses = 999,
                cooldownTime = 0f,
                data = new ShootActionData
                {
                    damage = 80f,
                    projectileSpeed = 30f,
                    range = 60f,
                    fireRate = 1f,
                    ammoCount = 5,
                    reloadTime = 3f,
                    accuracyTime = 2f,
                    pierceGuardAtMaxAccuracy = true,
                    simultaneousShots = 1,
                    spreadAngle = 0f
                }
            };

            // チャージ射撃
            chargedShootAction = new ActionData<ShootActionData>
            {
                energyCost = 25f,
                maxConsecutiveUses = 3,
                cooldownTime = 8f,
                data = new ShootActionData
                {
                    damage = 120f,
                    projectileSpeed = 70f,
                    range = 80f,
                    fireRate = 0.5f,
                    ammoCount = 1,
                    reloadTime = 0f,
                    accuracyTime = 0.5f,
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

        private void InitializeSkillActions()
        {
            // スキル1-5の初期化（デフォルト値）
            for ( int i = 1; i <= 5; i++ )
            {
                var skill = new ActionData<SkillActionData>
                {
                    energyCost = 25f,
                    maxConsecutiveUses = 2,
                    cooldownTime = 10f,
                    data = new SkillActionData
                    {
                        effectType = SkillEffectType.Damage,
                        effectValue = 50f,
                        effectRange = 5f,
                        effectDuration = 0f,
                        homingStrength = 0f,
                        unblockable = false,
                        effectName = $"DefaultSkill{i}",
                        activationDelay = 0.5f,
                        hitCount = 1,
                        hitInterval = 0.2f
                    }
                };

                switch ( i )
                {
                    case 1:
                        skill1Action = skill;
                        break;
                    case 2:
                        skill2Action = skill;
                        break;
                    case 3:
                        skill3Action = skill;
                        break;
                    case 4:
                        skill4Action = skill;
                        break;
                    case 5:
                        skill5Action = skill;
                        break;
                }
            }
        }

        private void InitializeManeuverActions()
        {
            // マニューバ1-5の初期化（デフォルト値）
            for ( int i = 1; i <= 5; i++ )
            {
                var maneuver = new ActionData<ManeuverActionData>
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
                        autoSkillAfterExecution = ActionType.None,
                        earlyUseEnergyMultiplier = 2f
                    }
                };

                switch ( i )
                {
                    case 1:
                        maneuver1Action = maneuver;
                        break;
                    case 2:
                        maneuver2Action = maneuver;
                        break;
                    case 3:
                        maneuver3Action = maneuver;
                        break;
                    case 4:
                        maneuver4Action = maneuver;
                        break;
                    case 5:
                        maneuver5Action = maneuver;
                        break;
                }
            }
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
    }
}