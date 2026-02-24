using NaughtyAttributes;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// ActionSettingOverride
// 
// 概要: ActionSettingの値を部分的に上書きするScriptableObject
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// 参照元のActionSettingの値をベースに、選択した値だけを上書きする。
// キャラクターごとに一部のパラメータだけ変更したい場合に使用。
// 上書きしたい値は継承元のフィールドに設定し、対応するオーバーライドフラグをONにする。
// 
// 入力元クラス:なし(ScriptableObject)
// 出力先クラス:BattleCharacterController, StateSystem
// 
// その他:
// ActionSettingを継承
// NaughtyAttributes使用(インスペクター表示強化)
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Setting
{
    /// <summary>
    /// ActionSettingの値を部分的に上書きするScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "ActionSettingOverride", menuName = "CombatSystem/ActionSettingOverride")]
    public class ActionSettingOverride : ActionSetting
    {
        #region 参照元設定

        [BoxGroup("参照元")]
        [Label("ベース設定")]
        [Tooltip("上書き対象のベース設定。チェックされていない値はこの設定の値を使用")]
        [SerializeField] private ActionSetting _baseSettings;

        #endregion

        #region 弱攻撃オーバーライドフラグ

        [BoxGroup("弱攻撃オーバーライド")]
        [Label("ダメージ値を上書き")]
        [SerializeField] private bool _overrideWeakAttackDamage;

        [BoxGroup("弱攻撃オーバーライド")]
        [Label("消費エネルギーを上書き")]
        [SerializeField] private bool _overrideWeakAttackEnergyCost;

        [BoxGroup("弱攻撃オーバーライド")]
        [Label("踏み込み速度を上書き")]
        [SerializeField] private bool _overrideWeakAttackStepSpeed;

        [BoxGroup("弱攻撃オーバーライド")]
        [Label("踏み込み継続時間を上書き")]
        [SerializeField] private bool _overrideWeakAttackStepDuration;

        [BoxGroup("弱攻撃オーバーライド")]
        [Label("実行後硬直時間を上書き")]
        [SerializeField] private bool _overrideWeakAttackStun;

        [BoxGroup("弱攻撃オーバーライド")]
        [Label("攻撃判定発生フレームを上書き")]
        [SerializeField] private bool _overrideWeakAttackStartFrame;

        [BoxGroup("弱攻撃オーバーライド")]
        [Label("攻撃判定持続フレームを上書き")]
        [SerializeField] private bool _overrideWeakAttackDurationFrame;

        #endregion

        #region 強攻撃オーバーライドフラグ

        [BoxGroup("強攻撃オーバーライド")]
        [Label("ダメージ値を上書き")]
        [SerializeField] private bool _overrideHeavyAttackDamage;

        [BoxGroup("強攻撃オーバーライド")]
        [Label("消費エネルギーを上書き")]
        [SerializeField] private bool _overrideHeavyAttackEnergyCost;

        [BoxGroup("強攻撃オーバーライド")]
        [Label("踏み込み速度を上書き")]
        [SerializeField] private bool _overrideHeavyAttackStepSpeed;

        [BoxGroup("強攻撃オーバーライド")]
        [Label("踏み込み継続時間を上書き")]
        [SerializeField] private bool _overrideHeavyAttackStepDuration;

        [BoxGroup("強攻撃オーバーライド")]
        [Label("実行後硬直時間を上書き")]
        [SerializeField] private bool _overrideHeavyAttackStun;

        [BoxGroup("強攻撃オーバーライド")]
        [Label("キャンセル時の消費エネルギーを上書き")]
        [SerializeField] private bool _overrideHeavyAttackCancelEnergyCost;

        [BoxGroup("強攻撃オーバーライド")]
        [Label("キャンセル時の硬直時間を上書き")]
        [SerializeField] private bool _overrideHeavyAttackCancelStun;

        [BoxGroup("強攻撃オーバーライド")]
        [Label("攻撃判定発生フレームを上書き")]
        [SerializeField] private bool _overrideHeavyAttackStartFrame;

        [BoxGroup("強攻撃オーバーライド")]
        [Label("攻撃判定持続フレームを上書き")]
        [SerializeField] private bool _overrideHeavyAttackDurationFrame;

        #endregion

        #region ブロッキングオーバーライドフラグ

        [BoxGroup("ブロッキングオーバーライド")]
        [Label("消費エネルギーを上書き")]
        [SerializeField] private bool _overrideBlockingEnergyCost;

        [BoxGroup("ブロッキングオーバーライド")]
        [Label("判定発生遅延を上書き")]
        [SerializeField] private bool _overrideBlockingStartDelay;

        [BoxGroup("ブロッキングオーバーライド")]
        [Label("判定継続時間を上書き")]
        [SerializeField] private bool _overrideBlockingDuration;

        [BoxGroup("ブロッキングオーバーライド")]
        [Label("実行後硬直時間を上書き")]
        [SerializeField] private bool _overrideBlockingStun;

        [BoxGroup("ブロッキングオーバーライド")]
        [Label("成功時の硬直時間を上書き")]
        [SerializeField] private bool _overrideBlockingSuccessStun;

        #endregion

        #region 回避オーバーライドフラグ

        [BoxGroup("回避オーバーライド")]
        [Label("消費エネルギーを上書き")]
        [SerializeField] private bool _overrideAvoidEnergyCost;

        [BoxGroup("回避オーバーライド")]
        [Label("移動速度を上書き")]
        [SerializeField] private bool _overrideAvoidSpeed;

        [BoxGroup("回避オーバーライド")]
        [Label("移動継続時間を上書き")]
        [SerializeField] private bool _overrideAvoidDuration;

        [BoxGroup("回避オーバーライド")]
        [Label("後ろ回避移動速度を上書き")]
        [SerializeField] private bool _overrideBackAvoidSpeed;

        [BoxGroup("回避オーバーライド")]
        [Label("後ろ回避継続時間を上書き")]
        [SerializeField] private bool _overrideBackAvoidDuration;

        [BoxGroup("回避オーバーライド")]
        [Label("実行後硬直時間を上書き")]
        [SerializeField] private bool _overrideAvoidStun;

        [BoxGroup("回避オーバーライド")]
        [Label("前回避硬直時間を上書き")]
        [SerializeField] private bool _overrideFrontAvoidStun;

        [BoxGroup("回避オーバーライド")]
        [Label("後ろ回避硬直時間を上書き")]
        [SerializeField] private bool _overrideBackAvoidStun;

        [BoxGroup("回避オーバーライド")]
        [Label("後ろ回避消費倍率を上書き")]
        [SerializeField] private bool _overrideBackAvoidUsageMultiplier;

        [BoxGroup("回避オーバーライド")]
        [Label("無敵判定発生遅延を上書き")]
        [SerializeField] private bool _overrideAvoidInvincibleStartDelay;

        [BoxGroup("回避オーバーライド")]
        [Label("無敵判定継続時間を上書き")]
        [SerializeField] private bool _overrideAvoidInvincibleDuration;

        [BoxGroup("回避オーバーライド")]
        [Label("前回避攻撃硬直時間を上書き")]
        [SerializeField] private bool _overrideForwardAvoidAttackStun;

        [BoxGroup("回避オーバーライド")]
        [Label("横回避攻撃硬直時間を上書き")]
        [SerializeField] private bool _overrideSideAvoidAttackStun;

        #endregion

        #region 移動オーバーライドフラグ

        [BoxGroup("移動オーバーライド")]
        [Label("通常移動速度を上書き")]
        [SerializeField] private bool _overrideMoveSpeed;

        [BoxGroup("移動オーバーライド")]
        [Label("ガード時硬直時間を上書き")]
        [SerializeField] private bool _overrideGuardStun;

        [BoxGroup("移動オーバーライド")]
        [Label("ガード成功時硬直時間を上書き")]
        [SerializeField] private bool _overrideGuardSuccessStun;

        [BoxGroup("移動オーバーライド")]
        [Label("攻撃可能距離を上書き")]
        [SerializeField] private bool _overrideAttackableDistance;

        [BoxGroup("移動オーバーライド")]
        [Label("後方移動減速倍率を上書き")]
        [SerializeField] private bool _overrideBackMoveMultiplier;

        #endregion

        #region エネルギー回復オーバーライドフラグ

        [BoxGroup("エネルギー回復オーバーライド")]
        [Label("毎秒自然回復量を上書き")]
        [SerializeField] private bool _overrideEnergyRecoveryRatePerSecond;

        [BoxGroup("エネルギー回復オーバーライド")]
        [Label("緊急時回復倍率を上書き")]
        [SerializeField] private bool _overrideEnergyRecoveryEmergencyMultiply;

        [BoxGroup("エネルギー回復オーバーライド")]
        [Label("ブロッキング成功時回復量を上書き")]
        [SerializeField] private bool _overrideBlockingSuccessEnergyRecovery;

        #endregion

        #region プロパティオーバーライド（弱攻撃）

        public override int WeakAttackDamage =>
            _overrideWeakAttackDamage ? _weakAttackDamage : _baseSettings.WeakAttackDamage;

        public override int WeakAttackEnergyCost =>
            _overrideWeakAttackEnergyCost ? _weakAttackEnergyCost : _baseSettings.WeakAttackEnergyCost;

        public override float WeakAttackStepSpeed =>
            _overrideWeakAttackStepSpeed ? _weakAttackStepSpeed : _baseSettings.WeakAttackStepSpeed;

        public override float WeakAttackStepDuration =>
            _overrideWeakAttackStepDuration ? _weakAttackStepDuration : _baseSettings.WeakAttackStepDuration;

        public override float WeakAttackStun =>
            _overrideWeakAttackStun ? _weakAttackStun : _baseSettings.WeakAttackStun;

        public override int WeakAttackStartFrame =>
            _overrideWeakAttackStartFrame ? _weakAttackStartFrame : _baseSettings.WeakAttackStartFrame;

        public override int WeakAttackDurationFrame =>
            _overrideWeakAttackDurationFrame ? _weakAttackDurationFrame : _baseSettings.WeakAttackDurationFrame;

        #endregion

        #region プロパティオーバーライド（強攻撃）

        public override int HeavyAttackDamage =>
            _overrideHeavyAttackDamage ? _heavyAttackDamage : _baseSettings.HeavyAttackDamage;

        public override int HeavyAttackEnergyCost =>
            _overrideHeavyAttackEnergyCost ? _heavyAttackEnergyCost : _baseSettings.HeavyAttackEnergyCost;

        public override float HeavyAttackStepSpeed =>
            _overrideHeavyAttackStepSpeed ? _heavyAttackStepSpeed : _baseSettings.HeavyAttackStepSpeed;

        public override float HeavyAttackStepDuration =>
            _overrideHeavyAttackStepDuration ? _heavyAttackStepDuration : _baseSettings.HeavyAttackStepDuration;

        public override float HeavyAttackStun =>
            _overrideHeavyAttackStun ? _heavyAttackStun : _baseSettings.HeavyAttackStun;

        public override int HeavyAttackCancelEnergyCost =>
            _overrideHeavyAttackCancelEnergyCost ? _heavyAttackCancelEnergyCost : _baseSettings.HeavyAttackCancelEnergyCost;

        public override float HeavyAttackCancelStun =>
            _overrideHeavyAttackCancelStun ? _heavyAttackCancelStun : _baseSettings.HeavyAttackCancelStun;

        public override int HeavyAttackStartFrame =>
            _overrideHeavyAttackStartFrame ? _heavyAttackStartFrame : _baseSettings.HeavyAttackStartFrame;

        public override int HeavyAttackDurationFrame =>
            _overrideHeavyAttackDurationFrame ? _heavyAttackDurationFrame : _baseSettings.HeavyAttackDurationFrame;

        public override int HeavyCancelInputFrame => HeavyAttackStartFrame;

        #endregion

        #region プロパティオーバーライド（ブロッキング）

        public override int BlockingEnergyCost =>
            _overrideBlockingEnergyCost ? _blockingEnergyCost : _baseSettings.BlockingEnergyCost;

        public override float BlockingStartDelay =>
            _overrideBlockingStartDelay ? _blockingStartDelay : _baseSettings.BlockingStartDelay;

        public override float BlockingDuration =>
            _overrideBlockingDuration ? _blockingDuration : _baseSettings.BlockingDuration;

        public override float BlockingStun =>
            _overrideBlockingStun ? _blockingStun : _baseSettings.BlockingStun;

        public override float BlockingSuccessStun =>
            _overrideBlockingSuccessStun ? _blockingSuccessStun : _baseSettings.BlockingSuccessStun;

        #endregion

        #region プロパティオーバーライド（回避）

        public override int AvoidEnergyCost =>
            _overrideAvoidEnergyCost ? _avoidEnergyCost : _baseSettings.AvoidEnergyCost;

        public override float BackAvoidUsageMultiplier =>
            _overrideBackAvoidUsageMultiplier ? _backAvoidUsageMultiplier : _baseSettings.BackAvoidUsageMultiplier;

        public override int BackAvoidEnergyCost
        {
            get
            {
                int baseCost = AvoidEnergyCost;
                float multiplier = BackAvoidUsageMultiplier;
                return (int)(baseCost * multiplier);
            }
        }

        public override float AvoidSpeed =>
            _overrideAvoidSpeed ? _avoidSpeed : _baseSettings.AvoidSpeed;

        public override float AvoidDuration =>
            _overrideAvoidDuration ? _avoidDuration : _baseSettings.AvoidDuration;

        public override float BackAvoidSpeed =>
            _overrideBackAvoidSpeed ? _backAvoidSpeed : _baseSettings.BackAvoidSpeed;

        public override float BackAvoidDuration =>
            _overrideBackAvoidDuration ? _backAvoidDuration : _baseSettings.BackAvoidDuration;

        public override float AvoidInvincibleStartDelay =>
            _overrideAvoidInvincibleStartDelay ? _avoidInvincibleStartDelay : _baseSettings.AvoidInvincibleStartDelay;

        public override float AvoidInvincibleDuration =>
            _overrideAvoidInvincibleDuration ? _avoidInvincibleDuration : _baseSettings.AvoidInvincibleDuration;

        public override float AvoidAttackInputDuration => AvoidDuration * 0.85f;

        public override float AvoidStun =>
            _overrideAvoidStun ? _avoidStun : _baseSettings.AvoidStun;

        public override float FrontAvoidStun =>
            _overrideFrontAvoidStun ? _frontAvoidStun : _baseSettings.FrontAvoidStun;

        public override float BackAvoidStun =>
            _overrideBackAvoidStun ? _backAvoidStun : _baseSettings.BackAvoidStun;

        public override float ForwardAvoidAttackStun =>
            _overrideForwardAvoidAttackStun ? _forwardAvoidAttackStun : _baseSettings.ForwardAvoidAttackStun;

        public override float SideAvoidAttackStun =>
            _overrideSideAvoidAttackStun ? _sideAvoidAttackStun : _baseSettings.SideAvoidAttackStun;

        #endregion

        #region プロパティオーバーライド（移動）

        public override float MoveSpeed =>
            _overrideMoveSpeed ? _moveSpeed : _baseSettings.MoveSpeed;

        public override float GuardStun =>
            _overrideGuardStun ? _guardStun : _baseSettings.GuardStun;

        public override float GuardSuccessStun =>
            _overrideGuardSuccessStun ? _guardSuccessStun : _baseSettings.GuardSuccessStun;

        public override float AttackableDistance =>
            _overrideAttackableDistance ? _attackableDistance : _baseSettings.AttackableDistance;

        public override float BackMoveMultiplier =>
            _overrideBackMoveMultiplier ? _backMoveMultiplier : _baseSettings.BackMoveMultiplier;

        public override float AttackableDistancePow => Mathf.Pow(AttackableDistance, 2);

        #endregion

        #region プロパティオーバーライド（エネルギー回復）

        public override float EnergyRecoveryRatePerSecond =>
            _overrideEnergyRecoveryRatePerSecond ? _energyRecoveryRatePerSecond : _baseSettings.EnergyRecoveryRatePerSecond;

        public override float EnergyRecoveryEmergencyMultiply =>
            _overrideEnergyRecoveryEmergencyMultiply ? _energyRecoveryEmergencyMultiply : _baseSettings.EnergyRecoveryEmergencyMultiply;

        public override float BlockingSuccessEnergyRecovery =>
            _overrideBlockingSuccessEnergyRecovery ? _blockingSuccessEnergyRecovery : _baseSettings.BlockingSuccessEnergyRecovery;

        #endregion

        #region バリデーション

        [Button("オーバーライド設定を検証", EButtonEnableMode.Editor)]
        private void ValidateOverrideSettings()
        {
            if (_baseSettings == null)
            {
                Debug.LogError($"[{name}] ベース設定が設定されていません！");
                return;
            }

            int overrideCount = 0;

            // 弱攻撃
            if (_overrideWeakAttackDamage)
                overrideCount++;
            if (_overrideWeakAttackEnergyCost)
                overrideCount++;
            if (_overrideWeakAttackStepSpeed)
                overrideCount++;
            if (_overrideWeakAttackStepDuration)
                overrideCount++;
            if (_overrideWeakAttackStun)
                overrideCount++;
            if (_overrideWeakAttackStartFrame)
                overrideCount++;
            if (_overrideWeakAttackDurationFrame)
                overrideCount++;

            // 強攻撃
            if (_overrideHeavyAttackDamage)
                overrideCount++;
            if (_overrideHeavyAttackEnergyCost)
                overrideCount++;
            if (_overrideHeavyAttackStepSpeed)
                overrideCount++;
            if (_overrideHeavyAttackStepDuration)
                overrideCount++;
            if (_overrideHeavyAttackStun)
                overrideCount++;
            if (_overrideHeavyAttackCancelEnergyCost)
                overrideCount++;
            if (_overrideHeavyAttackCancelStun)
                overrideCount++;
            if (_overrideHeavyAttackStartFrame)
                overrideCount++;
            if (_overrideHeavyAttackDurationFrame)
                overrideCount++;

            // ブロッキング
            if (_overrideBlockingEnergyCost)
                overrideCount++;
            if (_overrideBlockingStartDelay)
                overrideCount++;
            if (_overrideBlockingDuration)
                overrideCount++;
            if (_overrideBlockingStun)
                overrideCount++;
            if (_overrideBlockingSuccessStun)
                overrideCount++;

            // 回避
            if (_overrideAvoidEnergyCost)
                overrideCount++;
            if (_overrideAvoidSpeed)
                overrideCount++;
            if (_overrideAvoidDuration)
                overrideCount++;
            if (_overrideBackAvoidSpeed)
                overrideCount++;
            if (_overrideBackAvoidDuration)
                overrideCount++;
            if (_overrideAvoidStun)
                overrideCount++;
            if (_overrideFrontAvoidStun)
                overrideCount++;
            if (_overrideBackAvoidStun)
                overrideCount++;
            if (_overrideBackAvoidUsageMultiplier)
                overrideCount++;
            if (_overrideAvoidInvincibleStartDelay)
                overrideCount++;
            if (_overrideAvoidInvincibleDuration)
                overrideCount++;
            if (_overrideForwardAvoidAttackStun)
                overrideCount++;
            if (_overrideSideAvoidAttackStun)
                overrideCount++;

            // 移動
            if (_overrideMoveSpeed)
                overrideCount++;
            if (_overrideGuardStun)
                overrideCount++;
            if (_overrideGuardSuccessStun)
                overrideCount++;
            if (_overrideAttackableDistance)
                overrideCount++;
            if (_overrideBackMoveMultiplier)
                overrideCount++;

            // エネルギー回復
            if (_overrideEnergyRecoveryRatePerSecond)
                overrideCount++;
            if (_overrideEnergyRecoveryEmergencyMultiply)
                overrideCount++;
            if (_overrideBlockingSuccessEnergyRecovery)
                overrideCount++;

            Debug.Log($"[{name}] オーバーライド設定: {overrideCount}個の値を上書きしています（ベース: {_baseSettings.name}）");

            // 親クラスの検証も実行
            base.ValidateSettings();
        }

        [Button("全てのオーバーライドをリセット", EButtonEnableMode.Editor)]
        private void ResetAllOverrides()
        {
            // 弱攻撃
            _overrideWeakAttackDamage = false;
            _overrideWeakAttackEnergyCost = false;
            _overrideWeakAttackStepSpeed = false;
            _overrideWeakAttackStepDuration = false;
            _overrideWeakAttackStun = false;
            _overrideWeakAttackStartFrame = false;
            _overrideWeakAttackDurationFrame = false;

            // 強攻撃
            _overrideHeavyAttackDamage = false;
            _overrideHeavyAttackEnergyCost = false;
            _overrideHeavyAttackStepSpeed = false;
            _overrideHeavyAttackStepDuration = false;
            _overrideHeavyAttackStun = false;
            _overrideHeavyAttackCancelEnergyCost = false;
            _overrideHeavyAttackCancelStun = false;
            _overrideHeavyAttackStartFrame = false;
            _overrideHeavyAttackDurationFrame = false;

            // ブロッキング
            _overrideBlockingEnergyCost = false;
            _overrideBlockingStartDelay = false;
            _overrideBlockingDuration = false;
            _overrideBlockingStun = false;
            _overrideBlockingSuccessStun = false;

            // 回避
            _overrideAvoidEnergyCost = false;
            _overrideAvoidSpeed = false;
            _overrideAvoidDuration = false;
            _overrideBackAvoidSpeed = false;
            _overrideBackAvoidDuration = false;
            _overrideAvoidStun = false;
            _overrideFrontAvoidStun = false;
            _overrideBackAvoidStun = false;
            _overrideBackAvoidUsageMultiplier = false;
            _overrideAvoidInvincibleStartDelay = false;
            _overrideAvoidInvincibleDuration = false;
            _overrideForwardAvoidAttackStun = false;
            _overrideSideAvoidAttackStun = false;

            // 移動
            _overrideMoveSpeed = false;
            _overrideGuardStun = false;
            _overrideGuardSuccessStun = false;
            _overrideAttackableDistance = false;
            _overrideBackMoveMultiplier = false;

            // エネルギー回復
            _overrideEnergyRecoveryRatePerSecond = false;
            _overrideEnergyRecoveryEmergencyMultiply = false;
            _overrideBlockingSuccessEnergyRecovery = false;

            Debug.Log($"[{name}] 全てのオーバーライドをリセットしました");
        }

        #endregion
    }
}