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
        [SerializeField] private int _weakAttackDamage = 10;

        [BoxGroup("弱攻撃")]
        [Label("消費エネルギー")]
        [Tooltip("弱攻撃を実行する際に消費するエネルギー量")]
        [MinValue(0)]
        [SerializeField] private int _weakAttackEnergyCost = 5;

        [BoxGroup("弱攻撃")]
        [Label("踏み込み速度")]
        [Tooltip("弱攻撃時に敵に向かって踏み込む際の移動速度")]
        [MinValue(0f)]
        [SerializeField] private float _weakAttackStepSpeed = 3f;

        [BoxGroup("弱攻撃")]
        [Label("踏み込み継続時間")]
        [Tooltip("弱攻撃の踏み込みが継続する時間（秒）")]
        [MinValue(0f)]
        [SerializeField] private float _weakAttackStepDuration = 0.2f;

        [BoxGroup("弱攻撃")]
        [Label("実行後硬直時間")]
        [Tooltip("弱攻撃後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] private float _weakAttackStun = 0.2f;

        [BoxGroup("弱攻撃")]
        [Label("攻撃判定発生フレーム")]
        [Tooltip("弱攻撃時の判定発生フレーム")]
        [MinValue(0f)]
        [SerializeField] private int _weakAttackStartFrame = 2;

        [BoxGroup("弱攻撃")]
        [Label("攻撃判定持続フレーム")]
        [Tooltip("弱攻撃時の判定持続フレーム")]
        [MinValue(1f)]
        [SerializeField] private int _weakAttackDurationFrame = 5;

        // === プロパティ（読み取り専用） ===

        /// <summary>弱攻撃のダメージ値</summary>
        public int WeakAttackDamage => _weakAttackDamage;
        /// <summary>弱攻撃の消費エネルギー</summary>
        public int WeakAttackEnergyCost => _weakAttackEnergyCost;
        /// <summary>弱攻撃時の踏み込み速度</summary>
        public float WeakAttackStepSpeed => _weakAttackStepSpeed;
        /// <summary>弱攻撃時の踏み込み継続時間</summary>
        public float WeakAttackStepDuration => _weakAttackStepDuration;
        /// <summary>弱攻撃時の判定開始時間</summary>
        public int WeakAttackStartFrame => _weakAttackStartFrame;
        /// <summary>弱攻撃時の判定継続時間</summary>
        public int WeakAttackDurationFrame => _weakAttackDurationFrame;

        #endregion

        #region 強攻撃設定

        [BoxGroup("強攻撃")]
        [Label("ダメージ値")]
        [Tooltip("強攻撃が命中した際のダメージ量")]
        [MinValue(1)]
        [SerializeField] private int _heavyAttackDamage = 30;

        [BoxGroup("強攻撃")]
        [Label("消費エネルギー")]
        [Tooltip("強攻撃を実行する際に消費するエネルギー量")]
        [MinValue(0)]
        [SerializeField] private int _heavyAttackEnergyCost = 15;

        [BoxGroup("強攻撃")]
        [Label("踏み込み速度")]
        [Tooltip("強攻撃時に敵に向かって踏み込む際の移動速度")]
        [MinValue(0f)]
        [SerializeField] private float _heavyAttackStepSpeed = 5f;

        [BoxGroup("強攻撃")]
        [Label("踏み込み継続時間")]
        [Tooltip("強攻撃の踏み込みが継続する時間（秒）")]
        [MinValue(0f)]
        [SerializeField] private float _heavyAttackStepDuration = 0.3f;

        [BoxGroup("強攻撃")]
        [Label("実行後硬直時間")]
        [Tooltip("強攻撃後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] private float _heavyAttackStun = 0.5f;

        [BoxGroup("強攻撃")]
        [Label("キャンセル時の消費エネルギー")]
        [Tooltip("強攻撃をキャンセルする際に追加で消費するエネルギー量")]
        [MinValue(0)]
        [SerializeField] private int _heavyAttackCancelEnergyCost = 10;

        [BoxGroup("強攻撃")]
        [Label("キャンセル時の硬直時間")]
        [Tooltip("強攻撃をキャンセルした際の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] private float _heavyAttackCancelStun = 0.3f;

        [BoxGroup("強攻撃")]
        [Label("攻撃判定発生フレーム")]
        [Tooltip("強攻撃時の判定発生フレーム")]
        [MinValue(0f)]
        [SerializeField] private int _heavyAttackStartFrame = 2;

        [BoxGroup("強攻撃")]
        [Label("攻撃判定持続フレーム")]
        [Tooltip("強攻撃時の判定持続フレーム")]
        [MinValue(1f)]
        [SerializeField] private int _heavyAttackDurationFrame = 5;

        // === プロパティ（読み取り専用） ===

        /// <summary>強攻撃のダメージ値</summary>
        public int HeavyAttackDamage => _heavyAttackDamage;
        /// <summary>強攻撃の消費エネルギー</summary>
        public int HeavyAttackEnergyCost => _heavyAttackEnergyCost;
        /// <summary>強攻撃時の踏み込み速度</summary>
        public float HeavyAttackStepSpeed => _heavyAttackStepSpeed;
        /// <summary>強攻撃時の踏み込み継続時間</summary>
        public float HeavyAttackStepDuration => _heavyAttackStepDuration;
        /// <summary>強攻撃キャンセル時の消費エネルギー</summary>
        public int HeavyAttackCancelEnergyCost => _heavyAttackCancelEnergyCost;
        /// <summary>強攻撃時の判定開始時間</summary>
        public int HeavyAttackStartFrame => _heavyAttackStartFrame;
        /// <summary>強攻撃時の判定継続時間</summary>
        public int HeavyAttackDurationFrame => _heavyAttackDurationFrame;

        /// <summary>強攻撃時のキャンセル可能フレーム</summary>
        public int HeavyCancelInputFrame => _heavyAttackStartFrame;

        #endregion


        #region ブロッキング設定

        [BoxGroup("ブロッキング")]
        [Label("消費エネルギー")]
        [Tooltip("ブロッキングを実行する際に消費するエネルギー量")]
        [MinValue(0)]
        [SerializeField] private int _blockingEnergyCost = 3;

        [BoxGroup("ブロッキング")]
        [Label("判定発生遅延")]
        [Tooltip("ブロッキング実行から判定が有効になるまでの遅延時間（秒）\n短いほど即座に判定が発生する")]
        [MinValue(0f)]
        [SerializeField] private float _blockingStartDelay = 0.1f;

        [BoxGroup("ブロッキング")]
        [Label("判定継続時間")]
        [Tooltip("ブロッキング判定が有効な時間（秒）\nこの時間内に攻撃を受けると成功")]
        [MinValue(0f)]
        [SerializeField] private float _blockingDuration = 0.4f;

        [BoxGroup("ブロッキング")]
        [Label("実行後硬直時間")]
        [Tooltip("ブロッキング後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] private float _blockingStun = 0.3f;

        [BoxGroup("ブロッキング")]
        [Label("成功時の硬直時間")]
        [Tooltip("ブロッキング成功時の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] private float _blockingSuccessStun = 0.1f;

        // === プロパティ（読み取り専用） ===

        /// <summary>ブロッキングの消費エネルギー</summary>
        public int BlockingEnergyCost => _blockingEnergyCost;
        /// <summary>ブロッキング判定の発生遅延時間</summary>
        public float BlockingStartDelay => _blockingStartDelay;
        /// <summary>ブロッキング判定の継続時間</summary>
        public float BlockingDuration => _blockingDuration;
        /// <summary>ブロッキングの実行後硬直時間</summary>
        public float BlockingStun => _blockingStun;

        #endregion

        #region 回避設定

        [BoxGroup("回避")]
        [Label("消費エネルギー")]
        [Tooltip("回避を実行する際に消費するエネルギー量")]
        [MinValue(0)]
        [SerializeField] private int _avoidEnergyCost = 8;

        [BoxGroup("回避")]
        [Label("移動速度")]
        [Tooltip("回避中の移動速度")]
        [MinValue(0f)]
        [SerializeField] private float _avoidSpeed = 8f;

        [BoxGroup("回避")]
        [Label("移動継続時間")]
        [Tooltip("回避による移動が継続する時間（秒）")]
        [MinValue(0f)]
        [SerializeField] private float _avoidDuration = 0.4f;

        [BoxGroup("回避")]
        [Label("後ろ回避移動速度")]
        [Tooltip("後ろ回避中の移動速度")]
        [MinValue(0f)]
        [SerializeField] private float _backAvoidSpeed = 8f;

        [BoxGroup("回避")]
        [Label("後ろ移動継続時間")]
        [Tooltip("後ろ回避による移動が継続する時間（秒）")]
        [MinValue(0f)]
        [SerializeField] private float _backAvoidDuration = 0.4f;

        [BoxGroup("回避")]
        [Label("実行後硬直時間")]
        [Tooltip("回避後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] private float _avoidStun = 0.2f;

        [BoxGroup("回避")]
        [Label("前回避実行後硬直時間")]
        [Tooltip("前回避後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] private float _frontAvoidStun = 0.2f;

        [BoxGroup("回避")]
        [Label("後ろ回避実行後硬直時間")]
        [Tooltip("後ろ回避後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] private float _backAvoidStun = 0.8f;

        [BoxGroup("回避")]
        [Label("後ろ回避の消費スタミナ倍率")]
        [Tooltip("後ろ回避後のスタミナ消費倍率")]
        [MinValue(0f)]
        [SerializeField] private float _backAvoidUsageMultiplier = 0.8f;

        [BoxGroup("回避")]
        [Label("無敵判定発生遅延")]
        [Tooltip("回避実行から無敵判定が有効になるまでの遅延時間（秒）")]
        [MinValue(0f)]
        [InfoBox("無敵判定の発生が遅いと回避開始直後に攻撃を受ける可能性があります", EInfoBoxType.Normal)]
        [SerializeField] private float _avoidInvincibleStartDelay = 0.05f;

        [BoxGroup("回避")]
        [Label("無敵判定継続時間")]
        [Tooltip("無敵判定が有効な時間（秒）\nこの時間内は攻撃を受けない")]
        [MinValue(0f)]
        [SerializeField] private float _avoidInvincibleDuration = 0.3f;

        [BoxGroup("回避攻撃")]
        [Label("前回避攻撃の硬直時間")]
        [Tooltip("前回避攻撃後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] private float _forwardAvoidAttackStun = 0.25f;

        [BoxGroup("回避攻撃")]
        [Label("横回避攻撃の硬直時間")]
        [Tooltip("横回避攻撃後の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] private float _sideAvoidAttackStun = 0.25f;

        // === プロパティ（読み取り専用） ===

        /// <summary>回避の消費エネルギー</summary>
        public int AvoidEnergyCost => _avoidEnergyCost;
        /// <summary>後ろ回避の消費エネルギー</summary>
        public int BackAvoidEnergyCost => (int)(_avoidEnergyCost * _backAvoidUsageMultiplier);
        /// <summary>回避時の移動速度</summary>
        public float AvoidSpeed => _avoidSpeed;
        /// <summary>回避の継続時間</summary>
        public float AvoidDuration => _avoidDuration;
        /// <summary>後ろ回避時の移動速度</summary>
        public float BackAvoidSpeed => _backAvoidSpeed;
        /// <summary>後ろ回避の継続時間</summary>
        public float BackAvoidDuration => _backAvoidDuration;
        /// <summary>回避の無敵判定発生遅延時間</summary>
        public float AvoidInvincibleStartDelay => _avoidInvincibleStartDelay;
        /// <summary>回避の無敵判定継続時間</summary>
        public float AvoidInvincibleDuration => _avoidInvincibleDuration;
        /// <summary>回避攻撃の入力猶予時間</summary>
        public float AvoidAttackInputDuration => _avoidDuration * 0.85f;

        /// <summary>a攻撃可能な距離の二乗（計算用）</summary>
        [Pure]
        public float AttackableDistancePow => Mathf.Pow(_attackableDistance, 2);

        #endregion

        #region 移動設定

        [BoxGroup("移動")]
        [Label("通常移動速度")]
        [Tooltip("ガード状態での通常移動速度")]
        [MinValue(0f)]
        [SerializeField] private float _moveSpeed = 4f;

        [BoxGroup("移動")]
        [Label("ガード時の硬直時間")]
        [Tooltip("ガード状態時の硬直時間(秒) - 通常は0")]
        [MinValue(0f)]
        [SerializeField] private float _guardStun = 0f;

        [BoxGroup("移動")]
        [Label("ガード成功時の硬直時間")]
        [Tooltip("ガードで攻撃を受けた際の硬直時間(秒)")]
        [MinValue(0f)]
        [SerializeField] private float _guardSuccessStun = 0.15f;

        [BoxGroup("移動")]
        [Label("攻撃可能距離")]
        [Tooltip("攻撃可能な距離")]
        [MinValue(0f)]
        [SerializeField] private float _attackableDistance = 4f;

        [BoxGroup("移動")]
        [Label("後方移動時の減速倍率")]
        [Tooltip("後ろさがりの速度倍率")]
        [MinValue(0f)]
        [SerializeField] private float _backMoveMultiplier = 0.5f;

        // === プロパティ（読み取り専用） ===

        /// <summary>通常移動速度</summary>
        public float MoveSpeed => _moveSpeed;

        /// <summary>
        /// 後ろ下がり時の移動速度倍率
        /// </summary>
        public float BackMoveMultiplier => _backMoveMultiplier;

        #endregion

        #region エネルギー回復設定

        [BoxGroup("エネルギー回復")]
        [Label("毎秒自然回復量（%）")]
        [Tooltip("1秒あたりに自然回復するエネルギーの割合（%）\n例：3なら1秒で3%回復")]
        [MinValue(0f)]
        [SerializeField] private float _energyRecoveryRatePerSecond = 3f;


        [BoxGroup("エネルギー回復")]
        [Label("緊急時の自然回復倍率")]
        [Tooltip("緊急時のエネルギー回復倍率")]
        [MinValue(0f)]
        [SerializeField] private float _energyRecoveryEmergencyMultiply = 4f;

        [BoxGroup("エネルギー回復")]
        [Label("ブロッキング成功時回復量（%）")]
        [Tooltip("ブロッキングに成功した際に即座に回復するエネルギーの割合（%）")]
        [MinValue(0f)]
        [SerializeField] private float _blockingSuccessEnergyRecovery = 20f;

        // === プロパティ（読み取り専用） ===

        /// <summary>毎秒のエネルギー自然回復量（%）</summary>
        public float EnergyRecoveryRatePerSecond => _energyRecoveryRatePerSecond;
        /// <summary>緊急時のエネルギー自然回復倍率</summary>
        public float EnergyRecoveryEmergencyMultiply => _energyRecoveryEmergencyMultiply;
        /// <summary>ブロッキング成功時のエネルギー回復量（%）</summary>
        public float BlockingSuccessEnergyRecovery => _blockingSuccessEnergyRecovery;

        #endregion

        #region インデクサ（硬直時間の取得）

        /// <summary>
        /// ActionStateから対応する硬直時間を取得するインデクサ
        /// </summary>
        /// <param name="state">アクション状態</param>
        /// <returns>対応する硬直時間（秒）。該当しない場合は0</returns>
        public float this[ActionState state]
        {
            get
            {
                switch (state)
                {
                    case ActionState.ガード:
                        return _guardStun;
                    case ActionState.ブロッキング:
                        return _blockingStun;
                    case ActionState.前回避:
                        return _frontAvoidStun;
                    case ActionState.横回避:
                        return _avoidStun;
                    case ActionState.後ろ回避:
                        return _backAvoidStun;
                    case ActionState.前回避攻撃:
                        return _forwardAvoidAttackStun;
                    case ActionState.横回避攻撃:
                        return _sideAvoidAttackStun;
                    case ActionState.弱攻撃:
                        return _weakAttackStun;
                    case ActionState.強攻撃:
                        return _heavyAttackStun;
                    case ActionState.ブロッキング成功:
                        return _blockingSuccessStun;
                    case ActionState.ガード成功:
                        return _guardSuccessStun;
                    case ActionState.強攻撃キャンセル:
                        return _heavyAttackCancelStun;
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
        private void ValidateSettings()
        {
            bool hasWarnings = false;

            // 回避の無敵時間が移動時間より長い場合の警告
            if (_avoidInvincibleStartDelay + _avoidInvincibleDuration > _avoidDuration)
            {
                Debug.LogWarning($"[{name}] 回避の無敵判定が移動時間を超えています。" +
                    $"無敵時間合計: {_avoidInvincibleStartDelay + _avoidInvincibleDuration}秒 > 移動時間: {_avoidDuration}秒");
                hasWarnings = true;
            }

            // ブロッキング判定時間の警告
            if (_blockingStartDelay + _blockingDuration > 1f)
            {
                Debug.LogWarning($"[{name}] ブロッキングの判定時間が長すぎる可能性があります。" +
                    $"判定時間合計: {_blockingStartDelay + _blockingDuration}秒");
                hasWarnings = true;
            }

            // 強攻撃のダメージが弱攻撃より低い場合の警告
            if (_heavyAttackDamage <= _weakAttackDamage)
            {
                Debug.LogWarning($"[{name}] 強攻撃のダメージ（{_heavyAttackDamage}）が弱攻撃（{_weakAttackDamage}）以下です。");
                hasWarnings = true;
            }

            // エネルギーコストの妥当性チェック
            if (_heavyAttackEnergyCost <= _weakAttackEnergyCost)
            {
                Debug.LogWarning($"[{name}] 強攻撃のコスト（{_heavyAttackEnergyCost}）が弱攻撃（{_weakAttackEnergyCost}）以下です。");
                hasWarnings = true;
            }

            // 硬直時間の妥当性チェック
            if (_weakAttackStun < 0f || _heavyAttackStun < 0f || _blockingStun < 0f || _avoidStun < 0f)
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