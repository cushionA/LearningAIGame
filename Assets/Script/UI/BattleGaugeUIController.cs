//==============================================ファイルヘッダ=======================================================================
// BattleGaugeUIController
// 
// 概要: キャラクターの頭上に追従するHPバーとエネルギーバーを制御するクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// - キャラクターの頭上にWorldSpaceキャンバスを配置し追従
// - HPバーとエネルギーバーをfillAmountで制御
// - LitMotionによるゲージ変動アニメーション
// - エネルギー枯渇時の色変更（SerializeFieldで設定可能）
// 
// 購読対象:
// - DamageSystemBase.Observable: 被弾時のHP変化検知
// - StateSystem.CurrentState: 状態変化時のエネルギー確認
// - 毎フレーム: エネルギー回復の監視
// 
// 入力元クラス: StateSystem, DamageSystemBase
// 出力先: UI Image (fillAmount)
//=====================================================================================================================

using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Systems;
using LitMotion;
using NaughtyAttributes;
using R3;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace LearningAIGame.CombatSystem.UI
{
    /// <summary>
    /// キャラクター頭上追従型のHPバー・エネルギーバーUIコントローラー
    /// </summary>
    public class BattleGaugeUIController : MonoBehaviour
    {
        #region フィールド

        [Header("参照設定")]
        [ReadOnly]
        [Tooltip("状態管理システムへの参照")]
        private StateSystem _stateSystem;

        [SerializeField, Required]
        [Tooltip("被弾管理システムへの参照")]
        private DamageSystemBase _damageSystem;

        [SerializeField, Required]
        [Tooltip("追従対象のTransform")]
        private Transform _followTarget;

        [SerializeField]
        [Tooltip("キャラクター設定（最大HP/エネルギー取得用、未設定の場合はStateSystemから推定）")]
        private CharacterData _characterData;

        [Header("UI参照")]
        [SerializeField, Required]
        [Tooltip("HPバーのfill用Image")]
        private Image _hpFillImage;

        [SerializeField, Required]
        [Tooltip("エネルギーバーのfill用Image")]
        private Image _energyFillImage;

        [Header("追従設定")]
        [SerializeField]
        [Tooltip("追従対象からのオフセット（頭上の高さ）")]
        private Vector3 _followOffset = new Vector3(0f, 2.5f, 0f);

        [SerializeField]
        [Tooltip("UIがカメラを向くかどうか")]
        private bool _lookAtCamera = true;

        [Header("アニメーション設定")]
        [SerializeField, MinValue(0.01f)]
        [Tooltip("HPバー変動アニメーション時間")]
        private float _hpAnimationDuration = 0.3f;

        [SerializeField, MinValue(0.01f)]
        [Tooltip("エネルギーバー変動アニメーション時間")]
        private float _energyAnimationDuration = 0.15f;

        [SerializeField]
        [Tooltip("アニメーションのイージング")]
        private Ease _animationEase = Ease.OutQuad;

        [Header("色設定")]
        [SerializeField]
        [Tooltip("HPバーの通常色")]
        private Color _hpNormalColor = new Color(0.2f, 0.8f, 0.2f, 1f);

        [SerializeField]
        [Tooltip("HPバーの低HP時色")]
        private Color _hpLowColor = new Color(0.9f, 0.3f, 0.1f, 1f);

        [SerializeField, Range(0f, 1f)]
        [Tooltip("低HP判定の閾値（割合）")]
        private float _lowHpThreshold = 0.3f;

        [SerializeField]
        [Tooltip("エネルギーバーの通常色")]
        private Color _energyNormalColor = new Color(0.2f, 0.6f, 1f, 1f);

        [SerializeField]
        [Tooltip("エネルギー枯渇時の色")]
        private Color _energyExhaustColor = new Color(0.9f, 0.2f, 0.2f, 1f);

        [Header("デバッグ表示")]
        [ReadOnly]
        [Tooltip("現在のHP値")]
        private int _currentHp;

        [ReadOnly]
        [Tooltip("現在のエネルギー値")]
        private int _currentEnergy;

        [ReadOnly]
        [Tooltip("エネルギー枯渇状態")]
        private bool _isEnergyExhaust;

        /// <summary>
        /// キャッシュされたHP値（変化検知用）
        /// </summary>
        private int _cachedHp;

        /// <summary>
        /// キャッシュされたエネルギー値（変化検知用）
        /// </summary>
        private int _cachedEnergy;

        /// <summary>
        /// キャッシュされた枯渇状態（変化検知用）
        /// </summary>
        private bool _cachedIsEnergyExhaust;

        /// <summary>
        /// 最大HP（初期化時に取得）
        /// </summary>
        private int _maxHp;

        /// <summary>
        /// 最大エネルギー（初期化時に取得）
        /// </summary>
        private float _maxEnergy;

        /// <summary>
        /// メインカメラのキャッシュ
        /// </summary>
        private Camera _mainCamera;

        /// <summary>
        /// HPアニメーションのハンドル
        /// </summary>
        private MotionHandle _hpMotionHandle;

        /// <summary>
        /// エネルギーアニメーションのハンドル
        /// </summary>
        private MotionHandle _energyMotionHandle;

        #endregion

        #region ライフサイクル

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Awake()
        {
            ValidateReferences();
            _mainCamera = Camera.main;
        }

        /// <summary>
        /// 開始時に購読を設定
        /// </summary>
        public void Initialize(StateSystem stateSystem)
        {
            _stateSystem = stateSystem;
            CacheInitialValues();
            SubscribeSystems();
            InitializeGauges();
        }

        /// <summary>
        /// 毎フレームの更新処理
        /// </summary>
        private void Update()
        {
            UpdateFollowPosition();
            UpdateLookAtCamera();
            UpdateEnergyGauge();
        }

        /// <summary>
        /// 破棄時にアニメーションをキャンセル
        /// </summary>
        private void OnDestroy()
        {
            CancelAnimations();
        }

        #endregion

        #region 初期化

        /// <summary>
        /// 参照の検証
        /// </summary>
        private void ValidateReferences()
        {
            if (_stateSystem == null)
            {
                Debug.LogError($"[{nameof(BattleGaugeUIController)}] StateSystemが設定されていません！");
            }

            if (_damageSystem == null)
            {
                Debug.LogError($"[{nameof(BattleGaugeUIController)}] DamageSystemBaseが設定されていません！");
            }

            if (_followTarget == null)
            {
                Debug.LogError($"[{nameof(BattleGaugeUIController)}] 追従対象が設定されていません！");
            }

            if (_hpFillImage == null)
            {
                Debug.LogError($"[{nameof(BattleGaugeUIController)}] HPバーImageが設定されていません！");
            }

            if (_energyFillImage == null)
            {
                Debug.LogError($"[{nameof(BattleGaugeUIController)}] エネルギーバーImageが設定されていません！");
            }
        }

        /// <summary>
        /// 初期値をキャッシュ
        /// </summary>
        private void CacheInitialValues()
        {
            if (_stateSystem == null)
                return;

            // StateSystemから初期値を推定（初期化直後は最大値と想定）
            _maxHp = _stateSystem.Hp > 0 ? _stateSystem.Hp : 100;
            _maxEnergy = _stateSystem.Energy > 0 ? _stateSystem.Energy : 100;

            Debug.LogWarning($"[{nameof(BattleGaugeUIController)}] CharacterSettingsが未設定のため、StateSystemの現在値を最大値として使用します。");

            // 現在値をキャッシュ
            _cachedHp = _stateSystem.Hp;
            _cachedEnergy = _stateSystem.Energy;
            _cachedIsEnergyExhaust = false;

            Debug.Log($"[{nameof(BattleGaugeUIController)}] 初期化完了 - MaxHP: {_maxHp}, MaxEnergy: {_maxEnergy}");
        }

        /// <summary>
        /// 各システムの購読を設定
        /// </summary>
        private void SubscribeSystems()
        {
            // ダメージシステムの購読（被弾時）
            if (_damageSystem != null)
            {
                _damageSystem.Observable.Subscribe(OnDamageReceived).AddTo(this);
            }

            // 状態変化の購読
            if (_stateSystem != null)
            {
                _stateSystem.CurrentState.Subscribe(OnStateChanged).AddTo(this);
            }
        }

        /// <summary>
        /// ゲージの初期表示
        /// </summary>
        private void InitializeGauges()
        {
            if (_hpFillImage != null)
            {
                _hpFillImage.fillAmount = 1f;
                _hpFillImage.color = _hpNormalColor;
            }

            if (_energyFillImage != null)
            {
                _energyFillImage.fillAmount = 1f;
                _energyFillImage.color = _energyNormalColor;
            }
        }

        #endregion

        #region 購読コールバック

        /// <summary>
        /// ダメージ受信時のコールバック
        /// </summary>
        /// <param name="damageReport">ダメージ報告情報</param>
        private void OnDamageReceived(DamageReportInfo damageReport)
        {
            // HPの更新チェック
            UpdateHpGauge();
            // エネルギーも同時に確認（ダメージでエネルギーが変化する可能性）
            CheckAndUpdateEnergy();
        }

        /// <summary>
        /// 状態変化時のコールバック
        /// </summary>
        /// <param name="newState">新しい状態</param>
        private void OnStateChanged(StateSystem.ActionState newState)
        {
            // 状態変化時にHP・エネルギーをチェック
            UpdateHpGauge();
            CheckAndUpdateEnergy();
        }

        #endregion

        #region ゲージ更新

        /// <summary>
        /// HPゲージの更新
        /// </summary>
        private void UpdateHpGauge()
        {
            if (_stateSystem == null || _hpFillImage == null)
                return;

            int currentHp = _stateSystem.Hp;
            _currentHp = currentHp;

            // HP値に変化がなければスキップ
            if (currentHp == _cachedHp)
                return;

            _cachedHp = currentHp;

            // HP割合を計算
            float hpRatio = _maxHp > 0 ? (float)currentHp / _maxHp : 0f;
            hpRatio = Mathf.Clamp01(hpRatio);

            // アニメーションでゲージを更新
            AnimateHpGauge(hpRatio);

            // 低HP時の色変更
            UpdateHpColor(hpRatio);

            Debug.Log($"[{nameof(BattleGaugeUIController)}] HP更新: {currentHp}/{_maxHp} ({hpRatio:P0})");
        }

        /// <summary>
        /// エネルギーゲージの更新（毎フレーム呼び出し）
        /// </summary>
        private void UpdateEnergyGauge()
        {
            if (_stateSystem == null || _energyFillImage == null)
                return;

            // 現在のエネルギー値と枯渇状態を取得
            int currentEnergy = _stateSystem.Energy;
            float energyRatio = _stateSystem.EnergyRatio;

            _currentEnergy = currentEnergy;

            // エネルギー値に変化があれば更新
            if (currentEnergy != _cachedEnergy)
            {
                _cachedEnergy = currentEnergy;
                AnimateEnergyGauge(Mathf.Clamp01(energyRatio));
            }

            // 枯渇状態の変化をチェック
            CheckExhaustStateChange();
        }

        /// <summary>
        /// エネルギーの値変化をチェックして更新
        /// </summary>
        private void CheckAndUpdateEnergy()
        {
            if (_stateSystem == null)
                return;

            int currentEnergy = _stateSystem.Energy;
            float energyRatio = _stateSystem.EnergyRatio;

            _currentEnergy = currentEnergy;

            if (currentEnergy != _cachedEnergy)
            {
                _cachedEnergy = currentEnergy;
                AnimateEnergyGauge(Mathf.Clamp01(energyRatio));
            }

            CheckExhaustStateChange();
        }

        /// <summary>
        /// エネルギー枯渇状態の変化をチェック
        /// StateSystemのEnergyプロパティは枯渇中は0を返す設計を利用
        /// </summary>
        private void CheckExhaustStateChange()
        {
            if (_stateSystem == null)
                return;

            // StateSystemのEnergyは枯渇中は0を返す
            // EnergyRatioも同様に枯渇中は0を返す
            // 実際のエネルギーが回復中でも枯渇状態は解除されない設計
            bool isExhaust = _stateSystem.Energy <= 0;

            _isEnergyExhaust = isExhaust;

            if (isExhaust != _cachedIsEnergyExhaust)
            {
                _cachedIsEnergyExhaust = isExhaust;
                UpdateEnergyColor(isExhaust);

                Debug.Log($"[{nameof(BattleGaugeUIController)}] エネルギー枯渇状態変化: {(isExhaust ? "枯渇中" : "通常")}");
            }
        }

        #endregion

        #region アニメーション

        /// <summary>
        /// HPゲージのアニメーション
        /// </summary>
        /// <param name="targetRatio">目標の割合</param>
        private void AnimateHpGauge(float targetRatio)
        {
            // 既存のアニメーションをキャンセル
            if (_hpMotionHandle.IsActive())
            {
                _hpMotionHandle.Cancel();
            }

            float currentFill = _hpFillImage.fillAmount;

            _hpMotionHandle = LMotion.Create(currentFill, targetRatio, _hpAnimationDuration)
                .WithEase(_animationEase)
                .Bind(value => _hpFillImage.fillAmount = value);
        }

        /// <summary>
        /// エネルギーゲージのアニメーション
        /// </summary>
        /// <param name="targetRatio">目標の割合</param>
        private void AnimateEnergyGauge(float targetRatio)
        {
            // 既存のアニメーションをキャンセル
            if (_energyMotionHandle.IsActive())
            {
                _energyMotionHandle.Cancel();
            }

            float currentFill = _energyFillImage.fillAmount;

            _energyMotionHandle = LMotion.Create(currentFill, targetRatio, _energyAnimationDuration)
                .WithEase(_animationEase)
                .Bind(value => _energyFillImage.fillAmount = value);
        }

        /// <summary>
        /// すべてのアニメーションをキャンセル
        /// </summary>
        private void CancelAnimations()
        {
            if (_hpMotionHandle.IsActive())
            {
                _hpMotionHandle.Cancel();
            }

            if (_energyMotionHandle.IsActive())
            {
                _energyMotionHandle.Cancel();
            }
        }

        #endregion

        #region 色更新

        /// <summary>
        /// HP状態に応じた色更新
        /// </summary>
        /// <param name="hpRatio">HP割合</param>
        private void UpdateHpColor(float hpRatio)
        {
            if (_hpFillImage == null)
                return;

            // 低HP閾値以下なら警告色に遷移
            _hpFillImage.color = hpRatio <= _lowHpThreshold
                ? Color.Lerp(_hpLowColor, _hpNormalColor, hpRatio / _lowHpThreshold)
                : _hpNormalColor;
        }

        /// <summary>
        /// エネルギー枯渇状態に応じた色更新
        /// </summary>
        /// <param name="isExhaust">枯渇状態かどうか</param>
        private void UpdateEnergyColor(bool isExhaust)
        {
            if (_energyFillImage == null)
                return;

            _energyFillImage.color = isExhaust ? _energyExhaustColor : _energyNormalColor;
        }

        #endregion

        #region 追従処理

        /// <summary>
        /// 追従位置の更新
        /// </summary>
        private void UpdateFollowPosition()
        {
            if (_followTarget == null)
                return;

            transform.position = _followTarget.position + _followOffset;
        }

        /// <summary>
        /// カメラを向く処理
        /// </summary>
        private void UpdateLookAtCamera()
        {
            if (!_lookAtCamera || _mainCamera == null)
                return;

            // カメラの方向を向く（ビルボード処理）
            transform.rotation = _mainCamera.transform.rotation;
        }

        #endregion

        #region Public API

        /// <summary>
        /// 最大HPを設定（キャラクター初期化時などに呼び出す）
        /// </summary>
        /// <param name="maxHp">最大HP</param>
        public void SetMaxHp(int maxHp)
        {
            _maxHp = maxHp;
            Debug.Log($"[{nameof(BattleGaugeUIController)}] 最大HPを設定: {maxHp}");
        }

        /// <summary>
        /// 最大エネルギーを設定（キャラクター初期化時などに呼び出す）
        /// </summary>
        /// <param name="maxEnergy">最大エネルギー</param>
        public void SetMaxEnergy(float maxEnergy)
        {
            _maxEnergy = maxEnergy;
            Debug.Log($"[{nameof(BattleGaugeUIController)}] 最大エネルギーを設定: {maxEnergy}");
        }

        /// <summary>
        /// ゲージを即座に更新（アニメーションなし）
        /// </summary>
        public void ForceUpdateGauges()
        {
            if (_stateSystem == null)
                return;

            // アニメーションをキャンセル
            CancelAnimations();

            // HP
            float hpRatio = _maxHp > 0 ? (float)_stateSystem.Hp / _maxHp : 0f;
            _hpFillImage.fillAmount = Mathf.Clamp01(hpRatio);
            UpdateHpColor(hpRatio);
            _cachedHp = _stateSystem.Hp;

            // エネルギー
            _energyFillImage.fillAmount = Mathf.Clamp01(_stateSystem.EnergyRatio);
            _cachedEnergy = _stateSystem.Energy;

            // 枯渇状態
            bool isExhaust = _stateSystem.EnergyRatio <= 0f;
            UpdateEnergyColor(isExhaust);
            _cachedIsEnergyExhaust = isExhaust;
        }

        #endregion

        #region エディタ専用

#if UNITY_EDITOR
        /// <summary>
        /// コンテキストメニューからコンポーネントを設定
        /// </summary>
        [ContextMenu("Setup Components From Parent")]
        private void SetupComponentsFromParent()
        {
            var parent = transform.parent;
            if (parent == null)
            {
                Debug.LogWarning($"[{nameof(BattleGaugeUIController)}] 親オブジェクトが見つかりません");
                return;
            }

            _stateSystem = parent.GetComponentInChildren<StateSystem>();
            _damageSystem = parent.GetComponentInChildren<DamageSystemBase>();
            _followTarget = parent;

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[{nameof(BattleGaugeUIController)}] コンポーネントを親から設定しました");
        }

        [Button("ゲージ強制更新")]
        private void DebugForceUpdate()
        {
            ForceUpdateGauges();
        }

        [Button("枯渇状態テスト")]
        private void DebugTestExhaust()
        {
            UpdateEnergyColor(true);
        }

        [Button("通常状態テスト")]
        private void DebugTestNormal()
        {
            UpdateEnergyColor(false);
        }
#endif

        #endregion
    }
}