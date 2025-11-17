using Cysharp.Threading.Tasks;
using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Systems;
using LLMDataArchitect;
using NUnit.Framework;
using R3;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;
using static LearningAIGame.CombatSystem.Core.StateSystem;
using static LLMDataArchitect.StrategyData;

//==============================================ファイルヘッダ===========================================================
// CombatSimulationTest (Enhanced)
// 
// 概要: 2体のキャラクターが攻撃と防御を繰り返す戦闘シミュレーションテスト
// 
// 追加機能:
// - ターン数制限（デフォルト50ターン、カスタマイズ可能）
// - 攻撃処理の単体テスト（SimulateAttackCollisionTestベース）
// - 詳細なテキストログ出力
// 
// 制作者: 小さな座布団
//=====================================================================================================================

namespace LearningAIGame.CombatSystem.Tests
{
    /// <summary>
    /// 戦闘シミュレーションテストクラス（拡張版）
    /// </summary>
    [TestFixture]
    public class CombatSimulationTest
    {
        #region フィールド

        // キャラクターA (事前設定行動テーブル)
        private GameObject _characterA;
        private DebugStateSystem _stateSystemA;
        private DebugHitSystem _hitSystemA;
        private DamageSystemBase _damageSystemA;
        private BattleCharacterController _controllerA;

        // キャラクターB (LLM生成行動テーブル)
        private GameObject _characterB;
        private DebugStateSystem _stateSystemB;
        private DebugHitSystem _hitSystemB;
        private DamageSystemBase _damageSystemB;
        private BattleCharacterController _controllerB;

        // LLM通信
        private LLMCommunicator _llmCommunicator;
        private LLMInputData _inputDataB;
        private CombatTestAI _injectionAI_B;

        // 行動テーブル
        private CombatSimulationTestData _testData;
        private StrategyData _currentStrategyA;
        private StrategyData _currentStrategyB;
        private AIParameter _currentParameterA;
        private AIParameter _currentParameterB;

        // シミュレーション設定
        [SerializeField] private int _tableSwitchCycle = 6;
        [SerializeField] private int _initialHP = 500;
        [SerializeField] private int _energyRecoveryPerTurn = 6;
        [SerializeField] private bool _characterAFirst = true;
        [SerializeField] private int _maxTurns = 1000;
        private int _exhaustRecoverCount = 6; // エネルギー枯渇からの回復ターン数

        private int _exhaustCountA = 0;
        private int _exhaustCountB = 0;

        // シミュレーション状態
        private int _currentTurn = 0;
        private bool _isCharacterAAttacking;
        private bool _simulationComplete = false;
        private string _winner = "";
        private bool _isNeedLog = false;

        // プレハブパス
        private const string k_CHARACTER_A_PREFAB_PATH = "SimulateTestPlayer";
        private const string k_TEST_DATA_PATH = "ComplexTestSetting";

        // ログ記録
        private List<string> _combatLog = new List<string>();

        // 単体テスト用
        private HitReportInfo _receivedHitReport;
        private bool _hitReportReceived;

        // ★追加: 詳細ログ出力用
        private static readonly string _defaultLogOutputPath = @"C:\Users\tatuk\Desktop\GameDev\LearningAIGame\Assets\Tests\Result";
        private string _logOutputPath = _defaultLogOutputPath;
        private StringBuilder _detailedLog = new StringBuilder();
        private TurnLogData _currentTurnLog;

        private int _lightAttackDamage = 6;
        private int _heavyAttackDamage = 15;

        #endregion

        #region ★追加: ターンログデータ構造

        /// <summary>
        /// ターンごとのログデータ
        /// </summary>
        private class TurnLogData
        {
            public int TurnNumber;

            // ターン開始時の状態
            public int StartHpA;
            public int StartHpB;
            public int StartEnergyA;
            public int StartEnergyB;

            // 攻撃側・防御側
            public string AttackerName;
            public string DefenderName;

            // 戦術情報
            public string AttackerTactic;
            public string DefenderTactic;

            // 行動選択
            public string AttackAction;
            public string AttackCriteria;
            public string DefenseAction;
            public string DefenseCriteria;

            // 判定結果
            public string ActionResult;
            public int DamageDealt;
            public int DamageTaken;

            // 確定反撃
            public bool HasCounterAttack;
            public string CounterAttackResult;

            // 連続攻撃
            public bool HasContinuousAttack;
            public string ContinuousAttackResult;

            // ターン終了時の状態
            public int EndHpA;
            public int EndHpB;
            public int EndEnergyA;
            public int EndEnergyB;

            // 状況評価
            public string SituationAssessment;
        }

        #endregion

        #region ActionResultTable
        // (省略: 元のコードと同じ)

        private static class ActionResultTable
        {
            public enum ResultValue : sbyte
            {
                DefenseDirectCounterLight = 3,
                DefenseCounterHeavy = 2,
                DefenseCounterLight = 1,
                Draw = 0,
                AttackLight = -1,
                AttackHeavy = -2,
                AttackerDefenseCounterLight = -3,
                AttackerDefenseCounterHeavy = -4,
                AttackerCancelLight = -5,
                AttackerCancelHeavy = -6,
            }

            private static readonly Dictionary<(ActionState defense, ActionState attack), ResultValue> _table
                = new Dictionary<(ActionState, ActionState), ResultValue>
            {
                // 後ろ回避
                { (ActionState.後ろ回避, ActionState.弱攻撃), ResultValue.Draw },
                { (ActionState.後ろ回避, ActionState.強攻撃), ResultValue.Draw },
                { (ActionState.後ろ回避, ActionState.強攻撃キャンセル), ResultValue.Draw },
                { (ActionState.後ろ回避, ActionState.前回避), ResultValue.Draw },
                { (ActionState.後ろ回避, ActionState.前回避攻撃), ResultValue.AttackLight },
                { (ActionState.後ろ回避, ActionState.ブロッキング), ResultValue.Draw },

                // 横回避
                { (ActionState.横回避, ActionState.弱攻撃), ResultValue.Draw },
                { (ActionState.横回避, ActionState.強攻撃), ResultValue.AttackHeavy },
                { (ActionState.横回避, ActionState.強攻撃キャンセル), ResultValue.AttackLight },
                { (ActionState.横回避, ActionState.前回避), ResultValue.Draw },
                { (ActionState.横回避, ActionState.前回避攻撃), ResultValue.DefenseCounterLight },
                { (ActionState.横回避, ActionState.ブロッキング), ResultValue.DefenseCounterLight },

                // ガード
                { (ActionState.ガード, ActionState.弱攻撃), ResultValue.DefenseCounterLight },
                { (ActionState.ガード, ActionState.強攻撃), ResultValue.AttackHeavy },
                { (ActionState.ガード, ActionState.強攻撃キャンセル), ResultValue.Draw },
                { (ActionState.ガード, ActionState.前回避), ResultValue.Draw },
                { (ActionState.ガード, ActionState.前回避攻撃), ResultValue.DefenseCounterLight },
                { (ActionState.ガード, ActionState.ブロッキング), ResultValue.Draw },

                // 強攻撃ブロッキング
                { (ActionState.強ブロッキング, ActionState.弱攻撃), ResultValue.AttackLight },
                { (ActionState.強ブロッキング, ActionState.強攻撃), ResultValue.DefenseCounterLight },
                { (ActionState.強ブロッキング, ActionState.強攻撃キャンセル), ResultValue.AttackerCancelHeavy },
                { (ActionState.強ブロッキング, ActionState.前回避), ResultValue.AttackLight },
                { (ActionState.強ブロッキング, ActionState.前回避攻撃), ResultValue.AttackLight },
                { (ActionState.強ブロッキング, ActionState.ブロッキング), ResultValue.Draw },

                // 弱攻撃ブロッキング
                { (ActionState.弱ブロッキング, ActionState.弱攻撃), ResultValue.DefenseCounterHeavy },
                { (ActionState.弱ブロッキング, ActionState.強攻撃), ResultValue.AttackHeavy },
                { (ActionState.弱ブロッキング, ActionState.強攻撃キャンセル), ResultValue.AttackerCancelLight },
                { (ActionState.弱ブロッキング, ActionState.前回避), ResultValue.AttackLight },
                { (ActionState.弱ブロッキング, ActionState.前回避攻撃), ResultValue.DefenseCounterHeavy },
                { (ActionState.弱ブロッキング, ActionState.ブロッキング), ResultValue.Draw },

                // 横回避攻撃
                { (ActionState.横回避攻撃, ActionState.弱攻撃), ResultValue.DefenseDirectCounterLight },
                { (ActionState.横回避攻撃, ActionState.強攻撃), ResultValue.DefenseDirectCounterLight },
                { (ActionState.横回避攻撃, ActionState.強攻撃キャンセル), ResultValue.AttackerDefenseCounterHeavy },
                { (ActionState.横回避攻撃, ActionState.前回避), ResultValue.DefenseDirectCounterLight },
                { (ActionState.横回避攻撃, ActionState.前回避攻撃), ResultValue.DefenseDirectCounterLight },
                { (ActionState.横回避攻撃, ActionState.ブロッキング), ResultValue.DefenseDirectCounterLight },
                { (ActionState.横回避攻撃, ActionState.ガード), ResultValue.AttackerDefenseCounterLight },

                // 弱攻撃（防御側が弱攻撃で迎撃する場合）
                { (ActionState.弱攻撃, ActionState.弱攻撃), ResultValue.Draw },
                { (ActionState.弱攻撃, ActionState.強攻撃), ResultValue.DefenseDirectCounterLight },
                { (ActionState.弱攻撃, ActionState.強攻撃キャンセル), ResultValue.DefenseDirectCounterLight },
                { (ActionState.弱攻撃, ActionState.前回避), ResultValue.DefenseDirectCounterLight },
                { (ActionState.弱攻撃, ActionState.前回避攻撃), ResultValue.DefenseDirectCounterLight },
                { (ActionState.弱攻撃, ActionState.ブロッキング), ResultValue.AttackerDefenseCounterHeavy },
                { (ActionState.弱攻撃, ActionState.ガード), ResultValue.AttackerDefenseCounterLight },
            };

