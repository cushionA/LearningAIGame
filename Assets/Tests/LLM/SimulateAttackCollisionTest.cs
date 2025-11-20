using Cysharp.Threading.Tasks;
using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Systems;
using NUnit.Framework;
using R3;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// SimulateAttackCollisionTest
// 
// 概要: SimulateAttackCollisionメソッドの単体テスト
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// CombatSimulationTest内のSimulateAttackCollisionメソッドが正しく動作することを検証する。
// リフレクションによるAttackHit()呼び出しとStateSystemへの結果通知を確認する。
// テストケースごとに状態変化を記録し、各テスト終了時にログを出力する。
// 
// テスト項目:
// 1. 正常系: 攻撃ヒット時にStateSystemに正しい結果が通知される
// 2. ガード成功時の結果通知
// 3. ブロッキング成功時の結果通知
// 4. DamageStart実行時のコライダー有効化確認
// 
// その他:
// Unity Test Runner PlayMode専用
// 実際のHitSystemとStateSystemを使用（モック化なし）
//=====================================================================================================================

namespace LearningAIGame.CombatSystem.Tests
{
    /// <summary>
    /// SimulateAttackCollisionメソッドの単体テスト
    /// </summary>
    [TestFixture]
    public class SimulateAttackCollisionTest
    {
        #region フィールド

        // キャラクターA (攻撃側)
        private GameObject _characterA;
        private StateSystem _stateSystemA;
        BattleCharacterController _controllerA;
        private HitSystem _hitSystemA;
        private DamageSystemBase _damageSystemA;
        private AttackSystem _attackSystemA;
        private DefenseSystem _defenseSystemA;
        private MovementSystem _moveSystemA;

        // キャラクターB (防御側)
        private GameObject _characterB;
        private StateSystem _stateSystemB;
        BattleCharacterController _controllerB;
        private HitSystem _hitSystemB;
        private DamageSystemBase _damageSystemB;
        private AttackSystem _attackSystemB;
        private DefenseSystem _defenseSystemB;
        private MovementSystem _moveSystemB;

        // 結果検証用
        private HitReportInfo _receivedHitReport;
        private bool _hitReportReceived;

        // 状態変化記録
        private StateChangeRecorder _stateRecorder;

        // プレハブパス
        private const string k_CHARACTER_PREFAB_PATH = "HitSimulateTest";

        #endregion

        #region 状態変化記録クラス

        /// <summary>
        /// 状態変化記録エントリ
        /// </summary>
        private class StateChangeEntry
        {
            public float Timestamp { get; set; }
            public string CharacterName { get; set; }
            public ActionState PreviousState { get; set; }
            public ActionState NewState { get; set; }
            public int Energy { get; set; }
            public int Hp { get; set; }

            public override string ToString()
            {
                return $"[{Timestamp:F3}s] {CharacterName}: {PreviousState} → {NewState} (HP:{Hp}, EN:{Energy})";
            }
        }

        /// <summary>
        /// 状態変化記録クラス
        /// </summary>
        private class StateChangeRecorder : IDisposable
        {
            private readonly List<StateChangeEntry> _entries = new List<StateChangeEntry>();
            private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
            private float _startTime;
            private string _testName;

