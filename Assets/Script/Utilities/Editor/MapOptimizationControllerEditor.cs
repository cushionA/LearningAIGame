using UnityEngine;
using UnityEditor;
using System.Linq;
#if UNITY_EDITOR
namespace LearningAIGame.EditorTools
{
    /// <summary>
    /// MapOptimizationController のカスタムインスペクタ
    /// </summary>
    [CustomEditor(typeof(MapOptimizationController))]
    public class MapOptimizationControllerEditor : Editor
    {
        #region === フィールド ===

        private MapOptimizationController _target;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = new string[]
        {
            "距離ベース",
            "LOD",
            "Static",
            "Lightmap",
            "Occlusion"
        };

        #endregion

        #region === Unity Callbacks ===

        private void OnEnable()
        {
            _target = (MapOptimizationController)target;
        }

        public override void OnInspectorGUI()
        {
            // デフォルトのインスペクタ表示
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // タブ選択
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);

            EditorGUILayout.Space(5);

            // 選択されたタブに応じて表示
            switch (_selectedTab)
            {
                case 0:
                    DrawDistanceBasedTab();
                    break;
                case 1:
                    DrawLODTab();
                    break;
                case 2:
                    DrawStaticBatchingTab();
                    break;
                case 3:
                    DrawLightmapTab();
                    break;
                case 4:
                    DrawOcclusionTab();
                    break;
            }
        }

        #endregion

        #region === タブ1: 距離ベース最適化 ===

        private void DrawDistanceBasedTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("=== 距離ベース最適化 ===", EditorStyles.boldLabel);

            // 統計情報
            DrawStatistics();

            EditorGUILayout.Space(10);

