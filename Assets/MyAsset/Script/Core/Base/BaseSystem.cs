using UnityEngine;
using UniRx;
using System;
using System.Runtime.CompilerServices;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// 全システムクラスの基底クラス（ジェネリック版）
    /// UniRx を利用して「イベント発行・購読」の仕組みを提供する。
    /// 継承時に <typeparamref name="T"/> で流すデータ型を指定する。
    /// 例:
    /// - 値付きイベント: BaseSystem<int> (ダメージ量など)
    /// - 値なしイベント: BaseSystem<Unit> (状態変化通知など)
    /// </summary>
    /// <typeparam name="T">Observable を通じて流すデータの型</typeparam>
    public abstract class BaseSystem<T> : MonoBehaviour
    {
        #region === インスペクター設定 ===
        [Header("システム基本設定")]
        [SerializeField] protected bool enableDebugLogs = false;
        #endregion

        #region === 参照キャッシュ ===
        // キャラクターの制御クラス
        protected BattleCharacterController characterController;

        // キャラクターの設定データ
        protected CharacterSettings settings;
        #endregion

        #region === UniRx関連フィールド ===
        /// <summary>
        /// 購読解除をまとめて行うためのコンテナ
        /// </summary>
        protected CompositeDisposable disposables = new CompositeDisposable();

        /// <summary>
        /// このシステムが発行するイベントの実体（Subject）
        /// - OnNext(value) でイベント発行
        /// - AsObservable() で購読専用に変換
        /// </summary>
        protected Subject<T> systemSubject = new Subject<T>();
        #endregion

        #region === 初期化処理 ===
        /// <summary>
        /// 初期化エントリーポイント。
        /// 外部からキャラクター関連の参照を受け取り、設定する。
        /// </summary>
        public virtual void Initialize(BattleCharacterController controller, CharacterSettings characterSettings)
        {
            this.characterController = controller;
            this.settings = characterSettings;

            // 初期化フック（継承先で独自処理可能）
            OnInitialized();

            // イベント購読開始（継承先で実装）
            SetupObservables();
        }

        /// <summary>
        /// 初期化直後に呼ばれるフック。
        /// 継承先で必要ならオーバーライドして利用。
        /// </summary>
        protected virtual void OnInitialized() { }

        /// <summary>
        /// UniRx の購読を設定するためのフック。
        /// 継承先でオーバーライドして利用。
        /// </summary>
        protected virtual void SetupObservables() { }

        #endregion

        #region === Observable プロパティ ===

        /// <summary>
        /// 外部から購読できる Observable。
        /// - systemSubject を AsObservable() でラップすることで
        ///   外部からは購読のみ可能（OnNextは禁止）。
        /// </summary>
        public virtual IObservable<T> Observable => systemSubject.AsObservable();

        /// <summary>
        /// イベントを発行する（Subject.OnNext をラップ）。
        /// 継承先から呼び出してイベントを流す。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void NotifyObservers(T value) => systemSubject.OnNext(value);

        #endregion

        #region === デバッグログ ===

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DebugLog(string message)
        {
            if ( enableDebugLogs )
                Debug.Log($"[{GetType().Name}] {message}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DebugLogWarning(string message)
        {
            if ( enableDebugLogs )
                Debug.LogWarning($"[{GetType().Name}] {message}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DebugLogError(string message)
        {
            Debug.LogError($"[{GetType().Name}] {message}");
        }

        #endregion

        #region === 設定アクセス用プロパティ ===

        /// <summary>キャラクター設定データへのアクセス</summary>
        protected CharacterSettings Settings => settings;

        /// <summary>キャラクター制御クラスへのアクセス</summary>
        protected BattleCharacterController Controller => characterController;

        #endregion

        #region === ライフサイクル管理 ===

        /// <summary>
        /// UnityのOnDestroy。  
        /// イベント購読やSubjectを安全に破棄する。
        /// </summary>
        protected virtual void OnDestroy()
        {
            systemSubject?.OnCompleted();
            systemSubject?.Dispose();
            disposables?.Dispose();
        }

        #endregion

        #region === 検証メソッド ===

        /// <summary>
        /// Initialize が正しく呼ばれているか検証する。
        /// （Controller や Settings が null でないか確認）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ValidateInitialization()
        {
            if ( characterController == null )
            {
                DebugLogError("CharacterControllerが設定されていません");
                return false;
            }

            if ( settings == null )
            {
                DebugLogError("CharacterSettingsが設定されていません");
                return false;
            }

            return true;
        }

        #endregion
    }
}
