using UnityEngine;
using NaughtyAttributes;

//==============================================ファイルヘッダ===========================================================
// InputSettings
// 
// 概要: 入力システムの設定を管理するScriptableObject
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// 入力感度、デッドゾーン、閾値などの設定を一元管理。
// NaughtyAttributesにより、Inspector上で直感的に編集可能。
// 複数のプリセット設定を用意し、プレイヤーの好みに応じて切り替え可能。
// 移動と方向入力のデッドゾーンを個別に設定可能。
// 
// 使用箇所: InputHandler
// 
// その他:
// プロジェクト内で1つのインスタンスを作成し、各InputHandlerから参照する
//=====================================================================================================================

namespace LearningAIGame.CombatSystem.Settings
{
    /// <summary>
    /// 入力システムの設定データ
    /// プロジェクト全体で共有される入力パラメータを管理
    /// </summary>
    [CreateAssetMenu(
        fileName = "InputSettings",
        menuName = "LearningAIGame/Settings/Input Settings",
        order = 1)]
    public class InputSettings : ScriptableObject
    {
        #region 基本設定

        [Header("=== 基本設定 ===")]
        [Tooltip("この設定の名前(識別用)")]
        public string settingName = "Default";

        [ResizableTextArea]
        [Tooltip("この設定の説明文")]
        public string description = "デフォルトの入力設定";

        [Header("連打制限")]
        [Tooltip("ボタン入力の最小間隔（秒）")]
        public float buttonInputInterval = 0.1f;

        #endregion

        #region マウス設定

        [Header("=== マウス設定 ===")]
        [Range(0.01f, 2.0f)]
        [Tooltip("マウス入力の感度\n高いほど少ない動きで大きく反応")]
        public float mouseSensitivity = 0.1f;

        [Tooltip("マウスのY軸を反転するか")]
        public bool invertMouseY = false;

        #endregion

        #region 移動スティック設定

        [Header("=== 移動スティック設定 ===")]
        [Range(0.5f, 3.0f)]
        [Tooltip("移動スティックの感度\n高いほど敏感に反応")]
        public float moveStickSensitivity = 1.0f;

        [MinMaxSlider(0.01f, 0.5f)]
        [Tooltip("移動スティックのデッドゾーン\n小さな傾きを無視して誤入力を防ぐ")]
        public Vector2 moveStickDeadzoneRange = new Vector2(0.1f, 0.3f);

        [ShowNativeProperty]
        public float MoveStickDeadzone => moveStickDeadzoneRange.x;

        #endregion

        #region 方向入力スティック設定

        [Header("=== 方向入力スティック設定 ===")]
        [Range(0.5f, 3.0f)]
        [Tooltip("方向入力スティックの感度\n攻撃・防御方向の切り替えに使用")]
        public float directionStickSensitivity = 1.0f;

        [MinMaxSlider(0.01f, 0.5f)]
        [Tooltip("方向入力スティックのデッドゾーン\n攻撃・防御方向の誤入力を防ぐ")]
        public Vector2 directionStickDeadzoneRange = new Vector2(0.15f, 0.35f);

        [ShowNativeProperty]
        public float DirectionStickDeadzone => directionStickDeadzoneRange.x;

        #endregion

        #region 構え変更設定

        [Header("=== 構え変更設定 ===")]
        [Range(0.1f, 0.9f)]
        [Tooltip("構えを変更するのに必要な最小入力値\n低いほど敏感に反応")]
        public float stanceChangeThreshold = 0.5f;

        [Range(30f, 60f)]
        [Tooltip("構えを判定する角度(度)\n45度が標準")]
        public float stanceAngleThreshold = 45f;

        [ShowNativeProperty]
        public float StanceAngleCos => Mathf.Cos(stanceAngleThreshold * Mathf.Deg2Rad);

        [Range(0f, 0.5f)]
        [Tooltip("構えが変わった後、元に戻らない時間")]
        public float stanceHoldTime = 0.1f;

