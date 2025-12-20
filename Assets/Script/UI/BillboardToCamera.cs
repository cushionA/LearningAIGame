using UnityEngine;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// 指定したカメラの正面に向き続けるシンプルなビルボード
    /// UIの表示が逆にならないように背面をカメラに向ける
    /// </summary>
    public class BillboardToCamera : MonoBehaviour
    {
        #region === インスペクター設定 ===

        [Header("カメラ設定")]
        [Tooltip("対象カメラ（nullの場合はMainCamera）")]
        [SerializeField] private Camera _targetCamera;

        #endregion

        #region === Unity ライフサイクル ===

        private void Start()
        {
            // カメラが未設定の場合はMainCameraを取得
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            if (_targetCamera == null)
                return;

            // カメラへの方向を計算
            Vector3 directionToCamera = _targetCamera.transform.position - transform.position;

            // Y軸方向の成分を0にして水平方向のみ考慮
            directionToCamera.y = 0f;

            // 方向がほぼゼロの場合はスキップ
            if (directionToCamera.sqrMagnitude < 0.001f)
                return;

            // その方向を向く（背面がカメラ側、正面がプレイヤー側）
            transform.rotation = Quaternion.LookRotation(directionToCamera);
        }

        #endregion
    }
}