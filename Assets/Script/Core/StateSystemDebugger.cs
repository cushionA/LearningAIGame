using UnityEngine;

// ============================================================================
// LearningAIGame - Combat System
// ============================================================================
// ファイル名: StateSystem.Debug.cs
// 概要: StateSystemのデバッグ機能を提供するpartialクラス
// 作成日: [作成日を記入してください]
// 作成者: [作成者名を記入してください]
// ============================================================================
// 説明:
// このクラスはStateSystemのデバッグ情報表示機能を実装します。
// エディタ実行時のみ有効で、以下の機能を提供します:
// - OnGUIによるリアルタイムデバッグ情報の画面表示
// - 状態フラグ、エネルギー、硬直時間などの監視
// - 回避攻撃のバッファ状態の詳細表示
// - コンテキストメニューによる情報出力とUI切替
// ============================================================================
namespace LearningAIGame.CombatSystem.Core
{
    public partial class StateSystem
    {
        #region Debug

#if UNITY_EDITOR

        /// <summary>
        /// デバッグ情報を表示するかどうか
        /// </summary>
        [Header("デバッグ設定")]
        [SerializeField]
        private bool _showDebugInfo = true;

        /// <summary>
        /// デバッグUIの表示位置
        /// </summary>
        [SerializeField]
        private Vector2 _debugWindowPosition = new Vector2(10, 10);

        /// <summary>
        /// デバッグUIのサイズ
        /// </summary>
        [SerializeField]
        private Vector2 _debugWindowSize = new Vector2(450, 800);

        /// <summary>
        /// デバッグログを出力するか
        /// </summary>
        [SerializeField]
        private bool _logDebugInfo = false;

        /// <summary>
        /// ログ出力の間隔(秒)
        /// </summary>
        [SerializeField]
        private float _logInterval = 1f;

        private float _lastLogTime = 0f;

        /// <summary>
        /// デバッグ情報をGUIで表示
        /// </summary>
        private void OnGUI()
        {
            if (!_showDebugInfo)
                return;

            GUILayout.BeginArea(new Rect(_debugWindowPosition, _debugWindowSize));

            // 背景ボックス
            GUI.backgroundColor = new Color(0, 0, 0, 0.8f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = Color.white;

            // タイトル
            GUILayout.Label("=== State System Debug ===", GetHeaderStyle());

            // 基本情報
            DrawBasicInfo();

            GUILayout.Space(5);

            // 行動可能フラグ
            DrawActionFlags();

            GUILayout.Space(5);

            // 回避攻撃詳細情報 ← 新規追加
            DrawAvoidAttackInfo();

            GUILayout.Space(5);

            // 状態情報
            DrawStateInfo();

            GUILayout.Space(5);

            // エネルギー情報
            DrawEnergyInfo();

            GUILayout.Space(5);

            // 硬直時間
            DrawStunInfo();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 基本情報を描画
        /// </summary>
        private void DrawBasicInfo()
        {
            GUILayout.Label("[ 基本情報 ]", GetSubHeaderStyle());

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Current State:", GUILayout.Width(150));
            GUILayout.Label($"{CurrentState.CurrentValue}", GetValueStyle());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Last State:", GUILayout.Width(150));
            GUILayout.Label($"{LastState}", GetValueStyle());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Current Stance:", GUILayout.Width(150));
            GUILayout.Label($"{CurrentStance.CurrentValue}", GetValueStyle());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Move Vector:", GUILayout.Width(150));
            GUILayout.Label($"{MoveVector.CurrentValue.ToString("F2")}", GetValueStyle());
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 回避攻撃の詳細情報を描画
        /// </summary>
        private void DrawAvoidAttackInfo()
        {
            GUILayout.Label("[ 回避攻撃詳細 ]", GetSubHeaderStyle());

            // 回避攻撃可能状態か
            bool isAvoidAttackState = (CurrentState.CurrentValue & ActionState.回避攻撃可能) != 0;
            DrawStateFlag("回避攻撃可能状態", ActionState.回避攻撃可能);

            // バッファ時間の残り
            float remainingBuffer = Mathf.Max(0, _avoidAttackBufferLimit - Time.time);
            bool isBufferActive = remainingBuffer > 0;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"バッファ有効:", GUILayout.Width(150));
            GUILayout.Label($"{(isBufferActive ? "YES" : "NO")}",
                isBufferActive ? GetSuccessStyle() : GetErrorStyle());
            GUILayout.EndHorizontal();

            if (isBufferActive)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"残り受付時間:", GUILayout.Width(150));
                GUILayout.Label($"{remainingBuffer:F3}秒", GetValueStyle());
                GUILayout.EndHorizontal();

                // プログレスバー風の表示
                float progress = remainingBuffer / _actionSetting.AvoidAttackInputDuration;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"受付ゲージ:", GUILayout.Width(150));
                GUILayout.HorizontalSlider(progress, 0f, 1f, GUILayout.Width(150));
                GUILayout.EndHorizontal();
            }

