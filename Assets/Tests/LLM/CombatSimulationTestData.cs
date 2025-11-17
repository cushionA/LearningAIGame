using LearningAIGame.CombatSystem.Data;
using LLMDataArchitect;
using UnityEngine;

//==============================================ファイルヘッダ===========================================================
// CombatSimulationTestData
// 
// 概要: 戦闘シミュレーションテスト用のStrategyData配列を管理するScriptableObject
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// [Serializable]属性が付いたStrategyData配列をInspectorで編集可能な形で保持。
// GetNextTable()メソッドで順次テーブルを取得し、最後まで到達したら自動的に最初に戻る。
// CombatSimulationTestでキャラクターAの行動パターンを管理する際に使用。
// 
// 使用例:
// 1. Unity Editor → Create → Combat/Test → Combat Simulation Test Data
// 2. Strategiesフィールドにサイズを設定
// 3. 各要素のStrategyDataを編集
// 4. CombatSimulationTestのInspectorで参照
// 5. テスト内でGetNextTable()を呼び出し
// 
// 設計思想:
// - StrategyDataをそのまま使用（追加のラッパークラス不要）
// - シンプルなインデックス管理
// - Resetメソッドで初期状態に戻せる
// 
// その他:
// - Unity Editorでの編集性を重視
// - テスト実行中の動的な切り替えをサポート
//=====================================================================================================================

namespace LearningAIGame.CombatSystem.Tests
{
    /// <summary>
    /// 戦闘シミュレーションテスト用のStrategyDataコレクション
    /// </summary>
    [CreateAssetMenu(fileName = "CombatSimTestData", menuName = "Combat/Test/Combat Simulation Test Data", order = 1)]
    public class CombatSimulationTestData : ScriptableObject
    {
        #region フィールド

        [Header("戦術テーブル")]
        [Tooltip("順次使用される戦術データの配列")]
        [SerializeField]
        private StrategyData[] _strategies = new StrategyData[3];

        [Header("デバッグ情報")]
        [Tooltip("現在のインデックス（読み取り専用）")]
        [SerializeField]
#if UNITY_EDITOR
        [ReadOnly]
#endif
        private int _currentIndex = 0;

        /// <summary>
        /// AI戦術パラメーター設定
        /// </summary>
        [Header("AI設定")]
        [SerializeField]
        public AIParameterContainer strategyParameters;

        /// <summary>
        /// 現在のインデックス
        /// </summary>
        private int _runtimeIndex = 0;

        #endregion

        #region プロパティ

        /// <summary>
        /// 保持している戦術データの数
        /// </summary>
        public int Count => _strategies?.Length ?? 0;

        /// <summary>
        /// 現在のインデックス（読み取り専用）
        /// </summary>
        public int CurrentIndex => _runtimeIndex;

        /// <summary>
        /// 戦術データ配列が有効かどうか
        /// </summary>
        public bool IsValid => _strategies != null && _strategies.Length > 0;

        #endregion

        #region パブリックメソッド

        /// <summary>
        /// 次の戦術テーブルを取得
        /// 最後まで到達したら最初に戻る
        /// </summary>
        /// <returns>次のStrategyData。配列が空の場合はnull</returns>
        public StrategyData GetNextTable()
        {
            if (!IsValid)
            {
                Debug.LogWarning($"[{name}] 戦術データ配列が空です。");
                return null;
            }

            // 現在のインデックスのデータを取得
            StrategyData result = _strategies[_runtimeIndex];

            // インデックスを進める（循環）
            _runtimeIndex = (_runtimeIndex + 1) % _strategies.Length;

#if UNITY_EDITOR
            // Editor用にシリアライズされたインデックスも更新
            _currentIndex = _runtimeIndex;
#endif

            Debug.Log($"[{name}] テーブル切り替え: インデックス {_runtimeIndex - 1} → {result?.BasicTactic ?? "null"}");

            return result;
        }

        /// <summary>
        /// 現在の戦術テーブルを取得
        /// 使用しているテーブルの確認等に
        /// </summary>
        /// <returns>次のStrategyData。配列が空の場合はnull</returns>
        public StrategyData GetCurrentTable()
        {
            if (!IsValid)
            {
                Debug.LogWarning($"[{name}] 戦術データ配列が空です。");
                return null;
            }

            // 現在のインデックスのデータを取得
            StrategyData result = _strategies[_runtimeIndex];

            return result;
        }

        /// <summary>
        /// 現在のインデックスを取得（次回GetNextTableで返されるもの）
        /// </summary>
        /// <returns>現在の戦術データ。配列が空の場合はnull</returns>
        public StrategyData PeekCurrent()
        {
            if (!IsValid)
            {
                return null;
            }

            return _strategies[_runtimeIndex];
        }

        /// <summary>
        /// 指定インデックスの戦術データを取得
        /// </summary>
        /// <param name="index">取得するインデックス</param>
        /// <returns>指定インデックスの戦術データ。範囲外の場合はnull</returns>
        public StrategyData GetTableAt(int index)
        {
            if (!IsValid || index < 0 || index >= _strategies.Length)
            {
                Debug.LogWarning($"[{name}] 無効なインデックス: {index}");
                return null;
            }

            return _strategies[index];
        }