        #endregion

        #region デバッグ設定

        [Header("=== デバッグ設定 ===")]
        [Tooltip("画面に入力情報をデバッグ表示するか")]
        public bool showInputDebug = false;

        [ShowIf(nameof(showInputDebug))]
        [Tooltip("デバッグ情報の画面表示位置")]
        public Vector2 debugDisplayPosition = new Vector2(10, 10);

        [Tooltip("入力をコンソールにログ出力するか")]
        public bool logInputs = false;

        #endregion

        #region プリセット機能

        [Button("初心者向け")]
        private void ApplyBeginnerPreset()
        {
            settingName = "Beginner";
            description = "初心者向けの設定: 操作しやすく、誤入力が少ない";

            mouseSensitivity = 0.08f;
            moveStickSensitivity = 1.0f;
            directionStickSensitivity = 0.8f;
            moveStickDeadzoneRange = new Vector2(0.2f, 0.3f);
            directionStickDeadzoneRange = new Vector2(0.25f, 0.4f);
            stanceChangeThreshold = 0.6f;

            Debug.Log("プリセット適用: 初心者向け");
        }

        [Button("中級者向け")]
        private void ApplyIntermediatePreset()
        {
            settingName = "Intermediate";
            description = "中級者向けの設定: バランス型";

            mouseSensitivity = 0.1f;
            moveStickSensitivity = 1.0f;
            directionStickSensitivity = 1.0f;
            moveStickDeadzoneRange = new Vector2(0.15f, 0.25f);
            directionStickDeadzoneRange = new Vector2(0.15f, 0.35f);
            stanceChangeThreshold = 0.5f;

            Debug.Log("プリセット適用: 中級者向け");
        }

        [Button("上級者向け")]
        private void ApplyAdvancedPreset()
        {
            settingName = "Advanced";
            description = "上級者向けの設定: 反応が速く精密だが難易度高";

            mouseSensitivity = 0.15f;
            moveStickSensitivity = 1.2f;
            directionStickSensitivity = 1.2f;
            moveStickDeadzoneRange = new Vector2(0.08f, 0.15f);
            directionStickDeadzoneRange = new Vector2(0.1f, 0.2f);
            stanceChangeThreshold = 0.35f;

            Debug.Log("プリセット適用: 上級者向け");
        }

        [Button("競技向け")]
        private void ApplyCompetitivePreset()
        {
            settingName = "Competitive";
            description = "競技向けの設定: 最速反応、最高精度";

            mouseSensitivity = 0.12f;
            moveStickSensitivity = 1.5f;
            directionStickSensitivity = 1.5f;
            moveStickDeadzoneRange = new Vector2(0.05f, 0.12f);
            directionStickDeadzoneRange = new Vector2(0.05f, 0.15f);
            stanceChangeThreshold = 0.3f;

            Debug.Log("プリセット適用: 競技向け");
        }

        #endregion

        #region マウス感度プリセット

        [Button("低感度")]
        private void SetLowMouseSensitivity()
        {
            mouseSensitivity = 0.05f;
            Debug.Log("マウス感度: 低に設定");
        }

        [Button("中感度")]
        private void SetMediumMouseSensitivity()
        {
            mouseSensitivity = 0.1f;
            Debug.Log("マウス感度: 中に設定");
        }

        [Button("高感度")]
        private void SetHighMouseSensitivity()
        {
            mouseSensitivity = 0.2f;
            Debug.Log("マウス感度: 高に設定");
        }

        #endregion

        #region 移動デッドゾーンプリセット

        [Button("移動:タイト")]
        private void SetTightMoveDeadzone()
        {
            moveStickDeadzoneRange = new Vector2(0.05f, 0.15f);
            Debug.Log("移動デッドゾーン: タイトに設定");
        }

        [Button("移動:通常")]
        private void SetNormalMoveDeadzone()
        {
            moveStickDeadzoneRange = new Vector2(0.15f, 0.25f);
            Debug.Log("移動デッドゾーン: 通常に設定");
        }

