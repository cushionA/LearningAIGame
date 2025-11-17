using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

//==============================================ファイルヘッダ===========================================================
// LLMCommunicatorWithLogging
// 
// 概要: LLMCommunicatorを継承し、リクエスト/レスポンスのログ記録機能を追加したクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// LLMCommunicatorの全機能を継承しつつ、以下のログ機能を追加:
// - 各リクエストの応答時間を記録
// - 送信プロンプトと受信レスポンスを記録
// - 処理結果（成功/失敗/タイムアウト/キャンセル）を記録
// - OnDestroy時にJSON形式でファイル出力
// - 手動でのログ出力・クリアも可能
// 
// 出力先: Application.persistentDataPath/LLMLogs/
// 
// その他:
// デバッグや性能分析用途を想定
// 本番環境では元のLLMCommunicatorを使用することを推奨
//=====================================================================================================================

namespace LLMDataArchitect
{
    /// <summary>
    /// LLMCommunicatorを継承し、リクエスト/レスポンスのログ記録機能を追加したクラス
    /// </summary>
    public class LLMCommunicatorWithLogging : LLMCommunicator
    {
        #region ログデータ構造

        /// <summary>
        /// 単一のリクエスト/レスポンスログエントリ
        /// </summary>
        [Serializable]
        public class LogEntry
        {
            public int requestIndex;
            public string timestamp;
            public float responseTimeSeconds;
            public string prompt;
            public string response;
            public string result;
            public string parsedStrategy;
        }

        /// <summary>
        /// セッション全体のログデータ
        /// </summary>
        [Serializable]
        public class SessionLog
        {
            public string sessionStartTime;
            public string sessionEndTime;
            public int totalRequests;
            public int successCount;
            public int failureCount;
            public float averageResponseTime;
            public List<LogEntry> entries = new List<LogEntry>();
        }

        #endregion

        #region フィールド

        [Header("ログ設定")]
        [SerializeField]
        [Tooltip("ログファイルの出力先フォルダ名")]
        private string _logFolderName = "LLMLogs";

        [SerializeField]
        [Tooltip("ログファイル名のプレフィックス")]
        private string _logFilePrefix = "LLMLog";

        // ログデータ
        private SessionLog _sessionLog;
        private int _requestCounter = 0;
        private float _totalResponseTime = 0f;

        #endregion

        #region パブリックプロパティ

        /// <summary>
        /// 現在のセッションログを取得（読み取り専用）
        /// </summary>
        public SessionLog CurrentSessionLog => _sessionLog;

        /// <summary>
        /// ログ出力先のフルパスを取得
        /// </summary>
        public string LogDirectoryPath => Path.Combine(@"C:\Users\tatuk\Desktop\GameDev\LearningAIGame\Assets\Tests\Result", _logFolderName);

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            // 終了時にログを出力
            SaveLogToFile();
        }

        #endregion

        #region オーバーライドメソッド

        /// <summary>
        /// 初期化処理をオーバーライドしてログセッションを開始
        /// </summary>
        protected override void Initialize()
        {
            // セッションログを初期化
            _sessionLog = new SessionLog
            {
                sessionStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                entries = new List<LogEntry>()
            };

            _requestCounter = 0;
            _totalResponseTime = 0f;

            Debug.Log($"[LLMCommunicatorWithLogging] ログ記録を開始しました。出力先: {LogDirectoryPath}");

            // 親クラスの初期化を呼び出し
            base.Initialize();
        }