        /// <summary>
        /// インデックスを最初に戻す
        /// テスト開始時や再実行時に呼び出す
        /// </summary>
        public void ResetIndex()
        {
            _runtimeIndex = 0;
#if UNITY_EDITOR
            _currentIndex = 0;
#endif
            Debug.Log($"[{name}] インデックスをリセットしました。");
        }

        /// <summary>
        /// すべての戦術データを取得（デバッグ用）
        /// </summary>
        /// <returns>戦術データ配列のコピー</returns>
        public StrategyData[] GetAllStrategies()
        {
            if (!IsValid)
            {
                return new StrategyData[0];
            }

            // コピーを返す（元の配列の変更を防ぐ）
            StrategyData[] copy = new StrategyData[_strategies.Length];
            System.Array.Copy(_strategies, copy, _strategies.Length);
            return copy;
        }

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// ScriptableObjectが有効化された際の処理
        /// </summary>
        private void OnEnable()
        {
            // ランタイムインデックスを初期化
            _runtimeIndex = 0;
        }

        #endregion

        #region 検証

        /// <summary>
        /// データの妥当性を検証
        /// </summary>
        /// <param name="errorMessage">エラーメッセージ（検証失敗時）</param>
        /// <returns>検証成功時true</returns>
        public bool Validate(out string errorMessage)
        {
            if (_strategies == null || _strategies.Length == 0)
            {
                errorMessage = "戦術データ配列が空です。";
                return false;
            }

            // 各戦術データの妥当性チェック
            for (int i = 0; i < _strategies.Length; i++)
            {
                if (_strategies[i] == null)
                {
                    errorMessage = $"インデックス {i} の戦術データがnullです。";
                    return false;
                }

                // StrategyDataの必須フィールドチェック
                if (string.IsNullOrEmpty(_strategies[i].BasicTactic))
                {
                    errorMessage = $"インデックス {i} の基本戦術が設定されていません。";
                    return false;
                }

                if (string.IsNullOrEmpty(_strategies[i].AttackCriteria))
                {
                    errorMessage = $"インデックス {i} の攻撃基準が設定されていません。";
                    return false;
                }

                if (string.IsNullOrEmpty(_strategies[i].DefenseCriteria))
                {
                    errorMessage = $"インデックス {i} の防御基準が設定されていません。";
                    return false;
                }
            }

            errorMessage = "";
            return true;
        }

        #endregion

        #region デバッグ用

        /// <summary>
        /// デバッグ情報を文字列で返す
        /// </summary>
        public string GetDebugInfo()
        {
            if (!IsValid)
            {
                return $"[{name}] 戦術データなし";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"[{name}] 戦術データ情報:");
            sb.AppendLine($"  総数: {_strategies.Length}");
            sb.AppendLine($"  現在インデックス: {_runtimeIndex}");
            sb.AppendLine($"  次の戦術: {_strategies[_runtimeIndex]?.BasicTactic ?? "null"}");
            sb.AppendLine("  戦術一覧:");

            for (int i = 0; i < _strategies.Length; i++)
            {
                string marker = i == _runtimeIndex ? "→" : " ";
                string tactic = _strategies[i]?.BasicTactic ?? "null";
                sb.AppendLine($"    {marker} [{i}] {tactic}");
            }

            return sb.ToString();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Inspector上にデバッグ情報を表示
        /// </summary>
        [ContextMenu("デバッグ情報を表示")]
        private void PrintDebugInfo()
        {
            Debug.Log(GetDebugInfo());
        }

        /// <summary>
        /// デフォルトの戦術データを設定（Editor専用）
        /// </summary>
        [ContextMenu("デフォルト戦術を設定")]
        private void SetupDefaultStrategies()
        {
            _strategies = new StrategyData[3];

            // バランス型
            _strategies[0] = new StrategyData
            {
                BasicTactic = "Balanced",
                AttackCriteria = "Cumulative Probability",
                ContinuousAttackCriteria = "Recent Pattern Focus",
                DefenseCriteria = "Cumulative Probability",
                ContinuousDefenseCriteria = "Counterattack Focus",
                AnalysisResult = "Preset: Balanced strategy"
            };

            // 攻撃型
            _strategies[1] = new StrategyData
            {
                BasicTactic = "Aggressive",
                AttackCriteria = "Return Priority",
                ContinuousAttackCriteria = "Return Priority",
                DefenseCriteria = "Counterattack Focus",
                ContinuousDefenseCriteria = "Evasive Counter Priority",
                AnalysisResult = "Preset: Aggressive strategy"
            };

            // 防御型
            _strategies[2] = new StrategyData
            {
                BasicTactic = "Defensive",
                AttackCriteria = "Speed Priority",
                ContinuousAttackCriteria = "Speed Priority",
                DefenseCriteria = "Risk Avoidance",
                ContinuousDefenseCriteria = "Risk Avoidance",
                AnalysisResult = "Preset: Defensive strategy"
            };

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[{name}] デフォルト戦術を設定しました。");
        }

        /// <summary>
        /// データの妥当性を検証（Editor専用）
        /// </summary>
        [ContextMenu("データ検証")]
        private void ValidateData()
        {
            if (Validate(out string errorMessage))
            {
                Debug.Log($"[{name}] 検証成功: すべての戦術データが正常です。");
            }
            else
            {
                Debug.LogError($"[{name}] 検証失敗: {errorMessage}");
            }
        }
#endif

        #endregion
    }

#if UNITY_EDITOR
    /// <summary>
    /// 読み取り専用属性（Inspector表示用）
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }

    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
#endif
}