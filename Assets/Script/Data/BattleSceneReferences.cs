using TMPro;
using UnityEngine;
using LearningAIGame.UI.Battle;
using LearningAIGame.CombatSystem.UI;

namespace LearningAIGame.Scene
{
    /// <summary>
    /// バトルシーンの参照をまとめた構造体
    /// BattleSceneInitializerで設定し、GameManagerに渡す
    /// </summary>
    [System.Serializable]
    public struct BattleSceneReferences
    {
        [Header("=== Score UI ===")]
        [Tooltip("プレイヤースコア表示テキスト")]
        public TextMeshProUGUI PlayerScoreText;

        [Tooltip("NPCスコア表示テキスト")]
        public TextMeshProUGUI NpcScoreText;

        [Header("=== Spawn Points ===")]
        [Tooltip("プレイヤーのスポーン位置")]
        public Transform PlayerSpawnPoint;

        [Tooltip("NPCのスポーン位置")]
        public Transform NpcSpawnPoint;

        [Header("=== Gauge UI ===")]
        [Tooltip("戦闘ゲージ用Canvas")]
        public Canvas BattleGaugeCanvas;

        [Tooltip("プレイヤー用ScreenSpaceゲージコントローラー")]
        public ScreenSpaceGaugeUIController PlayerGaugeController;

        [Tooltip("NPC用ScreenSpaceゲージコントローラー")]
        public ScreenSpaceGaugeUIController NpcGaugeController;

        /// <summary>
        /// 全参照が有効かチェック
        /// </summary>
        /// <param name="errorMessage">エラーメッセージ（エラーがない場合はnull）</param>
        /// <returns>全参照が有効な場合true</returns>
        public bool Validate(out string errorMessage)
        {
            var errors = new System.Text.StringBuilder();
            int errorCount = 0;

            if (PlayerScoreText == null)
            {
                errors.AppendLine("  - PlayerScoreTextが未設定");
                errorCount++;
            }
            if (NpcScoreText == null)
            {
                errors.AppendLine("  - NpcScoreTextが未設定");
                errorCount++;
            }
            if (PlayerSpawnPoint == null)
            {
                errors.AppendLine("  - PlayerSpawnPointが未設定");
                errorCount++;
            }
            if (NpcSpawnPoint == null)
            {
                errors.AppendLine("  - NpcSpawnPointが未設定");
                errorCount++;
            }
            if (BattleGaugeCanvas == null)
            {
                errors.AppendLine("  - BattleGaugeCanvasが未設定");
                errorCount++;
            }
            if (PlayerGaugeController == null)
            {
                errors.AppendLine("  - PlayerGaugeControllerが未設定");
                errorCount++;
            }
            if (NpcGaugeController == null)
            {
                errors.AppendLine("  - NpcGaugeControllerが未設定");
                errorCount++;
            }

            if (errorCount > 0)
            {
                errorMessage = $"{errorCount}個の参照が未設定:\n{errors}";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}