        /// <summary>
        /// LLMリクエスト処理をオーバーライドしてログを記録
        /// </summary>
        protected override async UniTask RequestTacticalDecisionAsync(CancellationToken cancellationToken)
        {
            // 処理中の場合はスキップ
            if (_isProcessing)
            {
                Debug.LogWarning("前回のリクエストがまだ処理中です。スキップ");
                return;
            }

            _isProcessing = true;
            _requestCounter++;

            // ログエントリを準備
            var logEntry = new LogEntry
            {
                requestIndex = _requestCounter,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            };

            // 計測開始
            float startTime = Time.realtimeSinceStartup;

            try
            {
                // プロンプト生成
                string prompt = _promptGenerator.GeneratePromptByData(_inputData);
                logEntry.prompt = prompt;

                Debug.Log($"[Request #{_requestCounter}] プロンプト生成完了 (文字数: {prompt.Length})");

                // LLMにリクエスト送信
                var (isCanceled, strategy) = await RequestAsyncWithLogging(prompt, logEntry, cancellationToken)
                    .SuppressCancellationThrow();

                // 応答時間を記録
                logEntry.responseTimeSeconds = Time.realtimeSinceStartup - startTime;
                _totalResponseTime += logEntry.responseTimeSeconds;

                // キャンセルされた場合
                if (isCanceled)
                {
                    logEntry.result = "Cancelled";
                    Debug.Log($"[Request #{_requestCounter}] キャンセルされました。");
                    return;
                }

                // 応答が無効な場合
                if (strategy == null)
                {
                    logEntry.result = "Failed";
                    _sessionLog.failureCount++;
                    Debug.LogError($"[Request #{_requestCounter}] LLMからの応答が無効でした。");
                    return;
                }

                // 成功
                logEntry.result = "Success";
                logEntry.parsedStrategy = strategy.BasicTactic.ToString();
                _sessionLog.successCount++;

                // 入力データを更新
                _inputData.UpdateStrategy(strategy);

                Debug.Log($"[Request #{_requestCounter}] 戦術を更新しました: {strategy.BasicTactic} (応答時間: {logEntry.responseTimeSeconds:F2}秒)");
            }
            catch (Exception ex)
            {
                logEntry.responseTimeSeconds = Time.realtimeSinceStartup - startTime;
                logEntry.result = $"Error: {ex.Message}";
                _sessionLog.failureCount++;

                Debug.LogError($"[Request #{_requestCounter}] エラーが発生しました: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // ログエントリを追加
                _sessionLog.entries.Add(logEntry);
                _sessionLog.totalRequests = _requestCounter;

                _isProcessing = false;
            }
        }

        #endregion

        #region Privateメソッド

        /// <summary>
        /// LLMリクエストを送信し、ログを記録
        /// </summary>
        private async UniTask<StrategyData> RequestAsyncWithLogging(
            string prompt,
            LogEntry logEntry,
            CancellationToken cancellationToken)
        {
            // LLMにリクエスト送信
            var result = await _llmCharacter.Chat(prompt)
                .AsUniTask()
                .Timeout(TimeSpan.FromSeconds(_timeoutSeconds))
                .AttachExternalCancellation(cancellationToken)
                .SuppressCancellationThrow();

            // キャンセルまたはタイムアウトのチェック
            if (result.IsCanceled)
            {
                logEntry.response = "[Timeout or Cancelled]";
                Debug.LogWarning($"[Request #{logEntry.requestIndex}] タイムアウトまたはキャンセル ({_timeoutSeconds}秒)");
                return null;
            }

            // 応答の取得
            string output = result.Result;
            logEntry.response = output;

            if (string.IsNullOrEmpty(output))
            {
                Debug.LogError($"[Request #{logEntry.requestIndex}] 空の応答が返されました。");
                return null;
            }

            Debug.Log($"[Request #{logEntry.requestIndex}] LLM応答を受信 (文字数: {output.Length})");

            // JSON解析
            var (isSuccess, strategy) = StrategyData.TryFromJsonEnglish(output);

            if (!isSuccess)
            {
                logEntry.parsedStrategy = "[Parse Failed]";
                Debug.LogError($"[Request #{logEntry.requestIndex}] JSON解析に失敗しました。");
                return null;
            }

            return strategy;
        }

        #endregion

        #region Publicメソッド（ログ操作）

        /// <summary>
        /// 現在のログをJSONファイルに保存
        /// </summary>
        /// <returns>保存したファイルのパス。失敗時はnull</returns>
        public string SaveLogToFile()
        {
            if (_sessionLog == null || _sessionLog.entries.Count == 0)
            {
                Debug.LogWarning("[LLMCommunicatorWithLogging] 保存するログがありません。");
                return null;
            }

            try
            {
                // 終了時刻と平均応答時間を設定
                _sessionLog.sessionEndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _sessionLog.averageResponseTime = _requestCounter > 0
                    ? _totalResponseTime / _requestCounter
                    : 0f;

                // 出力先ディレクトリを作成
                if (!Directory.Exists(LogDirectoryPath))
                {
                    Directory.CreateDirectory(LogDirectoryPath);
                }

                // ファイル名を生成
                string fileName = $"{_logFilePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filePath = Path.Combine(LogDirectoryPath, fileName);

                // JSONにシリアライズして保存
                string json = JsonUtility.ToJson(_sessionLog, true);
                File.WriteAllText(filePath, json);

                Debug.Log($"[LLMCommunicatorWithLogging] ログを保存しました: {filePath}");
                Debug.Log($"  総リクエスト数: {_sessionLog.totalRequests}");
                Debug.Log($"  成功: {_sessionLog.successCount}, 失敗: {_sessionLog.failureCount}");
                Debug.Log($"  平均応答時間: {_sessionLog.averageResponseTime:F2}秒");

                return filePath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMCommunicatorWithLogging] ログの保存に失敗しました: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 現在のログをクリア
        /// </summary>
        public void ClearLog()
        {
            _sessionLog = new SessionLog
            {
                sessionStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                entries = new List<LogEntry>()
            };

            _requestCounter = 0;
            _totalResponseTime = 0f;

            Debug.Log("[LLMCommunicatorWithLogging] ログをクリアしました。");
        }

        /// <summary>
        /// 現在のログ内容をコンソールに出力
        /// </summary>
        public void PrintLogSummary()
        {
            if (_sessionLog == null)
            {
                Debug.Log("[LLMCommunicatorWithLogging] ログがありません。");
                return;
            }

            Debug.Log("\n=== LLM通信ログサマリー ===");
            Debug.Log($"セッション開始: {_sessionLog.sessionStartTime}");
            Debug.Log($"総リクエスト数: {_sessionLog.totalRequests}");
            Debug.Log($"成功: {_sessionLog.successCount}, 失敗: {_sessionLog.failureCount}");

            if (_requestCounter > 0)
            {
                Debug.Log($"平均応答時間: {_totalResponseTime / _requestCounter:F2}秒");
            }

            Debug.Log("----------------------------");

            foreach (var entry in _sessionLog.entries)
            {
                Debug.Log($"[#{entry.requestIndex}] {entry.timestamp}");
                Debug.Log($"  結果: {entry.result}, 応答時間: {entry.responseTimeSeconds:F2}秒");
                if (!string.IsNullOrEmpty(entry.parsedStrategy))
                {
                    Debug.Log($"  戦術: {entry.parsedStrategy}");
                }
            }

            Debug.Log("============================\n");
        }

        #endregion
    }
}