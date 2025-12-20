using UnityEngine;

namespace LearningAIGame.EditorTools
{
    /// <summary>
    /// 基準オブジェクトからの距離に応じてColliderを無効化するエディタツール
    /// シーンビューで視覚化しながら調整可能
    /// </summary>
    [ExecuteInEditMode]
    public class ColliderDistanceController : MonoBehaviour
    {
        #region === インスペクター設定 ===

        [Header("基準設定")]
        [SerializeField]
        [Tooltip("距離判定の基準となるオブジェクト")]
        private Transform referenceObject;

        [SerializeField]
        [Tooltip("この距離以上離れているColliderを無効化します")]
        [Min(0f)]
        private float distanceThreshold = 10f;

        [Header("視覚化設定")]
        [SerializeField]
        [Tooltip("シーンビューで範囲を表示するか")]
        private bool showGizmos = true;

        [SerializeField]
        [Tooltip("範囲球体の色")]
        private Color gizmoColor = new Color(0f, 1f, 0f, 0.2f);

        #endregion

        #region === Gizmo描画 ===

        /// <summary>
        /// シーンビューでの視覚化
        /// - 基準オブジェクトを中心とした閾値範囲の球体
        /// - 各Colliderの状態を色分け表示
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos || referenceObject == null)
            {
                return;
            }

            // 閾値範囲の球体を描画
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(referenceObject.position, distanceThreshold);

            // 各Colliderの状態を視覚化
            Collider[] colliders = this.GetComponentsInChildren<Collider>(true);

            foreach (Collider col in colliders)
            {
                if (col == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(referenceObject.position, col.transform.position);
                Vector3 colliderPos = col.transform.position;

                // 状態に応じて色分け
                if (!col.enabled)
                {
                    // グレー: 既に無効化済み
                    Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                }
                else if (distance >= distanceThreshold)
                {
                    // 赤: 無効化対象
                    Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
                }
                else
                {
                    // 緑: 閾値内（有効維持）
                    Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
                }

                // Colliderの位置に小さな球体を描画
                Gizmos.DrawSphere(colliderPos, 0.1f);

                // 基準オブジェクトとの線を描画
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawLine(referenceObject.position, colliderPos);
            }
        }

        #endregion

        #region === Public Methods (Editorから呼び出し) ===

        /// <summary>
        /// 距離に応じてColliderを無効化する
        /// </summary>
        /// <returns>無効化したColliderの数</returns>
        public int DisableCollidersByDistance()
        {
            if (referenceObject == null)
            {
                Debug.LogError("[ColliderDistanceController] 基準オブジェクトが設定されていません");
                return 0;
            }

            Collider[] colliders = this.GetComponentsInChildren<Collider>(true);
            int disabledCount = 0;

            foreach (Collider col in colliders)
            {
                if (col == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(referenceObject.position, col.transform.position);

                if (distance >= distanceThreshold && col.enabled)
                {
                    col.enabled = false;
                    disabledCount++;
                }
            }

            return disabledCount;
        }

        /// <summary>
        /// すべてのColliderを有効化する
        /// </summary>
        /// <returns>有効化したColliderの数</returns>
        public int EnableAllColliders()
        {
            Collider[] colliders = this.GetComponentsInChildren<Collider>(true);
            int enabledCount = 0;

            foreach (Collider col in colliders)
            {
                if (col == null)
                {
                    continue;
                }

                if (!col.enabled)
                {
                    col.enabled = true;
                    enabledCount++;
                }
            }

            return enabledCount;
        }

        /// <summary>
        /// 現在の状態を取得（デバッグ用）
        /// </summary>
        public ColliderStatistics GetStatistics()
        {
            if (referenceObject == null)
            {
                return new ColliderStatistics();
            }

            Collider[] colliders = this.GetComponentsInChildren<Collider>(true);
            ColliderStatistics stats = new ColliderStatistics();

            foreach (Collider col in colliders)
            {
                if (col == null)
                {
                    continue;
                }

                stats.totalCount++;

                if (col.enabled)
                {
                    stats.enabledCount++;

                    float distance = Vector3.Distance(referenceObject.position, col.transform.position);
                    if (distance >= distanceThreshold)
                    {
                        stats.targetCount++;
                    }
                }
                else
                {
                    stats.disabledCount++;
                }
            }

            return stats;
        }

        #endregion

        #region === 入れ子構造体 ===

        /// <summary>
        /// Colliderの統計情報
        /// </summary>
        public struct ColliderStatistics
        {
            public int totalCount;       // 総数
            public int enabledCount;     // 有効な数
            public int disabledCount;    // 無効な数
            public int targetCount;      // 無効化対象の数（閾値外かつ有効）
        }

        #endregion
    }
}