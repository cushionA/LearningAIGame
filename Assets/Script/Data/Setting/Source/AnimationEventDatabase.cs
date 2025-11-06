using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;

namespace LearningAIGame.Battle.AnimationEvent
{
    /// <summary>
    /// エフェクト設定
    /// </summary>
    [System.Serializable]
    public class EffectSetting
    {
        [Header("識別情報")]
        [Tooltip("アニメーションイベントから呼び出すID")]
        public string effectId;

        [Header("エフェクト設定")]
        [Tooltip("再生するエフェクトのPrefab")]
        [Required]
        public GameObject effectPrefab;

        [Header("位置設定")]
        [Tooltip("エフェクトを再生するボーン位置（None=キャラクタールート）")]
        public HumanBodyBones spawnBone = HumanBodyBones.Hips;

        [Header("再生設定")]
        [Tooltip("エフェクトの自動非アクティブ化時間（秒）。0以下の場合は自動非アクティブ化しない")]
        [MinValue(0f)]
        public float autoDeactivateTime = 2f;

        [Tooltip("デバッグログを出力するか")]
        public bool enableDebugLog = false;

        /// <summary>
        /// 設定が有効かどうかを検証
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(effectId))
            {
                Debug.LogError($"[EffectSetting] effectIdが設定されていません");
                return false;
            }