            // 総合判定
            GUILayout.BeginHorizontal();
            GUILayout.Label($"最終判定:", GUILayout.Width(150));
            GUILayout.Label($"{(CanAvoidAttack ? "実行可能" : "実行不可")}",
                CanAvoidAttack ? GetSuccessStyle() : GetErrorStyle());
            GUILayout.EndHorizontal();

            // 実行不可の理由を表示
            if (!CanAvoidAttack)
            {
                GUILayout.Label("実行不可の理由:", GetSubHeaderStyle());

                if (_characterData.IsEnergyExhaust)
                {
                    GUILayout.Label("  - エネルギー枯渇", GetErrorStyle());
                }
                if (Time.time < _moveStunTime)
                {
                    GUILayout.Label("  - 硬直中", GetErrorStyle());
                }
                if (!isAvoidAttackState)
                {
                    GUILayout.Label("  - 回避攻撃可能状態ではない", GetErrorStyle());
                }
                if (!isBufferActive)
                {
                    GUILayout.Label("  - バッファ時間切れ", GetErrorStyle());
                }
            }
        }

        /// <summary>
        /// 行動可能フラグを描画
        /// </summary>
        private void DrawActionFlags()
        {
            GUILayout.Label("[ 行動可能フラグ ]", GetSubHeaderStyle());

            DrawFlag("攻撃可能", CanAttack);
            DrawFlag("ガード方向切替可能", CanChangeGuardDirection);
            DrawFlag("ブロッキング可能", CanBlock);
            DrawFlag("回避可能", CanAvoid);
            DrawFlag("強攻撃キャンセル可能", CanCancelHeavyAttack);
            DrawFlag("移動可能", CanMove);
        }

        /// <summary>
        /// 状態情報を描画
        /// </summary>
        private void DrawStateInfo()
        {
            GUILayout.Label("[ 状態チェック ]", GetSubHeaderStyle());

            // 各状態フラグのチェック
            DrawStateFlag("ガード中", ActionState.ガード);
            DrawStateFlag("ブロッキング中", ActionState.ブロッキング);
            DrawStateFlag("回避中", ActionState.回避);
            DrawStateFlag("攻撃中", ActionState.攻撃);
            DrawStateFlag("防御中", ActionState.防御);
        }

