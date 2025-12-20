#if UNITY_EDITOR
using UnityEngine;

//==============================================ファイルヘッダ=========================================================
// StrategyAI.Editor
// 
// 概要: StrategyAIのエディター専用デバッグUI表示拡張
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// [デバッグUI表示]
// - 画面右側にStrategyAIの戦術情報をリアルタイム表示
// - F1キーで表示ON/OFF切り替え
// 
// [表示内容]
// - 基本情報: 状態、状況、構え、移動ベクトル
// - 行動可能フラグ: 攻撃/防御/移動可能状態
// - タイミング制御: 次回行動までの待機時間
// - 戦術情報: 基本戦術、判断基準
// - 距離情報: 敵との距離、距離状態
// - 統計: 攻撃/防御成功率
// 
// 注意:
// - 既存のStrategyAIクラスにpartialキーワードを追加する必要があります
// - エディター専用（ビルドには含まれません）
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.AI
{
    /// <summary>
    /// StrategyAIのエディター専用デバッグUI拡張
    /// </summary>
    public partial class StrategyAI
    {
        #region デバッグUI設定

        /// <summary>
        /// デバッグUI表示フラグ
        /// </summary>
        private bool _showDebugUI = true;

        /// <summary>
        /// 表示切り替えキー
        /// </summary>
        private const KeyCode k_ToggleKey = KeyCode.F1;

        /// <summary>
        /// UIの幅
        /// </summary>
        private const float k_UIWidth = 280f;

        /// <summary>
        /// UIの右マージン
        /// </summary>
        private const float k_RightMargin = 10f;

        /// <summary>
        /// UIの上マージン
        /// </summary>
        private const float k_TopMargin = 10f;

        #endregion

        #region スタイル

        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _yesStyle;
        private GUIStyle _noStyle;
        private GUIStyle _warningStyle;
        private bool _stylesInitialized = false;
        private Texture2D _backgroundTexture;
        private Texture2D _barBackgroundTexture;

        /// <summary>
        /// GUIスタイルの初期化
        /// </summary>
        private void InitializeStyles()
        {
            if (_stylesInitialized)
                return;

            // テクスチャのキャッシュ
            _backgroundTexture = CreateColorTexture(new Color(0.1f, 0.1f, 0.1f, 0.85f));
            _barBackgroundTexture = CreateColorTexture(new Color(0.2f, 0.2f, 0.2f));

            // ボックススタイル
            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = _backgroundTexture;

            // ヘッダースタイル
            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = new Color(0.4f, 0.8f, 1f);
            _headerStyle.fontSize = 12;

            // ラベルスタイル
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.normal.textColor = Color.gray;
            _labelStyle.fontSize = 11;

            // 値スタイル
            _valueStyle = new GUIStyle(GUI.skin.label);
            _valueStyle.normal.textColor = Color.white;
            _valueStyle.fontSize = 11;

            // YESスタイル（緑）
            _yesStyle = new GUIStyle(GUI.skin.label);
            _yesStyle.normal.textColor = new Color(0.3f, 1f, 0.3f);
            _yesStyle.fontSize = 11;

            // NOスタイル（赤）
            _noStyle = new GUIStyle(GUI.skin.label);
            _noStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);
            _noStyle.fontSize = 11;

            // 警告スタイル（黄）
            _warningStyle = new GUIStyle(GUI.skin.label);
            _warningStyle.normal.textColor = new Color(1f, 1f, 0.3f);
            _warningStyle.fontSize = 11;

            _stylesInitialized = true;
        }

        /// <summary>
        /// 単色テクスチャを生成
        /// </summary>
        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        #endregion

        #region OnGUI

        /// <summary>
        /// デバッグUI描画
        /// </summary>
        private void OnGUI()
        {
            // キー入力で表示切り替え
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == k_ToggleKey)
            {
                _showDebugUI = !_showDebugUI;
                Event.current.Use();
            }

            if (!_showDebugUI)
                return;

            InitializeStyles();

            // 右側に配置
            float xPos = Screen.width - k_UIWidth - k_RightMargin;
            float yPos = k_TopMargin;

            GUILayout.BeginArea(new Rect(xPos, yPos, k_UIWidth, Screen.height - k_TopMargin * 2), _boxStyle);
            GUILayout.BeginVertical();

            // タイトル
            DrawTitle();

            // 基本情報
            DrawBasicInfo();

            // 行動可能フラグ
            DrawActionFlags();

            // タイミング制御
            DrawTimingControl();

            // 戦術情報
            DrawStrategyInfo();

            // 距離情報
            DrawDistanceInfo();

            // 統計
            DrawStatistics();

            // 操作説明
            DrawHelpText();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion

        #region 描画メソッド

        /// <summary>
        /// タイトル描画
        /// </summary>
        private void DrawTitle()
        {
            GUIStyle titleStyle = new GUIStyle(_headerStyle);
            titleStyle.fontSize = 14;
            titleStyle.normal.textColor = new Color(0.2f, 1f, 0.5f);

            GUILayout.Label("=== StrategyAI Debug ===", titleStyle);
            GUILayout.Space(5);
        }

        /// <summary>
        /// 基本情報描画
        /// </summary>
        private void DrawBasicInfo()
        {
            DrawSectionHeader("【基本情報】");

            // 現在の状態
            string stateText = _myStateSystem != null
                ? _myStateSystem.CurrentState.CurrentValue.ToString()
                : "---";
            DrawLabelValue("Current State:", stateText);

            // 現在の状況
            DrawLabelValue("Condition:", _currentCondition.ToString());

            // 現在の構え
            string stanceText = _myStateSystem != null
                ? _myStateSystem.CurrentStance.CurrentValue.ToString()
                : "---";
            DrawLabelValue("Stance:", stanceText);

            // 移動ベクトル
            DrawLabelValue("Move Vector:", $"({_moveInputVector.x:F2}, {_moveInputVector.y:F2}, {_moveInputVector.z:F2})");

            GUILayout.Space(5);
        }

        /// <summary>
        /// 行動可能フラグ描画
        /// </summary>
        private void DrawActionFlags()
        {
            DrawSectionHeader("【行動可能フラグ】");

            if (_myStateSystem != null)
            {
                var state = _myStateSystem.CurrentState.CurrentValue;

                DrawFlagRow("攻撃可能:", (state & Core.StateSystem.ActionState.攻撃可能) != 0);
                DrawFlagRow("防御可能:", (state & Core.StateSystem.ActionState.防御可能) != 0);
                DrawFlagRow("移動可能:", _myStateSystem.CanMove);
                DrawFlagRow("移動中:", _isMoving);
            }
            else
            {
                GUILayout.Label("StateSystem未設定", _warningStyle);
            }

            GUILayout.Space(5);
        }

        /// <summary>
        /// タイミング制御描画
        /// </summary>
        private void DrawTimingControl()
        {
            DrawSectionHeader("【タイミング制御】");

            float nextAttack = Mathf.Max(0, _nextAttackTime - _currentTime);
            float nextStep = Mathf.Max(0, _nextStepTime - _currentTime);
            float nextStance = Mathf.Max(0, _nextStanceChangeTime - _currentTime);

            DrawTimerRow("次回攻撃まで:", nextAttack);
            DrawTimerRow("次回ステップまで:", nextStep);
            DrawTimerRow("次回構え変更まで:", nextStance);

            if (_isMoving)
            {
                float remainingMove = _movementDuration - (_currentTime - _movementStartTime);
                DrawTimerRow("移動残り時間:", Mathf.Max(0, remainingMove));
            }

            GUILayout.Space(5);
        }

        /// <summary>
        /// 戦術情報描画
        /// </summary>
        private void DrawStrategyInfo()
        {
            DrawSectionHeader("【戦術情報】");

            if (_llmData?.CurrentStrategy != null)
            {
                DrawLabelValue("基本戦術:", _llmData.CurrentStrategy.BasicTactic.ToString());
                DrawLabelValue("攻撃基準:", _llmData.CurrentStrategy.AttackCriteria.ToString());
                DrawLabelValue("防御基準:", _llmData.CurrentStrategy.DefenseCriteria.ToString());
            }
            else
            {
                GUILayout.Label("戦術データ未設定", _warningStyle);
            }

            if (_currentParameter != null)
            {
                DrawProgressBar("攻撃頻度:", _currentParameter.attackFrequency, new Color(1f, 0.5f, 0.2f));
                DrawProgressBar("移動積極性:", _currentParameter.movementAggressiveness, new Color(0.2f, 0.7f, 1f));
            }

            GUILayout.Space(5);
        }

        /// <summary>
        /// 距離情報描画
        /// </summary>
        private void DrawDistanceInfo()
        {
            DrawSectionHeader("【距離情報】");

            if (_myStateSystem != null && _enemyStateSystem != null)
            {
                float distance = Vector3.Distance(_myStateSystem.Position, _enemyStateSystem.Position);
                DrawLabelValue("敵との距離:", $"{distance:F1}m");

                // 距離状態の判定と色分け
                string status;
                GUIStyle statusStyle;

                if (_currentParameter != null)
                {
                    if (_currentParameter.IsInDangerRange(distance))
                    {
                        status = "危険！";
                        statusStyle = _noStyle;
                    }
                    else if (_currentParameter.IsInPreferredRange(distance))
                    {
                        status = "適正範囲";
                        statusStyle = _yesStyle;
                    }
                    else if (distance < _currentParameter.preferredCombatDistanceRange.x)
                    {
                        status = "近すぎ";
                        statusStyle = _warningStyle;
                    }
                    else
                    {
                        status = "遠すぎ";
                        statusStyle = _warningStyle;
                    }
                }
                else
                {
                    status = "---";
                    statusStyle = _labelStyle;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("距離状態:", _labelStyle, GUILayout.Width(100));
                GUILayout.Label(status, statusStyle);
                GUILayout.EndHorizontal();

                // 攻撃範囲内かどうか
                float attackRange = Mathf.Sqrt(_myStateSystem.AttackRangePow);
                bool inAttackRange = distance <= attackRange;
                DrawFlagRow("攻撃範囲内:", inAttackRange);
            }
            else
            {
                GUILayout.Label("StateSystem未設定", _warningStyle);
            }

            GUILayout.Space(5);
        }

        /// <summary>
        /// 統計描画
        /// </summary>
        private void DrawStatistics()
        {
            DrawSectionHeader("【統計】");

            if (_strategyResult != null)
            {
                float attackRate = _strategyResult.GetSuccessRate(LLMDataArchitect.StrategyResult.ConditionType.Attack);
                float defenseRate = _strategyResult.GetSuccessRate(LLMDataArchitect.StrategyResult.ConditionType.Defense);
                float seqAttackRate = _strategyResult.GetSuccessRate(LLMDataArchitect.StrategyResult.ConditionType.SequentialAttack);
                float seqDefenseRate = _strategyResult.GetSuccessRate(LLMDataArchitect.StrategyResult.ConditionType.SequentialDefense);

                DrawProgressBar("攻撃成功率:", attackRate, GetRateColor(attackRate));
                DrawProgressBar("防御成功率:", defenseRate, GetRateColor(defenseRate));
                DrawProgressBar("連続攻撃成功率:", seqAttackRate, GetRateColor(seqAttackRate));
                DrawProgressBar("連続防御成功率:", seqDefenseRate, GetRateColor(seqDefenseRate));
            }
            else
            {
                GUILayout.Label("統計データなし", _labelStyle);
            }

            GUILayout.Space(5);
        }

        /// <summary>
        /// 操作説明描画
        /// </summary>
        private void DrawHelpText()
        {
            GUILayout.FlexibleSpace();
            GUIStyle helpStyle = new GUIStyle(_labelStyle);
            helpStyle.fontSize = 9;
            helpStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            GUILayout.Label($"[{k_ToggleKey}] 表示切り替え", helpStyle);
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// セクションヘッダー描画
        /// </summary>
        private void DrawSectionHeader(string text)
        {
            GUILayout.Label(text, _headerStyle);
        }

        /// <summary>
        /// ラベルと値のペア描画
        /// </summary>
        private void DrawLabelValue(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(100));
            GUILayout.Label(value, _valueStyle);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// フラグ行描画（YES/NO）
        /// </summary>
        private void DrawFlagRow(string label, bool value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(100));
            GUILayout.Label(value ? "✓ YES" : "✗ NO", value ? _yesStyle : _noStyle);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// タイマー行描画
        /// </summary>
        private void DrawTimerRow(string label, float seconds)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(100));

            GUIStyle style = seconds > 0 ? _warningStyle : _yesStyle;
            string text = seconds > 0 ? $"{seconds:F1}秒" : "Ready!";
            GUILayout.Label(text, style);

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// プログレスバー描画
        /// </summary>
        private void DrawProgressBar(string label, float value, Color barColor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(100));

            // バー背景
            Rect barRect = GUILayoutUtility.GetRect(100, 14);
            GUI.DrawTexture(barRect, _barBackgroundTexture);

            // バー本体
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(value), barRect.height);
            GUI.DrawTexture(fillRect, CreateColorTexture(barColor));

            // パーセント表示
            GUIStyle percentStyle = new GUIStyle(_valueStyle);
            percentStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(barRect, $"{value * 100:F0}%", percentStyle);

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 成功率に応じた色を取得
        /// </summary>
        private Color GetRateColor(float rate)
        {
            if (rate >= 0.7f)
                return new Color(0.3f, 1f, 0.3f);
            if (rate >= 0.4f)
                return new Color(1f, 1f, 0.3f);
            return new Color(1f, 0.3f, 0.3f);
        }

        #endregion
    }
}
#endif