            // 実行ボタン
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("距離に応じてコンポーネントを無効化", GUILayout.Height(40)))
            {
                ExecuteDistanceOptimization();
            }

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("すべてのコンポーネントを有効化", GUILayout.Height(40)))
            {
                ExecuteEnableAll();
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();

            // ヘルプ
            EditorGUILayout.HelpBox(
                "基準オブジェクトから指定距離以上離れたコンポーネントを無効化します。\n" +
                "シーンビューで視覚的に確認できます。",
                MessageType.Info);
        }

        private void DrawStatistics()
        {
            MapOptimizationController.OptimizationStatistics stats = _target.GetStatistics();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("統計情報", EditorStyles.boldLabel);

            // Collider
            if (stats.totalColliders > 0)
            {
                EditorGUILayout.LabelField($"Collider: {stats.totalColliders}個");
                EditorGUILayout.LabelField($"  有効: {stats.enabledColliders}", GreenStyle());
                EditorGUILayout.LabelField($"  無効: {stats.disabledColliders}", GrayStyle());
                if (stats.colliderTargets > 0)
                {
                    EditorGUILayout.LabelField($"  無効化対象: {stats.colliderTargets}", RedStyle());
                }
                EditorGUILayout.Space(3);
            }

            // Renderer
            if (stats.totalRenderers > 0)
            {
                EditorGUILayout.LabelField($"Renderer: {stats.totalRenderers}個");
                EditorGUILayout.LabelField($"  有効: {stats.enabledRenderers}", GreenStyle());
                EditorGUILayout.LabelField($"  無効: {stats.disabledRenderers}", GrayStyle());
                if (stats.rendererTargets > 0)
                {
                    EditorGUILayout.LabelField($"  無効化対象: {stats.rendererTargets}", RedStyle());
                }
                EditorGUILayout.Space(3);
            }

            // Light
            if (stats.totalLights > 0)
            {
                EditorGUILayout.LabelField($"Light: {stats.totalLights}個");
                EditorGUILayout.LabelField($"  有効: {stats.enabledLights}", GreenStyle());
                EditorGUILayout.LabelField($"  無効: {stats.disabledLights}", GrayStyle());
                EditorGUILayout.Space(3);
            }

            // AudioSource
            if (stats.totalAudioSources > 0)
            {
                EditorGUILayout.LabelField($"AudioSource: {stats.totalAudioSources}個");
                EditorGUILayout.LabelField($"  有効: {stats.enabledAudioSources}", GreenStyle());
                EditorGUILayout.LabelField($"  無効: {stats.disabledAudioSources}", GrayStyle());
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region === タブ2: LOD ===

        private void DrawLODTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("=== LOD自動設定 ===", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "メッシュレンダラーに自動的にLODGroupを追加します。\n" +
                "距離に応じてメッシュの詳細度を切り替えることで描画負荷を軽減します。",
                MessageType.Info);

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.5f, 0.5f, 1f);
            if (GUILayout.Button("LODを自動設定", GUILayout.Height(40)))
            {
                ExecuteLODSetup();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region === タブ3: Static Batching ===

        private void DrawStaticBatchingTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("=== Static Batching支援 ===", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "動かないオブジェクトを自動検出してStaticフラグを設定します。\n" +
                "ドローコールを削減し、描画パフォーマンスが向上します。",
                MessageType.Info);

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(1f, 0.8f, 0.5f);
            if (GUILayout.Button("Static Batchingを設定", GUILayout.Height(40)))
            {
                ExecuteStaticBatching();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();

            // 詳細情報
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("判定条件", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("以下のコンポーネントがないオブジェクトをStaticと判定:");
            EditorGUILayout.LabelField("• Rigidbody");
            EditorGUILayout.LabelField("• Animator");
            EditorGUILayout.LabelField("• CharacterController");
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region === タブ4: Lightmap ===

        private void DrawLightmapTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("=== Lightmap Static設定 ===", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "静的オブジェクトにLightmap Staticフラグを設定します。\n" +
                "ライトベイク後、リアルタイムライト計算が不要になります。",
                MessageType.Info);

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(1f, 1f, 0.5f);
            if (GUILayout.Button("Lightmap Staticを設定", GUILayout.Height(40)))
            {
                ExecuteLightmapStatic();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();

            // 追加情報
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("次のステップ", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. Window > Rendering > Lighting を開く");
            EditorGUILayout.LabelField("2. Generate Lighting をクリック");
            EditorGUILayout.LabelField("3. ライトマップがベイクされます");
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region === タブ5: Occlusion Culling ===

        private void DrawOcclusionTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("=== Occlusion Culling支援 ===", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "カメラから見えないオブジェクトの描画をスキップします。\n" +
                "Occlusion Cullingのベイク設定を支援します。",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // 情報取得
            if (GUILayout.Button("Occlusion情報を表示", GUILayout.Height(30)))
            {
                ShowOcclusionInfo();
            }

            EditorGUILayout.EndVertical();

            // ベイク手順
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Occlusion Cullingベイク手順", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. Window > Rendering > Occlusion Culling を開く");
            EditorGUILayout.LabelField("2. Bake タブで設定を調整");
            EditorGUILayout.LabelField("3. Bake ボタンをクリック");
            EditorGUILayout.LabelField("4. ベイク完了後、Visualization で確認");
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region === 実行メソッド ===

        /// <summary>
        /// 距離ベース最適化を実行
        /// </summary>
        private void ExecuteDistanceOptimization()
        {
            RecordUndo("Distance Optimization");

            MapOptimizationController.OptimizationResult result = _target.OptimizeByDistance();

            string message = "最適化完了:\n";
            if (result.collidersDisabled > 0)
            {
                message += $"• Collider: {result.collidersDisabled}個無効化\n";
            }
            if (result.renderersDisabled > 0)
            {
                message += $"• Renderer: {result.renderersDisabled}個無効化\n";
            }
            if (result.lightsDisabled > 0)
            {
                message += $"• Light: {result.lightsDisabled}個無効化\n";
            }
            if (result.audioSourcesDisabled > 0)
            {
                message += $"• AudioSource: {result.audioSourcesDisabled}個無効化\n";
            }

            Debug.Log($"[MapOptimization] {message}");
            EditorUtility.DisplayDialog("最適化完了", message, "OK");

            MarkDirtyAndRepaint();
        }

        /// <summary>
        /// 全コンポーネント有効化を実行
        /// </summary>
        private void ExecuteEnableAll()
        {
            RecordUndo("Enable All Components");

            MapOptimizationController.OptimizationResult result = _target.EnableAllComponents();

            string message = "有効化完了:\n";
            if (result.collidersEnabled > 0)
            {
                message += $"• Collider: {result.collidersEnabled}個有効化\n";
            }
            if (result.renderersEnabled > 0)
            {
                message += $"• Renderer: {result.renderersEnabled}個有効化\n";
            }
            if (result.lightsEnabled > 0)
            {
                message += $"• Light: {result.lightsEnabled}個有効化\n";
            }
            if (result.audioSourcesEnabled > 0)
            {
                message += $"• AudioSource: {result.audioSourcesEnabled}個有効化\n";
            }

            Debug.Log($"[MapOptimization] {message}");

            MarkDirtyAndRepaint();
        }

        /// <summary>
        /// LOD設定を実行
        /// </summary>
        private void ExecuteLODSetup()
        {
            RecordUndo("LOD Setup");

            MapOptimizationController.LODSetupResult result = _target.SetupLOD();

            string message = $"LOD設定完了:\n" +
                           $"• 設定: {result.setupCount}個\n" +
                           $"• スキップ: {result.skippedCount}個";

            Debug.Log($"[MapOptimization] {message}");
            EditorUtility.DisplayDialog("LOD設定完了", message, "OK");

            MarkDirtyAndRepaint();
        }

        /// <summary>
        /// Static Batching設定を実行
        /// </summary>
        private void ExecuteStaticBatching()
        {
            RecordUndo("Static Batching Setup");

            MapOptimizationController.StaticBatchingResult result = _target.SetupStaticBatching();

            string message = $"Static Batching設定完了:\n" +
                           $"• 新規Static化: {result.madeStaticCount}個\n" +
                           $"• 既にStatic: {result.alreadyStaticCount}個";

            Debug.Log($"[MapOptimization] {message}");
            EditorUtility.DisplayDialog("Static Batching完了", message, "OK");

            MarkDirtyAndRepaint();
        }

        /// <summary>
        /// Lightmap Static設定を実行
        /// </summary>
        private void ExecuteLightmapStatic()
        {
            RecordUndo("Lightmap Static Setup");

            MapOptimizationController.LightmapStaticResult result = _target.SetupLightmapStatic();

            string message = $"Lightmap Static設定完了:\n" +
                           $"• 設定: {result.setupCount}個\n" +
                           $"• スキップ: {result.skippedCount}個\n\n" +
                           "次は Window > Rendering > Lighting でライトをベイクしてください。";

            Debug.Log($"[MapOptimization] {message}");
            EditorUtility.DisplayDialog("Lightmap Static完了", message, "OK");

            MarkDirtyAndRepaint();
        }

        /// <summary>
        /// Occlusion情報を表示
        /// </summary>
        private void ShowOcclusionInfo()
        {
            MapOptimizationController.OcclusionCullingInfo info = _target.GetOcclusionCullingInfo();

            string message = $"Occlusion Culling情報:\n" +
                           $"• 総Renderer数: {info.totalRendererCount}個\n" +
                           $"• 推奨Area中心: {info.recommendedAreaCenter}\n" +
                           $"• 推奨Areaサイズ: {info.recommendedAreaSize}\n\n" +
                           "Window > Rendering > Occlusion Culling からベイクしてください。";

            Debug.Log($"[MapOptimization] {message}");
            EditorUtility.DisplayDialog("Occlusion情報", message, "OK");
        }

        #endregion

        #region === ユーティリティ ===

        /// <summary>
        /// Undo記録
        /// </summary>
        private void RecordUndo(string operationName)
        {
            Undo.RecordObject(_target.gameObject, operationName);

            // すべての子コンポーネントも記録
            Component[] allComponents = _target.GetComponentsInChildren<Component>(true);
            foreach (Component comp in allComponents)
            {
                if (comp != null)
                {
                    Undo.RecordObject(comp, operationName);
                }
            }
        }

        /// <summary>
        /// シーンをDirtyマークして再描画
        /// </summary>
        private void MarkDirtyAndRepaint()
        {
            EditorUtility.SetDirty(_target);
            SceneView.RepaintAll();
        }

        /// <summary>
        /// スタイル: 緑
        /// </summary>
        private GUIStyle GreenStyle()
        {
            return new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
        }

        /// <summary>
        /// スタイル: 赤
        /// </summary>
        private GUIStyle RedStyle()
        {
            return new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };
        }

        /// <summary>
        /// スタイル: グレー
        /// </summary>
        private GUIStyle GrayStyle()
        {
            return new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray } };
        }

        #endregion
    }
}
#endif