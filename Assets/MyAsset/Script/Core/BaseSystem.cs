using UnityEngine;
using R3;
using System;
using System.Runtime.CompilerServices;
using LearningAIGame.CombatSystem.Core;

//=====================================================================================================================
// LearningAIGame
// 
// 概要: キャラクターコントローラーが実行する行動関連クラスの基底クラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// キャラクターコントローラーから指示された行動を実施し、結果や開始を状態管理に投げる。
// 
// 入力元クラス:BattleCharacterController
// 出力先クラス:StateSystem
// 
// その他:
// UniRx → R3対応
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Core
{
    #region === 列挙型定義 ===

    /// <summary>
    /// 攻撃/防御の方向
    /// </summary>
    public enum StanceType : byte
    {
        Up,     // 上
        Left,   // 左
        Right,  // 右
        None    // ガード無し
    }

    #endregion

    /// <summary>
    /// 全システムクラスの基底クラス（ジェネリック版）
    /// <para>
    /// R3 を利用して「イベント発行・購読」の仕組みを提供する。
    /// 継承時に <typeparamref name="T"/> で流すデータ型を指定する。
    /// </para>
    /// </summary>
    /// <typeparam name="T">Observable を通じて流すデータの型</typeparam>
    public abstract class BaseSystem<T> : MonoBehaviour
    {
        #region === 参照キャッシュ ===

        /// <summary>
        /// 移動に使用するクラス
        /// </summary>
        protected MoveController moveController;

        #endregion

        #region === R3関連フィールド ===

        /// <summary>
        /// 購読解除をまとめて行うためのコンテナ
        /// </summary>
        protected CompositeDisposable disposables = new CompositeDisposable();

        /// <summary>
        /// このシステムが発行するイベントの実体（Subject）
        /// </summary>
        protected Subject<T> systemSubject = new Subject<T>();

        #endregion

        #region === 初期化処理 ===

        /// <summary>
        /// 初期化エントリーポイント
        /// </summary>
        public virtual void Initialize()
        {
            OnInitialized();
            SetupObservables();
        }

        protected virtual void OnInitialized() { }

        protected virtual void SetupObservables() { }

        #endregion

        #region === Observable プロパティ ===

        /// <summary>
        /// 外部から購読できる Observable
        /// </summary>
        /// <value>このシステムが発行するイベントを購読するためのObservable</value>
        /// <remarks>
        /// Subject<T>が直接IObservable<T>を実装しているため、そのまま返します。
        /// </remarks>
        public virtual Observable<T> Observable => systemSubject;

        /// <summary>
        /// イベントを発行する（Subject.OnNext をラップ）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void NotifyObservers(T value) => systemSubject.OnNext(value);

        #endregion

        #region === ライフサイクル管理 ===

        protected virtual void OnDestroy()
        {
            systemSubject.OnCompleted();
            systemSubject.Dispose();
            disposables.Dispose();
        }

        #endregion
    }
}
