using NaughtyAttributes;
using UnityEngine;

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

        // === プロパティ（読み取り専用） ===

        /// <summary>弱攻撃のダメージ値</summary>
        public int WeakAttackDamage => _weakAttackDamage;
        /// <summary>弱攻撃の消費エネルギー</summary>
        public int WeakAttackEnergyCost => _weakAttackEnergyCost;
        /// <summary>弱攻撃時の踏み込み速度</summary>
        public float WeakAttackStepSpeed => _weakAttackStepSpeed;
        /// <summary>弱攻撃時の踏み込み継続時間</summary>
        public float WeakAttackStepDuration => _weakAttackStepDuration;

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
        [Label("キャンセル時の消費エネルギー")]
        [Tooltip("強攻撃をキャンセルする際に追加で消費するエネルギー量")]
        [MinValue(0)]
        [SerializeField] private int _heavyAttackCancelEnergyCost = 10;

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

        // === プロパティ（読み取り専用） ===

        /// <summary>ブロッキングの消費エネルギー</summary>
        public int BlockingEnergyCost => _blockingEnergyCost;
        /// <summary>ブロッキング判定の発生遅延時間</summary>
        public float BlockingStartDelay => _blockingStartDelay;
        /// <summary>ブロッキング判定の継続時間</summary>
        public float BlockingDuration => _blockingDuration;

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

        // === プロパティ（読み取り専用） ===

        /// <summary>回避の消費エネルギー</summary>
        public int AvoidEnergyCost => _avoidEnergyCost;
        /// <summary>回避時の移動速度</summary>
        public float AvoidSpeed => _avoidSpeed;
        /// <summary>回避の継続時間</summary>
        public float AvoidDuration => _avoidDuration;
        /// <summary>回避の無敵判定発生遅延時間</summary>
        public float AvoidInvincibleStartDelay => _avoidInvincibleStartDelay;
        /// <summary>回避の無敵判定継続時間</summary>
        public float AvoidInvincibleDuration => _avoidInvincibleDuration;
        /// <summary>回避攻撃の入力猶予時間</summary>
        public float AvoidAttackInputDuration => _avoidInvincibleDuration * 0.5f;

        #endregion

        #region 移動設定

        [BoxGroup("移動")]
        [Label("通常移動速度")]
        [Tooltip("ガード状態での通常移動速度")]
        [MinValue(0f)]
        [SerializeField] private float _moveSpeed = 4f;

        // === プロパティ（読み取り専用） ===

        /// <summary>通常移動速度</summary>
        public float MoveSpeed => _moveSpeed;

        #endregion

        #region エネルギー回復設定

        [BoxGroup("エネルギー回復")]
        [Label("毎秒自然回復量（%）")]
        [Tooltip("1秒あたりに自然回復するエネルギーの割合（%）\n例：3なら1秒で3%回復")]
        [MinValue(0f)]
        [SerializeField] private float _energyRecoveryRatePerSecond = 3f;

        [BoxGroup("エネルギー回復")]
        [Label("ブロッキング成功時回復量（%）")]
        [Tooltip("ブロッキングに成功した際に即座に回復するエネルギーの割合（%）")]
        [MinValue(0f)]
        [SerializeField] private float _blockingSuccessEnergyRecovery = 20f;

        // === プロパティ（読み取り専用） ===

        /// <summary>毎秒のエネルギー自然回復量（%）</summary>
        public float EnergyRecoveryRatePerSecond => _energyRecoveryRatePerSecond;
        /// <summary>ブロッキング成功時のエネルギー回復量（%）</summary>
        public float BlockingSuccessEnergyRecovery => _blockingSuccessEnergyRecovery;

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

            if (!hasWarnings)
            {
                Debug.Log($"[{name}] 設定値の検証が完了しました。問題は見つかりませんでした。");
            }
        }

        #endregion
    }
}