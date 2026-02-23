// ============================================================
// ResultRankDisplay.cs
// プロジェクト: LearningAIGame
// 機能: クリアタイムに応じた役職ランクをリザルト画面に表示する
// ============================================================

using LearningAIGame.CombatSystem.Singleton;
using TMPro;
using UnityEngine;

/// <summary>
/// クリアタイムに応じた役職ランクをリザルト画面に表示する
/// </summary>
public class ResultRankDisplay : MonoBehaviour
{
    // ============================================================
    // Inspector設定
    // ============================================================

    [Header("テキスト参照")]
    [SerializeField] private TextMeshProUGUI _rankText;   // 例: 1分30秒で【社長】として
    [SerializeField] private TextMeshProUGUI _resultText; // 復職成功

    // ============================================================
    // 役職ランク閾値（秒）
    // 速いほど上位役職。この値以下なら該当役職に昇格
    // ============================================================

    private static readonly (float maxTime, string title)[] _rankTable =
    {
        (  200f, "社長" ),
        ( 240f, "専務" ),
        ( 280f, "主任" ),
        ( 320f, "部長" ),
        ( 400f, "課長" ),
        ( 450f, "係長" ),
        ( 500f, "平社員" ),
        ( 600f, "見習い" ),
        ( float.MaxValue, "雑巾" ),
    };

    // ============================================================
    // Unity ライフサイクル
    // ============================================================

    private void Start()
    {
        DisplayResult();
    }

    // ============================================================
    // Private メソッド
    // ============================================================

    /// <summary>
    /// クリアタイムを取得してリザルトを表示する
    /// </summary>
    private void DisplayResult()
    {
        float clearTime = GameManager.Instance.ClearTime;
        string timeText = FormatTime(clearTime);
        string rank = GetRank(clearTime);

        _rankText.text = $"{timeText}で【{rank}】として";
        _resultText.text = "復職成功！！";
    }

    /// <summary>
    /// 秒数を「X分XX秒」形式にフォーマットする
    /// </summary>
    /// <param name="seconds">秒数</param>
    /// <returns>フォーマットされた時間文字列</returns>
    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int min = totalSeconds / 60;
        int sec = totalSeconds % 60;

        return min > 0 ? $"{min}分{sec:D2}秒" : $"{sec}秒";
    }

    /// <summary>
    /// クリアタイムに応じた役職を返す
    /// </summary>
    /// <param name="seconds">クリアタイム（秒）</param>
    /// <returns>役職名</returns>
    private string GetRank(float seconds)
    {
        foreach (var (maxTime, title) in _rankTable)
        {
            if (seconds <= maxTime)
                return title;
        }

        // 念のため末尾フォールバック（RankTableに float.MaxValue があるので通常到達しない）
        return "雑巾";
    }
}