            /// <summary>
            /// 記録開始
            /// </summary>
            public void StartRecording(StateSystem stateSystemA, StateSystem stateSystemB, string testName)
            {
                _testName = testName;
                _startTime = Time.time;
                _entries.Clear();

                // 既存のサブスクリプションをクリア
                foreach (var sub in _subscriptions)
                {
                    sub?.Dispose();
                }
                _subscriptions.Clear();

                // CurrentStateがnullの場合は警告を出して終了
                if (stateSystemA.CurrentState == null || stateSystemB.CurrentState == null)
                {
                    Debug.LogWarning($"[StateRecorder] {testName}: CurrentStateが初期化されていません。記録をスキップします。");
                    return;
                }

                Debug.Log($"[StateRecorder] {testName}: 状態変化の記録を開始しました");

                // キャラクターAの状態変化を監視
                var subA = stateSystemA.CurrentState
                    .Pairwise()
                    .Subscribe(pair =>
                    {
                        Debug.Log($"[StateRecorder] {testName}: キャラクターAの状態変化を記録 - {pair.Previous} → {pair.Current}");
                        _entries.Add(new StateChangeEntry
                        {
                            Timestamp = Time.time - _startTime,
                            CharacterName = "A",
                            PreviousState = pair.Previous,
                            NewState = pair.Current,
                            Energy = stateSystemA.Energy,
                            Hp = stateSystemA.Hp
                        });
                    });

                // キャラクターBの状態変化を監視
                var subB = stateSystemB.CurrentState
                    .Pairwise()
                    .Subscribe(pair =>
                    {
                        Debug.Log($"[StateRecorder] {testName}: キャラクターBの状態変化を記録 - {pair.Previous} → {pair.Current}");
                        _entries.Add(new StateChangeEntry
                        {
                            Timestamp = Time.time - _startTime,
                            CharacterName = "B",
                            PreviousState = pair.Previous,
                            NewState = pair.Current,
                            Energy = stateSystemB.Energy,
                            Hp = stateSystemB.Hp
                        });
                    });

                _subscriptions.Add(subA);
                _subscriptions.Add(subB);
            }

            /// <summary>
            /// 記録を出力
            /// </summary>
            public void PrintLog()
            {
                Debug.Log($"\n=== 状態変化ログ ({_testName}) ===");
                Debug.Log($"記録エントリ数: {_entries.Count}");
                Debug.Log("-------------------");

                if (_entries.Count == 0)
                {
                    Debug.Log("(状態変化なし)");
                }
                else
                {
                    foreach (var entry in _entries)
                    {
                        Debug.Log(entry.ToString());
                    }
                }

                Debug.Log("===================\n");
            }

            /// <summary>
            /// 記録停止
            /// </summary>
            public void StopRecording()
            {
                foreach (var sub in _subscriptions)
                {
                    sub?.Dispose();
                }
                _subscriptions.Clear();
            }

            /// <summary>
            /// 記録取得
            /// </summary>
            public IReadOnlyList<StateChangeEntry> GetEntries() => _entries;

            /// <summary>
            /// 破棄
            /// </summary>
            public void Dispose()
            {
                StopRecording();
            }
        }

        #endregion

        #region セットアップ・ティアダウン

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null; // Awake/Start実行を待機

            // プレハブロードとインスタンス化
            LoadAndInstantiateCharacters();

            // コンポーネント取得
            GetComponents();

            // StateSystemのObserver登録
            _hitReportReceived = false;
            _hitSystemA.Observable.Subscribe(OnHitReportReceived);
            _hitSystemB.Observable.Subscribe(OnHitReportReceived);

            // 状態変化記録の初期化（各テストケースで開始する）
            _stateRecorder = new StateChangeRecorder();

            _stateSystemA.CreateLLMSourceData();
            _stateSystemB.CreateLLMSourceData();

            while (_stateSystemA.CurrentState == null || _stateSystemB.CurrentState == null)
            {
                Debug.Log("[SetUp] CurrentStateが初期化されていません。テストの信頼性に影響する可能性があります。");
                yield return null; // 実行を待機
            }

            Debug.Log("[SetUp] テスト環境セットアップ完了");
        }

        /// <summary>
        /// プレハブの読み込みとインスタンス化
        /// </summary>
        private void LoadAndInstantiateCharacters()
        {
            // プレハブロード
            GameObject prefab = Resources.Load<GameObject>(k_CHARACTER_PREFAB_PATH);
            Assert.IsNotNull(prefab, $"キャラクタープレハブが見つかりません: {k_CHARACTER_PREFAB_PATH}");

            _characterA = UnityEngine.Object.Instantiate(prefab);
            _characterA.name = "CharacterA";
            _characterA.transform.position = Vector3.zero;

            // キャラクターB（子オブジェクトから取得）
            _characterB = _characterA.transform.GetChild(1).gameObject;
            _characterB.name = "CharacterB";
            _characterB.transform.position = Vector3.forward * 2f;

            Debug.Log("[Setup] キャラクタープレハブをインスタンス化しました");
        }

