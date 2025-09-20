using UnityEngine;
using UniRx;
using System;
using System.Runtime.CompilerServices;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// 全システムクラスの基底クラス（ジェネリック版）
    /// <para>
    /// UniRx を利用して「イベント発行・購読」の仕組みを提供する。
    /// 継承時に <typeparamref name="T"/> で流すデータ型を指定する。
    /// </para>
    /// <para>使用例:</para>
    /// <list type="bullet">
    /// <item><description>値付きイベント: BaseSystem&lt;int&gt; (ダメージ量など)</description></item>
    /// <item><description>値なしイベント: BaseSystem&lt;Unit&gt; (状態変化通知など)</description></item>
    /// </list>
    /// </summary>
    /// <typeparam name="T">Observable を通じて流すデータの型</typeparam>
    /// <remarks>
    /// このクラスは戦闘システムアーキテクチャの基盤となる抽象クラスです。
    /// 以下の設計原則に従って実装されています：
    /// <list type="number">
    /// <item><description>責任分離: 各システムは自分の機能のみに集中</description></item>
    /// <item><description>中央集権的状態管理: StateSystemが全ての状態を統合管理</description></item>
    /// <item><description>報告ベース通信: システム間の通信は全てCharacterController経由</description></item>
    /// <item><description>設定ベース挙動制御: ロジックではなく設定値で挙動の違いを表現</description></item>
    /// </list>
    /// </remarks>
    public abstract class BaseSystem<T> : MonoBehaviour
    {
        #region === インスペクター設定 ===

        /// <summary>
        /// デバッグログの出力を有効にするかどうか
        /// </summary>
        /// <remarks>
        /// このフラグがtrueの場合、このシステムの動作に関する詳細なログが出力されます。
        /// 開発・デバッグ時のみ有効にし、リリース時は無効にすることを推奨します。
        /// </remarks>
        [Header("システム基本設定")]
        [SerializeField] protected bool enableDebugLogs = false;

        #endregion

        #region === 参照キャッシュ ===

        /// <summary>
        /// キャラクターの制御クラスへの参照
        /// </summary>
        /// <remarks>
        /// このシステムが属するキャラクターのメインコントローラーです。
        /// 他システムとの通信や共通リソースへのアクセスはこのクラス経由で行います。
        /// </remarks>
        protected BattleCharacterController characterController;

        /// <summary>
        /// キャラクターの設定データへの参照
        /// </summary>
        /// <remarks>
        /// このキャラクターに関する各種パラメータや設定値を格納したScriptableObjectです。
        /// 継承先のシステムは、この設定データを参照して動作を決定します。
        /// </remarks>
        protected CharacterSettings settings;

        #endregion

        #region === UniRx関連フィールド ===

        /// <summary>
        /// 購読解除をまとめて行うためのコンテナ
        /// </summary>
        /// <remarks>
        /// このシステムが行う全てのObservable購読をまとめて管理します。
        /// オブジェクト破棄時に一括で購読解除することで、メモリリークを防止します。
        /// </remarks>
        protected CompositeDisposable disposables = new CompositeDisposable();

        /// <summary>
        /// このシステムが発行するイベントの実体（Subject）
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>OnNext(value) でイベント発行</description></item>
        /// <item><description>AsObservable() で購読専用に変換</description></item>
        /// </list>
        /// 直接的なアクセスは継承先クラス内でのみ許可され、
        /// 外部からは<see cref="Observable"/>プロパティ経由でアクセスします。
        /// </remarks>
        protected Subject<T> systemSubject = new Subject<T>();

        #endregion

        #region === 初期化処理 ===

        /// <summary>
        /// 初期化エントリーポイント
        /// </summary>
        /// <param name="controller">このシステムが属するキャラクターのコントローラー</param>
        /// <param name="characterSettings">キャラクターの設定データ</param>
        /// <remarks>
        /// このメソッドは以下の順序で処理を実行します：
        /// <list type="number">
        /// <item><description>参照の設定</description></item>
        /// <item><description>OnInitialized()フックの呼び出し</description></item>
        /// <item><description>SetupObservables()でイベント購読の設定</description></item>
        /// </list>
        /// 継承先クラスでは、このメソッドを直接オーバーライドするのではなく、
        /// OnInitialized()やSetupObservables()をオーバーライドしてください。
        /// </remarks>
        /// <exception cref="ArgumentNullException">controller または characterSettings が null の場合</exception>
        public virtual void Initialize(BattleCharacterController controller, CharacterSettings characterSettings)
        {
            this.characterController = controller ?? throw new ArgumentNullException(nameof(controller));
            this.settings = characterSettings ?? throw new ArgumentNullException(nameof(characterSettings));

            // 初期化フック（継承先で独自処理可能）
            OnInitialized();

            // イベント購読開始（継承先で実装）
            SetupObservables();
        }

        /// <summary>
        /// 初期化直後に呼ばれるフック
        /// </summary>
        /// <remarks>
        /// 継承先クラスで独自の初期化処理を行いたい場合にオーバーライドしてください。
        /// このメソッドが呼ばれる時点で、characterControllerとsettingsは既に設定済みです。
        /// </remarks>
        protected virtual void OnInitialized() { }

        /// <summary>
        /// UniRx の購読を設定するためのフック
        /// </summary>
        /// <remarks>
        /// 継承先クラスで他のシステムからのイベントを購読したい場合にオーバーライドしてください。
        /// 設定した購読は必ず disposables に AddTo() してください。
        /// <example>
        /// <code>
        /// protected override void SetupObservables()
        /// {
        ///     otherSystem.Observable
        ///         .Subscribe(OnOtherSystemEvent)
        ///         .AddTo(disposables);
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        protected virtual void SetupObservables() { }

        #endregion

        #region === Observable プロパティ ===

        /// <summary>
        /// 外部から購読できる Observable
        /// </summary>
        /// <value>このシステムが発行するイベントを購読するためのObservable</value>
        /// <remarks>
        /// systemSubject を AsObservable() でラップすることで、
        /// 外部からは購読のみ可能（OnNextは禁止）な状態で公開されます。
        /// <example>
        /// <code>
        /// // 他のクラスからの購読例
        /// movementSystem.Observable
        ///     .Subscribe(position => Debug.Log($"位置更新: {position}"))
        ///     .AddTo(disposables);
        /// </code>
        /// </example>
        /// </remarks>
        public virtual IObservable<T> Observable => systemSubject.AsObservable();

        /// <summary>
        /// イベントを発行する（Subject.OnNext をラップ）
        /// </summary>
        /// <param name="value">発行するイベントデータ</param>
        /// <remarks>
        /// 継承先クラスから呼び出してイベントを流します。
        /// このメソッドは高頻度で呼ばれる可能性があるため、AggressiveInlining属性が付与されています。
        /// <example>
        /// <code>
        /// // 継承先での使用例
        /// private void OnDamageTaken(int damage)
        /// {
        ///     // 何らかの処理...
        ///     NotifyObservers(damage); // イベント発行
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void NotifyObservers(T value) => systemSubject.OnNext(value);

        #endregion

        #region === デバッグログ ===

        /// <summary>
        /// 通常のデバッグログを出力します
        /// </summary>
        /// <param name="message">出力するメッセージ</param>
        /// <remarks>
        /// enableDebugLogsがtrueの場合のみログが出力されます。
        /// ログにはクラス名が自動的に付与されます。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DebugLog(string message)
        {
            if ( enableDebugLogs )
                Debug.Log($"[{GetType().Name}] {message}");
        }

        /// <summary>
        /// 警告レベルのデバッグログを出力します
        /// </summary>
        /// <param name="message">出力するメッセージ</param>
        /// <remarks>
        /// enableDebugLogsがtrueの場合のみログが出力されます。
        /// 通常のデバッグログより重要度が高い警告メッセージに使用してください。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DebugLogWarning(string message)
        {
            if ( enableDebugLogs )
                Debug.LogWarning($"[{GetType().Name}] {message}");
        }

        /// <summary>
        /// エラーレベルのデバッグログを出力します
        /// </summary>
        /// <param name="message">出力するメッセージ</param>
        /// <remarks>
        /// enableDebugLogsの設定に関係なく必ず出力されます。
        /// システムの動作に深刻な影響を与えるエラーの報告に使用してください。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DebugLogError(string message)
        {
            Debug.LogError($"[{GetType().Name}] {message}");
        }

        #endregion

        #region === 設定アクセス用プロパティ ===

        /// <summary>
        /// キャラクター設定データへの読み取り専用アクセス
        /// </summary>
        /// <value>このシステムが属するキャラクターの設定データ</value>
        /// <remarks>
        /// 継承先クラスで設定値を参照する際に使用してください。
        /// このプロパティはnullチェック済みの安全なアクセスを提供します。
        /// </remarks>
        protected CharacterSettings Settings => settings;

        /// <summary>
        /// キャラクター制御クラスへの読み取り専用アクセス
        /// </summary>
        /// <value>このシステムが属するキャラクターのメインコントローラー</value>
        /// <remarks>
        /// 他システムとの通信や共通リソースへのアクセスに使用してください。
        /// このプロパティはnullチェック済みの安全なアクセスを提供します。
        /// </remarks>
        protected BattleCharacterController Controller => characterController;

        #endregion

        #region === ライフサイクル管理 ===

        /// <summary>
        /// オブジェクト破棄時の後処理
        /// </summary>
        /// <remarks>
        /// UnityのOnDestroyメソッドです。
        /// 以下の順序でクリーンアップを実行します：
        /// <list type="number">
        /// <item><description>systemSubjectのComplete通知</description></item>
        /// <item><description>systemSubjectの破棄</description></item>
        /// <item><description>全購読の一括解除</description></item>
        /// </list>
        /// 継承先でオーバーライドする場合は、必ずbase.OnDestroy()を呼び出してください。
        /// </remarks>
        protected virtual void OnDestroy()
        {
            systemSubject?.OnCompleted();
            systemSubject?.Dispose();
            disposables?.Dispose();
        }

        #endregion

        #region === 検証メソッド ===

        /// <summary>
        /// 初期化が正しく完了しているかを検証します
        /// </summary>
        /// <returns>初期化が完了している場合はtrue、そうでなければfalse</returns>
        /// <remarks>
        /// 以下の項目をチェックします：
        /// <list type="bullet">
        /// <item><description>CharacterControllerが設定されているか</description></item>
        /// <item><description>CharacterSettingsが設定されているか</description></item>
        /// </list>
        /// 継承先クラスの処理開始前に呼び出して、安全性を確保してください。
        /// <example>
        /// <code>
        /// public void SomeMethod()
        /// {
        ///     if (!ValidateInitialization()) return;
        ///     // 安全に処理を実行...
        /// }
        /// </code>
        /// </example>
        /// </remarks>
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