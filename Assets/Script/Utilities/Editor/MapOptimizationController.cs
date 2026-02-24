using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace LearningAIGame.EditorTools
{
    /// <summary>
    /// マップ全体の最適化を統合管理するエディタツール
    /// - 距離ベースのコンポーネント無効化（Collider/Renderer/Light/AudioSource）
    /// - LOD自動設定
    /// - Static Batching支援
    /// - Lightmap Static自動設定
    /// - Occlusion Culling支援
    /// </summary>
    [ExecuteInEditMode]
    public class MapOptimizationController : MonoBehaviour
    {
        #region === インスペクター設定 ===

        [Header("基準設定")]
        [SerializeField]
        [Tooltip("距離判定の基準となるオブジェクト")]
        private Transform _referenceObject;

        [SerializeField]
        [Tooltip("この距離以上離れているコンポーネントを無効化します")]
        [Min(0f)]
        private float _distanceThreshold = 10f;

        [Header("距離ベース無効化の対象")]
        [SerializeField]
        [Tooltip("Colliderを無効化対象にする")]
        private bool _optimizeColliders = true;

        [SerializeField]
        [Tooltip("Rendererを無効化対象にする")]
        private bool _optimizeRenderers = true;

        [SerializeField]
        [Tooltip("Lightを無効化対象にする")]
        private bool _optimizeLights = false;

        [SerializeField]
        [Tooltip("AudioSourceを無効化対象にする")]
        private bool _optimizeAudioSources = false;

        [Header("Renderer詳細設定")]
        [SerializeField]
        [Tooltip("Rendererを無効化しても影だけは残す")]
        private bool _keepShadowsWhenDisabled = false;

        [Header("LOD設定")]
        [SerializeField]
        [Tooltip("LODを自動設定する")]
        private bool _enableLODSetup = false;

        [SerializeField]
        [Tooltip("LODの距離段階（近→遠の順）")]
        private float[] _lodDistances = new float[] { 30f, 60f, 100f };

        [SerializeField]
        [Tooltip("LODの品質割合（近→遠の順、0-1）")]
        private float[] _lodQualityRatios = new float[] { 1.0f, 0.5f, 0.25f };

        [Header("Static Batching設定")]
        [SerializeField]
        [Tooltip("Static Batchingを自動設定する")]
        private bool _enableStaticBatching = false;

        [SerializeField]
        [Tooltip("動かないオブジェクトを自動検出してStaticにする")]
        private bool _autoDetectStaticObjects = true;

        [Header("Lightmap設定")]
        [SerializeField]
        [Tooltip("Lightmap Staticを自動設定する")]
        private bool _enableLightmapStatic = false;

        [SerializeField]
        [Tooltip("推奨Lightmap解像度（ピクセル/ユニット）")]
        private float _recommendedLightmapScale = 1.0f;

        [Header("Occlusion Culling設定")]
        [SerializeField]
        [Tooltip("Occlusion Culling支援機能を有効にする")]
        private bool _enableOcclusionCulling = false;

        [SerializeField]
        [Tooltip("Occlusion Areaの推奨サイズ")]
        private Vector3 _occlusionAreaSize = new Vector3(50f, 10f, 50f);

        [Header("視覚化設定")]
        [SerializeField]
        [Tooltip("シーンビューで範囲を表示するか")]
        private bool _showGizmos = true;

        [SerializeField]
        [Tooltip("範囲球体の色")]
        private Color _gizmoColor = new Color(0f, 1f, 0f, 0.2f);

        #endregion

        #region === Gizmo描画 ===

        /// <summary>
        /// シーンビューでの視覚化
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!_showGizmos || _referenceObject == null)
            {
                return;
            }

            // 閾値範囲の球体を描画
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(_referenceObject.position, _distanceThreshold);

            // LOD距離の視覚化
            if (_enableLODSetup)
            {
                foreach (float distance in _lodDistances)
                {
                    Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
                    Gizmos.DrawWireSphere(_referenceObject.position, distance);
                }
            }

            // 各コンポーネントの状態を視覚化
            VisualizeComponentStates();
        }

        /// <summary>
        /// コンポーネント状態の視覚化
        /// </summary>
        private void VisualizeComponentStates()
        {
            Transform[] allTransforms = this.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allTransforms)
            {
                if (t == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(_referenceObject.position, t.position);
                bool isWithinThreshold = distance < _distanceThreshold;

                // 無効化対象のコンポーネントがあるか確認
                bool hasTargetComponents = false;
                if (_optimizeColliders && t.GetComponent<Collider>() != null)
                {
                    hasTargetComponents = true;
                }
                if (_optimizeRenderers && t.GetComponent<Renderer>() != null)
                {
                    hasTargetComponents = true;
                }
                if (_optimizeLights && t.GetComponent<Light>() != null)
                {
                    hasTargetComponents = true;
                }
                if (_optimizeAudioSources && t.GetComponent<AudioSource>() != null)
                {
                    hasTargetComponents = true;
                }

                if (!hasTargetComponents)
                {
                    continue;
                }

                // 状態に応じて色分け
                if (isWithinThreshold)
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.5f); // 緑: 有効維持
                }
                else
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // 赤: 無効化対象
                }

                Gizmos.DrawSphere(t.position, 0.1f);
            }
        }

        #endregion

        #region === 1. 距離ベース無効化 ===

        /// <summary>
        /// 距離に応じてコンポーネントを無効化する
        /// </summary>
        public OptimizationResult OptimizeByDistance()
        {
            if (_referenceObject == null)
            {
                Debug.LogError("[MapOptimizationController] 基準オブジェクトが設定されていません");
                return new OptimizationResult();
            }

            OptimizationResult result = new OptimizationResult();

            if (_optimizeColliders)
            {
                result.collidersDisabled = DisableCollidersByDistance();
            }

            if (_optimizeRenderers)
            {
                result.renderersDisabled = DisableRenderersByDistance();
            }

            if (_optimizeLights)
            {
                result.lightsDisabled = DisableLightsByDistance();
            }

            if (_optimizeAudioSources)
            {
                result.audioSourcesDisabled = DisableAudioSourcesByDistance();
            }

            return result;
        }

        /// <summary>
        /// Colliderを距離に応じて無効化
        /// </summary>
        private int DisableCollidersByDistance()
        {
            Collider[] colliders = this.GetComponentsInChildren<Collider>(true);
            int disabledCount = 0;

            foreach (Collider col in colliders)
            {
                if (col == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(_referenceObject.position, col.transform.position);

                if (distance >= _distanceThreshold && col.enabled)
                {
                    col.enabled = false;
                    disabledCount++;
                }
            }

            return disabledCount;
        }

        /// <summary>
        /// Rendererを距離に応じて無効化
        /// </summary>
        private int DisableRenderersByDistance()
        {
            Renderer[] renderers = this.GetComponentsInChildren<Renderer>(true);
            int disabledCount = 0;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(_referenceObject.position, renderer.transform.position);

                if (distance >= _distanceThreshold && renderer.enabled)
                {
                    // 影だけ残す設定の場合
                    if (_keepShadowsWhenDisabled)
                    {
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    }
                    else
                    {
                        renderer.enabled = false;
                    }
                    disabledCount++;
                }
            }

            return disabledCount;
        }

        /// <summary>
        /// Lightを距離に応じて無効化
        /// </summary>
        private int DisableLightsByDistance()
        {
            Light[] lights = this.GetComponentsInChildren<Light>(true);
            int disabledCount = 0;

            foreach (Light light in lights)
            {
                if (light == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(_referenceObject.position, light.transform.position);

                if (distance >= _distanceThreshold && light.enabled)
                {
                    light.enabled = false;
                    disabledCount++;
                }
            }

            return disabledCount;
        }

        /// <summary>
        /// AudioSourceを距離に応じて無効化
        /// </summary>
        private int DisableAudioSourcesByDistance()
        {
            AudioSource[] audioSources = this.GetComponentsInChildren<AudioSource>(true);
            int disabledCount = 0;

            foreach (AudioSource audioSource in audioSources)
            {
                if (audioSource == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(_referenceObject.position, audioSource.transform.position);

                if (distance >= _distanceThreshold && audioSource.enabled)
                {
                    audioSource.enabled = false;
                    disabledCount++;
                }
            }

            return disabledCount;
        }

        /// <summary>
        /// すべてのコンポーネントを有効化
        /// </summary>
        public OptimizationResult EnableAllComponents()
        {
            OptimizationResult result = new OptimizationResult();

            if (_optimizeColliders)
            {
                result.collidersEnabled = EnableAllColliders();
            }

            if (_optimizeRenderers)
            {
                result.renderersEnabled = EnableAllRenderers();
            }

            if (_optimizeLights)
            {
                result.lightsEnabled = EnableAllLights();
            }

            if (_optimizeAudioSources)
            {
                result.audioSourcesEnabled = EnableAllAudioSources();
            }

            return result;
        }

        private int EnableAllColliders()
        {
            Collider[] colliders = this.GetComponentsInChildren<Collider>(true);
            int enabledCount = 0;

            foreach (Collider col in colliders)
            {
                if (col != null && !col.enabled)
                {
                    col.enabled = true;
                    enabledCount++;
                }
            }

            return enabledCount;
        }

        private int EnableAllRenderers()
        {
            Renderer[] renderers = this.GetComponentsInChildren<Renderer>(true);
            int enabledCount = 0;

            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && !renderer.enabled)
                {
                    renderer.enabled = true;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    enabledCount++;
                }
            }

            return enabledCount;
        }

        private int EnableAllLights()
        {
            Light[] lights = this.GetComponentsInChildren<Light>(true);
            int enabledCount = 0;

            foreach (Light light in lights)
            {
                if (light != null && !light.enabled)
                {
                    light.enabled = true;
                    enabledCount++;
                }
            }

            return enabledCount;
        }

        private int EnableAllAudioSources()
        {
            AudioSource[] audioSources = this.GetComponentsInChildren<AudioSource>(true);
            int enabledCount = 0;

            foreach (AudioSource audioSource in audioSources)
            {
                if (audioSource != null && !audioSource.enabled)
                {
                    audioSource.enabled = true;
                    enabledCount++;
                }
            }

            return enabledCount;
        }

        #endregion

        #region === 2. LOD自動設定 ===

        /// <summary>
        /// LODを自動設定する
        /// </summary>
        public LODSetupResult SetupLOD()
        {
            if (!_enableLODSetup)
            {
                Debug.LogWarning("[MapOptimizationController] LOD設定が無効です");
                return new LODSetupResult();
            }

            LODSetupResult result = new LODSetupResult();
            MeshRenderer[] renderers = this.GetComponentsInChildren<MeshRenderer>(true);

            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                // 既にLODGroupがある場合はスキップ
                LODGroup existingLODGroup = renderer.GetComponent<LODGroup>();
                if (existingLODGroup != null)
                {
                    result.skippedCount++;
                    continue;
                }

                // LODGroup追加
                LODGroup lodGroup = renderer.gameObject.AddComponent<LODGroup>();

                // LODレベルを設定
                LOD[] lods = new LOD[_lodDistances.Length];

                for (int i = 0; i < _lodDistances.Length; i++)
                {
                    Renderer[] lodRenderers = new Renderer[] { renderer };
                    float screenRelativeHeight = CalculateScreenHeight(_lodDistances[i]);
                    lods[i] = new LOD(screenRelativeHeight, lodRenderers);
                }

                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();

                result.setupCount++;
            }

            return result;
        }

        /// <summary>
        /// 距離からスクリーン相対高さを計算
        /// </summary>
        private float CalculateScreenHeight(float distance)
        {
            // 簡易的な計算（カメラのFOVを考慮した正確な計算が必要な場合は調整）
            float maxDistance = _lodDistances[_lodDistances.Length - 1];
            return 1f - (distance / maxDistance);
        }

        #endregion

        #region === 3. Static Batching支援 ===

        /// <summary>
        /// Static Batchingを自動設定
        /// </summary>
        public StaticBatchingResult SetupStaticBatching()
        {
            if (!_enableStaticBatching)
            {
                Debug.LogWarning("[MapOptimizationController] Static Batching設定が無効です");
                return new StaticBatchingResult();
            }

            StaticBatchingResult result = new StaticBatchingResult();
            GameObject[] allObjects = this.GetComponentsInChildren<Transform>(true)
                .Select(t => t.gameObject)
                .ToArray();

            foreach (GameObject obj in allObjects)
            {
                if (obj == null)
                {
                    continue;
                }

                // 既にStaticの場合はスキップ
                if (obj.isStatic)
                {
                    result.alreadyStaticCount++;
                    continue;
                }

                // 動かないオブジェクトかチェック
                if (_autoDetectStaticObjects && IsStaticObject(obj))
                {
                    obj.isStatic = true;
                    result.madeStaticCount++;
                }
            }

            return result;
        }

        /// <summary>
        /// オブジェクトが静的かどうか判定
        /// </summary>
        private bool IsStaticObject(GameObject obj)
        {
            // Rigidbodyがあれば動的
            if (obj.GetComponent<Rigidbody>() != null)
            {
                return false;
            }

            // Animatorがあれば動的
            if (obj.GetComponent<Animator>() != null)
            {
                return false;
            }

            // Characterコントローラーがあれば動的
            if (obj.GetComponent<CharacterController>() != null)
            {
                return false;
            }

            // デフォルトは静的として扱う
            return true;
        }

        #endregion

        #region === 4. Lightmap Static自動設定 ===

        /// <summary>
        /// Lightmap Staticを自動設定
        /// </summary>
        public LightmapStaticResult SetupLightmapStatic()
        {
            if (!_enableLightmapStatic)
            {
                Debug.LogWarning("[MapOptimizationController] Lightmap Static設定が無効です");
                return new LightmapStaticResult();
            }

            LightmapStaticResult result = new LightmapStaticResult();

#if UNITY_EDITOR
            MeshRenderer[] renderers = this.GetComponentsInChildren<MeshRenderer>(true);

            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                GameObject obj = renderer.gameObject;

                // 動的オブジェクトはスキップ
                if (!IsStaticObject(obj))
                {
                    result.skippedCount++;
                    continue;
                }

                // Lightmap Staticフラグを設定
                UnityEditor.GameObjectUtility.SetStaticEditorFlags(
                    obj,
                    UnityEditor.StaticEditorFlags.ContributeGI
                );

                result.setupCount++;
            }