        /// <summary>
        /// コンポーネントの取得
        /// </summary>
        private void GetComponents()
        {
            // キャラクターA
            _stateSystemA = _characterA.GetComponent<StateSystem>();
            Assert.IsNotNull(_stateSystemA, "キャラクターAのStateSystemが見つかりません");

            _controllerA = _characterA.GetComponent<BattleCharacterController>();
            Assert.IsNotNull(_controllerA, "キャラクターAのBattleCharacterControllerが見つかりません");

            _hitSystemA = _characterA.GetComponent<HitSystem>();
            Assert.IsNotNull(_hitSystemA, "キャラクターAのHitSystemが見つかりません");

            _damageSystemA = _characterA.GetComponent<DamageSystemBase>();
            Assert.IsNotNull(_damageSystemA, "キャラクターAのDamageSystemが見つかりません");

            _attackSystemA = _characterA.GetComponent<AttackSystem>();
            Assert.IsNotNull(_attackSystemA, "キャラクターAのAttackSystemが見つかりません");

            _defenseSystemA = _characterA.GetComponent<DefenseSystem>();
            Assert.IsNotNull(_defenseSystemA, "キャラクターAのDefenseSystemが見つかりません");

            _moveSystemA = _characterA.GetComponent<MovementSystem>();
            Assert.IsNotNull(_moveSystemA, "キャラクターAのMovementSystemが見つかりません");

            // キャラクターB
            _stateSystemB = _characterB.GetComponent<StateSystem>();
            Assert.IsNotNull(_stateSystemB, "キャラクターBのStateSystemが見つかりません");

            _controllerB = _characterB.GetComponent<BattleCharacterController>();
            Assert.IsNotNull(_controllerB, "キャラクターBのBattleCharacterControllerが見つかりません");

            _hitSystemB = _characterB.GetComponent<HitSystem>();
            Assert.IsNotNull(_hitSystemB, "キャラクターBのHitSystemが見つかりません");

            _damageSystemB = _characterB.GetComponent<DamageSystemBase>();
            Assert.IsNotNull(_damageSystemB, "キャラクターBのDamageSystemが見つかりません");

            _attackSystemB = _characterB.GetComponent<AttackSystem>();
            Assert.IsNotNull(_attackSystemB, "キャラクターBのAttackSystemが見つかりません");
            _defenseSystemB = _characterB.GetComponent<DefenseSystem>();
            Assert.IsNotNull(_defenseSystemB, "キャラクターBのDefenseSystemが見つかりません");
            _moveSystemB = _characterB.GetComponent<MovementSystem>();
            Assert.IsNotNull(_moveSystemB, "キャラクターBのMovementSystemが見つかりません");

            Debug.Log("[Setup] すべてのコンポーネントを取得しました");
        }

        [TearDown]
        public void TearDown()
        {
            // 状態変化記録を停止・破棄
            _stateRecorder?.Dispose();

            if (_characterA != null)
                UnityEngine.Object.DestroyImmediate(_characterA);

            if (_characterB != null)
                UnityEngine.Object.DestroyImmediate(_characterB);

            Debug.Log("[TearDown] テスト環境クリーンアップ完了");
        }

        #endregion

        #region テストケース