            public static ResultValue GetResult(ActionState defenseAction, ActionState attackAction)
            {
                if (_table.TryGetValue((defenseAction, attackAction), out ResultValue result))
                {
                    return result;
                }
                return ResultValue.Draw;
            }
        }

        #endregion

        #region セットアップ・ティアダウン

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;

            LoadAndInstantiateCharacters();
            GetComponents();
            SetupInitialState();
            InitializeActionTables();

            // HitReport監視
            _hitReportReceived = false;
            _hitSystemA.Observable.Subscribe(OnHitReportReceived);
            _hitSystemB.Observable.Subscribe(OnHitReportReceived);

            _stateSystemA.Hp = _initialHP;
            _stateSystemB.Hp = _initialHP;

            // ★追加: 詳細ログ初期化
            _detailedLog.Clear();
            InitializeDetailedLog();

            Debug.Log("[CombatSimTest SetUp] テスト環境のセットアップが完了しました");
        }

        [TearDown]
        public void TearDown()
        {
            OutputCombatLog();

            // ★追加: 詳細ログをファイルに出力
            SaveDetailedLog();

            if (_characterA != null)
                UnityEngine.Object.DestroyImmediate(_characterA);

            if (_characterB != null)
                UnityEngine.Object.DestroyImmediate(_characterB);

            Debug.Log("[CombatSimTest TearDown] テスト環境のクリーンアップが完了しました");
        }

        // (LoadAndInstantiateCharacters, GetComponents, SetupInitialState, InitializeActionTables は元のコードと同じ)

        private void LoadAndInstantiateCharacters()
        {
            _testData = Resources.Load<CombatSimulationTestData>(k_TEST_DATA_PATH);
            Assert.IsNotNull(_testData, $"テストデータが見つかりません: {k_TEST_DATA_PATH}");

            var prefabA = Resources.Load<GameObject>(k_CHARACTER_A_PREFAB_PATH);
            Assert.IsNotNull(prefabA, $"キャラクターAプレハブが見つかりません: {k_CHARACTER_A_PREFAB_PATH}");

            _characterA = UnityEngine.Object.Instantiate(prefabA);
            _characterA.name = "CharacterA";
            _characterA.transform.position = Vector3.zero;

            _characterB = _characterA.transform.GetChild(1).gameObject;
            _characterB.transform.SetParent(null);
            _characterB.name = "CharacterB";
            _characterB.transform.position = Vector3.forward * 5f;

            Debug.Log("[Setup] キャラクタープレハブをインスタンス化しました");
        }

        private void GetComponents()
        {
            _stateSystemA = _characterA.GetComponent<DebugStateSystem>();
            Assert.IsNotNull(_stateSystemA);

            _hitSystemA = _characterA.GetComponent<DebugHitSystem>();
            Assert.IsNotNull(_hitSystemA);

            _damageSystemA = _characterA.GetComponent<DamageSystemBase>();
            Assert.IsNotNull(_damageSystemA);

            _controllerA = _characterA.GetComponent<BattleCharacterController>();
            Assert.IsNotNull(_controllerA);

            _stateSystemB = _characterB.GetComponent<DebugStateSystem>();
            Assert.IsNotNull(_stateSystemB);

            _hitSystemB = _characterB.GetComponent<DebugHitSystem>();
            Assert.IsNotNull(_hitSystemB);

            _damageSystemB = _characterB.GetComponent<DamageSystemBase>();
            Assert.IsNotNull(_damageSystemB);

            _controllerB = _characterB.GetComponent<BattleCharacterController>();
            Assert.IsNotNull(_controllerB);

            _llmCommunicator = _characterB.GetComponent<LLMCommunicator>();
            Assert.IsNotNull(_llmCommunicator);

            _injectionAI_B = _characterB.GetComponent<CombatTestAI>();
            Assert.IsNotNull(_injectionAI_B);

            Debug.Log("[Setup] すべてのコンポーネントを取得しました");
        }

        private void SetupInitialState()
        {
            _isCharacterAAttacking = _characterAFirst;
            _inputDataB = _llmCommunicator.GetCurrentInputData();
            Assert.IsNotNull(_inputDataB);

            Debug.Log("[Setup] 初期状態を設定しました");
        }

        private void InitializeActionTables()
        {
            Assert.IsNotNull(_testData);
            Assert.IsTrue(_testData.IsValid);

            if (!_testData.Validate(out string errorMessage))
            {
                Assert.Fail($"テストデータの検証に失敗: {errorMessage}");
            }

            _testData.ResetIndex();

            _currentStrategyA = _testData.GetNextTable();
            _currentStrategyB = _injectionAI_B.LLMData.CurrentStrategy;

            _currentParameterA = _testData.strategyParameters.GetStrategyParameters(_currentStrategyA.BasicTactic);
            _currentParameterB = _testData.strategyParameters.GetStrategyParameters(_currentStrategyB.BasicTactic);

            Debug.Log($"[Setup] 行動テーブル初期化完了 (Aの戦術数: {_testData.Count})");
            Debug.Log($"[Setup] 初期戦術A: {_currentStrategyA.BasicTactic}");
        }

        private void OnHitReportReceived(HitReportInfo info)
        {
            _receivedHitReport = info;
            _hitReportReceived = true;
            Debug.Log($"[Observer] HitReport受信: {info.hitResultType}");
        }

        #endregion

        #region ★追加: 詳細ログ出力

        /// <summary>
        /// ログ出力パスを設定
        /// </summary>
        public void SetLogOutputPath(string path)
        {
            _logOutputPath = path;
        }