        [Button("移動:ルーズ")]
        private void SetLooseMoveDeadzone()
        {
            moveStickDeadzoneRange = new Vector2(0.25f, 0.35f);
            Debug.Log("移動デッドゾーン: ルーズに設定");
        }

        #endregion

        #region 方向デッドゾーンプリセット

        [Button("方向:タイト")]
        private void SetTightDirectionDeadzone()
        {
            directionStickDeadzoneRange = new Vector2(0.05f, 0.15f);
            Debug.Log("方向デッドゾーン: タイトに設定");
        }

        [Button("方向:通常")]
        private void SetNormalDirectionDeadzone()
        {
            directionStickDeadzoneRange = new Vector2(0.15f, 0.35f);
            Debug.Log("方向デッドゾーン: 通常に設定");
        }

        [Button("方向:ルーズ")]
        private void SetLooseDirectionDeadzone()
        {
            directionStickDeadzoneRange = new Vector2(0.25f, 0.45f);
            Debug.Log("方向デッドゾーン: ルーズに設定");
        }

        #endregion

        #region 構え変更プリセット

        [Button("敏感")]
        private void SetSensitiveStance()
        {
            stanceChangeThreshold = 0.3f;
            Debug.Log("構え変更: 敏感に設定");
        }

        [Button("通常")]
        private void SetNormalStance()
        {
            stanceChangeThreshold = 0.5f;
            Debug.Log("構え変更: 通常に設定");
        }

        [Button("慎重")]
        private void SetConservativeStance()
        {
            stanceChangeThreshold = 0.7f;
            Debug.Log("構え変更: 慎重に設定");
        }

        #endregion

        #region 検証メソッド

        [Button("設定を検証")]
        private void ValidateSettings()
        {
            bool isValid = true;
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("=== 入力設定検証レポート ===");

            // マウス感度チェック
            if (mouseSensitivity < 0.01f || mouseSensitivity > 2.0f)
            {
                report.AppendLine("❌ マウス感度が範囲外です");
                isValid = false;
            }

            // 移動デッドゾーンチェック
            if (MoveStickDeadzone < 0.01f || MoveStickDeadzone > 0.5f)
            {
                report.AppendLine("❌ 移動デッドゾーンが範囲外です");
                isValid = false;
            }

            // 方向デッドゾーンチェック
            if (DirectionStickDeadzone < 0.01f || DirectionStickDeadzone > 0.5f)
            {
                report.AppendLine("❌ 方向デッドゾーンが範囲外です");
                isValid = false;
            }

            // 構え閾値チェック
            if (stanceChangeThreshold < 0.1f || stanceChangeThreshold > 0.9f)
            {
                report.AppendLine("❌ 構え変更閾値が範囲外です");
                isValid = false;
            }

            if (isValid)
            {
                report.AppendLine("✅ すべての設定が正常です");
            }

            Debug.Log(report.ToString());
        }

        [Button("デフォルトに戻す")]
        private void ResetToDefault()
        {
            ApplyIntermediatePreset();
            Debug.Log("設定をデフォルトに戻しました");
        }

        #endregion

        #region ユーティリティメソッド

        [Button("設定情報を表示")]
        private void LogSettingsSummary()
        {
            string summary = $@"
=== {settingName} の設定 ===
{description}

【マウス設定】
- 感度: {mouseSensitivity:F3}
- 反転: {(invertMouseY ? "有効" : "無効")}

【移動スティック設定】
- 感度: {moveStickSensitivity:F2}
- デッドゾーン: {MoveStickDeadzone:F3}

【方向入力スティック設定】
- 感度: {directionStickSensitivity:F2}
- デッドゾーン: {DirectionStickDeadzone:F3}

【構え設定】
- 変更閾値: {stanceChangeThreshold:F2}
- 判定角度: {stanceAngleThreshold}度

";
            Debug.Log(summary);
        }

        #endregion
    }
}