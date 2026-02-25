using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
namespace LearningAIGame.EditorTools
{
    /// <summary>
    /// ColliderDistanceController のカスタムインスペクタ
    /// </summary>
    [CustomEditor(typeof(ColliderDistanceController))]
    public class ColliderDistanceControllerEditor : Editor
    {
        #region === フィールド ===

        private ColliderDistanceController _target;

        #endregion

        #region === Unity Callbacks ===

        private void OnEnable()
        {
            _target = (ColliderDistanceController)target;
        }

        public override void OnInspectorGUI()
        {
            // デフォルトのインスペクタ表示
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // 統計情報の表示
            DrawStatistics();

            EditorGUILayout.Space(10);

            // 実行ボタン
            DrawActionButtons();

            EditorGUILayout.Space(5);

            // ヘルプボックス
            DrawHelpBox();
        }

        #endregion

        #region === GUI描画メソッド ===

        /// <summary>
        /// 統計情報を表示
        /// </summary>
        private void DrawStatistics()
        {
            ColliderDistanceController.ColliderStatistics stats = _target.GetStatistics();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("=== Collider統計情報 ===", EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"総数: {stats.totalCount}");
            EditorGUILayout.LabelField($"有効: {stats.enabledCount}", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } });
            EditorGUILayout.LabelField($"無効: {stats.disabledCount}", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray } });

            if (stats.targetCount > 0)
            {
                EditorGUILayout.LabelField($"無効化対象: {stats.targetCount}", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } });
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 実行ボタンを表示
        /// </summary>
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 無効化ボタン
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("距離に応じてColliderを無効化", GUILayout.Height(30)))
            {
                ExecuteDisableColliders();
            }

            EditorGUILayout.Space(5);

            // 全有効化ボタン
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("すべてのColliderを有効化", GUILayout.Height(30)))
            {
                ExecuteEnableAllColliders();
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// ヘルプボックスを表示
        /// </summary>
        private void DrawHelpBox()
        {
            EditorGUILayout.HelpBox(
                "シーンビューで範囲を視覚的に確認できます:\n" +
                "• 緑: 閾値内（有効維持）\n" +
                "• 赤: 閾値外（無効化対象）\n" +
                "• グレー: 既に無効化済み",
                MessageType.Info);
        }

        #endregion

        #region === 実行メソッド ===

        /// <summary>
        /// Collider無効化を実行
        /// </summary>
        private void ExecuteDisableColliders()
        {
            // Undo記録
            Undo.RecordObject(_target.gameObject, "Disable Colliders by Distance");

            // 子オブジェクトのすべてのColliderも記録
            Collider[] colliders = _target.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                if (col != null)
                {
                    Undo.RecordObject(col, "Disable Collider");
                }
            }

            // 実行
            int disabledCount = _target.DisableCollidersByDistance();

            // 結果をログ出力
            Debug.Log($"[ColliderDistanceController] {disabledCount}個のColliderを無効化しました");

            // シーンを変更済みとしてマーク
            EditorUtility.SetDirty(_target);

            // シーンビューを再描画
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 全Collider有効化を実行
        /// </summary>
        private void ExecuteEnableAllColliders()
        {
            // Undo記録
            Undo.RecordObject(_target.gameObject, "Enable All Colliders");

            // 子オブジェクトのすべてのColliderも記録
            Collider[] colliders = _target.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                if (col != null)
                {
                    Undo.RecordObject(col, "Enable Collider");
                }
            }

            // 実行
            int enabledCount = _target.EnableAllColliders();

            // 結果をログ出力
            Debug.Log($"[ColliderDistanceController] {enabledCount}個のColliderを有効化しました");

            // シーンを変更済みとしてマーク
            EditorUtility.SetDirty(_target);

            // シーンビューを再描画
            SceneView.RepaintAll();
        }

        #endregion
    }
}
#endif