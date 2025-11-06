using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace LearningAIGame.Battle.AnimationEvent
{
    /// <summary>
    /// アニメーションイベントハンドラー
    /// アニメーションイベントから文字列IDを受け取り、エフェクトや音声を制御する
    /// キャラクターにアタッチして使用
    /// </summary>
    public class AnimationEventHandler : MonoBehaviour
    {
        [Header("データベース")]
        [Tooltip("使用するアニメーションイベントデータベース")]
        [Required]
        [SerializeField] private AnimationEventDatabase _database;

        [Header("キャラクター設定")]
        [Tooltip("ボーン位置取得用のAnimator（未設定時は自動取得）")]
        [SerializeField] private Animator _animator;

        [Header("音声設定")]
        [Tooltip("エフェクト音声用のAudioSource")]
        [Required]
        [SerializeField] private AudioSource _effectAudioSource;

        [Header("デバッグ設定")]
        [Tooltip("デバッグログを出力するか")]
        [SerializeField] private bool _enableDebugLog = false;

        [Tooltip("現在再生中のエフェクト一覧（デバッグ用）")]
        [ReadOnly]
        [SerializeField] private List<string> _activeEffects = new List<string>();

        // インスタンス化されたエフェクトのキャッシュ
        private Dictionary<string, ParticleSystem> _effectInstances = new Dictionary<string, ParticleSystem>();

        // Prefabからインスタンスへのマッピング（重複防止用）
        private Dictionary<GameObject, ParticleSystem> _prefabToInstance = new Dictionary<GameObject, ParticleSystem>();

        // 自動非アクティブ化管理用
        private Dictionary<ParticleSystem, CancellationTokenSource> _autoDeactivateTasks = new Dictionary<ParticleSystem, CancellationTokenSource>();
        private CancellationTokenSource _destroyCts;

        #region Unity Lifecycle

        private void Awake()
        {
            _destroyCts = new CancellationTokenSource();

            if (_database == null)
            {
                Debug.LogError($"[AnimationEventHandler] データベースが設定されていません: {gameObject.name}", this);
                return;
            }

            // データベースを初期化
            _database.Initialize();

            // Animatorの自動取得
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
                if (_animator == null)
                {
                    Debug.LogWarning($"[AnimationEventHandler] Animatorが見つかりません。ボーン位置の取得ができません: {gameObject.name}", this);
                }
            }

            // エフェクトAudioSourceのデフォルト設定
            if (_effectAudioSource == null)
            {
                _effectAudioSource = GetComponent<AudioSource>();
                if (_effectAudioSource == null)
                {
                    _effectAudioSource = gameObject.AddComponent<AudioSource>();
                    _effectAudioSource.playOnAwake = false;
                }
            }

            // エフェクトPrefabをインスタンス化
            InstantiateEffects();
        }

        private void OnDestroy()
        {
            // 全ての自動非アクティブ化タスクをキャンセル
            foreach (var kvp in _autoDeactivateTasks)
            {
                kvp.Value?.Cancel();
                kvp.Value?.Dispose();
            }
            _autoDeactivateTasks.Clear();

            _destroyCts?.Cancel();
            _destroyCts?.Dispose();

            // インスタンス化されたエフェクトを破棄（重複排除済みなので、ユニークなインスタンスのみ）
            foreach (var kvp in _prefabToInstance)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            _prefabToInstance.Clear();
            _effectInstances.Clear();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// エフェクトPrefabをインスタンス化
        /// </summary>
        private void InstantiateEffects()
        {
            if (_database == null)
            {
                return;
            }

            foreach (var effectSetting in _database.effects)
            {
                if (effectSetting == null || !effectSetting.IsValid())
                {
                    continue;
                }

                // 既にインスタンス化済みの場合はスキップ
                if (_effectInstances.ContainsKey(effectSetting.effectId))
                {
                    Debug.LogWarning($"[AnimationEventHandler] エフェクトID '{effectSetting.effectId}' は既にインスタンス化されています", this);
                    continue;
                }

                ParticleSystem particleSystem;

                // 同じPrefabのインスタンスが既に存在するかチェック
                if (_prefabToInstance.TryGetValue(effectSetting.effectPrefab, out var existingInstance))
                {
                    // 既存のインスタンスを共有
                    particleSystem = existingInstance;

                    if (_enableDebugLog)
                    {
                        Debug.Log($"[AnimationEventHandler] エフェクトインスタンスを共有: {effectSetting.effectId} (Prefab: {effectSetting.effectPrefab.name})", this);
                    }
                }
                else
                {
                    // 新規にPrefabをインスタンス化
                    var effectInstance = Instantiate(effectSetting.effectPrefab, transform);
                    effectInstance.name = $"{effectSetting.effectPrefab.name}_Shared";

                    // ParticleSystemを取得
                    particleSystem = effectInstance.GetComponent<ParticleSystem>();
                    if (particleSystem == null)
                    {
                        Debug.LogError($"[AnimationEventHandler] エフェクトPrefabにParticleSystemがありません: {effectSetting.effectId}", this);
                        Destroy(effectInstance);
                        continue;
                    }

                    // 初期状態は非アクティブ
                    effectInstance.SetActive(false);

                    // Prefab→インスタンスのマッピングに登録
                    _prefabToInstance[effectSetting.effectPrefab] = particleSystem;

                    if (_enableDebugLog)
                    {
                        Debug.Log($"[AnimationEventHandler] エフェクトをインスタンス化: {effectSetting.effectId} (Prefab: {effectSetting.effectPrefab.name})", this);
                    }
                }

                // ID→インスタンスのマッピングに登録
                _effectInstances[effectSetting.effectId] = particleSystem;
            }
        }

        #endregion

        #region Animation Event Methods

        /// <summary>
        /// エフェクトを再生（アニメーションイベントから呼ばれる）
        /// </summary>
        /// <param name="effectId">再生するエフェクトのID</param>
        public void PlayEffect(string effectId)
        {
            if (string.IsNullOrEmpty(effectId))
            {
                Debug.LogWarning($"[AnimationEventHandler] 空のエフェクトIDが指定されました: {gameObject.name}", this);
                return;
            }

            if (_database == null)
            {
                Debug.LogError($"[AnimationEventHandler] データベースが設定されていません: {gameObject.name}", this);
                return;
            }

            var effectSetting = _database.GetEffectSetting(effectId);
            if (effectSetting == null)
            {
                return; // データベース側でログ出力済み
            }

            if (!effectSetting.IsValid())
            {
                return; // データ検証でログ出力済み
            }

            // インスタンスが存在するか確認
            if (!_effectInstances.TryGetValue(effectId, out var particleSystem))
            {
                Debug.LogError($"[AnimationEventHandler] エフェクトインスタンスが見つかりません: {effectId}", this);
                return;
            }

            PlayEffectInternal(effectSetting, particleSystem).Forget();
        }

        /// <summary>
        /// 音声を再生（アニメーションイベントから呼ばれる）
        /// </summary>
        /// <param name="audioId">再生する音声のID</param>
        public void PlayAudio(string audioId)
        {
            if (string.IsNullOrEmpty(audioId))
            {
                Debug.LogWarning($"[AnimationEventHandler] 空の音声IDが指定されました: {gameObject.name}", this);
                return;
            }

            if (_database == null)
            {
                Debug.LogError($"[AnimationEventHandler] データベースが設定されていません: {gameObject.name}", this);
                return;
            }

            var audioSetting = _database.GetAudioSetting(audioId);
            if (audioSetting == null)
            {
                return; // データベース側でログ出力済み
            }

            if (!audioSetting.IsValid())
            {
                return; // データ検証でログ出力済み
            }

            PlayAudioInternal(audioSetting);
        }

        /// <summary>
        /// エフェクトと音声を同時再生（アニメーションイベントから呼ばれる）
        /// </summary>
        /// <param name="effectId">再生するエフェクトのID</param>
        public void PlayEffectAndAudio(string effectId)
        {
            // IDが同じと仮定（例: "sword_slash"）
            PlayEffect(effectId);
            PlayAudio(effectId);
        }

        /// <summary>
        /// 特定のエフェクトを停止（アニメーションイベントから呼ばれる）
        /// </summary>
        /// <param name="effectId">停止するエフェクトのID</param>
        public void StopEffect(string effectId)
        {
            if (string.IsNullOrEmpty(effectId))
            {
                return;
            }

            var effectSetting = _database.GetEffectSetting(effectId);
            if (effectSetting == null)
            {
                return;
            }

            if (!_effectInstances.TryGetValue(effectId, out var particleSystem))
            {
                return;
            }

            StopEffectInternal(effectSetting, particleSystem);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// 指定されたボーンのTransformを取得
        /// </summary>
        private Transform GetBoneTransform(HumanBodyBones bone)
        {
            // LastBone（デフォルト値）の場合はキャラクタールートを使用
            if (bone == HumanBodyBones.LastBone || _animator == null)
            {
                return transform;
            }

            var boneTransform = _animator.GetBoneTransform(bone);
            if (boneTransform == null)
            {
                Debug.LogWarning($"[AnimationEventHandler] ボーン '{bone}' が見つかりません。ルート位置を使用します。", this);
                return transform;
            }

            return boneTransform;
        }

        /// <summary>
        /// エフェクト再生の内部処理
        /// </summary>
        private async UniTaskVoid PlayEffectInternal(EffectSetting effectSetting, ParticleSystem particleSystem)
        {
            // ボーン位置を取得して適用
            Transform spawnTransform = GetBoneTransform(effectSetting.spawnBone);
            particleSystem.transform.position = spawnTransform.position;
            particleSystem.transform.rotation = spawnTransform.rotation;

            // アクティブ化
            particleSystem.gameObject.SetActive(true);

            if (_enableDebugLog || effectSetting.enableDebugLog)
            {
                Debug.Log($"[AnimationEventHandler] エフェクト再生: {effectSetting.effectId} at {effectSetting.spawnBone}", this);
            }

            // デバッグ用リストに追加
            if (!_activeEffects.Contains(effectSetting.effectId))
            {
                _activeEffects.Add(effectSetting.effectId);
            }

            // ParticleSystemを再生
            particleSystem.Play(true);

            // 自動非アクティブ化
            if (effectSetting.autoDeactivateTime > 0f)
            {
                // 既存の自動非アクティブ化タスクがあればキャンセル
                if (_autoDeactivateTasks.TryGetValue(particleSystem, out var existingCts))
                {
                    existingCts?.Cancel();
                    existingCts?.Dispose();
                    _autoDeactivateTasks.Remove(particleSystem);
                }

                // 新しいキャンセルトークンを作成
                var cts = CancellationTokenSource.CreateLinkedTokenSource(_destroyCts.Token);
                _autoDeactivateTasks[particleSystem] = cts;

                bool isCancel = await UniTask.Delay(
                    TimeSpan.FromSeconds(effectSetting.autoDeactivateTime),
                    cancellationToken: cts.Token
                ).SuppressCancellationThrow();

                if (!isCancel)
                {
                    // タスク完了後、自動でクリーンアップ
                    _autoDeactivateTasks.Remove(particleSystem);
                    cts.Dispose();

                    StopEffectInternal(effectSetting, particleSystem);
                }
            }
        }

        /// <summary>
        /// エフェクト停止の内部処理
        /// </summary>
        private void StopEffectInternal(EffectSetting effectSetting, ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                return;
            }

            // 自動非アクティブ化タスクをキャンセル
            if (_autoDeactivateTasks.TryGetValue(particleSystem, out var cts))
            {
                cts?.Cancel();
                cts?.Dispose();
                _autoDeactivateTasks.Remove(particleSystem);
            }

            // ParticleSystemを停止
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // 非アクティブ化
            particleSystem.gameObject.SetActive(false);

            if (_enableDebugLog || effectSetting.enableDebugLog)
            {
                Debug.Log($"[AnimationEventHandler] エフェクト停止: {effectSetting.effectId}", this);
            }

            // デバッグ用リストから削除
            _activeEffects.Remove(effectSetting.effectId);
        }

        /// <summary>
        /// 音声再生の内部処理
        /// </summary>
        private void PlayAudioInternal(AudioSetting audioSetting)
        {
            if (_effectAudioSource == null)
            {
                Debug.LogError($"[AnimationEventHandler] AudioSourceが設定されていません", this);
                return;
            }

            var audioClip = audioSetting.audioClip;

            // AudioSourceの設定を適用
            _effectAudioSource.volume = audioSetting.volume;
            _effectAudioSource.pitch = audioSetting.pitch;
            _effectAudioSource.loop = audioSetting.loop;

            // 再生（OneShot使用で複数音声の同時再生をサポート）
            if (!audioSetting.loop)
            {
                _effectAudioSource.PlayOneShot(audioClip, audioSetting.volume);
            }
            else
            {
                _effectAudioSource.clip = audioClip;
                _effectAudioSource.Play();
            }

            if (_enableDebugLog || audioSetting.enableDebugLog)
            {
                Debug.Log($"[AnimationEventHandler] 音声再生: {audioSetting.audioId} ({audioClip.name})", this);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// エフェクトインスタンスを取得（外部から位置制御するため）
        /// </summary>
        public ParticleSystem GetEffectInstance(string effectId)
        {
            if (_effectInstances.TryGetValue(effectId, out var instance))
            {
                return instance;
            }
            return null;
        }

        #endregion

        #region Editor Debug Methods

        /// <summary>
        /// 全てのアクティブなエフェクトを停止（デバッグ用）
        /// </summary>
        [Button("全エフェクト停止")]
        private void StopAllEffects()
        {
            if (_database == null)
            {
                Debug.LogWarning("[AnimationEventHandler] データベースが設定されていません");
                return;
            }

            foreach (var effectSetting in _database.effects)
            {
                if (effectSetting != null && _effectInstances.TryGetValue(effectSetting.effectId, out var particleSystem))
                {
                    if (particleSystem != null && particleSystem.gameObject.activeSelf)
                    {
                        StopEffectInternal(effectSetting, particleSystem);
                    }
                }
            }

            Debug.Log("[AnimationEventHandler] 全エフェクトを停止しました");
        }

        /// <summary>
        /// テスト用エフェクト再生（デバッグ用）
        /// </summary>
        [Button("テストエフェクト再生")]
        private void TestPlayEffect()
        {
            if (_database == null || _database.effects.Count == 0)
            {
                Debug.LogWarning("[AnimationEventHandler] 再生できるエフェクトがありません");
                return;
            }

            var testSetting = _database.effects[0];
            if (testSetting != null && testSetting.IsValid())
            {
                PlayEffect(testSetting.effectId);
            }
        }

        #endregion
    }
}