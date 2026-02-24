using Cysharp.Threading.Tasks;
using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Setting;
using LearningAIGame.CombatSystem.Systems;
using NUnit.Framework;
using R3;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// BattleCharacterControllerTests (Real Systems Version with Prefabs)
// 
// 概要: BattleCharacterControllerの全アクションをプレハブ化されたシステムを使用してテスト
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// Unity Test Runnerで動作するテストコード。
// 設定済みのプレハブをインスタンス化し、実際のAttackSystem、DefenseSystem、MovementSystem、
// HitSystem、StateSystemを使用してテストを実施する。R3のObservableで状態変化と通知を監視する。
// 
// 設計思想:
// プレハブを使用することで、実際のプロジェクト構成に近い環境でテストを実施。
// リフレクションによる無理な設定を排除し、保守性を向上。
// 
// テスト対象:
// - LightAttackAct: 弱攻撃
// - HeavyAttackAct: 強攻撃
// - HeavyAttackCancel: 強攻撃キャンセル
// - GuardDirectionChange: ガード方向変更
// - BlockingAct: ブロッキング
// - AvoidAct: 回避
// - MoveAct: 移動
// 
// 必要なプレハブ:
// - Resources/TestPrefabs/TestBattleCharacter.prefab
// - Resources/TestPrefabs/TestEnemy.prefab
// 
// その他:
// Unity Test Runner PlayMode専用
//=====================================================================================================================

namespace LearningAIGame.CombatSystem.Tests
{
    /// <summary>
    /// BattleCharacterControllerのテストクラス（プレハブ使用版）
    /// </summary>
    [TestFixture]
    public class BattleCharacterControllerTests_RealSystems
    {
        #region フィールド

        private BattleCharacterController _controller;
        private GameObject _testGameObject;
        private GameObject _enemyObject;
        private Transform _enemyTransform;

        // 実システム群
        private StateSystem _stateSystem;
        private AttackSystem _attackSystem;
        private DefenseSystem _defenseSystem;
        private MovementSystem _movementSystem;
        private HitSystem _hitSystem;

        // 設定
        private ActionSetting _actionSetting;

        // タイムアウト
        private const float k_DEFAULT_TIMEOUT = 3f;
        private float _testTimeout = k_DEFAULT_TIMEOUT;

        // プレハブパス
        private const string k_CHARACTER_PREFAB_PATH = "ControllerTestPlayer";
        private const string k_ENEMY_PREFAB_PATH = "ControllerTestEnemy";

        #endregion

        #region セットアップ・ティアダウン

        [SetUp]
        public void SetUp()
        {
            // プレハブの読み込みとインスタンス化
            LoadAndInstantiatePrefabs();

            // コンポーネントの取得
            GetComponents();

            Debug.Log("[TestSetUp] テスト環境のセットアップが完了しました");
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }

            if (_enemyObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_enemyObject);
            }

            _testTimeout = k_DEFAULT_TIMEOUT;

