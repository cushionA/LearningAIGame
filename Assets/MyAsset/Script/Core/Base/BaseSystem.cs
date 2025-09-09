using UnityEngine;
using UniRx;
using System;
using System.Runtime.CompilerServices;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// 全システムクラスの基底クラス
    /// UniRxのObservableパターンとCharacterSettings参照を提供
    /// </summary>
    public abstract class BaseSystem : MonoBehaviour
    {
        [Header("システム基本設定")]
        [SerializeField] protected bool enableDebugLogs = false;
        
        // === CharacterController参照 ===
        protected BattleCharacterController characterController;
        protected CharacterSettings settings;
        
        // === UniRx Observables ===
        protected CompositeDisposable disposables = new CompositeDisposable();
        
        // === 初期化 ===
        public virtual void Initialize(BattleCharacterController controller, CharacterSettings characterSettings)
        {
            this.characterController = controller;
            this.settings = characterSettings;
            
            // 初期化後の設定
            OnInitialized();
            
            // Observableの購読開始
            SetupObservables();
        }
        
        protected virtual void OnInitialized()
        {
            // 継承先でオーバーライド可能
        }
        
        protected virtual void SetupObservables()
        {
            // 継承先でObservableの設定を行う
        }
        
        // === デバッグログ ===
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[{GetType().Name}] {message}");
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DebugLogWarning(string message)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[{GetType().Name}] {message}");
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DebugLogError(string message)
        {
            Debug.LogError($"[{GetType().Name}] {message}");
        }
        
        // === 設定アクセス ===
        protected CharacterSettings Settings => settings;
        protected BattleCharacterController Controller => characterController;
        
        // === Observable プロパティ（継承先で実装） ===
        protected virtual IObservable<Unit> Observable => UniRx.Observable.Empty<Unit>();
        
        // === 破棄処理 ===
        protected virtual void OnDestroy()
        {
            disposables?.Dispose();
        }
        
        // === 検証メソッド ===
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ValidateInitialization()
        {
            if (characterController == null)
            {
                DebugLogError("CharacterControllerが設定されていません");
                return false;
            }
            
            if (settings == null)
            {
                DebugLogError("CharacterSettingsが設定されていません");
                return false;
            }
            
            return true;
        }
    }

    /// <summary>
    /// 特定の型のObservableを持つシステム用基底クラス
    /// </summary>
    /// <typeparam name="T">Observableで流すデータの型</typeparam>
    public abstract class BaseSystem<T> : BaseSystem
    {
        // === 型付きObservable ===
        protected Subject<T> systemSubject = new Subject<T>();
        
        public IObservable<T> Observable => systemSubject.AsObservable();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void NotifyObservers(T value)
        {
            systemSubject.OnNext(value);
        }
        
        protected override void OnDestroy()
        {
            systemSubject?.OnCompleted();
            systemSubject?.Dispose();
            base.OnDestroy();
        }
    }
}