        /// <summary>
        /// エネルギー情報を描画
        /// </summary>
        private void DrawEnergyInfo()
        {
            GUILayout.Label("[ エネルギー情報 ]", GetSubHeaderStyle());

            GUILayout.BeginHorizontal();
            GUILayout.Label($"HP:", GUILayout.Width(150));
            GUILayout.Label($"{_characterData.Hp} / {_characterData.MaxHp}", GetValueStyle());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Energy:", GUILayout.Width(150));
            GUILayout.Label($"{Energy} / {_characterData.MaxEnergy}", GetValueStyle());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Energy Exhausted:", GUILayout.Width(150));
            GUILayout.Label($"{_characterData.IsEnergyExhaust}",
                _characterData.IsEnergyExhaust ? GetErrorStyle() : GetSuccessStyle());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Dead:", GUILayout.Width(150));
            GUILayout.Label($"{_characterData.IsDead}",
                _characterData.IsDead ? GetErrorStyle() : GetSuccessStyle());
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 硬直時間を描画
        /// </summary>
        private void DrawStunInfo()
        {
            GUILayout.Label("[ 硬直情報 ]", GetSubHeaderStyle());

            float remainingStun = Mathf.Max(0, _moveStunTime - Time.time);
            bool isStunned = remainingStun > 0;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"硬直中:", GUILayout.Width(150));
            GUILayout.Label($"{(isStunned ? "YES" : "NO")}",
                isStunned ? GetErrorStyle() : GetSuccessStyle());
            GUILayout.EndHorizontal();

            if (isStunned)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"残り硬直時間:", GUILayout.Width(150));
                GUILayout.Label($"{remainingStun:F2}秒", GetValueStyle());
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 行動可能フラグを描画(ヘルパー)
        /// </summary>
        private void DrawFlag(string label, bool value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}:", GUILayout.Width(150));
            GUILayout.Label(value ? "✓ YES" : "✗ NO",
                value ? GetSuccessStyle() : GetErrorStyle());
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 状態フラグを描画(ヘルパー)
        /// </summary>
        private void DrawStateFlag(string label, ActionState flag)
        {
            bool isActive = (CurrentState.CurrentValue & flag) != 0;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}:", GUILayout.Width(150));
            GUILayout.Label(isActive ? "✓ YES" : "✗ NO",
                isActive ? GetSuccessStyle() : GetErrorStyle());
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// ヘッダースタイル取得
        /// </summary>
        private GUIStyle GetHeaderStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 14;
            style.normal.textColor = Color.yellow;
            return style;
        }

        /// <summary>
        /// サブヘッダースタイル取得
        /// </summary>
        private GUIStyle GetSubHeaderStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 12;
            style.normal.textColor = Color.cyan;
            return style;
        }

        /// <summary>
        /// 値表示スタイル取得
        /// </summary>
        private GUIStyle GetValueStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.white;
            return style;
        }

        /// <summary>
        /// 成功スタイル取得(緑)
        /// </summary>
        private GUIStyle GetSuccessStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.green;
            style.fontStyle = FontStyle.Bold;
            return style;
        }

        /// <summary>
        /// エラースタイル取得(赤)
        /// </summary>
        private GUIStyle GetErrorStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.red;
            style.fontStyle = FontStyle.Bold;
            return style;
        }

        /// <summary>
        /// デバッグ情報をログ出力
        /// Update から呼ぶ
        /// </summary>
        private void DebugLogInfo()
        {
            if (!_logDebugInfo)
                return;
            if (Time.time - _lastLogTime < _logInterval)
                return;

            _lastLogTime = Time.time;

            Debug.Log("=== StateSystem Debug Info ===");
            Debug.Log($"State: {CurrentState.CurrentValue} (Last: {LastState})");
            Debug.Log($"Stance: {CurrentStance.CurrentValue}");
            Debug.Log($"HP: {_characterData.Hp}/{_characterData.MaxHp}");
            Debug.Log($"Energy: {Energy}/{_characterData.MaxEnergy}");
            Debug.Log($"Action Flags: Attack={CanAttack}, Block={CanBlock}, Avoid={CanAvoid}, Move={CanMove}");
            Debug.Log($"Stun: {Mathf.Max(0, _moveStunTime - Time.time):F2}s");
        }

        /// <summary>
        /// Context Menu でデバッグ情報を出力
        /// </summary>
        [UnityEngine.ContextMenu("デバッグ情報を出力")]
        private void DebugPrintInfo()
        {
            Debug.Log("=== StateSystem 詳細情報 ===");
            Debug.Log($"現在の状態: {CurrentState.CurrentValue}");
            Debug.Log($"前回の状態: {LastState}");
            Debug.Log($"構え方向: {CurrentStance.CurrentValue}");
            Debug.Log($"移動ベクトル: {MoveVector.CurrentValue}");
            Debug.Log("");
            Debug.Log("--- 行動可能フラグ ---");
            Debug.Log($"攻撃可能: {CanAttack}");
            Debug.Log($"回避攻撃可能: {CanAvoidAttack}");  // ← 追加
            Debug.Log($"ガード方向切替可能: {CanChangeGuardDirection}");
            Debug.Log($"ブロッキング可能: {CanBlock}");
            Debug.Log($"回避可能: {CanAvoid}");
            Debug.Log($"強攻撃キャンセル可能: {CanCancelHeavyAttack}");
            Debug.Log($"移動可能: {CanMove}");
            Debug.Log("");
            Debug.Log("--- 回避攻撃情報 ---");  // ← 追加
            Debug.Log($"回避攻撃バッファ終了時刻: {_avoidAttackBufferLimit}");
            Debug.Log($"残りバッファ時間: {Mathf.Max(0, _avoidAttackBufferLimit - Time.time):F3}秒");
            Debug.Log($"回避攻撃可能状態: {(CurrentState.CurrentValue & ActionState.回避攻撃可能) != 0}");
            Debug.Log("");
            Debug.Log("--- キャラクター情報 ---");
            Debug.Log($"HP: {_characterData.Hp} / {_characterData.MaxHp}");
            Debug.Log($"Energy: {Energy} / {_characterData.MaxEnergy}");
            Debug.Log($"Energy枯渇中: {_characterData.IsEnergyExhaust}");
            Debug.Log($"死亡: {_characterData.IsDead}");
            Debug.Log("");
            Debug.Log("--- 硬直情報 ---");
            Debug.Log($"硬直終了時刻: {_moveStunTime}");
            Debug.Log($"残り硬直時間: {Mathf.Max(0, _moveStunTime - Time.time):F2}秒");
            Debug.Log("");
            Debug.Log("--- 行動履歴 ---");
            Debug.Log($"履歴数: {_actHistory.Count}");
            if (_actHistory.Count > 0)
            {
                Debug.Log($"直近5件: {string.Join(", ", _actHistory.GetRange(Mathf.Max(0, _actHistory.Count - 5), Mathf.Min(5, _actHistory.Count)))}");
            }
        }

        /// <summary>
        /// Context Menu でデバッグUIの表示切替
        /// </summary>
        [UnityEngine.ContextMenu("デバッグUI表示切替")]
        private void ToggleDebugUI()
        {
            _showDebugInfo = !_showDebugInfo;
            Debug.Log($"デバッグUI表示: {(_showDebugInfo ? "ON" : "OFF")}");
        }

        /// <summary>
        /// Context Menu でログ出力の切替
        /// </summary>
        [UnityEngine.ContextMenu("デバッグログ出力切替")]
        private void ToggleDebugLog()
        {
            _logDebugInfo = !_logDebugInfo;
            Debug.Log($"デバッグログ出力: {(_logDebugInfo ? "ON" : "OFF")}");
        }

#endif

        #endregion
    }
}