#endif

            return result;
        }

        #endregion

        #region === 5. Occlusion Culling支援 ===

        /// <summary>
        /// Occlusion Culling支援情報を取得
        /// </summary>
        public OcclusionCullingInfo GetOcclusionCullingInfo()
        {
            if (!_enableOcclusionCulling)
            {
                Debug.LogWarning("[MapOptimizationController] Occlusion Culling設定が無効です");
                return new OcclusionCullingInfo();
            }

            OcclusionCullingInfo info = new OcclusionCullingInfo();

            // レンダラーの統計
            Renderer[] renderers = this.GetComponentsInChildren<Renderer>(true);
            info.totalRendererCount = renderers.Length;

            // Occlusion Area推奨位置の計算（マップの中心）
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                foreach (Renderer r in renderers)
                {
                    if (r != null)
                    {
                        combinedBounds.Encapsulate(r.bounds);
                    }
                }
                info.recommendedAreaCenter = combinedBounds.center;
                info.recommendedAreaSize = combinedBounds.size;
            }

            return info;
        }

        #endregion

        #region === 統計情報 ===

        /// <summary>
        /// 現在の最適化状態を取得
        /// </summary>
        public OptimizationStatistics GetStatistics()
        {
            OptimizationStatistics stats = new OptimizationStatistics();

            if (_referenceObject == null)
            {
                return stats;
            }

            // Collider統計
            if (_optimizeColliders)
            {
                Collider[] colliders = this.GetComponentsInChildren<Collider>(true);
                stats.totalColliders = colliders.Length;
                stats.enabledColliders = colliders.Count(c => c != null && c.enabled);
                stats.disabledColliders = colliders.Count(c => c != null && !c.enabled);
                stats.colliderTargets = colliders.Count(c =>
                    c != null && c.enabled &&
                    Vector3.Distance(_referenceObject.position, c.transform.position) >= _distanceThreshold
                );
            }

            // Renderer統計
            if (_optimizeRenderers)
            {
                Renderer[] renderers = this.GetComponentsInChildren<Renderer>(true);
                stats.totalRenderers = renderers.Length;
                stats.enabledRenderers = renderers.Count(r => r != null && r.enabled);
                stats.disabledRenderers = renderers.Count(r => r != null && !r.enabled);
                stats.rendererTargets = renderers.Count(r =>
                    r != null && r.enabled &&
                    Vector3.Distance(_referenceObject.position, r.transform.position) >= _distanceThreshold
                );
            }

            // Light統計
            if (_optimizeLights)
            {
                Light[] lights = this.GetComponentsInChildren<Light>(true);
                stats.totalLights = lights.Length;
                stats.enabledLights = lights.Count(l => l != null && l.enabled);
                stats.disabledLights = lights.Count(l => l != null && !l.enabled);
            }

            // AudioSource統計
            if (_optimizeAudioSources)
            {
                AudioSource[] audioSources = this.GetComponentsInChildren<AudioSource>(true);
                stats.totalAudioSources = audioSources.Length;
                stats.enabledAudioSources = audioSources.Count(a => a != null && a.enabled);
                stats.disabledAudioSources = audioSources.Count(a => a != null && !a.enabled);
            }

            return stats;
        }

        #endregion

        #region === データ構造 ===

        /// <summary>
        /// 最適化結果
        /// </summary>
        public struct OptimizationResult
        {
            public int collidersDisabled;
            public int renderersDisabled;
            public int lightsDisabled;
            public int audioSourcesDisabled;
            public int collidersEnabled;
            public int renderersEnabled;
            public int lightsEnabled;
            public int audioSourcesEnabled;
        }

        /// <summary>
        /// 統計情報
        /// </summary>
        public struct OptimizationStatistics
        {
            public int totalColliders;
            public int enabledColliders;
            public int disabledColliders;
            public int colliderTargets;

            public int totalRenderers;
            public int enabledRenderers;
            public int disabledRenderers;
            public int rendererTargets;

            public int totalLights;
            public int enabledLights;
            public int disabledLights;

            public int totalAudioSources;
            public int enabledAudioSources;
            public int disabledAudioSources;
        }

        /// <summary>
        /// LOD設定結果
        /// </summary>
        public struct LODSetupResult
        {
            public int setupCount;
            public int skippedCount;
        }

        /// <summary>
        /// Static Batching結果
        /// </summary>
        public struct StaticBatchingResult
        {
            public int madeStaticCount;
            public int alreadyStaticCount;
        }

        /// <summary>
        /// Lightmap Static結果
        /// </summary>
        public struct LightmapStaticResult
        {
            public int setupCount;
            public int skippedCount;
        }

        /// <summary>
        /// Occlusion Culling情報
        /// </summary>
        public struct OcclusionCullingInfo
        {
            public int totalRendererCount;
            public Vector3 recommendedAreaCenter;
            public Vector3 recommendedAreaSize;
        }

        #endregion
    }
}