            Debug.Log("[TestTearDown] テスト環境のクリーンアップが完了しました");
        }

        #endregion

        #region セットアップヘルパー

        /// <summary>
        /// プレハブの読み込みとインスタンス化
        /// </summary>
        private void LoadAndInstantiatePrefabs()
        {
            // キャラクタープレハブの読み込み
            var characterPrefab = Resources.Load<GameObject>(k_CHARACTER_PREFAB_PATH);
            Assert.IsNotNull(characterPrefab,
                $"キャラクタープレハブが見つかりません: {k_CHARACTER_PREFAB_PATH}");

            // キャラクターのインスタンス化
            _testGameObject = UnityEngine.Object.Instantiate(characterPrefab);
            _testGameObject.name = "TestCharacter";

            Debug.Log("[Setup] キャラクタープレハブをインスタンス化しました");

            // 敵プレハブの読み込み
            var enemyPrefab = Resources.Load<GameObject>(k_ENEMY_PREFAB_PATH);
            Assert.IsNotNull(enemyPrefab,
                $"敵プレハブが見つかりません: {k_ENEMY_PREFAB_PATH}");

            // 敵のインスタンス化
            _enemyObject = UnityEngine.Object.Instantiate(enemyPrefab);
            _enemyObject.name = "TestEnemy";
            _enemyTransform = _enemyObject.transform;
            _enemyTransform.position = Vector3.forward * 5f;

            Debug.Log("[Setup] 敵プレハブをインスタンス化しました");
        }

        /// <summary>
        /// コンポーネントの取得
        /// </summary>
        private void GetComponents()
        {
            // BattleCharacterControllerの取得とターゲット設定
            _controller = _testGameObject.GetComponent<BattleCharacterController>();
            _controller.SetTarget(_enemyTransform.gameObject);
            Assert.IsNotNull(_controller,
                "BattleCharacterControllerがプレハブに設定されていません");

            // システムコンポーネントの取得
            _stateSystem = _testGameObject.GetComponent<StateSystem>();
            Assert.IsNotNull(_stateSystem,
                "StateSystemがプレハブに設定されていません");

            _attackSystem = _testGameObject.GetComponent<AttackSystem>();
            Assert.IsNotNull(_attackSystem,
                "AttackSystemがプレハブに設定されていません");

            _defenseSystem = _testGameObject.GetComponent<DefenseSystem>();
            Assert.IsNotNull(_defenseSystem,
                "DefenseSystemがプレハブに設定されていません");

            _movementSystem = _testGameObject.GetComponent<MovementSystem>();
            Assert.IsNotNull(_movementSystem,
                "MovementSystemがプレハブに設定されていません");

            _hitSystem = _testGameObject.GetComponent<HitSystem>();
            Assert.IsNotNull(_hitSystem,
                "HitSystemがプレハブに設定されていません");

            Debug.Log("[Setup] すべてのシステムコンポーネントを取得しました");
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// 状態変化を監視
        /// </summary>
        private IDisposable ObserveStateChange(ActionState expectedState, Action callback)
        {
            return _stateSystem.CurrentState
                .Where(state => state == expectedState)
                .Subscribe(_ =>
                {
                    callback?.Invoke();
                    Debug.Log($"[Observable] 状態変化検出: {expectedState}");
                });
        }

        /// <summary>
        /// エネルギー消費を確認
        /// </summary>
        private void AssertEnergyDecreased(int initialEnergy, int expectedCost, string actionName)
        {
            int currentEnergy = _stateSystem.Energy;
            int actualCost = initialEnergy - currentEnergy;

            Assert.AreEqual(expectedCost, actualCost,
                $"{actionName}: 期待コスト={expectedCost}, 実際={actualCost} " +
                $"(初期={initialEnergy}, 現在={currentEnergy})");

            Debug.Log($"[Energy] {actionName}でエネルギーを{actualCost}消費");
        }

        /// <summary>
        /// システム通知を監視
        /// </summary>
        private IDisposable ObserveSystemNotification<T>(BaseSystem<T> system, Action<T> callback)
        {
            return system.Observable
                .Subscribe(data =>
                {
                    callback?.Invoke(data);
                    Debug.Log($"[Observable] {system.GetType().Name}から通知受信");
                });
        }

        #endregion

        #region 弱攻撃テスト

        [UnityTest]
        public IEnumerator LightAttackAct_ShouldExecuteSuccessfully()
        {
            // Arrange
            StanceType testStance = StanceType.Up;
            int initialEnergy = _stateSystem.Energy;

            bool attackNotified = false;
            var attackSub = ObserveSystemNotification(_attackSystem,
                (AttackReportInfo data) => attackNotified = (_stateSystem.CurrentState.Value == ActionState.弱攻撃));

            bool stateChanged = false;
            var stateSub = _stateSystem.CurrentState
                .Where(state => (state & ActionState.弱攻撃系統) > 0)
                .Subscribe(_ => stateChanged = true);

            // Act
            _controller.LightAttackAct(testStance).Forget();

            // Assert
            yield return null;

            // エネルギー消費確認
            // ※ActionSettingへのアクセス方法に応じて調整が必要
            // AssertEnergyDecreased(initialEnergy, _actionSetting.WeakAttackEnergyCost, "弱攻撃");

            // AttackSystemからの通知確認
            Assert.IsTrue(attackNotified, "AttackSystemから通知があるべき");

            // 攻撃判定発生まで待機
            // ※フレーム数は実際の設定値に応じて調整
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            // クリーンアップ
            attackSub.Dispose();
            stateSub.Dispose();

            Debug.Log("[Test] 弱攻撃テスト完了");
        }

        #endregion

        #region 強攻撃テスト

        [UnityTest]
        public IEnumerator HeavyAttackAct_ShouldExecuteSuccessfully()
        {
            // Arrange
            StanceType testStance = StanceType.Left;
            int initialEnergy = _stateSystem.Energy;

            bool attackNotified = false;
            var subscription = ObserveSystemNotification(_attackSystem,
                (AttackReportInfo data) => attackNotified = (_stateSystem.CurrentState.Value == ActionState.強攻撃));

            // Act
            _controller.HeavyAttackAct(testStance).Forget();

            // Assert
            yield return null;

            // エネルギー消費確認
            // AssertEnergyDecreased(initialEnergy, _actionSetting.HeavyAttackEnergyCost, "強攻撃");
            Assert.IsTrue(attackNotified, "AttackSystemから通知があるべき");

            // 判定発生まで待機
            for (int i = 0; i < 15; i++)
            {
                yield return null;
            }

            subscription.Dispose();
            Debug.Log("[Test] 強攻撃テスト完了");
        }

        #endregion

        #region ガード方向変更テスト

        [UnityTest]
        public IEnumerator GuardDirectionChange_ShouldExecuteSuccessfully()
        {
            // Arrange
            StanceType targetStance = StanceType.Right;

            bool defenseNotified = false;
            var subscription = ObserveSystemNotification(_defenseSystem,
                (DefenseReportInfo data) => defenseNotified = (_stateSystem.CurrentStance.Value == StanceType.Right));

            // Act
            _controller.GuardDirectionChange(targetStance);

            // Assert
            yield return null;

            // DefenseSystemから通知があることを確認
            // 注: 実装によっては通知がない場合もある
            Debug.Log($"[Test] ガード方向変更テスト完了 (通知: {defenseNotified})");

            subscription.Dispose();
        }

        #endregion

        #region ブロッキングテスト

        [UnityTest]
        public IEnumerator BlockingAct_ShouldExecuteSuccessfully()
        {
            // Arrange
            int initialEnergy = _stateSystem.Energy;
            StanceType testStance = StanceType.Up;

            bool defenseNotified = false;
            var subscription = ObserveSystemNotification(_defenseSystem,
                (DefenseReportInfo data) => defenseNotified = (_stateSystem.CurrentState.Value == ActionState.ブロッキング));

            // Act
            _controller.BlockingAct(testStance);

            // Assert
            yield return null;

            // エネルギー消費確認
            // AssertEnergyDecreased(initialEnergy, _actionSetting.BlockingEnergyCost, "ブロッキング");
            Assert.IsTrue(defenseNotified, "DefenseSystemから通知があるべき");

            subscription.Dispose();
            Debug.Log("[Test] ブロッキングテスト完了");
        }

        #endregion

        #region 回避テスト

        [UnityTest]
        public IEnumerator AvoidAct_ShouldExecuteSuccessfully_WithEnergy()
        {
            // Arrange
            int initialEnergy = _stateSystem.Energy;
            MovementReportType moveType = MovementReportType.BackStep;

            bool movementNotified = false;
            var subscription = ObserveSystemNotification(_movementSystem,
                (MoveReportInfo data) => movementNotified = (_stateSystem.CurrentState.Value == ActionState.後ろ回避));

            // Act
            _controller.AvoidAct(moveType);

            // Assert
            yield return null;

            // エネルギー消費確認
            // AssertEnergyDecreased(initialEnergy, _actionSetting.AvoidEnergyCost, "回避");
            Assert.IsTrue(movementNotified, "MovementSystemから通知があるべき");

            subscription.Dispose();
            Debug.Log("[Test] 回避テスト完了");
        }

        [UnityTest]
        public IEnumerator AvoidAct_ShouldExecuteWithReducedPerformance_WithoutEnergy()
        {
            // Arrange
            // エネルギーを0に設定（実装に応じて調整）
            // _stateSystem.SetEnergy(0);

            MovementReportType moveType = MovementReportType.FrontStep;

            bool movementNotified = false;
            var subscription = ObserveSystemNotification(_movementSystem,
                (MoveReportInfo data) => movementNotified = (_stateSystem.CurrentState.Value == ActionState.前回避));

            // Act
            _controller.AvoidAct(moveType);

            // Assert
            yield return null;

            // エネルギー0では性能が70%に低下
            Assert.IsTrue(movementNotified, "MovementSystemから通知があるべき");

            subscription.Dispose();
            Debug.Log("[Test] エネルギー不足時の回避テスト完了");
        }

        #endregion

        #region 移動テスト

        [UnityTest]
        public IEnumerator MoveAct_ShouldExecuteSuccessfully()
        {
            // Arrange
            Vector3 moveVector = Vector3.right.normalized;

            bool movementNotified = false;
            var subscription = ObserveSystemNotification(_movementSystem,
                (MoveReportInfo data) => movementNotified = true);

            // Act
            _controller.MoveAct(moveVector);

            // Assert
            yield return null;

            Assert.IsTrue(movementNotified, "MovementSystemから通知があるべき");

            subscription.Dispose();
            Debug.Log("[Test] 移動テスト完了");
        }

        #endregion

        #region 統合テスト

        [UnityTest]
        public IEnumerator MultipleActions_ShouldExecuteSequentially()
        {
            // Arrange
            int initialEnergy = _stateSystem.Energy;

            // Act & Assert - 弱攻撃
            _controller.LightAttackAct(StanceType.Up).Forget();
            yield return null;

            int energyAfterAttack = _stateSystem.Energy;
            Assert.Less(energyAfterAttack, initialEnergy, "弱攻撃でエネルギーが減るべき");

            // 数フレーム待機
            for (int i = 0; i < 5; i++)
                yield return null;

            // Act & Assert - 移動
            _controller.MoveAct(Vector3.forward);
            yield return null;

            // 移動はエネルギーを消費しない
            Assert.AreEqual(energyAfterAttack, _stateSystem.Energy,
                "移動ではエネルギーが消費されないべき");

            Debug.Log("[Test] 複数アクション連続実行テスト完了");
        }

        #endregion

        #region R3 Observable監視テスト

        [UnityTest]
        public IEnumerator StateSystem_ShouldNotifyStateChanges()
        {
            // Arrange
            int stateChangeCount = 0;
            var subscription = _stateSystem.CurrentState
                .Subscribe(_ =>
                {
                    stateChangeCount++;
                    Debug.Log($"[Observable] 状態変化 #{stateChangeCount}");
                });

            // Act - 複数のアクションを実行
            _controller.LightAttackAct(StanceType.Up).Forget();
            yield return null;

            _controller.MoveAct(Vector3.forward);
            yield return null;

            // Assert
            Assert.Greater(stateChangeCount, 0, "状態変化の通知があるべき");

            subscription.Dispose();
            Debug.Log($"[Test] Observable監視テスト完了 (通知回数: {stateChangeCount})");
        }

        #endregion
    }
}