using NaughtyAttributes;
using System.Diagnostics.Contracts;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// ActionSetting
// 
// 概要: 戦闘システムの全アクションパラメータを管理するScriptableObject
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// 攻撃(弱/強)、防御(ブロッキング)、回避、移動の各アクションに関する
// ダメージ値、消費エネルギー、判定タイミング、移動速度などのパラメータを一元管理する。
// 
// 入力元クラス:なし(ScriptableObject)
// 出力先クラス:BattleCharacterController, StateSystem
// 
// その他:
// NaughtyAttributes使用(インスペクター表示強化)
// プロパティはvirtualで定義し、ActionSettingOverrideでオーバーライド可能
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Setting
{
    /// <summary>
    /// 各アクションの設定データを管理するScriptableObject
    /// 全てのアクションパラメータ（ダメージ、エネルギーコスト、判定タイミングなど）を一元管理
    /// </summary>
    [CreateAssetMenu(fileName = "ActionSetting", menuName = "CombatSystem/ActionSetting")]
    public class ActionSetting : ScriptableObject
    {
        #region 弱攻撃設定

        [BoxGroup("弱攻撃")]
        [Label("ダメージ値")]
        [Tooltip("弱攻撃が命中した際のダメージ量")]
        [MinValue(1)]
        [SerializeField] protected int _weakAttackDamage = 10;

        [BoxGroup("弱攻撃")]
        [Label("消費エネルギー")]
        [Tooltip("弱攻撃を実行する際に消費するエネルギー量")]
        [MinValue(0)]
        [SerializeField] protected int _weakAttackEnergyCost = 5;

        [BoxGroup("弱攻撃")]
        [Label("踏み込み速度")]
        [Tooltip("弱攻撃時に敵に向かって踏み込む際の移動速度")]
        [MinValue(0f)]
        [SerializeField] protected float _weakAttackStepSpeed = 3f;

        [BoxGroup("弱攻撃")]
        [Label("踏み込み継続時間")]
        [Tooltip("弱攻撃の踏み込みが継続する時間（秒）")]
        [MinValue(0f)]
        [SerializeField] protected float _weakAttackStepDuration = 0.2f;

        [BoxGroup("弱攻撃")]
        [Label("実行後硬直時間")]
        [Tooltip("弱攻撃後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] protected float _weakAttackStun = 0.2f;

        [BoxGroup("弱攻撃")]
        [Label("攻撃判定発生フレーム")]
        [Tooltip("弱攻撃時の判定発生フレーム")]
        [MinValue(0f)]
        [SerializeField] protected int _weakAttackStartFrame = 2;

        [BoxGroup("弱攻撃")]
        [Label("攻撃判定持続フレーム")]
        [Tooltip("弱攻撃時の判定持続フレーム")]
        [MinValue(1f)]
        [SerializeField] protected int _weakAttackDurationFrame = 5;

        // === プロパティ（読み取り専用・オーバーライド可能） ===

        /// <summary>弱攻撃のダメージ値</summary>
        public virtual int WeakAttackDamage => _weakAttackDamage;
        /// <summary>弱攻撃の消費エネルギー</summary>
        public virtual int WeakAttackEnergyCost => _weakAttackEnergyCost;
        /// <summary>弱攻撃時の踏み込み速度</summary>
        public virtual float WeakAttackStepSpeed => _weakAttackStepSpeed;
        /// <summary>弱攻撃時の踏み込み継続時間</summary>
        public virtual float WeakAttackStepDuration => _weakAttackStepDuration;
        /// <summary>弱攻撃時の硬直時間</summary>
        public virtual float WeakAttackStun => _weakAttackStun;
        /// <summary>弱攻撃時の判定開始フレーム</summary>
        public virtual int WeakAttackStartFrame => _weakAttackStartFrame;
        /// <summary>弱攻撃時の判定継続フレーム</summary>
        public virtual int WeakAttackDurationFrame => _weakAttackDurationFrame;

        #endregion

        #region 強攻撃設定

        [BoxGroup("強攻撃")]
        [Label("ダメージ値")]
        [Tooltip("強攻撃が命中した際のダメージ量")]
        [MinValue(1)]
        [SerializeField] protected int _heavyAttackDamage = 30;

        [BoxGroup("強攻撃")]
        [Label("消費エネルギー")]
        [Tooltip("強攻撃を実行する際に消費するエネルギー量")]
        [MinValue(0)]
        [SerializeField] protected int _heavyAttackEnergyCost = 15;

        [BoxGroup("強攻撃")]
        [Label("踏み込み速度")]
        [Tooltip("強攻撃時に敵に向かって踏み込む際の移動速度")]
        [MinValue(0f)]
        [SerializeField] protected float _heavyAttackStepSpeed = 5f;

        [BoxGroup("強攻撃")]
        [Label("踏み込み継続時間")]
        [Tooltip("強攻撃の踏み込みが継続する時間（秒）")]
        [MinValue(0f)]
        [SerializeField] protected float _heavyAttackStepDuration = 0.3f;

        [BoxGroup("強攻撃")]
        [Label("実行後硬直時間")]
        [Tooltip("強攻撃後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] protected float _heavyAttackStun = 0.5f;

        [BoxGroup("強攻撃")]
        [Label("キャンセル時の消費エネルギー")]
        [Tooltip("強攻撃をキャンセルする際に追加で消費するエネルギー量")]
        [MinValue(0)]
        [SerializeField] protected int _heavyAttackCancelEnergyCost = 10;

        [BoxGroup("強攻撃")]
        [Label("キャンセル時の硬直時間")]
        [Tooltip("強攻撃をキャンセルした際の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] protected float _heavyAttackCancelStun = 0.3f;

        [BoxGroup("強攻撃")]
        [Label("攻撃判定発生フレーム")]
        [Tooltip("強攻撃時の判定発生フレーム")]
        [MinValue(0f)]
        [SerializeField] protected int _heavyAttackStartFrame = 2;

        [BoxGroup("強攻撃")]
        [Label("攻撃判定持続フレーム")]
        [Tooltip("強攻撃時の判定持続フレーム")]
        [MinValue(1f)]
        [SerializeField] protected int _heavyAttackDurationFrame = 5;

        // === プロパティ（読み取り専用・オーバーライド可能） ===

        /// <summary>強攻撃のダメージ値</summary>
        public virtual int HeavyAttackDamage => _heavyAttackDamage;
        /// <summary>強攻撃の消費エネルギー</summary>
        public virtual int HeavyAttackEnergyCost => _heavyAttackEnergyCost;
        /// <summary>強攻撃時の踏み込み速度</summary>
        public virtual float HeavyAttackStepSpeed => _heavyAttackStepSpeed;
        /// <summary>強攻撃時の踏み込み継続時間</summary>
        public virtual float HeavyAttackStepDuration => _heavyAttackStepDuration;
        /// <summary>強攻撃時の硬直時間</summary>
        public virtual float HeavyAttackStun => _heavyAttackStun;
        /// <summary>強攻撃キャンセル時の消費エネルギー</summary>
        public virtual int HeavyAttackCancelEnergyCost => _heavyAttackCancelEnergyCost;
        /// <summary>強攻撃キャンセル時の硬直時間</summary>
        public virtual float HeavyAttackCancelStun => _heavyAttackCancelStun;
        /// <summary>強攻撃時の判定開始フレーム</summary>
        public virtual int HeavyAttackStartFrame => _heavyAttackStartFrame;
        /// <summary>強攻撃時の判定継続フレーム</summary>
        public virtual int HeavyAttackDurationFrame => _heavyAttackDurationFrame;

        /// <summary>強攻撃時のキャンセル可能フレーム</summary>
        public virtual int HeavyCancelInputFrame => _heavyAttackStartFrame;

        #endregion

        #region ブロッキング設定

        [BoxGroup("ブロッキング")]
        [Label("消費エネルギー")]
        [Tooltip("ブロッキングを実行する際に消費するエネルギー量")]
        [MinValue(0)]
        [SerializeField] protected int _blockingEnergyCost = 3;

        [BoxGroup("ブロッキング")]
        [Label("判定発生遅延")]
        [Tooltip("ブロッキング実行から判定が有効になるまでの遅延時間（秒）\n短いほど即座に判定が発生する")]
        [MinValue(0f)]
        [SerializeField] protected float _blockingStartDelay = 0.1f;

        [BoxGroup("ブロッキング")]
        [Label("判定継続時間")]
        [Tooltip("ブロッキング判定が有効な時間（秒）\nこの時間内に攻撃を受けると成功")]
        [MinValue(0f)]
        [SerializeField] protected float _blockingDuration = 0.4f;

        [BoxGroup("ブロッキング")]
        [Label("実行後硬直時間")]
        [Tooltip("ブロッキング後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] protected float _blockingStun = 0.3f;

        [BoxGroup("ブロッキング")]
        [Label("成功時の硬直時間")]
        [Tooltip("ブロッキング成功時の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] protected float _blockingSuccessStun = 0.1f;

        // === プロパティ（読み取り専用・オーバーライド可能） ===

        /// <summary>ブロッキングの消費エネルギー</summary>
        public virtual int BlockingEnergyCost => _blockingEnergyCost;
        /// <summary>ブロッキング判定の発生遅延時間</summary>
        public virtual float BlockingStartDelay => _blockingStartDelay;
        /// <summary>ブロッキング判定の継続時間</summary>
        public virtual float BlockingDuration => _blockingDuration;
        /// <summary>ブロッキングの実行後硬直時間</summary>
        public virtual float BlockingStun => _blockingStun;
        /// <summary>ブロッキング成功時の硬直時間</summary>
        public virtual float BlockingSuccessStun => _blockingSuccessStun;

        #endregion

        #region 回避設定

        [BoxGroup("回避")]
        [Label("消費エネルギー")]
        [Tooltip("回避を実行する際に消費するエネルギー量")]
        [MinValue(0)]
        [SerializeField] protected int _avoidEnergyCost = 8;

        [BoxGroup("回避")]
        [Label("移動速度")]
        [Tooltip("回避中の移動速度")]
        [MinValue(0f)]
        [SerializeField] protected float _avoidSpeed = 8f;

        [BoxGroup("回避")]
        [Label("移動継続時間")]
        [Tooltip("回避による移動が継続する時間（秒）")]
        [MinValue(0f)]
        [SerializeField] protected float _avoidDuration = 0.4f;

        [BoxGroup("回避")]
        [Label("後ろ回避移動速度")]
        [Tooltip("後ろ回避中の移動速度")]
        [MinValue(0f)]
        [SerializeField] protected float _backAvoidSpeed = 8f;

        [BoxGroup("回避")]
        [Label("後ろ移動継続時間")]
        [Tooltip("後ろ回避による移動が継続する時間（秒）")]
        [MinValue(0f)]
        [SerializeField] protected float _backAvoidDuration = 0.4f;

        [BoxGroup("回避")]
        [Label("実行後硬直時間")]
        [Tooltip("回避後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] protected float _avoidStun = 0.2f;

        [BoxGroup("回避")]
        [Label("前回避実行後硬直時間")]
        [Tooltip("前回避後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] protected float _frontAvoidStun = 0.2f;

        [BoxGroup("回避")]
        [Label("後ろ回避実行後硬直時間")]
        [Tooltip("後ろ回避後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] protected float _backAvoidStun = 0.8f;

        [BoxGroup("回避")]
        [Label("後ろ回避の消費スタミナ倍率")]
        [Tooltip("後ろ回避後のスタミナ消費倍率")]
        [MinValue(0f)]
        [SerializeField] protected float _backAvoidUsageMultiplier = 0.8f;

        [BoxGroup("回避")]
        [Label("無敵判定発生遅延")]
        [Tooltip("回避実行から無敵判定が有効になるまでの遅延時間（秒）")]
        [MinValue(0f)]
        [InfoBox("無敵判定の発生が遅いと回避開始直後に攻撃を受ける可能性があります", EInfoBoxType.Normal)]
        [SerializeField] protected float _avoidInvincibleStartDelay = 0.05f;

        [BoxGroup("回避")]
        [Label("無敵判定継続時間")]
        [Tooltip("無敵判定が有効な時間（秒）\nこの時間内は攻撃を受けない")]
        [MinValue(0f)]
        [SerializeField] protected float _avoidInvincibleDuration = 0.3f;

        [BoxGroup("回避攻撃")]
        [Label("前回避攻撃の硬直時間")]
        [Tooltip("前回避攻撃後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] protected float _forwardAvoidAttackStun = 0.25f;

        [BoxGroup("回避攻撃")]
        [Label("横回避攻撃の硬直時間")]
        [Tooltip("横回避攻撃後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] protected float _sideAvoidAttackStun = 0.25f;

        // === プロパティ（読み取り専用・オーバーライド可能） ===

        /// <summary>回避の消費エネルギー</summary>
        public virtual int AvoidEnergyCost => _avoidEnergyCost;
        /// <summary>後ろ回避の消費エネルギー</summary>
        public virtual int BackAvoidEnergyCost => (int)(_avoidEnergyCost * _backAvoidUsageMultiplier);
        /// <summary>回避時の移動速度</summary>
        public virtual float AvoidSpeed => _avoidSpeed;
        /// <summary>回避の継続時間</summary>
        public virtual float AvoidDuration => _avoidDuration;
        /// <summary>後ろ回避時の移動速度</summary>
        public virtual float BackAvoidSpeed => _backAvoidSpeed;
        /// <summary>後ろ回避の継続時間</summary>
        public virtual float BackAvoidDuration => _backAvoidDuration;
        /// <summary>回避の無敵判定発生遅延時間</summary>
        public virtual float AvoidInvincibleStartDelay => _avoidInvincibleStartDelay;
        /// <summary>回避の無敵判定継続時間</summary>
        public virtual float AvoidInvincibleDuration => _avoidInvincibleDuration;
        /// <summary>回避攻撃の入力猶予時間</summary>
        public virtual float AvoidAttackInputDuration => _avoidDuration * 0.85f;
        /// <summary>横回避時の硬直時間</summary>
        public virtual float AvoidStun => _avoidStun;
        /// <summary>前回避時の硬直時間</summary>
        public virtual float FrontAvoidStun => _frontAvoidStun;
        /// <summary>後ろ回避時の硬直時間</summary>
        public virtual float BackAvoidStun => _backAvoidStun;
        /// <summary>後ろ回避の消費スタミナ倍率</summary>
        public virtual float BackAvoidUsageMultiplier => _backAvoidUsageMultiplier;
        /// <summary>前回避攻撃時の硬直時間</summary>
        public virtual float ForwardAvoidAttackStun => _forwardAvoidAttackStun;
        /// <summary>横回避攻撃時の硬直時間</summary>
        public virtual float SideAvoidAttackStun => _sideAvoidAttackStun;

        #endregion

        #region 移動設定

        [BoxGroup("移動")]
        [Label("通常移動速度")]
        [Tooltip("ガード状態での通常移動速度")]
        [MinValue(0f)]
        [SerializeField] protected float _moveSpeed = 4f;

        [BoxGroup("移動")]
        [Label("ガード時の硬直時間")]
        [Tooltip("ガード状態時の硬直時間(秒) - 通常は0")]
        [MinValue(0f)]
        [SerializeField] protected float _guardStun = 0f;

        [BoxGroup("移動")]
        [Label("ガード成功時の硬直時間")]
        [Tooltip("ガードで攻撃を受けた際の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] protected float _guardSuccessStun = 0.15f;

        [BoxGroup("移動")]
        [Label("攻撃可能距離")]
        [Tooltip("攻撃可能な距離")]
        [MinValue(0f)]
        [SerializeField] protected float _attackableDistance = 4f;

        [BoxGroup("移動")]
        [Label("後方移動時の減速倍率")]
        [Tooltip("後ろさがりの速度倍率")]
        [MinValue(0f)]
        [SerializeField] protected float _backMoveMultiplier = 0.5f;

        // === プロパティ（読み取り専用・オーバーライド可能） ===

        /// <summary>通常移動速度</summary>
        public virtual float MoveSpeed => _moveSpeed;
        /// <summary>ガード時の硬直時間</summary>
        public virtual float GuardStun => _guardStun;
        /// <summary>ガード成功時の硬直時間</summary>
        public virtual float GuardSuccessStun => _guardSuccessStun;
        /// <summary>攻撃可能距離</summary>
        public virtual float AttackableDistance => _attackableDistance;
        /// <summary>後ろ下がり時の移動速度倍率</summary>
        public virtual float BackMoveMultiplier => _backMoveMultiplier;

        /// <summary>攻撃可能な距離の二乗（計算用）</summary>
        [Pure]
        public virtual float AttackableDistancePow => Mathf.Pow(_attackableDistance, 2);

        #endregion

        #region エネルギー回復設定

        [BoxGroup("エネルギー回復")]
        [Label("毎秒自然回復量（%）")]
        [Tooltip("1秒あたりに自然回復するエネルギーの割合（%）\n例：3なら1秒で3%回復")]
        [MinValue(0f)]
        [SerializeField] protected float _energyRecoveryRatePerSecond = 3f;

        [BoxGroup("エネルギー回復")]
        [Label("緊急時の自然回復倍率")]
        [Tooltip("緊急時のエネルギー回復倍率")]
        [MinValue(0f)]
        [SerializeField] protected float _energyRecoveryEmergencyMultiply = 4f;

        [BoxGroup("エネルギー回復")]
        [Label("ブロッキング成功時回復量（%）")]
        [Tooltip("ブロッキングに成功した際に即座に回復するエネルギーの割合（%）")]
        [MinValue(0f)]
        [SerializeField] protected float _blockingSuccessEnergyRecovery = 20f;

        // === プロパティ（読み取り専用・オーバーライド可能） ===

        /// <summary>毎秒のエネルギー自然回復量（%）</summary>
        public virtual float EnergyRecoveryRatePerSecond => _energyRecoveryRatePerSecond;
        /// <summary>緊急時のエネルギー自然回復倍率</summary>
        public virtual float EnergyRecoveryEmergencyMultiply => _energyRecoveryEmergencyMultiply;
        /// <summary>ブロッキング成功時のエネルギー回復量（%）</summary>
        public virtual float BlockingSuccessEnergyRecovery => _blockingSuccessEnergyRecovery;

        #endregion

        #region インデクサ（硬直時間の取得）

        /// <summary>
        /// ActionStateから対応する硬直時間を取得するインデクサ
        /// </summary>
        /// <param name="state">アクション状態</param>
        /// <returns>対応する硬直時間（秒）。該当しない場合は0</returns>
        public virtual float this[ActionState state]
        {
            get
            {
                switch (state)
                {
                    case ActionState.ガード:
                        return GuardStun;
                    case ActionState.ブロッキング:
                        return BlockingStun;
                    case ActionState.前回避:
                        return FrontAvoidStun;
                    case ActionState.横回避:
                        return AvoidStun;
                    case ActionState.後ろ回避:
                        return BackAvoidStun;
                    case ActionState.前回避攻撃:
                        return ForwardAvoidAttackStun;
                    case ActionState.横回避攻撃:
                        return SideAvoidAttackStun;
                    case ActionState.弱攻撃:
                        return WeakAttackStun;
                    case ActionState.強攻撃:
                        return HeavyAttackStun;
                    case ActionState.ブロッキング成功:
                        return BlockingSuccessStun;
                    case ActionState.ガード成功:
                        return GuardSuccessStun;
                    case ActionState.強攻撃キャンセル:
                        return HeavyAttackCancelStun;
                    default:
                        Debug.LogWarning($"[{name}] 未定義のActionState: {state}");
                        return 0f;
                }
            }
        }

        #endregion

        #region バリデーション

        /// <summary>
        /// 設定値の妥当性を検証
        /// </summary>
        [Button("設定値を検証", EButtonEnableMode.Editor)]
        protected virtual void ValidateSettings()
        {
            bool hasWarnings = false;

            // 回避の無敵時間が移動時間より長い場合の警告
            if (AvoidInvincibleStartDelay + AvoidInvincibleDuration > AvoidDuration)
            {
                Debug.LogWarning($"[{name}] 回避の無敵判定が移動時間を超えています。" +
                    $"無敵時間合計: {AvoidInvincibleStartDelay + AvoidInvincibleDuration}秒 > 移動時間: {AvoidDuration}秒");
                hasWarnings = true;
            }

            // ブロッキング判定時間の警告
            if (BlockingStartDelay + BlockingDuration > 1f)
            {
                Debug.LogWarning($"[{name}] ブロッキングの判定時間が長すぎる可能性があります。" +
                    $"判定時間合計: {BlockingStartDelay + BlockingDuration}秒");
                hasWarnings = true;
            }

            // 強攻撃のダメージが弱攻撃より低い場合の警告
            if (HeavyAttackDamage <= WeakAttackDamage)
            {
                Debug.LogWarning($"[{name}] 強攻撃のダメージ（{HeavyAttackDamage}）が弱攻撃（{WeakAttackDamage}）以下です。");
                hasWarnings = true;
            }

            // エネルギーコストの妥当性チェック
            if (HeavyAttackEnergyCost <= WeakAttackEnergyCost)
            {
                Debug.LogWarning($"[{name}] 強攻撃のコスト（{HeavyAttackEnergyCost}）が弱攻撃（{WeakAttackEnergyCost}）以下です。");
                hasWarnings = true;
            }

            // 硬直時間の妥当性チェック
            if (WeakAttackStun < 0f || HeavyAttackStun < 0f || BlockingStun < 0f || AvoidStun < 0f)
            {
                Debug.LogWarning($"[{name}] 硬直時間に負の値が設定されています。");
                hasWarnings = true;
            }

            if (!hasWarnings)
            {
                Debug.Log($"[{name}] 設定値の検証が完了しました。問題は見つかりませんでした。");
            }
        }

        #endregion
    }
}