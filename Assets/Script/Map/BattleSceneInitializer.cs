using UnityEngine;
using LearningAIGame.CombatSystem.Singleton;

namespace LearningAIGame.Scene
{
    /// <summary>
    /// バトルシーンの初期化を行うクラス
    /// シーンロード時にGameManagerへ参照を登録する
    /// </summary>
    public class BattleSceneInitializer : MonoBehaviour
    {
        #region Inspector設定

        [SerializeField]
        private BattleSceneReferences _references;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            RegisterToGameManager();
        }

        private void OnDestroy()
        {
            UnregisterFromGameManager();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// GameManagerへ参照を登録
        /// </summary>
        private void RegisterToGameManager()
        {
            if (!GameManager.HasInstance)
            {
                Debug.LogError("[BattleSceneInitializer] GameManagerが存在しません。BaseSceneを先にロードしてください。");
                return;
            }

            if (!_references.Validate(out var errorMessage))
            {
                Debug.LogError($"[BattleSceneInitializer] {errorMessage}");
                return;
            }

            GameManager.Instance.RegisterBattleReferences(_references);
        }

        /// <summary>
        /// GameManagerから参照を解除
        /// </summary>
        private void UnregisterFromGameManager()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.UnregisterBattleReferences();
            }
        }

        #endregion
    }
}