        /// <summary>
        /// 詳細ログのヘッダーを初期化
        /// </summary>
        private void InitializeDetailedLog()
        {
            _detailedLog.AppendLine("================================================================================");
            _detailedLog.AppendLine("                    戦闘シミュレーション詳細ログ");
            _detailedLog.AppendLine("================================================================================");
            _detailedLog.AppendLine($"実行日時: {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
            _detailedLog.AppendLine($"初期HP: {_initialHP}");
            _detailedLog.AppendLine($"エネルギー回復: {_energyRecoveryPerTurn}/ターン");
            _detailedLog.AppendLine($"戦術切替周期: {_tableSwitchCycle}ターン");
            _detailedLog.AppendLine($"最大ターン数: {_maxTurns}");
            _detailedLog.AppendLine($"先攻: {(_characterAFirst ? "キャラクターA (ルールベース)" : "キャラクターB (LLM)")}");
            _detailedLog.AppendLine("================================================================================");
            _detailedLog.AppendLine();
        }

        /// <summary>
        /// ターン開始時のログデータを初期化
        /// </summary>
        private void BeginTurnLog(int turn)
        {
            _currentTurnLog = new TurnLogData
            {
                TurnNumber = turn,
                StartHpA = _stateSystemA.Hp,
                StartHpB = _stateSystemB.Hp,
                StartEnergyA = _stateSystemA.Energy,
                StartEnergyB = _stateSystemB.Energy,
                AttackerName = _isCharacterAAttacking ? "A (ルールベース)" : "B (LLM)",
                DefenderName = _isCharacterAAttacking ? "B (LLM)" : "A (ルールベース)",
                AttackerTactic = _isCharacterAAttacking ? _currentStrategyA.BasicTactic : _currentStrategyB.BasicTactic,
                DefenderTactic = _isCharacterAAttacking ? _currentStrategyB.BasicTactic : _currentStrategyA.BasicTactic
            };
        }

        /// <summary>
        /// ターン終了時のログを書き込み
        /// </summary>
        private void EndTurnLog()
        {
            if (_currentTurnLog == null)
                return;

            _currentTurnLog.EndHpA = _stateSystemA.Hp;
            _currentTurnLog.EndHpB = _stateSystemB.Hp;
            _currentTurnLog.EndEnergyA = _stateSystemA.Energy;
            _currentTurnLog.EndEnergyB = _stateSystemB.Energy;

            // 状況評価
            int hpDiff = _stateSystemB.Hp - _stateSystemA.Hp;
            int energyDiff = _stateSystemB.Energy - _stateSystemA.Energy;
            _currentTurnLog.SituationAssessment = EvaluateSituationForLog(
                _stateSystemB.Hp - _stateSystemA.Hp,
                _stateSystemB.Energy - _stateSystemA.Energy);

            // ログ出力
            WriteTurnLog();
        }

        /// <summary>
        /// ターンログをStringBuilderに書き込み
        /// </summary>
        private void WriteTurnLog()
        {
            var log = _currentTurnLog;

            _detailedLog.AppendLine($"┌─────────────────────────────────────────────────────────────────────────────┐");
            _detailedLog.AppendLine($"│ ターン {log.TurnNumber,4}                                                                    │");
            _detailedLog.AppendLine($"├─────────────────────────────────────────────────────────────────────────────┤");

            // ターン開始時の状態
            _detailedLog.AppendLine($"│ 【開始時】                                                                  │");
            _detailedLog.AppendLine($"│   A: HP {log.StartHpA,3} / EN {log.StartEnergyA,3}    B: HP {log.StartHpB,3} / EN {log.StartEnergyB,3}                       │");
            _detailedLog.AppendLine($"├─────────────────────────────────────────────────────────────────────────────┤");

            // 行動情報
            _detailedLog.AppendLine($"│ 【行動】 攻撃側: {log.AttackerName,-15} → 防御側: {log.DefenderName,-15}       │");
            _detailedLog.AppendLine($"│   戦術: {log.AttackerTactic,-12} vs {log.DefenderTactic,-12}                            │");
            _detailedLog.AppendLine($"│                                                                             │");
            _detailedLog.AppendLine($"│   攻撃: {log.AttackAction,-20} ({log.AttackCriteria})              │");
            _detailedLog.AppendLine($"│   防御: {log.DefenseAction,-20} ({log.DefenseCriteria})              │");
            _detailedLog.AppendLine($"├─────────────────────────────────────────────────────────────────────────────┤");

            // 結果
            _detailedLog.AppendLine($"│ 【結果】 {log.ActionResult,-40}                      │");

            if (log.HasCounterAttack)
            {
                _detailedLog.AppendLine($"│   確定反撃: {log.CounterAttackResult,-30}                        │");
            }

            if (log.HasContinuousAttack)
            {
                _detailedLog.AppendLine($"│   連続攻撃: {log.ContinuousAttackResult,-30}                        │");
            }

            // ダメージ
            int netDamage = log.DamageDealt - log.DamageTaken;
            string damageSign = netDamage >= 0 ? "+" : "";
            _detailedLog.AppendLine($"│   与ダメ: {log.DamageDealt,3}  被ダメ: {log.DamageTaken,3}  (差: {damageSign}{netDamage})                              │");

            _detailedLog.AppendLine($"├─────────────────────────────────────────────────────────────────────────────┤");

            // ターン終了時の状態
            int hpChangeA = log.EndHpA - log.StartHpA;
            int hpChangeB = log.EndHpB - log.StartHpB;
            string hpChangeAStr = hpChangeA >= 0 ? $"+{hpChangeA}" : $"{hpChangeA}";
            string hpChangeBStr = hpChangeB >= 0 ? $"+{hpChangeB}" : $"{hpChangeB}";

            _detailedLog.AppendLine($"│ 【終了時】                                                                  │");
            _detailedLog.AppendLine($"│   A: HP {log.EndHpA,3} ({hpChangeAStr,4}) / EN {log.EndEnergyA,3}    B: HP {log.EndHpB,3} ({hpChangeBStr,4}) / EN {log.EndEnergyB,3}     │");
            _detailedLog.AppendLine($"│   状況: {log.SituationAssessment,-30}                              │");
            _detailedLog.AppendLine($"└─────────────────────────────────────────────────────────────────────────────┘");
            _detailedLog.AppendLine();
        }

        /// <summary>
        /// 戦術切替ログを書き込み
        /// </summary>
        private void WriteTacticChangeLog(string characterName, string oldTactic, string newTactic)
        {
            _detailedLog.AppendLine($"  ★ {characterName} 戦術切替: {oldTactic} → {newTactic}");
        }

        /// <summary>
        /// 最終結果を書き込み
        /// </summary>
        private void WriteFinalResultLog()
        {
            _detailedLog.AppendLine();
            _detailedLog.AppendLine("================================================================================");
            _detailedLog.AppendLine("                         戦闘シミュレーション結果");
            _detailedLog.AppendLine("================================================================================");
            _detailedLog.AppendLine($"  総ターン数: {_currentTurn}");
            _detailedLog.AppendLine($"  勝者: {_winner}");
            _detailedLog.AppendLine();
            _detailedLog.AppendLine($"  キャラクターA (ルールベース):");
            _detailedLog.AppendLine($"    最終HP: {_stateSystemA.Hp}");
            _detailedLog.AppendLine($"    最終エネルギー: {_stateSystemA.Energy}");
            _detailedLog.AppendLine();
            _detailedLog.AppendLine($"  キャラクターB (LLM):");
            _detailedLog.AppendLine($"    最終HP: {_stateSystemB.Hp}");
            _detailedLog.AppendLine($"    最終エネルギー: {_stateSystemB.Energy}");
            _detailedLog.AppendLine("================================================================================");
        }

        /// <summary>
        /// 詳細ログをファイルに保存
        /// </summary>
        private void SaveDetailedLog()
        {
            if (_isNeedLog == false)
                return;

            try
            {
                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(_logOutputPath))
                {
                    Directory.CreateDirectory(_logOutputPath);
                }

                string fileName = $"CombatLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(_logOutputPath, fileName);

                File.WriteAllText(filePath, _detailedLog.ToString(), Encoding.UTF8);
                Debug.Log($"[詳細ログ] 保存完了: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[詳細ログ] 保存失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 状況評価（ログ用）
        /// </summary>
        private string EvaluateSituationForLog(int hpDiffB, int energyDiffB)
        {
            // B視点での評価
            if (hpDiffB > 30 && energyDiffB > 50)
                return "B圧倒的有利";
            else if (hpDiffB > 15)
                return "B有利";
            else if (hpDiffB < -30 && energyDiffB < -50)
                return "B危機的";
            else if (hpDiffB < -15)
                return "B不利";
            else if (Math.Abs(hpDiffB) <= 10)
                return "互角";
            else
                return "拮抗";
        }

        #endregion

        #region ★追加: 攻撃処理の単体テスト
        // (元のコードと同じ)

        [UnityTest]
        public IEnumerator AttackCollision_LightAttack_ShouldHit()
        {
            AddLog("=== 弱攻撃ヒットテスト ===");

            yield return SimulateAttackCollision(_stateSystemA, ActionState.弱攻撃, ActionState.ガード, false);
            yield return new WaitForSeconds(2f);

            Assert.IsTrue(_hitReportReceived, "HitReportが通知されていません");
            Assert.AreEqual(HitResultType.Hit, _receivedHitReport.hitResultType);

            AddLog($"結果: {_receivedHitReport.hitResultType}, HP_B={_stateSystemB.Hp}");
            Debug.Log($"[Test Success] 弱攻撃ヒット確認 - HP_B={_stateSystemB.Hp}");
        }

        [UnityTest]
        public IEnumerator AttackCollision_HeavyAttack_ShouldHit()
        {
            AddLog("=== 強攻撃ヒットテスト ===");

            int iniHP = _stateSystemB.Hp;
            CharacterData characterData = _stateSystemB.GetCharacterData();

            yield return SimulateAttackCollision(_stateSystemA, ActionState.強攻撃, ActionState.ガード, false);

            yield return new WaitForSeconds(2f);

            Assert.IsTrue(_hitReportReceived, "HitReportが通知されていません");
            Assert.AreEqual(HitResultType.Hit, _receivedHitReport.hitResultType);

            Debug.Log($"[Debug]インスタンス一致: {characterData == _stateSystemB.GetCharacterData()}");

            AddLog($"結果: {_receivedHitReport.hitResultType},HP_A{_stateSystemA.Hp}, HP_B={_stateSystemB.Hp}");

            Assert.IsTrue(_stateSystemB.Hp == iniHP - _receivedHitReport.damage, "HPが減っていません");

            Debug.Log($"[Test Success] 強攻撃ヒット確認 - HP_B={_stateSystemB.Hp}");
        }

        [UnityTest]
        public IEnumerator AttackCollision_GuardSuccess_ShouldBlock()
        {
            AddLog("=== ガード成功テスト ===");

            yield return SimulateAttackCollision(_stateSystemA, ActionState.弱攻撃, ActionState.ガード, true);
            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(_hitReportReceived, "HitReportが通知されていません");
            Assert.AreEqual(HitResultType.Guard, _receivedHitReport.hitResultType);

            AddLog($"結果: {_receivedHitReport.hitResultType}");
            Debug.Log($"[Test Success] ガード成功確認");
        }

        [UnityTest]
        public IEnumerator AttackCollision_BlockingSuccess_ShouldBlock()
        {
            AddLog("=== ブロッキング成功テスト ===");

            yield return SimulateAttackCollision(_stateSystemA, ActionState.強攻撃, ActionState.ブロッキング, true);
            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(_hitReportReceived, "HitReportが通知されていません");
            Assert.AreEqual(HitResultType.Block, _receivedHitReport.hitResultType);

            AddLog($"結果: {_receivedHitReport.hitResultType}");
            Debug.Log($"[Test Success] ブロッキング成功確認");
        }

        [UnityTest]
        public IEnumerator AttackCollision_CounterAttack_ShouldDealDamage()
        {
            AddLog("=== 確定反撃テスト ===");

            int initialHP_A = _stateSystemA.Hp;

            yield return SimulateAttackCollision(_stateSystemA, ActionState.弱攻撃, ActionState.ブロッキング, true);
            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(HitResultType.Block, _receivedHitReport.hitResultType);
            AddLog($"ブロッキング成功: {_receivedHitReport.hitResultType}");

            yield return SimulateAttackCollision(_stateSystemB, ActionState.強攻撃, ActionState.ガード, false);
            yield return new WaitForSeconds(2f);

            Assert.AreEqual(HitResultType.Hit, _receivedHitReport.hitResultType);
            Assert.Less(_stateSystemA.Hp, initialHP_A, "確定反撃でHPが減っていません");

            AddLog($"確定反撃成功: HP_A {initialHP_A} → {_stateSystemA.Hp}");
            Debug.Log($"[Test Success] 確定反撃確認 - ダメージ={initialHP_A - _stateSystemA.Hp}");
        }

        #endregion

        #region メインシミュレーションテスト

        [UnityTest]
        public IEnumerator CombatSimulation_ShouldCompleteWithWinner()
        {
            // 戦闘テストではログを出す
            _isNeedLog = true;

            Debug.Log("=== 戦闘シミュレーション開始 ===");
            AddLog("=== 戦闘シミュレーション開始 ===");
            AddLog($"初期設定: HP={_initialHP}, 周期={_tableSwitchCycle}ターン, エネルギー回復={_energyRecoveryPerTurn}/ターン");
            AddLog($"先攻: {(_characterAFirst ? "キャラクターA" : "キャラクターB")}");
            AddLog($"★最大ターン数: {_maxTurns}ターン");
            AddLog("=====================================");

            while (!_simulationComplete && _currentTurn < _maxTurns)
            {
                _currentTurn++;
                AddLog($"\n--- ターン {_currentTurn} ---");

                // ★追加: ターンログ開始
                BeginTurnLog(_currentTurn);

                if (_currentTurn > 1 && (_currentTurn - 1) % _tableSwitchCycle == 0)
                {
                    yield return UpdateActionTables();
                }

                ValidateAndRestoreInstances();
                yield return ExecuteTurn(_currentTurn);
                CheckWinCondition();
                EndTurn();

                // ★追加: ターンログ終了
                EndTurnLog();

                yield return null;
            }

            if (_currentTurn >= _maxTurns && !_simulationComplete)
            {
                AddLog($"\n★ターン数制限({_maxTurns}ターン)に達しました");

                if (_stateSystemA.Hp > _stateSystemB.Hp)
                {
                    _winner = "キャラクターA (HP差)";
                }
                else if (_stateSystemB.Hp > _stateSystemA.Hp)
                {
                    _winner = "キャラクターB (HP差)";
                }
                else
                {
                    _winner = "引き分け (HP同値)";
                }

                _simulationComplete = true;
            }

            // ★追加: 最終結果ログ
            WriteFinalResultLog();

            OutputResult();
            Debug.Log("=== 戦闘シミュレーション完了 ===");
        }

        #endregion

        #region ターン処理

        private IEnumerator ExecuteTurn(int turn)
        {
            if (_isCharacterAAttacking)
            {
                AddLog("[攻撃側: A, 防御側: B]");
                yield return ExecuteAttackPhase(_characterA, _characterB, _stateSystemA, _stateSystemB,
                    _hitSystemA, _damageSystemB, _currentStrategyA, "A", turn);
            }
            else
            {
                AddLog("[攻撃側: B, 防御側: A]");
                yield return ExecuteAttackPhase(_characterB, _characterA, _stateSystemB, _stateSystemA,
                    _hitSystemB, _damageSystemA, _currentStrategyB, "B", turn);
            }

            _isCharacterAAttacking = !_isCharacterAAttacking;
        }

        private IEnumerator ExecuteAttackPhase(
            GameObject attacker, GameObject defender,
            DebugStateSystem attackerState, DebugStateSystem defenderState,
            DebugHitSystem attackerHitSystem, DamageSystemBase defenderDamageSystem,
            StrategyData strategy, string attackerName, int turn,
            bool isContinuousAttack = false)
        {
            string defenderName = attackerName == "A" ? "B" : "A";

            BattleCharacterController attackerController = attackerName == "A" ? _controllerA : _controllerB;
            BattleCharacterController defenderController = attackerName == "A" ? _controllerB : _controllerA;
            AIParameter attackerParameter = attackerName == "A" ? _currentParameterA : _currentParameterB;
            AIParameter defenderParameter = attackerName == "A" ? _currentParameterB : _currentParameterA;

            ActionCriteriaType attackCriteria = GetAttackCriteria(strategy.AttackCriteria);
            ActionState attackAction = DecideAttackAction(attackCriteria, defenderState);

            AddLog($"  {attackerName}の攻撃: {attackAction} (基準: {attackCriteria}) [EN: {attackerState.Energy}]");

            StrategyData defenderStrategy = GetOpponentStrategy(attackerName);
            ActionCriteriaType defenseCriteria = GetDefenseCriteria(defenderStrategy.DefenseCriteria);
            ActionState defenseAction = DecideDefenseAction(defenseCriteria, attackerState);

            AddLog($"  {defenderName}の防御: {defenseAction} (基準: {defenseCriteria}) [EN: {defenderState.Energy}]");

            // ★追加: ターンログに記録
            if (!isContinuousAttack && _currentTurnLog != null)
            {
                _currentTurnLog.AttackAction = attackAction.ToString();
                _currentTurnLog.AttackCriteria = attackCriteria.ToString().Replace("Attack_", "");
                _currentTurnLog.DefenseAction = defenseAction.ToString();
                _currentTurnLog.DefenseCriteria = defenseCriteria.ToString().Replace("Defense_", "");
            }

            var result = ActionResultTable.GetResult(defenseAction, attackAction);

            string resultText;
            string judgeInfo = isContinuousAttack ? $"  {turn}ターン目{attackerName}の連続攻撃判定結果: " : $"  {turn}ターン目{attackerName}の攻撃判定結果: ";


            if ((int)result > 0)
            {
                resultText = $"{defenderName}の防御成功";
                AddLog($" {judgeInfo}{resultText}");
            }
            else if ((int)result < 0)
            {
                resultText = $"{attackerName}の攻撃成功";
                AddLog($"  {judgeInfo} {resultText}");
            }
            else
            {
                resultText = "引き分け";
                AddLog($"  {judgeInfo} {resultText}");
            }


            // ★追加: 結果をログに記録
            if (!isContinuousAttack && _currentTurnLog != null)
            {
                _currentTurnLog.ActionResult = resultText;
            }

            if (defenseAction == ActionState.弱ブロッキング || defenseAction == ActionState.強ブロッキング)
            {
                defenseAction = ActionState.ブロッキング;
            }

            if (attackerName == "B")
            {
                _injectionAI_B.LLMData.StrategyResult.AddResult(
                    isContinuousAttack ? StrategyResult.ConditionType.SequentialDefense : StrategyResult.ConditionType.Defense,
                    (int)result > 0);
            }
            else
            {
                _injectionAI_B.LLMData.StrategyResult.AddResult(
                    isContinuousAttack ? StrategyResult.ConditionType.SequentialAttack : StrategyResult.ConditionType.Attack,
                    (int)result < 0);
            }

            if ((defenseAction == ActionState.横回避攻撃 && attackAction == ActionState.強攻撃キャンセル) &&
                result == ActionResultTable.ResultValue.AttackerDefenseCounterHeavy)
            {
                ExecuteAction(attackerController, ActionState.強攻撃キャンセル, StanceType.Left);
                yield return new WaitForSeconds(0.1f);
                attackerState.SetNeutral();
                attackAction = ActionState.ブロッキング;
            }
            else if (result == ActionResultTable.ResultValue.AttackerCancelHeavy)
            {
                ExecuteAction(defenderController, ActionState.強攻撃キャンセル, StanceType.Right);
                yield return new WaitForSeconds(0.1f);
                attackerState.SetNeutral();
                attackAction = ActionState.強攻撃;
            }
            else if (result == ActionResultTable.ResultValue.AttackerCancelLight)
            {
                ExecuteAction(attackerController, ActionState.強攻撃キャンセル, StanceType.Left);
                yield return new WaitForSeconds(0.1f);
                attackerState.SetNeutral();
                attackAction = ActionState.弱攻撃;
            }


            bool allowContinuousAttack = false;
            int hpBeforeA = _stateSystemA.Hp;
            int hpBeforeB = _stateSystemB.Hp;

            if ((int)result > 0)
            {
                switch (defenseAction)
                {
                    case ActionState.後ろ回避:
                    case ActionState.横回避:
                    case ActionState.前回避:
                    case ActionState.ブロッキング:
                    case ActionState.ガード:
                        yield return SimulateAttackCollision(attackerState, attackAction, defenseAction, true);
                        break;
                    case ActionState.横回避攻撃:
                    case ActionState.弱攻撃:
                        yield return SimulateAttackCollision(defenderState, defenseAction, ActionState.ガード, false);
                        break;
                }
            }
            else if ((int)result < 0)
            {
                switch (attackAction)
                {
                    case ActionState.弱攻撃:
                    case ActionState.強攻撃:
                        yield return SimulateAttackCollision(attackerState, attackAction, defenseAction, false);
                        break;
                    case ActionState.ブロッキング:
                    case ActionState.ガード:
                        yield return SimulateAttackCollision(defenderState, defenseAction, attackAction, true);
                        break;
                }
                allowContinuousAttack = true;
            }
            else
            {
                _hitSystemA.isHit = false;
                _hitSystemB.isHit = false;
                ExecuteAction(attackerController, attackAction);
                ExecuteAction(defenderController, defenseAction);
                attackerState.SetNeutral();
                defenderState.SetNeutral();
            }

            if (result == ActionResultTable.ResultValue.DefenseCounterLight ||
                result == ActionResultTable.ResultValue.DefenseCounterHeavy)
            {
                AddLog($"  [確定反撃判定] 防御成功 - {defenderName}が反撃可能");
                bool isHeavyCounter = (result == ActionResultTable.ResultValue.DefenseCounterHeavy);

                int hpBeforeCounter = attackerName == "A" ? _stateSystemA.Hp : _stateSystemB.Hp;
                yield return ExecuteCounterAttack(defenderParameter, defenderState, isHeavyCounter);
                int hpAfterCounter = attackerName == "A" ? _stateSystemA.Hp : _stateSystemB.Hp;

                // ★追加: 確定反撃ログ
                if (_currentTurnLog != null)
                {
                    _currentTurnLog.HasCounterAttack = true;
                    int counterDamage = hpBeforeCounter - hpAfterCounter;
                    _currentTurnLog.CounterAttackResult = counterDamage > 0
                        ? $"{defenderName}が{counterDamage}ダメージ"
                        : "不発";

                    if (attackerName == "A")
                    {
                        _currentTurnLog.DamageTaken += counterDamage;
                    }
                    else
                    {
                        _currentTurnLog.DamageDealt += counterDamage;
                    }
                }

                allowContinuousAttack = false;
            }
            else if (result == ActionResultTable.ResultValue.AttackerDefenseCounterHeavy ||
                     result == ActionResultTable.ResultValue.AttackerDefenseCounterLight)
            {
                bool isHeavyCounter = (result == ActionResultTable.ResultValue.AttackerDefenseCounterHeavy);
                yield return ExecuteCounterAttack(attackerParameter, attackerState, isHeavyCounter);
            }

            if (!isContinuousAttack && allowContinuousAttack &&
                ShouldContinueAttack(attackerParameter, attackerState.Energy))
            {
                // ★追加: 連続攻撃フラグ
                if (_currentTurnLog != null)
                {
                    _currentTurnLog.HasContinuousAttack = true;
                }

                yield return null;
                yield return ExecuteAttackPhase(attacker, defender, attackerState, defenderState,
                    attackerHitSystem, defenderDamageSystem, strategy, attackerName, turn, isContinuousAttack: true);
            }

            // ★修正: 全ての攻撃処理が終わった後にダメージを計算
            if (!isContinuousAttack && _currentTurnLog != null)
            {
                int totalDamageToA = hpBeforeA - _stateSystemA.Hp;
                int totalDamageToB = hpBeforeB - _stateSystemB.Hp;

                if (attackerName == "A")
                {
                    _currentTurnLog.DamageDealt = totalDamageToB;
                    _currentTurnLog.DamageTaken = totalDamageToA;
                }
                else
                {
                    _currentTurnLog.DamageDealt = totalDamageToA;
                    _currentTurnLog.DamageTaken = totalDamageToB;
                }
            }
        }

        // (ExecuteAction, SimulateAttackCollision, ExecuteCounterAttack, ShouldContinueAttack, EndTurn は元のコードと同じ)

        public static void ExecuteAction(BattleCharacterController controller, ActionState action, StanceType stance = StanceType.Up)
        {
            switch (action)
            {
                case ActionState.弱攻撃:
                    controller.LightAttackAct(stance).Forget();
                    break;
                case ActionState.強攻撃:
                    controller.HeavyAttackAct(stance).Forget();
                    break;
                case ActionState.ガード:
                    controller.GuardDirectionChange(stance);
                    break;
                case ActionState.ブロッキング:
                    controller.BlockingAct(stance);
                    break;
                case ActionState.前回避:
                    controller.AvoidAct(MovementReportType.FrontStep);
                    break;
                case ActionState.横回避:
                    controller.AvoidAct(stance == StanceType.Left ? MovementReportType.LeftStep : MovementReportType.RightStep);
                    break;
                case ActionState.後ろ回避:
                    controller.AvoidAct(MovementReportType.BackStep);
                    break;
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

        private IEnumerator SimulateAttackCollision(
            DebugStateSystem attackerStateSystem,
            ActionState attackerState,
            ActionState defenderState,
            bool isDefenseSuccess)
        {
            BattleCharacterController attackerController;
            BattleCharacterController defenderController;
            DebugHitSystem attackerHitSystem;
            DamageSystemBase defenderDamageSystem;
            DebugStateSystem defenderStateSystem = (attackerStateSystem == _stateSystemA) ? _stateSystemB : _stateSystemA;

            int initialHP = defenderStateSystem.Hp;

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

            attackerHitSystem.isHit = true;

            ExecuteAction(defenderController, defenderState, isDefenseSuccess ? StanceType.Up : StanceType.Left);
            if (defenderDamageSystem is DamageSystemMock damageMock)
            {
                damageMock.MockSetting(defenderState, isDefenseSuccess ? StanceType.Up : StanceType.Left);
            }


            ExecuteAction(attackerController, attackerState);

            //ActionState actAttack = attackerState == ActionState.強攻撃 ? ActionState.強攻撃 : ActionState.弱攻撃;

            yield return new WaitForSeconds(0.1f);

            //if (defenderDamageSystem is DamageSystemMock damageMock)
            //{
            //    damageMock.MockSetting(defenderState, isDefenseSuccess ? StanceType.Up : StanceType.Left);
            //}

            //MethodInfo attackHitMethod = typeof(HitSystem).GetMethod("AttackHit",
            //    BindingFlags.NonPublic | BindingFlags.Instance);

            //Assert.IsNotNull(attackHitMethod, "AttackHitメソッドがリフレクションで取得できませんでした");

            //attackHitMethod.Invoke(attackerHitSystem, null);

            //yield return null;

            _stateSystemA.SetNeutral();
            _stateSystemB.SetNeutral();

            //if (!isDefenseSuccess && initialHP == defenderStateSystem.Hp)
            //{
            //    if (actAttack == ActionState.弱攻撃)
            //    {
            //        defenderStateSystem.DebugDamage(_lightAttackDamage);
            //    }
            //    else if (actAttack == ActionState.強攻撃)
            //    {
            //        defenderStateSystem.DebugDamage(_heavyAttackDamage);
            //    }
            //}

        }

        private IEnumerator ExecuteCounterAttack(
            AIParameter counterParameter,
            DebugStateSystem counterState,
            bool isHeavyAttack)
        {
            AddLog($"  [確定反撃] 開始 - Heavy:{isHeavyAttack}, Energy:{counterState.Energy}");

            if (isHeavyAttack)
            {
                if (counterParameter.ShouldPunish())
                {
                    AddLog($"  [確定反撃] ShouldPunish=true");

                    if (counterState.Energy >= counterParameter.heavyAttackMinEnergy)
                    {
                        AddLog($"  [確定反撃] 強攻撃を実行");
                        yield return SimulateAttackCollision(counterState, ActionState.強攻撃, ActionState.ガード, false);
                    }
                    else if (counterState.Energy >= counterParameter.lightAttackMinEnergy)
                    {
                        AddLog($"  [確定反撃] 弱攻撃を実行 (エネルギー不足)");
                        yield return SimulateAttackCollision(counterState, ActionState.弱攻撃, ActionState.ガード, false);
                    }
                    else
                    {
                        AddLog($"  [確定反撃] エネルギー不足で実行不可");
                    }
                }
                else
                {
                    AddLog($"  [確定反撃] ShouldPunish=false");
                }
            }
            else if (counterState.Energy >= counterParameter.lightAttackMinEnergy)
            {
                AddLog($"  [確定反撃] 弱攻撃を実行");
                yield return SimulateAttackCollision(counterState, ActionState.弱攻撃, ActionState.ガード, false);
            }
            else
            {
                AddLog($"  [確定反撃] エネルギー不足で実行不可");
            }
        }

        private bool ShouldContinueAttack(AIParameter aiParameter, int energy)
        {
            return (aiParameter.ShouldComboAttack() && energy >= aiParameter.comboMinEnergy);
        }

        private void EndTurn()
        {
            RecoverEnergy(_stateSystemA, _energyRecoveryPerTurn);
            RecoverEnergy(_stateSystemB, _energyRecoveryPerTurn);

            if (_stateSystemA.Energy == 0)
            {
                _exhaustCountA++;

                if (_exhaustCountA >= _exhaustRecoverCount)
                {
                    AddLog("  ★ キャラクターAが3ターン連続でエネルギー切れ。次ターンにエネルギーを大幅回復。");
                    RecoverEnergy(_stateSystemA, 100);
                    _exhaustCountA = 0;
                }
            }

            if (_stateSystemB.Energy == 0)
            {
                _exhaustCountB++;

                if (_exhaustCountB >= _exhaustRecoverCount)
                {
                    AddLog("  ★ キャラクターAが3ターン連続でエネルギー切れ。次ターンにエネルギーを大幅回復。");
                    RecoverEnergy(_stateSystemB, 100);
                    _exhaustCountB = 0;
                }
            }

            AddLog($"エネルギー回復: +{_energyRecoveryPerTurn}");
            AddLog($"  A: Energy={_stateSystemA.Energy}, HP={_stateSystemA.Hp}");
            AddLog($"  B: Energy={_stateSystemB.Energy}, HP={_stateSystemB.Hp}");
        }

        #endregion

        #region 行動決定ロジック
        // (元のコードと同じ - 省略)

        private ActionState DecideAttackAction(ActionCriteriaType criteria, DebugStateSystem opponentState)
        {
            var attackerState = _isCharacterAAttacking ? _stateSystemA : _stateSystemB;

            if (attackerState.Energy <= 0)
            {
                return ActionState.ガード;
            }

            LLMLogData AttackerLog = _isCharacterAAttacking ? _injectionAI_B.LLMData.PlayerLog : _injectionAI_B.LLMData.NPCLog;

            ActionState desiredAction;

            switch (criteria)
            {
                case ActionCriteriaType.Attack_CumulativeProbability:
                    ActionState mostUsedDefense = AttackerLog.ActionLog.MostUsedDefense;
                    desiredAction = AntiActionSelect(mostUsedDefense);
                    AddLog($"    [攻撃判断] 累積最多防御: {mostUsedDefense} → 対策: {desiredAction}");
                    break;

                case ActionCriteriaType.Attack_RecentPatternFocus:
                    ActionState recentDefense = AttackerLog.RecentMostUsedDefense;
                    desiredAction = AntiActionSelect(recentDefense);
                    AddLog($"    [攻撃判断] 最近最多防御: {recentDefense} → 対策: {desiredAction}");
                    break;

                case ActionCriteriaType.Attack_SpeedPriority:
                    desiredAction = ActionState.弱攻撃;
                    break;

                case ActionCriteriaType.Attack_ReturnPriority:
                    desiredAction = ActionState.強攻撃;
                    break;

                case ActionCriteriaType.Attack_FeintFocus:
                    desiredAction = ActionState.強攻撃キャンセル;
                    break;

                case ActionCriteriaType.Attack_DispersionFocus:
                    desiredAction = !_isCharacterAAttacking ? _injectionAI_B.LLMData.PlayerLog.ActionLog.LeastUsedAttack : _injectionAI_B.LLMData.NPCLog.ActionLog.LeastUsedAttack;
                    AddLog($"    [攻撃判断] 分散重視: {desiredAction}");
                    break;

                case ActionCriteriaType.Attack_EnergyEfficiency:
                    desiredAction = ActionState.弱攻撃;
                    break;

                default:
                    desiredAction = ActionState.弱攻撃;
                    break;
            }

            var res = AdjustActionByEnergy(desiredAction, attackerState.EnergyRatio, isAttack: true);
            Debug.Log($"[Debug] Desire: {desiredAction} → Adjusted: {res} (Energy: {attackerState.EnergyRatio})");
            return res;
        }

        private ActionState DecideDefenseAction(ActionCriteriaType criteria, DebugStateSystem opponentState)
        {
            var defenderState = _isCharacterAAttacking ? _stateSystemB : _stateSystemA;
            LLMLogData enemyLog = !_isCharacterAAttacking ? _injectionAI_B.LLMData.PlayerLog : _injectionAI_B.LLMData.NPCLog;

            ActionState desiredAction;

            switch (criteria)
            {
                case ActionCriteriaType.Defense_CumulativeProbability:
                    ActionState mostUsedAttack = enemyLog.ActionLog.MostUsedAttack;
                    desiredAction = AntiActionSelect(mostUsedAttack);
                    AddLog($"    [防御判断] 累積最多攻撃: {mostUsedAttack} → 対策: {desiredAction}");
                    break;

                case ActionCriteriaType.Defense_RecentPatternFocus:
                    ActionState recentAttack = enemyLog.RecentMostUsedAttack;
                    desiredAction = AntiActionSelect(recentAttack);
                    AddLog($"    [防御判断] 最近最多攻撃: {recentAttack} → 対策: {desiredAction}");
                    break;

                case ActionCriteriaType.Defense_CounterattackFocus:
                    desiredAction = ActionState.弱攻撃;
                    break;

                case ActionCriteriaType.Defense_ReturnPriority:
                    if (enemyLog.ActionLog.LightAttackPercentage >= enemyLog.ActionLog.HeavyAttackPercentage)
                    {
                        desiredAction = ActionState.弱ブロッキング;
                        AddLog($"    [防御判断] 敵は弱攻撃多用 ({enemyLog.ActionLog.LightAttackPercentage:P0}) → 弱ブロッキング");
                    }
                    else
                    {
                        desiredAction = ActionState.強ブロッキング;
                        AddLog($"    [防御判断] 敵は強攻撃多用 ({enemyLog.ActionLog.HeavyAttackPercentage:P0}) → 強ブロッキング");
                    }
                    break;

                case ActionCriteriaType.Defense_RiskAvoidance:
                    desiredAction = ActionState.後ろ回避;
                    break;

                case ActionCriteriaType.Defense_EvasiveCounterPriority:
                    desiredAction = ActionState.横回避攻撃;
                    break;

                case ActionCriteriaType.Defense_DispersionFocus:
                    desiredAction = _isCharacterAAttacking ? _injectionAI_B.LLMData.PlayerLog.ActionLog.LeastUsedDefense : _injectionAI_B.LLMData.NPCLog.ActionLog.LeastUsedDefense;
                    AddLog($"    [防御判断] 分散重視: {desiredAction}");
                    break;

                default:
                    desiredAction = ActionState.ガード;
                    break;
            }

            return desiredAction;
        }

        private ActionState AntiActionSelect(ActionState type) => type switch
        {
            ActionState.後ろ回避 => ActionState.前回避攻撃,
            ActionState.横回避 => ActionState.強攻撃,
            ActionState.前回避 => ActionState.強攻撃,
            ActionState.ブロッキング => ActionState.強攻撃キャンセル,
            ActionState.弱攻撃 => ActionState.弱ブロッキング,
            ActionState.強攻撃 => ActionState.強ブロッキング,
            ActionState.強攻撃キャンセル => ActionState.弱攻撃,
            ActionState.横回避攻撃 => ActionState.強攻撃キャンセル,
            ActionState.前回避攻撃 => ActionState.ブロッキング,
            ActionState.デフォルト攻撃 => ActionState.ガード,
            ActionState.デフォルト防御 => ActionState.弱攻撃,
            _ => ActionState.弱攻撃
        };

        private ActionCriteriaType GetAttackCriteria(string criteriaStr)
        {
            return StrategyData.GetAttackCriteria(criteriaStr);
        }

        private ActionCriteriaType GetDefenseCriteria(string criteriaStr)
        {
            return StrategyData.GetDefenseCriteria(criteriaStr);
        }

        private StrategyData GetOpponentStrategy(string attackerName)
        {
            return attackerName == "A" ? _currentStrategyB : _currentStrategyA;
        }

        /// <summary>
        /// エネルギーに応じてアクションを調整（修正版）
        /// </summary>
        private ActionState AdjustActionByEnergy(ActionState desiredAction, float currentEnergyRatio, bool isAttack)
        {
            AIParameter aiParameter = _isCharacterAAttacking ? _currentParameterA : _currentParameterB;

            // エネルギー0以下は攻撃不可
            if (currentEnergyRatio <= 0)
            {
                AddLog($"  [エネルギー切れ] 攻撃不可");
                return ActionState.ガード;
            }

            // 強攻撃系の場合
            if (desiredAction == ActionState.強攻撃 || desiredAction == ActionState.強攻撃キャンセル)
            {
                // 強攻撃に必要なエネルギーがある場合はそのまま
                if (currentEnergyRatio >= aiParameter.heavyAttackMinEnergy)
                {
                    return desiredAction;
                }
                // 弱攻撃分のエネルギーがある場合は格下げ
                else if (currentEnergyRatio >= aiParameter.lightAttackMinEnergy)
                {
                    AddLog($"  [エネルギー不足] {desiredAction} → 弱攻撃");
                    return ActionState.弱攻撃;
                }
                // どちらも不足
                else
                {
                    AddLog($"  [エネルギー不足] 攻撃不可");
                    return ActionState.ガード;
                }
            }

            // 弱攻撃系の場合
            if (desiredAction == ActionState.弱攻撃 ||
                desiredAction == ActionState.前回避攻撃 ||
                desiredAction == ActionState.横回避攻撃)
            {
                if (currentEnergyRatio >= aiParameter.lightAttackMinEnergy)
                {
                    return desiredAction;
                }
                else
                {
                    AddLog($"  [エネルギー不足] 攻撃不可");
                    return ActionState.ガード;
                }
            }

            // 防御系アクションはそのまま返す
            return desiredAction;
        }

        #endregion

        #region 行動テーブル更新

        private IEnumerator UpdateActionTables()
        {
            AddLog("\n### 行動テーブル更新 ###");

            string oldTacticA = _currentStrategyA?.BasicTactic ?? "None";
            _currentStrategyA = _testData.GetNextTable();
            AddLog($"キャラクターA: {_currentStrategyA.BasicTactic}に切り替え");

            // ★追加: 戦術切替ログ
            _detailedLog.AppendLine($"  ★ A 戦術切替: {oldTacticA} → {_currentStrategyA.BasicTactic}");

            AddLog("キャラクターB: LLMに戦術リクエスト中...");
            string oldTacticB = _currentStrategyB?.BasicTactic ?? "None";
            yield return RequestLLMStrategy();

            // ★追加: 戦術切替ログ
            _detailedLog.AppendLine($"  ★ B 戦術切替: {oldTacticB} → {_currentStrategyB.BasicTactic}");

            _currentParameterA = _testData.strategyParameters.GetStrategyParameters(_currentStrategyA.BasicTactic);
            _currentParameterB = _testData.strategyParameters.GetStrategyParameters(_currentStrategyB.BasicTactic);

            AddLog("### 更新完了 ###\n");
        }

        private IEnumerator RequestLLMStrategy()
        {
            yield return Task.Delay(10000);

            var requestTask = _llmCommunicator.RequestTacticalDecisionAsync();
            yield return requestTask.ToCoroutine();

            _currentStrategyB = _injectionAI_B.LLMData.CurrentStrategy;

            if (_currentStrategyB != null)
            {
                AddLog($"キャラクターB: {_currentStrategyB.BasicTactic}に更新");
            }
            else
            {
                AddLog("キャラクターB: LLM更新失敗、前回の戦術を継続");
            }
        }

        #endregion

        #region 勝敗判定

        private void CheckWinCondition()
        {
            bool aIsDead = _stateSystemA.Hp <= 0;
            bool bIsDead = _stateSystemB.Hp <= 0;

            if (aIsDead && bIsDead)
            {
                _winner = "引き分け";
                _simulationComplete = true;
            }
            else if (aIsDead)
            {
                _winner = "キャラクターB";
                _simulationComplete = true;
            }
            else if (bIsDead)
            {
                _winner = "キャラクターA";
                _simulationComplete = true;
            }
        }

        #endregion

        #region ヘルパーメソッド
        // (ValidateAndRestoreInstances, RecoverEnergy, AddLog, OutputCombatLog, OutputResult は元のコードと同じ - 長いので省略)

        private void ValidateAndRestoreInstances()
        {
            // (元のコードと同じ)
            List<string> errors = new List<string>();
            List<string> restored = new List<string>();

            if (_characterA == null)
            {
                errors.Add("キャラクターAのGameObjectが破棄されています");
            }
            else
            {
                if (_stateSystemA == null)
                {
                    _stateSystemA = _characterA.GetComponent<DebugStateSystem>();
                    if (_stateSystemA == null)
                        errors.Add("キャラクターAのStateSystemが見つかりません");
                    else
                        restored.Add("キャラクターAのStateSystem");
                }
                if (_hitSystemA == null)
                {
                    _hitSystemA = _characterA.GetComponent<DebugHitSystem>();
                    if (_hitSystemA == null)
                        errors.Add("キャラクターAのHitSystemが見つかりません");
                    else
                        restored.Add("キャラクターAのHitSystem");
                }
                if (_damageSystemA == null)
                {
                    _damageSystemA = _characterA.GetComponent<DamageSystemBase>();
                    if (_damageSystemA == null)
                        errors.Add("キャラクターAのDamageSystemが見つかりません");
                    else
                        restored.Add("キャラクターAのDamageSystem");
                }
                if (_controllerA == null)
                {
                    _controllerA = _characterA.GetComponent<BattleCharacterController>();
                    if (_controllerA == null)
                        errors.Add("キャラクターAのBattleCharacterControllerが見つかりません");
                    else
                        restored.Add("キャラクターAのBattleCharacterController");
                }
            }

            if (_characterB == null)
            {
                errors.Add("キャラクターBのGameObjectが破棄されています");
            }
            else
            {
                if (_stateSystemB == null)
                {
                    _stateSystemB = _characterB.GetComponent<DebugStateSystem>();
                    if (_stateSystemB == null)
                        errors.Add("キャラクターBのStateSystemが見つかりません");
                    else
                        restored.Add("キャラクターBのStateSystem");
                }
                if (_hitSystemB == null)
                {
                    _hitSystemB = _characterB.GetComponent<DebugHitSystem>();
                    if (_hitSystemB == null)
                        errors.Add("キャラクターBのHitSystemが見つかりません");
                    else
                        restored.Add("キャラクターBのHitSystem");
                }
                if (_damageSystemB == null)
                {
                    _damageSystemB = _characterB.GetComponent<DamageSystemBase>();
                    if (_damageSystemB == null)
                        errors.Add("キャラクターBのDamageSystemが見つかりません");
                    else
                        restored.Add("キャラクターBのDamageSystem");
                }
                if (_controllerB == null)
                {
                    _controllerB = _characterB.GetComponent<BattleCharacterController>();
                    if (_controllerB == null)
                        errors.Add("キャラクターBのBattleCharacterControllerが見つかりません");
                    else
                        restored.Add("キャラクターBのBattleCharacterController");
                }
                if (_llmCommunicator == null)
                {
                    _llmCommunicator = _characterB.GetComponent<LLMCommunicator>();
                    if (_llmCommunicator == null)
                        errors.Add("キャラクターBのLLMCommunicatorが見つかりません");
                    else
                        restored.Add("キャラクターBのLLMCommunicator");
                }
                if (_injectionAI_B == null)
                {
                    _injectionAI_B = _characterB.GetComponent<CombatTestAI>();
                    if (_injectionAI_B == null)
                        errors.Add("キャラクターBのCombatTestAIが見つかりません");
                    else
                        restored.Add("キャラクターBのCombatTestAI");
                }
            }

            if (_inputDataB == null && _llmCommunicator != null)
            {
                _inputDataB = _llmCommunicator.GetCurrentInputData();
                if (_inputDataB == null)
                    errors.Add("LLMInputDataが取得できません");
                else
                    restored.Add("LLMInputData");
            }

            if (_testData == null)
            {
                _testData = Resources.Load<CombatSimulationTestData>(k_TEST_DATA_PATH);
                if (_testData == null)
                    errors.Add($"テストデータが見つかりません: {k_TEST_DATA_PATH}");
                else
                    restored.Add("CombatSimulationTestData");
            }

            if (_currentStrategyA == null && _testData != null)
            {
                _currentStrategyA = _testData.GetCurrentTable();
                if (_currentStrategyA == null)
                    errors.Add("キャラクターAの現在の戦術データがnullです");
                else
                    restored.Add("キャラクターAの戦術データ");
            }

            if (_currentStrategyB == null && _injectionAI_B != null && _injectionAI_B.LLMData != null)
            {
                _currentStrategyB = _injectionAI_B.LLMData.CurrentStrategy;
                if (_currentStrategyB == null)
                    errors.Add("キャラクターBの現在の戦術データがnullです");
                else
                    restored.Add("キャラクターBの戦術データ");
            }

            if (_currentParameterA == null && _testData != null && _currentStrategyA != null)
            {
                _currentParameterA = _testData.strategyParameters.GetStrategyParameters(_currentStrategyA.BasicTactic);
                if (_currentParameterA == null)
                    errors.Add($"キャラクターAのAIパラメーターが取得できません");
                else
                    restored.Add("キャラクターAのAIパラメーター");
            }

            if (_currentParameterB == null && _testData != null && _currentStrategyB != null)
            {
                _currentParameterB = _testData.strategyParameters.GetStrategyParameters(_currentStrategyB.BasicTactic);
                if (_currentParameterB == null)
                    errors.Add($"キャラクターBのAIパラメーターが取得できません");
                else
                    restored.Add("キャラクターBのAIパラメーター");
            }

            if (restored.Count > 0)
            {
                AddLog("\n[復元] 以下のインスタンスを復元しました:");
                foreach (var item in restored)
                    AddLog($"  - {item}");
            }

            if (errors.Count > 0)
            {
                AddLog("\n[致命的エラー] 以下のインスタンスの復元に失敗しました:");
                foreach (var error in errors)
                {
                    AddLog($"  - {error}");
                    Debug.LogError($"[ValidateAndRestoreInstances] {error}");
                }
                Assert.Fail($"インスタンスの検証に失敗しました。失敗要因:\n{string.Join("\n", errors)}");
            }
            else
            {
                AddLog("\n[検証完了] すべてのインスタンスが正常に存在しています。\n");
            }
        }

        private void RecoverEnergy(DebugStateSystem state, int amount)
        {
            state.DebugRecoverEnergy(amount);
        }

        private void AddLog(string message)
        {
            _combatLog.Add(message);
        }

        private void OutputCombatLog()
        {
            Debug.Log("\n=== 戦闘ログ ===");
            foreach (var log in _combatLog)
            {
                Debug.Log(log);
            }
            Debug.Log("=================\n");
        }

        private void OutputResult()
        {
            AddLog("\n=====================================");
            AddLog("=== 戦闘シミュレーション結果 ===");
            AddLog($"総ターン数: {_currentTurn}");
            AddLog($"勝者: {_winner}");
            AddLog($"キャラクターA: HP={_stateSystemA.Hp}, Energy={_stateSystemA.Energy}");
            AddLog($"キャラクターB: HP={_stateSystemB.Hp}, Energy={_stateSystemB.Energy}");
            AddLog("=====================================");

            Assert.IsTrue(_simulationComplete, "シミュレーションが完了していません");
            Assert.IsFalse(string.IsNullOrEmpty(_winner), "勝者が決定していません");

            Debug.Log($"\n### テスト完了: 勝者={_winner}, ターン数={_currentTurn} ###");
        }

        #endregion
    }
}