            if (effectPrefab == null)
            {
                Debug.LogError($"[EffectSetting] effectPrefabが設定されていません: {effectId}");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 音声設定
    /// </summary>
    [System.Serializable]
    public class AudioSetting
    {
        [Header("識別情報")]
        [Tooltip("アニメーションイベントから呼び出すID")]
        public string audioId;

        [Header("音声設定")]
        [Tooltip("再生するAudioClip")]
        [Required]
        public AudioClip audioClip;

        [Header("再生設定")]
        [Tooltip("音量（0.0 ~ 1.0）")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("ピッチ")]
        [Range(0.1f, 3f)]
        public float pitch = 1f;

        [Tooltip("ループ再生するか")]
        public bool loop = false;

        [Tooltip("デバッグログを出力するか")]
        public bool enableDebugLog = false;

        /// <summary>
        /// 設定が有効かどうかを検証
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(audioId))
            {
                Debug.LogError($"[AudioSetting] audioIdが設定されていません");
                return false;
            }

            if (audioClip == null)
            {
                Debug.LogError($"[AudioSetting] audioClipが設定されていません: {audioId}");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// アニメーションイベント用のエフェクトと音声データベース
    /// 全てのエフェクトと音声設定を一元管理する
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationEventDatabase", menuName = "LearningAIGame/Battle/AnimationEvent/Database")]
    public class AnimationEventDatabase : ScriptableObject
    {
        [Header("エフェクト設定")]
        [Tooltip("管理する全てのエフェクト設定")]
        [ReorderableList]
        public List<EffectSetting> effects = new List<EffectSetting>();

        [Header("音声設定")]
        [Tooltip("管理する全ての音声設定")]
        [ReorderableList]
        public List<AudioSetting> audios = new List<AudioSetting>();

        [Header("デバッグ")]
        [Tooltip("デバッグログを出力するか")]
        public bool enableDebugLog = false;

        // 高速検索用のキャッシュ
        private Dictionary<string, EffectSetting> _effectCache;
        private Dictionary<string, AudioSetting> _audioCache;

        /// <summary>
        /// 初期化（キャッシュ構築）
        /// </summary>
        public void Initialize()
        {
            BuildEffectCache();
            BuildAudioCache();

            if (enableDebugLog)
            {
                Debug.Log($"[AnimationEventDatabase] 初期化完了 - エフェクト:{effects.Count}件, 音声:{audios.Count}件");
            }
        }

        /// <summary>
        /// エフェクト設定を取得
        /// </summary>
        public EffectSetting GetEffectSetting(string effectId)
        {
            if (_effectCache == null)
            {
                BuildEffectCache();
            }

            if (_effectCache.TryGetValue(effectId, out var setting))
            {
                return setting;
            }

            Debug.LogWarning($"[AnimationEventDatabase] エフェクトID '{effectId}' が見つかりません", this);
            return null;
        }

        /// <summary>
        /// 音声設定を取得
        /// </summary>
        public AudioSetting GetAudioSetting(string audioId)
        {
            if (_audioCache == null)
            {
                BuildAudioCache();
            }

            if (_audioCache.TryGetValue(audioId, out var setting))
            {
                return setting;
            }

            Debug.LogWarning($"[AnimationEventDatabase] 音声ID '{audioId}' が見つかりません", this);
            return null;
        }

        /// <summary>
        /// エフェクトキャッシュを構築
        /// </summary>
        private void BuildEffectCache()
        {
            _effectCache = new Dictionary<string, EffectSetting>();

            foreach (var setting in effects)
            {
                if (setting == null)
                    continue;
                if (!setting.IsValid())
                    continue;

                if (_effectCache.ContainsKey(setting.effectId))
                {
                    Debug.LogError($"[AnimationEventDatabase] 重複したエフェクトID '{setting.effectId}' が検出されました", this);
                    continue;
                }

                _effectCache[setting.effectId] = setting;
            }
        }

        /// <summary>
        /// 音声キャッシュを構築
        /// </summary>
        private void BuildAudioCache()
        {
            _audioCache = new Dictionary<string, AudioSetting>();

            foreach (var setting in audios)
            {
                if (setting == null)
                    continue;
                if (!setting.IsValid())
                    continue;

                if (_audioCache.ContainsKey(setting.audioId))
                {
                    Debug.LogError($"[AnimationEventDatabase] 重複した音声ID '{setting.audioId}' が検出されました", this);
                    continue;
                }

                _audioCache[setting.audioId] = setting;
            }
        }

        /// <summary>
        /// 設定を検証（Editor用）
        /// </summary>
        [Button("設定を検証")]
        private void ValidateData()
        {
            Debug.Log("===== AnimationEventDatabase 検証開始 =====");

            int validEffects = 0;
            int invalidEffects = 0;
            foreach (var setting in effects)
            {
                if (setting == null)
                {
                    invalidEffects++;
                    continue;
                }

                if (setting.IsValid())
                {
                    validEffects++;
                }
                else
                {
                    invalidEffects++;
                }
            }

            int validAudios = 0;
            int invalidAudios = 0;
            foreach (var setting in audios)
            {
                if (setting == null)
                {
                    invalidAudios++;
                    continue;
                }

                if (setting.IsValid())
                {
                    validAudios++;
                }
                else
                {
                    invalidAudios++;
                }
            }

            Debug.Log($"エフェクト: 有効={validEffects}件, 無効={invalidEffects}件");
            Debug.Log($"音声: 有効={validAudios}件, 無効={invalidAudios}件");

            // 重複チェック
            var duplicateEffects = effects
                .Where(s => s != null && !string.IsNullOrEmpty(s.effectId))
                .GroupBy(s => s.effectId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var id in duplicateEffects)
            {
                Debug.LogError($"重複したエフェクトID: {id}");
            }

            var duplicateAudios = audios
                .Where(s => s != null && !string.IsNullOrEmpty(s.audioId))
                .GroupBy(s => s.audioId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var id in duplicateAudios)
            {
                Debug.LogError($"重複した音声ID: {id}");
            }

            Debug.Log("===== 検証完了 =====");
        }

        /// <summary>
        /// ParticleSystemから全エフェクトの時間を自動設定（Editor用）
        /// </summary>
        [Button("全エフェクトの時間を自動設定")]
        private void SetAllEffectTimesFromParticleSystem()
        {
            int updated = 0;
            int skipped = 0;

            foreach (var setting in effects)
            {
                if (setting == null || setting.effectPrefab == null)
                {
                    skipped++;
                    continue;
                }

                var particleSystem = setting.effectPrefab.GetComponent<ParticleSystem>();
                if (particleSystem == null)
                {
                    skipped++;
                    continue;
                }

                var main = particleSystem.main;
                if (main.loop)
                {
                    Debug.LogWarning($"[AnimationEventDatabase] ParticleSystemがループ設定されています（スキップ）: {setting.effectId}");
                    skipped++;
                    continue;
                }

                float duration = main.duration;
                float maxLifetime = main.startLifetime.constantMax;
                float totalTime = Mathf.Max(duration, maxLifetime);

                setting.autoDeactivateTime = totalTime;
                updated++;

                if (enableDebugLog)
                {
                    Debug.Log($"[AnimationEventDatabase] {setting.effectId}: {totalTime}秒に設定");
                }
            }

            Debug.Log($"[AnimationEventDatabase] エフェクト時間の自動設定完了: 更新={updated}件, スキップ={skipped}件");

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}