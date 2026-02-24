using UnityEngine;

/// <summary>
/// ゲームの進行状態に応じて動作するマネージャーインターフェース
/// </summary>
public interface IGameHelper
{
    /// <summary>
    /// ゲーム開始前などで動作をロックする
    /// </summary>
    public void Lock();

    /// <summary>
    /// 動作ロックを解除する
    /// </summary>
    public void Unlock();

    /// <summary>
    /// 初期状態にセットアップする
    /// </summary>
    public void SetUp();

    /// <summary>
    /// 戦闘開始時の処理
    /// </summary>
    public void RoundStart();

    /// <summary>
    /// 戦闘終了時の処理
    /// </summary>
    public void RoundEnd();

    /// <summary>
    /// ゲーム終了時の処理
    /// </summary>
    public void GameEnd();
}