        /// <summary>
        /// SimulateAttackCollisionが正しく動作し、StateSystemに結果が届くことを検証
        /// </summary>
        [UnityTest]
        public IEnumerator SimulateAttackCollision_ShouldNotifyStateSystem_WhenAttackHits()
        {
            // 状態変化記録開始
            _stateRecorder.StartRecording(_stateSystemA, _stateSystemB, "AttackHits");

            // Act: SimulateAttackCollisionを実行
            yield return SimulateAttackCollision(_stateSystemA, ActionState.弱攻撃, ActionState.ガード, false);

            // 追加で数フレーム待機（通知処理の完了を確実にする）
            // 攻撃の持続フレーム消化してからじゃないと報告処理が走らないので長めに
            yield return new WaitForSeconds(2f);

            Debug.Log($"CharacterB {_stateSystemB.Hp}");

            // Assert: StateSystemに結果が通知されたことを検証
            Assert.IsTrue(_hitReportReceived, "HitReportがStateSystemに通知されていません");
            Assert.AreEqual(HitResultType.Hit, _receivedHitReport.hitResultType,
                "ヒット結果が期待値と異なります");

            Debug.Log($"[Test Success] HitResult: {_receivedHitReport.hitResultType}");

            // 状態変化ログを出力
            _stateRecorder.PrintLog();
            _stateRecorder.StopRecording();
        }

        /// <summary>
        /// SimulateAttackCollisionが正しく動作し、StateSystemに結果が届くことを検証
        /// </summary>
        [UnityTest]
        public IEnumerator SimulateAttackCollision_ShouldNotifyStateSystem_WhenHeavyAttackHits()
        {
            // 状態変化記録開始
            _stateRecorder.StartRecording(_stateSystemA, _stateSystemB, "AttackHits");

            // Act: SimulateAttackCollisionを実行
            yield return SimulateAttackCollision(_stateSystemA, ActionState.強攻撃, ActionState.ガード, false);

            // 追加で数フレーム待機（通知処理の完了を確実にする）
            // 攻撃の持続フレーム消化してからじゃないと報告処理が走らないので長めに
            yield return new WaitForSeconds(2f);

            Debug.Log($"CharacterB {_stateSystemB.Hp}");

            // Assert: StateSystemに結果が通知されたことを検証
            Assert.IsTrue(_hitReportReceived, "HitReportがStateSystemに通知されていません");
            Assert.AreEqual(HitResultType.Hit, _receivedHitReport.hitResultType,
                "ヒット結果が期待値と異なります");

            Debug.Log($"[Test Success] HitResult: {_receivedHitReport.hitResultType}");

            // 状態変化ログを出力
            _stateRecorder.PrintLog();
            _stateRecorder.StopRecording();
        }

        /// <summary>
        /// SimulateAttackCollisionが正しく動作し、StateSystemに結果が届くことを検証
        /// </summary>
        [UnityTest]
        public IEnumerator SimulateAttackCollision_ShouldNotifyStateSystem_WhenAvoidAttackHits()
        {
            // 状態変化記録開始
            _stateRecorder.StartRecording(_stateSystemA, _stateSystemB, "AttackHits");

            // Act: SimulateAttackCollisionを実行
            yield return SimulateAttackCollision(_stateSystemA, ActionState.横回避攻撃, ActionState.ガード, false);

            // 追加で数フレーム待機（通知処理の完了を確実にする）
            // 攻撃の持続フレーム消化してからじゃないと報告処理が走らないので長めに
            yield return new WaitForSeconds(2f);

            Debug.Log($"CharacterB {_stateSystemB.Hp}");

            // Assert: StateSystemに結果が通知されたことを検証
            Assert.IsTrue(_hitReportReceived, "HitReportがStateSystemに通知されていません");
            Assert.AreEqual(HitResultType.Hit, _receivedHitReport.hitResultType,
                "ヒット結果が期待値と異なります");

            Debug.Log($"[Test Success] HitResult: {_receivedHitReport.hitResultType}");

            // 状態変化ログを出力
            _stateRecorder.PrintLog();
            _stateRecorder.StopRecording();
        }

