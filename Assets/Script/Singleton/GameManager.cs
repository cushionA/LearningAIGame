using UnityEngine;

namespace LearningAIGame.CombatSystem.Singleton
{
    /// <summary>
    /// ゲーム全体を管理するシングルトンマネージャー
    /// シーン遷移時も破棄されない
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        /// <summary>
        /// GameManagerのシングルトンインスタンス
        /// </summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[GameManager] インスタンスが存在しません。シーンにGameManagerを配置してください。");
                }
                return _instance;
            }
        }

        /// <summary>
        /// インスタンスが存在するかチェック
        /// </summary>
        public static bool HasInstance => _instance != null;

        private void Awake()
        {
            // 既にインスタンスが存在する場合
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[GameManager] 既にインスタンスが存在します。重複したGameManagerを破棄します: {gameObject.name}");
                Destroy(gameObject);
                return;
            }

            // インスタンスを設定
            _instance = this;

            // シーン遷移時も破棄されないようにする
            DontDestroyOnLoad(gameObject);

            // 初期化処理
            Initialize();
        }

        private void OnDestroy()
        {
            // 自分自身が破棄される場合のみインスタンスをクリア
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// GameManagerの初期化処理
        /// </summary>
        private void Initialize()
        {
            // フレームレートを60に設定
            Application.targetFrameRate = 60;
        }
    }
}