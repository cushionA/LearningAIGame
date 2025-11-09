using UnityEngine;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// BoxColliderの実際のサイズをデバッグ
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class BoxColliderDebugger : MonoBehaviour
    {
        [Header("表示設定")]
        [SerializeField]
        private Color gizmoColor = Color.green;

        [SerializeField]
        private bool showWorldSize = true;

        private BoxCollider boxCollider;
        private Rigidbody rb;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            AnalyzeCollider();
        }

        private void AnalyzeCollider()
        {
            Vector3 worldSize = GetWorldSize();
            Vector3 worldCenter = GetWorldCenter();

            Debug.Log("=== BoxCollider 分析 ===");
            Debug.Log($"Transform Scale: {transform.lossyScale}");
            Debug.Log($"Collider Size (設定): {boxCollider.size}");
            Debug.Log($"Collider Size (実際): {worldSize}");
            Debug.Log($"Collider Center (設定): {boxCollider.center}");
            Debug.Log($"Collider Center (実際): {worldCenter}");

            // 警告チェック
            if (worldSize.x < 0.1f || worldSize.y < 0.1f || worldSize.z < 0.1f)
            {
                Debug.LogError("⚠⚠⚠ コライダーが10cm未満です！すり抜けの原因になります！");
                Debug.LogError($"実際のサイズ: {worldSize.x:F4}m × {worldSize.y:F4}m × {worldSize.z:F4}m");
            }

            if (transform.lossyScale.x < 0.1f || transform.lossyScale.y < 0.1f || transform.lossyScale.z < 0.1f)
            {
                Debug.LogWarning("⚠ Transform Scale が 0.1 未満です。これが問題の原因かもしれません。");
                Debug.LogWarning("解決策: 親オブジェクトを作ってScaleを正規化してください。");
            }

            if (rb != null && rb.mass < 10f)
            {
                Debug.LogWarning($"⚠ Mass が {rb.mass} と軽すぎます。70に設定することを推奨します。");
            }
        }

        /// <summary>
        /// ワールド座標でのサイズを取得
        /// </summary>
        private Vector3 GetWorldSize()
        {
            return new Vector3(
                boxCollider.size.x * Mathf.Abs(transform.lossyScale.x),
                boxCollider.size.y * Mathf.Abs(transform.lossyScale.y),
                boxCollider.size.z * Mathf.Abs(transform.lossyScale.z)
            );
        }

        /// <summary>
        /// ワールド座標での中心を取得
        /// </summary>
        private Vector3 GetWorldCenter()
        {
            return transform.TransformPoint(boxCollider.center);
        }

        private void OnDrawGizmos()
        {
            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();

            // コライダーの実際の形を描画
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            Gizmos.matrix = Matrix4x4.identity;

            // ワールドサイズを表示
            if (showWorldSize)
            {
                Vector3 worldSize = GetWorldSize();
                Vector3 worldCenter = GetWorldCenter();

                // サイズが小さすぎる場合は赤で警告
                if (worldSize.x < 0.1f || worldSize.y < 0.1f || worldSize.z < 0.1f)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(worldCenter, worldSize);

                    // 警告マーク
                    Gizmos.DrawWireSphere(worldCenter, 0.05f);
                }

                // 原点を表示
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, 0.02f);

#if UNITY_EDITOR
                // サイズ情報をテキスト表示
                Vector3 textPos = worldCenter + Vector3.up * 0.5f;
                UnityEditor.Handles.Label(textPos,
                    $"実際のサイズ:\n{worldSize.x:F3}m × {worldSize.y:F3}m × {worldSize.z:F3}m\n" +
                    $"Scale: {transform.lossyScale}");
#endif
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log($"衝突: {collision.gameObject.name}");
        }

        private void OnCollisionExit(Collision collision)
        {
            Debug.Log($"衝突終了: {collision.gameObject.name}");
        }

        private void OnGUI()
        {
            if (boxCollider == null)
                return;

            Vector3 worldSize = GetWorldSize();

            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.Box("=== BoxCollider Debug ===");

            GUILayout.Label($"Transform Scale: {transform.lossyScale}");
            GUILayout.Label($"Position: {transform.position}");

            GUILayout.Space(10);
            GUILayout.Label("--- Collider Size ---");
            GUILayout.Label($"設定値: {boxCollider.size}");
            GUILayout.Label($"実際: {worldSize}");

            if (worldSize.magnitude < 0.3f)
            {
                GUILayout.Space(10);
                GUI.color = Color.red;
                GUILayout.Box("⚠⚠⚠ コライダーが小さすぎます！");
                GUILayout.Box("すり抜けの原因になっています！");
                GUI.color = Color.white;
            }

            if (rb != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("--- Rigidbody ---");
                GUILayout.Label($"Mass: {rb.mass}");
                GUILayout.Label($"Velocity: {rb.linearVelocity}");
                GUILayout.Label($"Position: {rb.position}");
            }

            GUILayout.EndArea();
        }
    }
}