        /// <summary>
        /// ガード成功時の結果がStateSystemに届くことを検証
        /// </summary>
        [UnityTest]
        public IEnumerator SimulateAttackCollision_ShouldNotifyGuardResult_WhenDefenderGuards()
        {
            // 状態変化記録開始
            _stateRecorder.StartRecording(_stateSystemA, _stateSystemB, "GuardSuccess");

            // Act: SimulateAttackCollisionを実行
            yield return SimulateAttackCollision(_stateSystemA, ActionState.弱攻撃, ActionState.ガード, true);

            yield return new WaitForSeconds(0.1f);

            // Assert: ガード結果が通知されたことを検証
            Assert.IsTrue(_hitReportReceived, "HitReportがStateSystemに通知されていません");
            Assert.AreEqual(HitResultType.Guard, _receivedHitReport.hitResultType,
                "ガード結果が期待値と異なります");

            Debug.Log($"[Test Success] HitResult: {_receivedHitReport.hitResultType}");

            // 状態変化ログを出力
            _stateRecorder.PrintLog();
            _stateRecorder.StopRecording();
        }

        /// <summary>
        /// ブロッキング成功時の結果がStateSystemに届くことを検証
        /// </summary>
        [UnityTest]
        public IEnumerator SimulateAttackCollision_ShouldNotifyBlockResult_WhenDefenderBlocks()
        {
            // 状態変化記録開始
            _stateRecorder.StartRecording(_stateSystemA, _stateSystemB, "BlockSuccess");

            // Act: SimulateAttackCollisionを実行
            yield return SimulateAttackCollision(_stateSystemA, ActionState.強攻撃, ActionState.ブロッキング, true);

            yield return new WaitForSeconds(0.1f);

            // Assert: ブロッキング結果が通知されたことを検証
            Assert.IsTrue(_hitReportReceived, "HitReportがStateSystemに通知されていません");
            Assert.AreEqual(HitResultType.Block, _receivedHitReport.hitResultType,
                "ブロッキング結果が期待値と異なります");

            Debug.Log($"[Test Success] HitResult: {_receivedHitReport.hitResultType}");

            // 状態変化ログを出力
            _stateRecorder.PrintLog();
            _stateRecorder.StopRecording();
        }

        /// <summary>
        /// SimulateAttackCollisionが正しく動作し、StateSystemに結果が届くことを検証
        /// </summary>
        [UnityTest]
        public IEnumerator SimulateAttackCollision_ShouldNotifyStateSystem_WhenCounter()
        {
            // 状態変化記録開始
            _stateRecorder.StartRecording(_stateSystemA, _stateSystemB, "BlockSuccess");

            // Act: SimulateAttackCollisionを実行
            yield return SimulateAttackCollision(_stateSystemA, ActionState.弱攻撃, ActionState.ブロッキング, true);

            yield return new WaitForSeconds(0.1f);

            // Assert: ブロッキング結果が通知されたことを検証
            Assert.IsTrue(_hitReportReceived, "HitReportがStateSystemに通知されていません");
            Assert.AreEqual(HitResultType.Block, _receivedHitReport.hitResultType,
                "ブロッキング結果が期待値と異なります");

            Debug.Log($"[TestPart Success] HitResult: {_receivedHitReport.hitResultType}");


            // Act: SimulateAttackCollisionを実行
            yield return SimulateAttackCollision(_stateSystemB, ActionState.強攻撃, ActionState.ガード, false);

            // 追加で数フレーム待機（通知処理の完了を確実にする）
            // 攻撃の持続フレーム消化してからじゃないと報告処理が走らないので長めに
            yield return new WaitForSeconds(2f);

            Debug.Log($"CharacterA {_stateSystemA.Hp}");

            // Assert: StateSystemに結果が通知されたことを検証
            Assert.IsTrue(_hitReportReceived, "HitReportがStateSystemに通知されていません");
            Assert.AreEqual(HitResultType.Hit, _receivedHitReport.hitResultType,
                "ヒット結果が期待値と異なります");

            Debug.Log($"[Test Success] HitResult: {_receivedHitReport.hitResultType}");

            // 状態変化ログを出力
            _stateRecorder.PrintLog();
            _stateRecorder.StopRecording();
        }

        #endregion

        #region テスト対象メソッド（元のコードから抽出）

