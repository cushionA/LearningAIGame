/// <summary>
/// 遊び方説明UIのウインドウ切り替え制御
/// ImageContainer配下のウインドウを左右クリックで順番に表示する
/// </summary>

using LearningAIGame.CombatSystem.Singleton;
using UnityEngine;

public class DescriptionUIController : MonoBehaviour
{
    #region フィールド

    [SerializeField, Header("表示するウインドウ配列（表示順）")]
    private GameObject[] _windows;

    private int _currentIndex;
    private bool _isActive;

    #endregion

    #region 公開メソッド

    /// <summary>
    /// ウインドウを最初から開く（ボタンイベント用）
    /// </summary>
    public void Open()
    {
        if (_windows == null || _windows.Length == 0)
            return;

        _currentIndex = 0;
        _isActive = true;
        gameObject.SetActive(true);
        ShowCurrent();
    }

    #endregion

    #region ライフサイクル

    private void Update()
    {
        if (!_isActive)
            return;

        // Escで閉じる
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        // クリック判定
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.mousePosition.x > Screen.width * 0.5f)
                Next();
            else
                Previous();
        }
    }

    #endregion

    #region 内部処理

    private void Next()
    {
        if (_currentIndex >= _windows.Length - 1)
        {
            // 最後のウインドウで右クリック → 閉じる
            Close();
            return;
        }

        GameManager.Instance.PlayButtonClickSE();
        _currentIndex++;
        ShowCurrent();
    }

    private void Previous()
    {
        // 最初のウインドウで左クリック → 何もしない
        if (_currentIndex <= 0)
            return;

        GameManager.Instance.PlayButtonClickSE();
        _currentIndex--;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        for (int i = 0; i < _windows.Length; i++)
        {
            _windows[i].SetActive(i == _currentIndex);
        }
    }

    private void Close()
    {
        GameManager.Instance.PlayButtonClickSE();
        _isActive = false;
        gameObject.SetActive(false);
    }

    #endregion
}