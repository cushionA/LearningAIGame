//==============================================ファイルヘッダ=======================================================================
// ScreenSpaceGaugeUIController
// 
// 概要: Screen Space Canvas上の固定位置にHPバーとエネルギーバーを表示するクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// - Screen Space Canvas上でHPバーとエネルギーバーをfillAmountで制御
// - LitMotionによるゲージ変動アニメーション
// - エネルギー枯渇時の色変更（SerializeFieldで設定可能）
// - プレイヤーUIなど、画面固定位置での使用を想定
// 
// 購読対象:
// - DamageSystemBase.Observable: 被弾時のHP変化検知
// - StateSystem.CurrentState: 状態変化時のエネルギー確認
// - 毎フレーム: エネルギー回復の監視
// 
// 入力元クラス: StateSystem, DamageSystemBase
// 出力先: UI Image (fillAmount)
//
// 使用方法:
// 1. Canvas (Screen Space - Overlay) を作成
// 2. HP/Energyバー用のImageを配置
// 3. このスクリプトをアタッチ
// 4. BindToCharacterでキャラクターをバインド
//=====================================================================================================================

using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Systems;
using LitMotion;
using NaughtyAttributes;
using R3;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LearningAIGame.CombatSystem.UI
{
    /// <summary>
    /// Screen Space Canvas上の固定位置表示用HP・エネルギーバーUIコントローラー
    /// プレイヤーUIなど、画面固定位置での使用を想定
    /// </summary>
    public class ScreenSpaceGaugeUIController : MonoBehaviour
    {
        #region フィールド

        [Header("UI参照")]
        [SerializeField, Required]
        [Tooltip("HPバーのfill用Image")]
        private Image _hpFillImage;

        [SerializeField, Required]
        [Tooltip("エネルギーバーのfill用Image")]
        private Image _energyFillImage;

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
        [SerializeField, ReadOnly]
        [Tooltip("現在のHP値")]
        private int _currentHp;

        [SerializeField, ReadOnly]
        [Tooltip("現在のエネルギー値")]
        private int _currentEnergy;

        [SerializeField, ReadOnly]
        [Tooltip("エネルギー枯渇状態")]
        private bool _isEnergyExhaust;

        [SerializeField, ReadOnly]
        [Tooltip("バインド済みフラグ")]
        private bool _isBound;

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
        /// HPアニメーションのハンドル
        /// </summary>
        private MotionHandle _hpMotionHandle;

        /// <summary>
        /// エネルギーアニメーションのハンドル
        /// </summary>
        private MotionHandle _energyMotionHandle;

        /// <summary>
        /// 購読管理用
        /// </summary>
        private IDisposable _damageSubscription;
        private IDisposable _stateSubscription;

        [Tooltip("状態管理システムへの参照")]
        private StateSystem _stateSystem;

        [Tooltip("被弾管理システムへの参照")]
        private DamageSystemBase _damageSystem;

        [Tooltip("キャラクター設定（最大HP/エネルギー取得用）")]
        private CharacterData _characterData;

        [SerializeField]
        private TextMeshProUGUI _nameText;

        #endregion

        #region ライフサイクル

        /// <summary>
        /// 毎フレームの更新処理
        /// </summary>
        private void Update()
        {
            if (_isBound)
            {
                UpdateEnergyGauge();
            }
        }

        /// <summary>
        /// 破棄時にアニメーションをキャンセル
        /// </summary>
        private void OnDestroy()
        {
            CancelAnimations();
            DisposeSubscriptions();
        }

        #endregion

        #region 初期化

        /// <summary>
        /// 購読を破棄
        /// </summary>
        private void DisposeSubscriptions()
        {
            _damageSubscription?.Dispose();
            _damageSubscription = null;
            _stateSubscription?.Dispose();
            _stateSubscription = null;
        }

        /// <summary>
        /// 初期値をキャッシュ
        /// </summary>
        private void CacheInitialValues()
        {
            if (_stateSystem == null)
                return;

            // CharacterDataがあれば使用、なければStateSystemから推定
            if (_characterData != null)
            {
                _maxHp = _characterData.MaxHp;
                _maxEnergy = _characterData.MaxEnergy;
            }
            else
            {
                _maxHp = _stateSystem.Hp > 0 ? _stateSystem.Hp : 100;
                _maxEnergy = _stateSystem.Energy > 0 ? _stateSystem.Energy : 100;
                Debug.LogWarning($"[{nameof(ScreenSpaceGaugeUIController)}] CharacterDataが未設定のため、StateSystemの現在値を最大値として使用します。");
            }

            // 現在値をキャッシュ
            _cachedHp = _stateSystem.Hp;
            _cachedEnergy = _stateSystem.Energy;
            _cachedIsEnergyExhaust = false;

            Debug.Log($"[{nameof(ScreenSpaceGaugeUIController)}] 初期化完了 - MaxHP: {_maxHp}, MaxEnergy: {_maxEnergy}");
        }

        /// <summary>
        /// 各システムの購読を設定
        /// </summary>
        private void SubscribeSystems()
        {
            // 既存の購読を破棄
            DisposeSubscriptions();

            // ダメージシステムの購読（被弾時）
            if (_damageSystem != null)
            {
                _damageSubscription = _damageSystem.Observable.Subscribe(OnDamageReceived);
            }

            // 状態変化の購読
            if (_stateSystem != null)
            {
                _stateSubscription = _stateSystem.CurrentState.Subscribe(OnStateChanged);
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
        private void OnDamageReceived(DamageReportInfo damageReport)
        {
            UpdateHpGauge();
            CheckAndUpdateEnergy();
        }

        /// <summary>
        /// 状態変化時のコールバック
        /// </summary>
        private void OnStateChanged(StateSystem.ActionState newState)
        {
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

            if (currentHp == _cachedHp)
                return;

            _cachedHp = currentHp;

            float hpRatio = _maxHp > 0 ? (float)currentHp / _maxHp : 0f;
            hpRatio = Mathf.Clamp01(hpRatio);

            AnimateHpGauge(hpRatio);
            UpdateHpColor(hpRatio);

            Debug.Log($"[{nameof(ScreenSpaceGaugeUIController)}] HP更新: {currentHp}/{_maxHp} ({hpRatio:P0})");
        }

        /// <summary>
        /// エネルギーゲージの更新（毎フレーム呼び出し）
        /// </summary>
        private void UpdateEnergyGauge()
        {
            if (_stateSystem == null || _energyFillImage == null)
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
        /// </summary>
        private void CheckExhaustStateChange()
        {
            if (_stateSystem == null)
                return;

            bool isExhaust = _stateSystem.Energy <= 0;
            _isEnergyExhaust = isExhaust;

            if (isExhaust != _cachedIsEnergyExhaust)
            {
                _cachedIsEnergyExhaust = isExhaust;
                UpdateEnergyColor(isExhaust);
            }
        }

        #endregion

        #region アニメーション

        private void AnimateHpGauge(float targetRatio)
        {
            if (_hpMotionHandle.IsActive())
            {
                _hpMotionHandle.Cancel();
            }

            float currentFill = _hpFillImage.fillAmount;

            _hpMotionHandle = LMotion.Create(currentFill, targetRatio, _hpAnimationDuration)
                .WithEase(_animationEase)
                .Bind(value => _hpFillImage.fillAmount = value);
        }

        private void AnimateEnergyGauge(float targetRatio)
        {
            if (_energyMotionHandle.IsActive())
            {
                _energyMotionHandle.Cancel();
            }

            float currentFill = _energyFillImage.fillAmount;

            _energyMotionHandle = LMotion.Create(currentFill, targetRatio, _energyAnimationDuration)
                .WithEase(_animationEase)
                .Bind(value => _energyFillImage.fillAmount = value);
        }

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

        private void UpdateHpColor(float hpRatio)
        {
            if (_hpFillImage == null)
                return;

            _hpFillImage.color = hpRatio <= _lowHpThreshold
                ? Color.Lerp(_hpLowColor, _hpNormalColor, hpRatio / _lowHpThreshold)
                : _hpNormalColor;
        }

        private void UpdateEnergyColor(bool isExhaust)
        {
            if (_energyFillImage == null)
                return;

            _energyFillImage.color = isExhaust ? _energyExhaustColor : _energyNormalColor;
        }

        #endregion

        #region Public API

        /// <summary>
        /// キャラクターにゲージをバインドする
        /// </summary>
        public void BindToCharacter(StateSystem state, DamageSystemBase damage, CharacterData characterData, string name)
        {
            if (state == null)
            {
                Debug.LogError($"[{nameof(ScreenSpaceGaugeUIController)}] StateSystemがnullです");
                return;
            }

            _nameText.text = name;
            _stateSystem = state;
            _damageSystem = damage;
            _characterData = characterData;

            // 購読を再設定
            CacheInitialValues();
            SubscribeSystems();
            InitializeGauges();

            _isBound = true;

            Debug.Log($"[{nameof(ScreenSpaceGaugeUIController)}] {name} にバインドしました");
        }

        /// <summary>
        /// バインドを解除
        /// </summary>
        public void Unbind()
        {
            DisposeSubscriptions();
            CancelAnimations();

            _stateSystem = null;
            _damageSystem = null;
            _characterData = null;
            _isBound = false;

            Debug.Log($"[{nameof(ScreenSpaceGaugeUIController)}] バインド解除");
        }

        /// <summary>
        /// ゲージを即座に更新（アニメーションなし）
        /// </summary>
        public void ForceUpdateGauges()
        {
            if (_stateSystem == null)
                return;

            CancelAnimations();

            float hpRatio = _maxHp > 0 ? (float)_stateSystem.Hp / _maxHp : 0f;
            _hpFillImage.fillAmount = Mathf.Clamp01(hpRatio);
            UpdateHpColor(hpRatio);
            _cachedHp = _stateSystem.Hp;

            _energyFillImage.fillAmount = Mathf.Clamp01(_stateSystem.EnergyRatio);
            _cachedEnergy = _stateSystem.Energy;

            bool isExhaust = _stateSystem.EnergyRatio <= 0f;
            UpdateEnergyColor(isExhaust);
            _cachedIsEnergyExhaust = isExhaust;
        }

        #endregion

        #region エディタ専用

#if UNITY_EDITOR
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