        /// <summary>
        /// リフレクションを用いて攻撃衝突をシミュレート
        /// 1v1専用の簡素化実装: HitSystem.AttackHit()を直接呼び出す
        /// </summary>
        private IEnumerator SimulateAttackCollision(
            StateSystem attackerStateSystem,
            ActionState attackerState,
            ActionState defenderState,
            bool isDefenseSuccess
            )
        {
            BattleCharacterController attackerController;
            BattleCharacterController defenderController;
            HitSystem attackerHitSystem;
            DamageSystemBase defenderDamageSystem;

            if (attackerStateSystem == _stateSystemA)
            {
                attackerController = _controllerA;
                defenderController = _controllerB;
                attackerHitSystem = _hitSystemA;
                defenderDamageSystem = _damageSystemB;
            }
            else
            {
                attackerController = _controllerB;
                defenderController = _controllerA;
                attackerHitSystem = _hitSystemB;
                defenderDamageSystem = _damageSystemA;
            }


            ExecuteAction(attackerController, attackerState);
            ExecuteAction(defenderController, defenderState);

            // 回避攻撃の場合、攻撃が当たるまで少し待機
            if (((attackerState | defenderState) & (ActionState.横回避攻撃 | ActionState.横回避攻撃)) > 0)
            {
                yield return new WaitForSeconds(0.7f);
            }

            // DamageSystemMockにガード成功を設定
            if (defenderDamageSystem is DamageSystemMock damageMock)
            {
                damageMock.MockSetting(defenderState, isDefenseSuccess ? StanceType.Up : StanceType.Left);
            }

            yield return null; // 1フレーム待機

            // AttackHit()を直接呼び出し（引数不要）
            MethodInfo attackHitMethod = typeof(HitSystem).GetMethod("AttackHit",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(attackHitMethod, "AttackHitメソッドがリフレクションで取得できませんでした");

            attackHitMethod.Invoke(attackerHitSystem, null);

            yield return null; // 判定処理待機

            _stateSystemA.SetNeutral();
            _stateSystemB.SetNeutral();
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// ActionStateに応じてBattleCharacterControllerのメソッドを実行
        /// </summary>
        /// <param name="controller">実行対象のコントローラー</param>
        /// <param name="action">実行するアクション</param>
        /// <param name="stance">構え方向（デフォルト: StanceType.Up）</param>
        public static void ExecuteAction(BattleCharacterController controller, ActionState action, StanceType stance = StanceType.Up)
        {
            switch (action)
            {
                // 攻撃系
                case ActionState.弱攻撃:
                    controller.LightAttackAct(stance).Forget();
                    break;

                case ActionState.強攻撃:
                    controller.HeavyAttackAct(stance).Forget();
                    break;

                // 防御系
                case ActionState.ガード:
                    controller.GuardDirectionChange(stance);
                    break;

                case ActionState.ブロッキング:
                    controller.BlockingAct(stance);
                    break;

                // 回避系
                case ActionState.前回避:
                    controller.AvoidAct(MovementReportType.FrontStep);
                    break;

                case ActionState.横回避:
                    // 横回避は左右をランダムで決定（または引数で指定可能にする）
                    controller.AvoidAct(stance == StanceType.Left ? MovementReportType.LeftStep : MovementReportType.RightStep);
                    break;

                case ActionState.後ろ回避:
                    controller.AvoidAct(MovementReportType.BackStep);
                    break;

                // 回避攻撃系
                case ActionState.前回避攻撃:
                    controller.AvoidAttackAct(MovementReportType.FrontStep).Forget();
                    break;

                case ActionState.横回避攻撃:
                    controller.AvoidAttackAct(stance == StanceType.Left ? MovementReportType.LeftStep : MovementReportType.RightStep).Forget();
                    break;

                default:
                    Debug.LogWarning($"未対応のActionState: {action}");
                    break;
            }
        }

        /// <summary>
        /// StateSystemからのHitReport受信コールバック
        /// </summary>
        private void OnHitReportReceived(HitReportInfo info)
        {
            _receivedHitReport = info;
            _hitReportReceived = true;
            Debug.Log($"[Observer] HitReport受信: {info.hitResultType}");
        }

        #endregion
    }
}