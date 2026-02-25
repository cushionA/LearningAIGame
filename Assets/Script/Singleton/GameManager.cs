using Cysharp.Threading.Tasks;
using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Systems;
using LearningAIGame.Scene;
using LearningAIGame.UI.Battle;
using LearningAIGame.UI.Common;
using LLMDataArchitect;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace LearningAIGame.CombatSystem.Singleton
{
    /// <summary>
    /// ゲーム状態
    /// </summary>
    public enum GameState
    {
        None,
        Title,
        Battle,
        Pause,
        Result
    }

    /// <summary>
    /// 敵キャラ設定
    /// </summary>
    [Serializable]
    public class EnemyBattleData
    {
        [Tooltip("敵キャラのプレハブ")]
        public GameObject EnemyPrefab;

        [Tooltip("この敵との勝利に必要なラウンド数")]
        public int RequiredWins = 2;

        [Tooltip("敵の名前（UI表示用）")]
        public string EnemyName;
    }

    /// <summary>
    /// ゲーム全体を管理するシングルトンマネージャー
    /// BaseSceneに常駐し、サブシーンをAdditive方式で管理する
    /// 
    /// シーン構成:
    /// [BaseScene] ← 常駐（GameManager、共通システム）
    ///     ├── [TitleScene] Additive
    ///     ├── [BattleScene] Additive
    ///     └── [ResultScene] Additive
    /// 
    /// 主な遷移:
    /// [Title] → ゲーム開始 → [Battle1] → [Battle2] → ... → [Result] → タイトルへ / リトライ
    /// 
    /// UI表示状態:
    /// [Title]  : RetryMenu=非表示, BattleGauge=非表示
    /// [Battle] : RetryMenu=非表示, BattleGauge=表示
    /// [Result] : RetryMenu=表示,   BattleGauge=非表示
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region 定数

        private const string k_TitleSceneName = "Title";
        private const string k_BattleSceneName = "BattleScene";
        private const string k_ResultSceneName = "Result";

        #endregion

        #region Inspector設定

        [Header("=== Base Scene References ===")]
        [SerializeField]
        private GameProgressUIController _uiController;

        [SerializeField]
        private RetryMenuUIController _retryMenuUIController;

        [SerializeField]
        private DescriptionUIController _descriptionUIController;

        [Header("=== Character Settings ===")]
        [Tooltip("プレイヤーキャラのプレハブ")]
        [SerializeField]
        private GameObject _playerPrefab;

        [Tooltip("敵キャラのバトル設定リスト（順番に戦う）")]
        [SerializeField]
        private EnemyBattleData[] _enemyBattleDataList;

        [Header("=== Audio ===")]
        [SerializeField]
        private AudioSource _seAudioSource;

        [SerializeField]
        private AudioClip _buttonClickSE;

        [SerializeField]
        private AudioClip _buttonHoverSE;

        [SerializeField]
        private AudioClip _congratulationSE;

        [SerializeField]
        private AudioClip _entranceSE;

        [SerializeField]
        private AudioClip _roundAnnounceSE;

        [SerializeField]
        private AudioClip _battleStartSE;

        [SerializeField]
        private AudioClip _roundWinSE;

        [SerializeField]
        private AudioClip _roundLoseSE;

        [SerializeField]
        private AudioClip _battleWinSE;

        [SerializeField]
        private AudioClip _battleLoseSE;

        [SerializeField, Range(0f, 1f)]
        private float _seVolume = 1f;

        [Header("=== Settings ===")]
        [SerializeField]
        private float _sceneTransitionDuration = 0.5f;

        [Header("=== Events - Pause ===")]
        [SerializeField]
        private UnityEvent _onOpenPause;

        [SerializeField]
        private UnityEvent _onClosePause;

        /// <summary>
        /// リザルト画面のカメラ位置（シーン遷移時にカメラをここに移動してからフェードインする）
        /// </summary>
        [SerializeField]
        private Transform _resultCameraPlace;

        /// <summary>
        /// タイトル画面のカメラ位置（シーン遷移時にカメラをここに移動してからフェードインする）
        /// </summary>
        [SerializeField]
        private Transform _titleCameraPlace;

        /// <summary>
        /// これが真の場合、デバッグ用にLLMの応答を保存します。
        /// </summary>
        public bool IsDebugMode = false;

        #endregion

        #region Runtime References

        private GameObject _player;
        private GameObject _npc;

        #endregion

        #region Private Fields

        private GameState _currentState = GameState.None;
        private GameState _stateBeforePause = GameState.None;
        private string _currentSubSceneName = null;
        private int _currentBattleIndex = 0;
        private int _playerScoreNum = 0;
        private int _npcScoreNum = 0;
        private int _totalBattleWins = 0;
        private int _retryCount = 0;
        private List<IGameHelper> _gameManagerHelper;
        private GameObject _winner;
        private bool _isTransitioning = false;
        private bool _isBattleActive = false;
        private float _battleStartTime = 0f;
        private float _clearTime = 0f;
        private float _pauseStartTime = 0f;
        private float _totalPausedTime = 0f;
        private static GameManager _instance;
        private LLMCommunicator _communicator;

        // バトルシーン参照（構造体で一括管理）
        [SerializeField]
        [ReadOnly]
        private BattleSceneReferences _battleRefs;

        #endregion

        #region Properties

        /// <summary>
        /// GameManagerのシングルトンインスタンス
        /// </summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[GameManager] インスタンスが存在しません。BaseSceneにGameManagerを配置してください。");
                }
                return _instance;
            }
        }

        /// <summary>
        /// インスタンスが存在するかチェック
        /// </summary>
        public static bool HasInstance => _instance != null;

        /// <summary>
        /// 現在のゲーム状態
        /// </summary>
        public GameState CurrentState => _currentState;

        /// <summary>
        /// プレイヤーのスコア（現在のバトル内）
        /// </summary>
        public int PlayerScore => _playerScoreNum;

        /// <summary>
        /// NPCのスコア（現在のバトル内）
        /// </summary>
        public int NpcScore => _npcScoreNum;

        /// <summary>
        /// 現在のバトルインデックス
        /// </summary>
        public int CurrentBattleIndex => _currentBattleIndex;

        /// <summary>
        /// 総バトル数
        /// </summary>
        public int TotalBattleCount => _enemyBattleDataList.Length;

        /// <summary>
        /// 総バトル勝利数
        /// </summary>
        public int TotalBattleWins => _totalBattleWins;

        /// <summary>
        /// リトライ回数
        /// </summary>
        public int RetryCount => _retryCount;

        /// <summary>
        /// 最後の勝者（プレイヤーが勝ったかどうか）
        /// </summary>
        public bool IsPlayerWinner => _winner == _player;

        /// <summary>
        /// シーン遷移中かどうか
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// ポーズ中かどうか
        /// </summary>
        public bool IsPaused => _currentState == GameState.Pause;

        /// <summary>
        /// バトル中（ポーズ可能）かどうか
        /// </summary>
        public bool IsBattleActive => _isBattleActive;

        /// <summary>
        /// 現在の敵データ
        /// </summary>
        public EnemyBattleData CurrentEnemyData =>
            _currentBattleIndex < _enemyBattleDataList.Length
                ? _enemyBattleDataList[_currentBattleIndex]
                : null;

        /// <summary>
        /// 現在のNPCインスタンス
        /// </summary>
        public GameObject CurrentNpcInstance => _npc;

        /// <summary>
        /// プレイヤーインスタンス
        /// </summary>
        public GameObject PlayerInstance => _player;

        /// <summary>
        /// 現在のバトルに必要な勝利数
        /// </summary>
        public int CurrentRequiredWins => CurrentEnemyData?.RequiredWins ?? 3;

        /// <summary>
        /// 確定したクリア時間（秒）
        /// </summary>
        public float ClearTime => _clearTime;

        /// <summary>
        /// 現在の経過時間（秒）- ポーズ時間を除外
        /// </summary>
        public float CurrentElapsedTime
        {
            get
            {
                if (_battleStartTime <= 0f)
                    return 0f;

                float elapsed = Time.realtimeSinceStartup - _battleStartTime - _totalPausedTime;

                if (_currentState == GameState.Pause)
                {
                    elapsed -= (Time.realtimeSinceStartup - _pauseStartTime);
                }

                return Mathf.Max(0f, elapsed);
            }
        }

        /// <summary>
        /// クリア時間をフォーマット済み文字列で取得（MM:SS.ss）
        /// </summary>
        public string ClearTimeFormatted => FormatTime(_clearTime);

        /// <summary>
        /// 現在の経過時間をフォーマット済み文字列で取得（MM:SS.ss）
        /// </summary>
        public string CurrentElapsedTimeFormatted => FormatTime(CurrentElapsedTime);

        /// <summary>
        /// LLM通信コンポーネント
        /// </summary>
        public LLMCommunicator LLMCommunicator => _communicator;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[GameManager] 既にインスタンスが存在します。重複したGameManagerを破棄します: {gameObject.name}");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            Application.targetFrameRate = 60;
        }

        /// <summary>
        /// 開始処理
        /// </summary>
        private void Start()
        {
            Debug.Log($"[GameManager] Start - _uiController: {_uiController != null}");

            if (_uiController != null)
            {
                Debug.Log("[GameManager] BlackoutImmediate 呼び出し前");
                _uiController.BlackoutImmediate();
                Debug.Log("[GameManager] BlackoutImmediate 呼び出し後");
            }
            else
            {
                Debug.LogError("[GameManager] _uiController が null です！");
            }

            SetUIState_Title();
            LoadTitleSceneAsync().Forget();

            _communicator = this.GetComponent<LLMCommunicator>();
        }

        /// <summary>
        /// 破棄時処理
        /// </summary>
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region UI State Control

        /// <summary>
        /// タイトル画面時のUI状態を設定
        /// </summary>
        private void SetUIState_Title()
        {
            if (_retryMenuUIController != null && _currentState != GameState.Result)
            {
                _retryMenuUIController.HideImmediate();
                SetBattleGaugeVisible(false);
                Debug.Log("[GameManager] UI状態: Title（RetryMenu=非表示, BattleGauge=非表示）");
            }
        }

        /// <summary>
        /// 戦闘開始時のUI状態を設定
        /// </summary>
        private void SetUIState_BattleStart()
        {
            if (_retryMenuUIController != null)
            {
                _retryMenuUIController.HideImmediate();
            }

            SetBattleGaugeVisible(true);
            Debug.Log("[GameManager] UI状態: BattleStart（RetryMenu=非表示, BattleGauge=表示）");
        }

        /// <summary>
        /// リザルト画面時のUI状態を設定
        /// </summary>
        /// <param name="isWin">プレイヤーが勝利したかどうか</param>
        private async UniTask SetUIState_ResultAsync(bool isWin)
        {
            SetBattleGaugeVisible(false);

            if (_retryMenuUIController != null)
            {
                if (isWin)
                {
                    _retryMenuUIController.SetRetryLabel("最初から");
                    _retryMenuUIController.SetRetryFromBeginning(true);
                }
                else
                {
                    _retryMenuUIController.SetRetryLabel("リトライ");
                    _retryMenuUIController.SetRetryFromBeginning(false);
                }
                await _retryMenuUIController.ShowAsync();
            }

            Debug.Log($"[GameManager] UI状態: Result（RetryMenu=表示, BattleGauge=非表示, Win={isWin}）");
        }

        /// <summary>
        /// 戦闘ゲージCanvasの表示/非表示を設定
        /// </summary>
        /// <param name="visible">表示するかどうか</param>
        private void SetBattleGaugeVisible(bool visible)
        {
            if (_battleRefs.BattleGaugeCanvas != null)
            {
                _battleRefs.BattleGaugeCanvas.enabled = visible;
            }
        }

        #endregion

        #region Public API - Audio

        /// <summary>
        /// ボタンクリック音を再生
        /// </summary>
        public void PlayButtonClickSE() => PlaySE(_buttonClickSE);

        /// <summary>
        /// ボタンホバー音を再生
        /// </summary>
        public void PlayButtonHoverSE() => PlaySE(_buttonHoverSE);

        /// <summary>
        /// 勝利の音を再生
        /// </summary>
        public void PlayCongratulationSE() => PlaySE(_congratulationSE);

        /// <summary>
        /// ラウンド表示音を再生
        /// </summary>
        public void PlayRoundAnnounceSE() => PlaySE(_roundAnnounceSE);

        /// <summary>
        /// 戦闘開始音を再生
        /// </summary>
        public void PlayBattleStartSE() => PlaySE(_battleStartSE);

        /// <summary>
        /// ラウンド勝利音を再生
        /// </summary>
        public void PlayRoundWinSE() => PlaySE(_roundWinSE);

        /// <summary>
        /// バトル勝利音を再生
        /// </summary>
        public void PlayBattleWinSE() => PlaySE(_battleWinSE);

        /// <summary>
        /// ラウンド敗北音を再生
        /// </summary>
        public void PlayRoundLoseSE() => PlaySE(_roundLoseSE);

        /// <summary>
        /// バトル敗北音を再生
        /// </summary>
        public void PlayBattleLoseSE() => PlaySE(_battleLoseSE);

        /// <summary>
        /// SE再生（汎用）
        /// </summary>
        /// <param name="clip">再生するオーディオクリップ</param>
        public void PlaySE(AudioClip clip)
        {
            if (_seAudioSource == null || clip == null)
                return;
            _seAudioSource.PlayOneShot(clip, _seVolume);
        }

        #endregion

        #region Public API - タイトル画面ボタン用

        /// <summary>
        /// ゲーム開始（タイトル → バトル）
        /// </summary>
        public void StartGame()
        {
            if (_currentState != GameState.Title || _isTransitioning)
            {
                Debug.LogWarning("[GameManager] タイトル画面以外またはシーン遷移中にStartGameが呼ばれました");
                return;
            }

            PlayButtonClickSE();
            Debug.Log("[GameManager] ゲーム開始");
            ResetAllProgress();
            StartBattleAtIndex(0);
        }

        /// <summary>
        /// ゲーム終了
        /// </summary>
        public void ExitGame()
        {
            PlayButtonClickSE();
            Debug.Log("[GameManager] ゲーム終了");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 遊び方画面を開く
        /// </summary>
        public void OpenHowToPlay()
        {
            PlayButtonClickSE();
            Debug.Log("[GameManager] 遊び方を開く");
            _descriptionUIController.Open();
        }

        #endregion

        #region Public API - ポーズ画面用

        /// <summary>
        /// ポーズ状態をトグル
        /// </summary>
        public void TogglePause()
        {
            Debug.Log("[GameManager] ポーズ状態をトグル");
            if (_currentState == GameState.Pause)
            {
                ClosePause();
            }
            else
            {
                OpenPause();
            }
        }

        /// <summary>
        /// ポーズ画面を開く
        /// </summary>
        private void OpenPause()
        {
            if (_currentState != GameState.Battle || !_isBattleActive)
            {
                Debug.LogWarning("[GameManager] バトル中以外ではポーズできません");
                return;
            }

            PlayButtonClickSE();
            Debug.Log("[GameManager] ポーズを開く");

            _stateBeforePause = _currentState;
            _currentState = GameState.Pause;
            Time.timeScale = 0f;
            _pauseStartTime = Time.realtimeSinceStartup;

            if (_retryMenuUIController != null)
            {
                _retryMenuUIController.ShowImmediate();
            }
        }

        /// <summary>
        /// ポーズ画面を閉じる
        /// </summary>
        private void ClosePause()
        {
            if (_currentState != GameState.Pause)
            {
                Debug.LogWarning("[GameManager] ポーズ中以外ではポーズ解除できません");
                return;
            }

            PlayButtonClickSE();
            Debug.Log("[GameManager] ポーズを閉じる");

            _currentState = _stateBeforePause;
            Time.timeScale = 1f;
            _totalPausedTime += Time.realtimeSinceStartup - _pauseStartTime;

            if (_retryMenuUIController != null)
            {
                _retryMenuUIController.HideImmediate();
            }
        }

        #endregion

        #region Public API - 敗北画面/リザルト画面ボタン用

        /// <summary>
        /// タイトルへ戻る（どの画面からでも使用可能）
        /// </summary>
        public async Task ReturnToTitle()
        {
            if (_isTransitioning)
                return;

            PlayButtonClickSE();

            if (_currentState == GameState.Pause)
            {
                Time.timeScale = 1f;
            }

            _isBattleActive = false;

            if (_retryMenuUIController != null)
            {
                _retryMenuUIController.HideImmediate();
            }

            Debug.Log("[GameManager] タイトルへ戻る");
            await TransitionToSceneAsync(k_TitleSceneName, GameState.Title);

            // カメラ位置をタイトル用に
            _titleCameraPlace.GetPositionAndRotation(out Vector3 titleCamPos, out Quaternion titleCamRot);
            Camera.main.transform.SetLocalPositionAndRotation(titleCamPos, titleCamRot);


            if (_uiController != null)
            {
                await _uiController.BlackoutReleaseAsync();
            }
        }

        /// <summary>
        /// リトライ（現在のバトルを最初からやり直し）
        /// </summary>
        public void RetryCurrentBattle()
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[GameManager] シーン遷移中にRetryCurrentBattleが呼ばれました");
                return;
            }

            PlayButtonClickSE();

            if (_currentState == GameState.Pause)
            {
                Time.timeScale = 1f;
            }

            if (_retryMenuUIController != null)
            {
                _retryMenuUIController.HideImmediate();
            }

            _retryCount++;
            Debug.Log($"[GameManager] リトライ（バトル {_currentBattleIndex + 1}）リトライ回数: {_retryCount}");

            ResetCurrentBattleScore();
            StartBattleAtIndex(_currentBattleIndex);
        }

        /// <summary>
        /// 最初からリトライ（バトル1からやり直し）
        /// </summary>
        public void RetryFromBeginning()
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[GameManager] シーン遷移中にRetryFromBeginningが呼ばれました");
                return;
            }

            PlayButtonClickSE();

            if (_currentState == GameState.Pause)
            {
                Time.timeScale = 1f;
            }

            if (_retryMenuUIController != null)
            {
                _retryMenuUIController.HideImmediate();
            }

            Debug.Log("[GameManager] 最初からリトライ");
            ResetAllProgress();
            StartBattleAtIndex(0);
        }

        #endregion

        #region Public API - バトル開始

        /// <summary>
        /// 指定したバトルインデックスでバトルを開始する
        /// </summary>
        /// <param name="battleIndex">開始するバトルのインデックス（0始まり）</param>
        public void StartBattleAtIndex(int battleIndex)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[GameManager] シーン遷移中にStartBattleAtIndexが呼ばれました");
                return;
            }

            if (battleIndex < 0 || battleIndex >= _enemyBattleDataList.Length)
            {
                Debug.LogError($"[GameManager] 無効なバトルインデックス: {battleIndex}（有効範囲: 0-{_enemyBattleDataList.Length - 1}）");
                return;
            }

            _currentBattleIndex = battleIndex;
            ResetCurrentBattleScore();

            Debug.Log($"[GameManager] バトル {battleIndex + 1} を開始します（敵: {CurrentEnemyData?.EnemyName ?? "デフォルト"}）");
            TransitionToSceneAsync(k_BattleSceneName, GameState.Battle).Forget();
        }

        #endregion

        #region Public API - バトル中

        /// <summary>
        /// 撃破されたキャラからの報告を受け取る
        /// </summary>
        /// <param name="defeatedCharacter">撃破されたキャラクターのGameObject</param>
        public void DefeatedReport(GameObject defeatedCharacter)
        {
            if (_currentState != GameState.Battle || _winner != null)
                return;

            _isBattleActive = false;

            if (defeatedCharacter == _player)
            {
                _winner = _npc;
                _npcScoreNum++;
                _battleRefs.NpcScoreText.text = _npcScoreNum.ToString();

                if (_npcScoreNum >= CurrentRequiredWins)
                {
                    PlayerLoseBattle().Forget();
                }
                else
                {
                    RoundEnd().Forget();
                }
            }
            else if (defeatedCharacter == _npc)
            {
                _winner = _player;
                _playerScoreNum++;
                _battleRefs.PlayerScoreText.text = _playerScoreNum.ToString();

                if (_playerScoreNum >= CurrentRequiredWins)
                {
                    PlayerWinBattle().Forget();
                }
                else
                {
                    RoundEnd().Forget();
                }
            }
        }

        #endregion

        #region Public API - シーン参照登録

        /// <summary>
        /// バトルシーンの参照を登録する
        /// </summary>
        /// <param name="references">バトルシーン参照構造体</param>
        public void RegisterBattleReferences(BattleSceneReferences references)
        {
            _battleRefs = references;

            SetupPlayer();
            SetupCurrentEnemy();

            Debug.Log("[GameManager] バトルシーン参照を登録しました");
            BattleInitialize().Forget();
        }

        /// <summary>
        /// バトルシーンの参照をクリアする
        /// </summary>
        public void UnregisterBattleReferences()
        {
            _player = null;
            _npc = null;
            _gameManagerHelper = null;
            _battleRefs = default;

            Debug.Log("[GameManager] バトルシーン参照をクリアしました");
        }

        #endregion

        #region シーン遷移処理

        /// <summary>
        /// タイトルシーンを非同期でロード
        /// </summary>
        private async UniTaskVoid LoadTitleSceneAsync()
        {
            _isTransitioning = true;

            await SceneManager.LoadSceneAsync(k_TitleSceneName, LoadSceneMode.Additive);
            _currentSubSceneName = k_TitleSceneName;
            _currentState = GameState.Title;

            SetUIState_Title();
            Debug.Log("[GameManager] タイトルシーンをロードしました");

            if (_uiController != null)
            {
                await _uiController.BlackoutReleaseAsync();
            }

            await UniTask.Delay(500);

            PlaySE(_entranceSE);

            _isTransitioning = false;
        }

        /// <summary>
        /// シーン遷移を非同期で実行
        /// </summary>
        /// <param name="nextSceneName">遷移先のシーン名</param>
        /// <param name="nextState">遷移後のゲーム状態</param>
        private async UniTask TransitionToSceneAsync(string nextSceneName, GameState nextState)
        {
            _isTransitioning = true;
            _isBattleActive = false;

            // シーン遷移前にキャラクターを破棄
            CleanupCharacters();

            if (_uiController != null)
            {
                await _uiController.BlackoutAsync();
            }

            if (!string.IsNullOrEmpty(_currentSubSceneName))
            {
                await SceneManager.UnloadSceneAsync(_currentSubSceneName);
                Debug.Log($"[GameManager] {_currentSubSceneName} をアンロードしました");
            }

            await SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
            _currentSubSceneName = nextSceneName;
            _currentState = nextState;

            Debug.Log($"[GameManager] {nextSceneName} をロードしました");

            if (nextState == GameState.Title)
            {
                SetUIState_Title();
            }

            if (nextState != GameState.Battle)
            {
                _isTransitioning = false;
            }
        }

        /// <summary>
        /// キャラクターをクリーンアップする
        /// </summary>
        private void CleanupCharacters()
        {
            if (_player != null)
            {
                Destroy(_player);
                _player = null;
            }

            if (_npc != null)
            {
                Destroy(_npc);
                _npc = null;
            }

            _gameManagerHelper = null;
        }

        #endregion

        #region バトル状態管理処理

        /// <summary>
        /// プレイヤーキャラクターを生成
        /// </summary>
        private void SetupPlayer()
        {
            if (_player != null)
            {
                Destroy(_player);
            }

            if (_playerPrefab == null)
            {
                Debug.LogError("[GameManager] プレイヤープレハブが設定されていません");
                return;
            }

            var spawnPos = _battleRefs.PlayerSpawnPoint != null ? _battleRefs.PlayerSpawnPoint.position : Vector3.zero;
            var spawnRot = _battleRefs.PlayerSpawnPoint != null ? _battleRefs.PlayerSpawnPoint.rotation : Quaternion.identity;

            _player = Instantiate(_playerPrefab, spawnPos, spawnRot);
            Debug.Log("[GameManager] プレイヤーキャラを生成しました");
        }

        /// <summary>
        /// 現在の敵キャラを生成
        /// </summary>
        private void SetupCurrentEnemy()
        {
            if (_npc != null)
            {
                Destroy(_npc);
            }

            var enemyData = CurrentEnemyData;

            if (enemyData == null || enemyData.EnemyPrefab == null)
            {
                Debug.LogError($"[GameManager] バトル {_currentBattleIndex} の敵プレハブが設定されていません");
                return;
            }

            var spawnPos = _battleRefs.NpcSpawnPoint != null ? _battleRefs.NpcSpawnPoint.position : Vector3.zero;
            var spawnRot = _battleRefs.NpcSpawnPoint != null ? _battleRefs.NpcSpawnPoint.rotation : Quaternion.identity;

            _npc = Instantiate(enemyData.EnemyPrefab, spawnPos, spawnRot);

            Debug.Log($"[GameManager] 敵キャラ '{enemyData.EnemyName}' を生成しました");
        }

        /// <summary>
        /// キャラクター間のターゲット設定を行う
        /// </summary>
        private void SetupTargets()
        {
            // GetComponentsInChildren で子オブジェクトも含めて取得
            var playerSetters = _player.GetComponentsInChildren<ITargetSet>();
            foreach (var setter in playerSetters)
            {
                setter.SetTarget(_npc);
            }

            var npcSetters = _npc.GetComponentsInChildren<ITargetSet>();
            foreach (var setter in npcSetters)
            {
                setter.SetTarget(_player);
            }
        }

        /// <summary>
        /// 次のバトルへ移行
        /// </summary>
        private async UniTask TransitionToNextBattle()
        {
            _currentBattleIndex++;
            ResetCurrentBattleScore();

            Debug.Log($"[GameManager] 次のバトルへ（バトル {_currentBattleIndex + 1}/{TotalBattleCount}）");

            if (_uiController != null)
            {
                await _uiController.BlackoutAsync();
            }

            if (_npc != null)
            {
                Destroy(_npc);
                _npc = null;
            }

            SetupCurrentEnemy();
            GetHelpers();

            await UniTask.DelayFrame(1);

            SetupTargets();

            var playerState = _player.GetComponent<StateSystem>();
            var npcState = _npc.GetComponent<StateSystem>();
            var playerDamageSystem = _player.GetComponent<DamageSystemBase>();
            var npcDamageSystem = _npc.GetComponent<DamageSystemBase>();

            // LLM通信コンポーネントを初期化してキャラクター情報を注入
            _communicator.InjectionNewBattle(playerState, npcState);

            // ScreenSpaceGaugeUIControllerへのバインド（NPCは新規、Playerは再バインド）
            _battleRefs.PlayerGaugeController.BindToCharacter(
                playerState, playerDamageSystem, playerState.CharacterData, "Player");
            _battleRefs.NpcGaugeController.BindToCharacter(
                npcState, npcDamageSystem, npcState.CharacterData, CurrentEnemyData.EnemyName);

            foreach (var helper in _gameManagerHelper)
            {
                helper.SetUp();
                helper.Lock();
            }

            _battleRefs.NpcScoreText.text = "0";
            _battleRefs.PlayerScoreText.text = "0";

            await RoundStartAsync();
        }

        /// <summary>
        /// バトル開始時の初期化処理
        /// </summary>
        private async UniTaskVoid BattleInitialize()
        {
            _battleRefs.PlayerScoreText.text = "0";
            _battleRefs.NpcScoreText.text = "0";

            // 少し待ってからターゲット設定を行う（キャラクターの初期化完了を待つため）
            await UniTask.DelayFrame(3);

            SetupTargets();

            SetUIState_BattleStart();
            GetHelpers();

            var playerState = _player.GetComponent<StateSystem>();
            var npcState = _npc.GetComponent<StateSystem>();
            var playerDamageSystem = _player.GetComponent<DamageSystemBase>();
            var npcDamageSystem = _npc.GetComponent<DamageSystemBase>();

            // LLM通信コンポーネントを初期化してキャラクター情報を注入
            await _communicator.InitializeWithInjection(playerState, npcState);

            // ScreenSpaceGaugeUIControllerへのバインド
            _battleRefs.PlayerGaugeController.BindToCharacter(
                playerState, playerDamageSystem, playerState.CharacterData, "Player");
            _battleRefs.NpcGaugeController.BindToCharacter(
                npcState, npcDamageSystem, npcState.CharacterData, CurrentEnemyData.EnemyName);

            foreach (var helper in _gameManagerHelper)
            {
                helper.SetUp();
                helper.Lock();
            }

            if (_currentBattleIndex == 0)
            {
                _battleStartTime = Time.realtimeSinceStartup;
                _totalPausedTime = 0f;
                _clearTime = 0f;
                Debug.Log("[GameManager] 時間計測開始");
            }

            await RoundStartAsync();
            _isTransitioning = false;
        }

        /// <summary>
        /// ラウンド開始時の処理
        /// </summary>
        private async UniTask RoundStartAsync()
        {
            foreach (var helper in _gameManagerHelper)
            {
                helper.RoundStart();
            }

            // 少し待つ
            await UniTask.DelayFrame(60);

            _battleRefs.PlayerSpawnPoint.GetPositionAndRotation(out var position, out var rotation);
            _battleRefs.NpcSpawnPoint.GetPositionAndRotation(out var npcPosition, out var npcRotation);

            // 10回まで位置と回転を強制セットして確認（まれに位置ズレすることがあるため）
            int counter = 0;
            while ((_player.transform.position != position || _npc.transform.position != npcPosition)
                    && counter < 10)
            {
                _player.transform.SetPositionAndRotation(position, rotation);
                _npc.transform.SetPositionAndRotation(npcPosition, npcRotation);
                await UniTask.DelayFrame(1);
                counter++;
            }

            Debug.Log($"位置確認{_player.transform.position == _battleRefs.PlayerSpawnPoint.position} {_npc.transform.position == position}");

            if (_uiController != null)
            {
                await _uiController.BlackoutReleaseAsync();

                PlayRoundAnnounceSE();
                await _uiController.ShowRoundAsync(_playerScoreNum + _npcScoreNum + 1);

                PlayBattleStartSE();
                await _uiController.ShowFightAsync();
            }

            _isBattleActive = true;

            foreach (var helper in _gameManagerHelper)
            {
                helper.Unlock();
            }

            _winner = null;
        }

        /// <summary>
        /// ラウンド終了時の処理
        /// </summary>
        private async UniTaskVoid RoundEnd()
        {
            _isBattleActive = false;

            foreach (var helper in _gameManagerHelper)
            {
                helper.Lock();
                helper.RoundEnd();
            }

            if (_uiController != null)
            {
                if (_winner == _player)
                {
                    PlayRoundWinSE();
                    await _uiController.ShowPlayerWinAsync();
                }
                else
                {
                    PlayRoundLoseSE();
                    await _uiController.ShowPlayerLoseAsync();
                }
            }

            if (_uiController != null)
            {
                await _uiController.BlackoutAsync();
            }

            await RoundStartAsync();
        }

        /// <summary>
        /// プレイヤーがバトルに勝利した際の処理
        /// </summary>
        private async UniTaskVoid PlayerWinBattle()
        {
            _isBattleActive = false;

            foreach (var helper in _gameManagerHelper)
            {
                helper.Lock();
                helper.GameEnd();
            }

            _totalBattleWins++;

            if (_uiController != null)
            {
                PlayRoundWinSE();
                await _uiController.ShowPlayerWinAsync();
                PlayBattleWinSE();
                await _uiController.ShowGameSetAsync();
            }

            await UniTask.Delay(1000);

            if (_currentBattleIndex + 1 < _enemyBattleDataList.Length)
            {
                await TransitionToNextBattle();
            }
            else
            {
                _clearTime = CurrentElapsedTime;
                await TransitionToResult();
                Debug.Log($"[GameManager] 全バトル終了。クリア時間: {ClearTimeFormatted}");
                await UniTask.Delay(1000);

                if (_uiController != null)
                {
                    await _uiController.BlackoutReleaseAsync();
                }

                await UniTask.Delay(200);

                PlayCongratulationSE();
            }
        }

        /// <summary>
        /// プレイヤーがバトルに敗北した際の処理
        /// </summary>
        private async UniTaskVoid PlayerLoseBattle()
        {
            _isBattleActive = false;

            foreach (var helper in _gameManagerHelper)
            {
                helper.Lock();
                helper.GameEnd();
            }

            if (_uiController != null)
            {
                PlayRoundLoseSE();
                await _uiController.ShowPlayerLoseAsync();

                PlayBattleLoseSE();

                await _uiController.ShowGameSetAsync();
            }

            await UniTask.Delay(1000);

            _currentState = GameState.Result;
            await SetUIState_ResultAsync(isWin: false);
        }

        /// <summary>
        /// リザルトへ移行
        /// </summary>
        private async UniTask TransitionToResult()
        {
            Debug.Log($"[GameManager] リザルトへ");

            // シーン移動してリザルトシーンのUIを表示
            await TransitionToSceneAsync(k_ResultSceneName, GameState.Result);

            _resultCameraPlace.GetPositionAndRotation(out Vector3 resultCamPos, out Quaternion resultCamRot);
            Camera.main.transform.SetLocalPositionAndRotation(resultCamPos, resultCamRot);

            if (_npc != null)
            {
                Destroy(_npc);
                _npc = null;
            }

            await SetUIState_ResultAsync(isWin: true);

            _isTransitioning = false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// ヘルパーを取得する
        /// </summary>
        private void GetHelpers()
        {
            _gameManagerHelper = _player.GetComponentsInChildren<IGameHelper>().ToList();
            _gameManagerHelper.AddRange(_npc.GetComponentsInChildren<IGameHelper>());

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _gameManagerHelper.AddRange(mainCamera.GetComponentsInChildren<IGameHelper>());
                Debug.Log($"[GameManager] ヘルパーを {_gameManagerHelper.Count} 個取得しました（Camera: {mainCamera.name}）");
            }
            else
            {
                Debug.LogWarning("[GameManager] Camera.mainが見つかりません");
                Debug.Log($"[GameManager] ヘルパーを {_gameManagerHelper.Count} 個取得しました");
            }

            _gameManagerHelper = _gameManagerHelper == null ? new List<IGameHelper>() : _gameManagerHelper;
        }

        /// <summary>
        /// 現在のバトルのスコアをリセット
        /// </summary>
        private void ResetCurrentBattleScore()
        {
            _playerScoreNum = 0;
            _npcScoreNum = 0;
            _winner = null;
        }

        /// <summary>
        /// 全進行状況をリセット
        /// </summary>
        private void ResetAllProgress()
        {
            _currentBattleIndex = 0;
            _totalBattleWins = 0;
            _retryCount = 0;
            ResetCurrentBattleScore();

            _battleStartTime = 0f;
            _totalPausedTime = 0f;
            _clearTime = 0f;
        }

        /// <summary>
        /// 時間をフォーマット（MM:SS.ss）
        /// </summary>
        /// <param name="timeInSeconds">秒単位の時間</param>
        /// <returns>フォーマット済み文字列</returns>
        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            float seconds = timeInSeconds % 60f;
            return $"{minutes:00}:{seconds:05.2f}";
        }

        #endregion
    }
}