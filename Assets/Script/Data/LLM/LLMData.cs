using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static LearningAIGame.CombatSystem.Core.StateSystem;
using static LLMDataArchitect.ActionTable;

//==============================================ファイルヘッダ=========================================================
// LLMData
// 
// 概要: LLMへの入力データ構造とテストデータ生成機能を提供
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// [LLMInputData構造体]
// - RecentActionArray: 敵の直近行動履歴
// - MyData / EnemyData: 自分と敵のキャラクターデータ（HP、エネルギー）
// - ActionLog: 敵の行動確率統計（ActionProbabilityManager）
// - HitSituations / EnemyHitSituations: 与ダメージ/被ダメージ履歴
// - LastStrategy: 前回の戦略判断データ
// - ToJson: JSON形式にシリアライズ
// - CreateForTestSituation: テスト状況別データ生成
// - CreateCustom: カスタムパラメータからデータ生成
// 
// [ActionProbabilityManager]
// - 各行動（後ろ回避、横回避、前回避、ガード、ブロッキング、弱攻撃、強攻撃など）の実行確率を管理
// - InitializeBasicProbabilities: 基本確率で初期化
// 
// [HitSituation]
// - 攻撃ヒット/被弾時の状況記録（自分の行動、敵の行動、ダメージ量）
// - DamageReportInfoからの変換コンストラクタ提供
// 
// [StrategyData]
// - LLMの判断結果を格納（結論、理由、基本戦術、行動テーブル）
// 
// [ActionTable]
// - 状況別の行動マッピング（敵攻撃体勢、敵待機状態、有利/不利状況など）
// - CreateDefault / CreateAggressive / CreateDefensive: 戦術別テーブル生成
// - Validate: 行動テーブルの妥当性検証
// - GetStats: 攻撃/防御比率の統計情報取得
// 
// [補助クラス]
// - Vector3JsonConverter: System.Numerics.Vector3のJSON変換
// - ActionTableStats: 行動テーブルの統計情報（攻撃/防御数、割合、戦術傾向）
// 
// 入力元クラス: BattleCharacterController, StateSystem
// 出力先クラス: LLMシステム（JSON形式）
// 
// その他:
// テスト用データ生成機能（優勢/拮抗/劣勢/エネルギー不足/体力危険）を含む
// 新プロンプト形式に対応したJSON出力構造
//=====================================================================================================================
namespace LLMDataArchitect
{
    /// <summary>
    /// TestSituationType列挙型
    /// </summary>
    public enum TestSituationType
    {
        優勢,      // 自分有利
        拮抗,      // 互角
        劣勢,      // 敵有利
        エネルギー不足, // エネルギー危機
        体力危険    // 体力危機
    }

    /// <summary>
    /// LLMに入力するデータの構造体
    /// 最初にStateSystemからキャラデータとログデータの参照を取る
    /// </summary>
    public class LLMInputData
    {
        /// <summary>
        /// 自分のキャラクターデータ
        /// stateSystemから参照を取る
        /// </summary>
        public CharacterData PlayerData
        {
            get => _playerData;
            set => _playerData = value;
        }

        /// <summary>
        /// 敵のキャラクターデータ
        /// stateSystemから参照を取る
        /// </summary>
        public CharacterData NPCData
        {
            get => _npcData;
            set => _npcData = value;
        }

        /// <summary>
        /// 行動ログの集積
        /// 参照はいらない
        /// </summary>
        public ActionProbabilityManager ActionLog { get { return NPCLog.ActionLog; } set { NPCLog.ActionLog = value; } }

        /// <summary>
        ///  プレイヤーの行動ログ
        /// stateSystemから参照を取る
        /// </summary>
        public LLMLogData PlayerLog
        {
            get => _playerLog;
            private set => _playerLog = value;
        }

        /// <summary>
        ///  NPCの行動ログ
        /// stateSystemから参照を取る
        /// </summary>
        public LLMLogData NPCLog
        {
            get => _npcLog;
            private set => _npcLog = value;
        }

        /// <summary>
        /// NPCの戦術の成否を保存するデータ
        /// AIから参照を取る
        /// </summary>
        public StrategyResult StrategyResult
        {
            get => _strategyResult;
            private set => _strategyResult = value;
        }

        /// <summary>
        /// プレイヤーの行動、与ダメージ/被ダメージログ
        /// </summary>
        private CharacterData _playerData;

        /// <summary>
        /// AIキャラクターの行動、与ダメージ/被ダメージログ
        /// </summary>
        private CharacterData _npcData;

        /// <summary>
        /// プレイヤーの行動、与ダメージ/被ダメージログ
        /// </summary>
        private LLMLogData _playerLog;

        /// <summary>
        /// AIキャラクターの行動、与ダメージ/被ダメージログ
        /// </summary>
        private LLMLogData _npcLog;

        /// <summary>
        /// AIからの入力で戦術の記録を保存する
        /// 各戦術の成否を出力する
        /// </summary>
        private StrategyResult _strategyResult;

        /// <summary>
        /// 現在の判断データ
        /// LLMプロンプトを生成する際は前回の判断データとして使う
        /// </summary>
        public StrategyData? CurrentStrategy { get; set; }

        /// <summary>
        /// プロンプトを生成後、前回判断を書き換える
        /// 同時に直近のデータを書き換える処理
        /// </summary>
        public void UpdateStrategy(StrategyData newStrategy)
        {
            // 直近データは消さなくても入れ替わる
            // 直近データをリセット
            //PlayerLog.ClearAllRecentLogs();

            // NPCの直近データをリセット
            // NPCLog.ClearAllRecentLogs();

            // 戦術成否データをリセット
            _strategyResult.Clear();

            // 新しい戦術データを設定
            CurrentStrategy = newStrategy;
        }


        /// <summary>
        /// バトル開始時にプレイヤーとNPCにデータを設定する
        /// StateSystemにデータを注入する
        /// </summary>
        /// <param name="stateSystem"></param>
        /// <returns></returns>
        public void InjectionNewBattle(StateSystem player, StateSystem newNPC)
        {
            // 新しいヘルスデータと既存ログデータを注入
            newNPC.CreateLLMSourceData(new CharacterData(100, 100), _npcLog);
            player.CreateLLMSourceData(new CharacterData(100, 100), _playerLog);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="player"></param>
        /// <param name="npc"></param>
        public LLMInputData(StateSystem player, StateSystem npc, StrategyResult resultData)
        {

            // StateSystemに作成したデータを注入
            (_playerData, _playerLog) = (new CharacterData(100, 100), new LLMLogData(7, 7, 7));
            player.CreateLLMSourceData(_playerData, _playerLog);

            (_npcData, _npcLog) = (new CharacterData(100, 100), new LLMLogData(7, 7, 7));

            npc.CreateLLMSourceData(_npcData, _npcLog);

            CurrentStrategy = null;

            _strategyResult = resultData;
        }

        /// <summary>
        /// テストデータ用の空コンストラクタ
        /// </summary>
        public LLMInputData()
        {

        }

        #region テストデータ生成用
        /// <summary>
        /// 特定のテスト状況に応じたデータを生成
        /// </summary>
        public static LLMInputData CreateForTestSituation(TestSituationType situationType)
        {
            var rand = new Random();

            // 状況に応じた基本設定
            CharacterData myData, enemyData;
            CreateSituationBasedCharacters(situationType, out myData, out enemyData, rand);

            // 自分の状況に応じて敵の状況を決定
            TestSituationType enemySituationType = situationType switch
            {
                TestSituationType.優勢 => TestSituationType.劣勢,
                TestSituationType.劣勢 => TestSituationType.優勢,
                TestSituationType.拮抗 => TestSituationType.拮抗,
                TestSituationType.エネルギー不足 => TestSituationType.拮抗,
                TestSituationType.体力危険 => TestSituationType.優勢,
                _ => TestSituationType.拮抗
            };

            // プレイヤーの履歴
            var playerLog = new LLMLogData();
            playerLog.SetTestData(enemySituationType);

            // 自分の履歴の生成
            var npcLog = new LLMLogData();
            npcLog.SetTestData(situationType);

            // 敵の行動確率
            var actionLog = CreateActionProbabilities(enemySituationType);

            // 前回戦略
            var lastStrategy = CreateDefaultStrategy();

            var strategyResult = new StrategyResult();
            strategyResult.SetTestData(situationType);

            return new LLMInputData()
            {
                PlayerData = myData,
                NPCData = enemyData,
                PlayerLog = playerLog,
                NPCLog = npcLog,
                ActionLog = actionLog,

                StrategyResult = strategyResult,
                CurrentStrategy = lastStrategy
            };
        }

        /// <summary>
        /// 状況に応じたキャラクターデータ生成
        /// </summary>
        private static void CreateSituationBasedCharacters(TestSituationType situationType,
            out CharacterData myData, out CharacterData enemyData, Random rand)
        {
            switch (situationType)
            {
                case TestSituationType.優勢:
                    myData = new CharacterData
                    {
                        Hp = 180,
                        MaxHp = 200,
                        Energy = 85,
                        MaxEnergy = 100
                    };
                    enemyData = new CharacterData
                    {
                        Hp = 85,
                        MaxHp = 200,
                        Energy = 45,
                        MaxEnergy = 100
                    };
                    break;

                case TestSituationType.拮抗:
                    myData = new CharacterData
                    {
                        Hp = 150,
                        MaxHp = 200,
                        Energy = 60,
                        MaxEnergy = 100
                    };
                    enemyData = new CharacterData
                    {
                        Hp = 145,
                        MaxHp = 200,
                        Energy = 65,
                        MaxEnergy = 100
                    };
                    break;

                case TestSituationType.劣勢:
                    myData = new CharacterData
                    {
                        Hp = 65,
                        MaxHp = 200,
                        Energy = 35,
                        MaxEnergy = 100
                    };
                    enemyData = new CharacterData
                    {
                        Hp = 175,
                        MaxHp = 200,
                        Energy = 80,
                        MaxEnergy = 100
                    };
                    break;

                case TestSituationType.エネルギー不足:
                    myData = new CharacterData
                    {
                        Hp = 140,
                        MaxHp = 200,
                        Energy = 15,
                        MaxEnergy = 100
                    };
                    enemyData = new CharacterData
                    {
                        Hp = 155,
                        MaxHp = 200,
                        Energy = 70,
                        MaxEnergy = 100
                    };
                    break;

                case TestSituationType.体力危険:
                    myData = new CharacterData
                    {
                        Hp = 35,
                        MaxHp = 200,
                        Energy = 55,
                        MaxEnergy = 100
                    };
                    enemyData = new CharacterData
                    {
                        Hp = 120,
                        MaxHp = 200,
                        Energy = 60,
                        MaxEnergy = 100
                    };
                    break;

                default:
                    throw new ArgumentException($"Unknown situation type: {situationType}");
            }
        }

        /// <summary>
        /// 状況に応じた敵の行動履歴生成
        /// </summary>
        private static ActionState[] CreateRecentActions(TestSituationType situationType, Random rand)
        {
            switch (situationType)
            {
                case TestSituationType.優勢:
                    return new[] { ActionState.後ろ回避, ActionState.ガード, ActionState.横回避, ActionState.弱攻撃ブロッキング, ActionState.後ろ回避 };

                case TestSituationType.拮抗:
                    return new[] { ActionState.弱攻撃, ActionState.横回避, ActionState.強攻撃, ActionState.前回避, ActionState.弱攻撃 };

                case TestSituationType.劣勢:
                    return new[] { ActionState.強攻撃, ActionState.前回避攻撃, ActionState.弱攻撃, ActionState.横回避攻撃, ActionState.強攻撃 };

                case TestSituationType.エネルギー不足:
                    return new[] { ActionState.弱攻撃, ActionState.強攻撃キャンセル, ActionState.前回避, ActionState.弱攻撃, ActionState.強攻撃キャンセル };

                case TestSituationType.体力危険:
                    return new[] { ActionState.強攻撃, ActionState.前回避攻撃, ActionState.強攻撃, ActionState.弱攻撃, ActionState.前回避攻撃 };

                default:
                    throw new ArgumentException($"Unknown situation type: {situationType}");
            }
        }

        /// <summary>
        /// 効果的だった攻撃履歴の生成（最も高ダメージを記録）
        /// </summary>
        private static HitSituation[] CreateEffectiveAttackHistory(Random rand)
        {
            // 強攻撃が効果的だったシナリオ（敵の後ろ回避時に高ダメージ）
            return new[]
            {
            new HitSituation
            {
                HitState = ActionState.強攻撃,
                DamageState = ActionState.後ろ回避,
                // float計算を外し、intの範囲で生成します
                GetDamage = 14 + rand.Next(1, 11) // 15-24ダメージ
            },
            new HitSituation
            {
                HitState = ActionState.強攻撃,
                DamageState = ActionState.後ろ回避,
                GetDamage = 22 + rand.Next(1, 6) // 23-27ダメージ
            },
            new HitSituation
            {
                HitState = ActionState.強攻撃,
                DamageState = ActionState.後ろ回避,
                GetDamage = 24 + rand.Next(1, 4) // 25-27ダメージ
            }
        };
        }

        /// <summary>
        /// 危険だった防御履歴の生成（最も被ダメージが大きかった状況）
        /// </summary>
        private static HitSituation[] CreateDangerousDefenseHistory(Random rand)
        {
            // ガード中に敵の弱攻撃で軽微なダメージを受けたシナリオ
            return new[]
            {
            new HitSituation
            {
                HitState = ActionState.弱攻撃ブロッキング,
                DamageState = ActionState.弱攻撃,
                // float計算を外し、intの範囲で生成します
                GetDamage = rand.Next(1, 3) // 1-2の軽微ダメージ
            },
            new HitSituation
            {
                HitState = ActionState.弱攻撃ブロッキング,
                DamageState = ActionState.弱攻撃,
                GetDamage = 1 + rand.Next(1, 3) // 2-3の軽微ダメージ
            },
            new HitSituation
            {
                HitState = ActionState.弱攻撃ブロッキング,
                DamageState = ActionState.弱攻撃,
                GetDamage = 1 + rand.Next(1, 3) // 2-3の軽微ダメージ
            }
        };
        }

        /// <summary>
        /// 状況に応じた敵の行動確率生成
        /// </summary>
        private static ActionProbabilityManager CreateActionProbabilities(TestSituationType situationType)
        {
            var actionLog = new ActionProbabilityManager();

            actionLog.SetTestData(situationType);

            return actionLog;
        }

        /// <summary>
        /// ランダムなデータを生成するファクトリーメソッド（後方互換性のため残存）
        /// </summary>
        public static LLMInputData CreateRandom(int recentActionCount = 5, int hitCount = 3)
        {
            var rand = new Random();
            var situationType = (TestSituationType)rand.Next(Enum.GetValues(typeof(TestSituationType)).Length);
            return CreateForTestSituation(situationType);
        }

        /// <summary>
        /// 戦況タイプを判定
        /// </summary>
        private static TestSituationType DetermineSituationType(float myHpRatio, float enemyHpRatio, float myEnergyRatio)
        {
            if (myHpRatio < 0.3f)
                return TestSituationType.体力危険;
            if (myEnergyRatio < 0.3f)
                return TestSituationType.エネルギー不足;

            var hpDiff = (myHpRatio - enemyHpRatio) * 100f;
            if (hpDiff >= 20f)
                return TestSituationType.優勢;
            if (hpDiff <= -20f)
                return TestSituationType.劣勢;
            return TestSituationType.拮抗;
        }

        /// <summary>
        /// 戦況に応じた合理的な距離を計算
        /// </summary>
        private static float GetReasonableDistance(float myHpRatio, float enemyHpRatio, float myEnergyRatio)
        {
            // 優勢時は間合いを取り、劣勢時は接近戦になりがち
            if (myHpRatio > enemyHpRatio * 1.3f)
                return 25f;      // 優勢時は距離を取る
            if (myHpRatio < enemyHpRatio * 0.7f)
                return 8f;       // 劣勢時は接近戦
            if (myEnergyRatio < 0.3f)
                return 15f;      // エネルギー不足時は中距離
            return 18f;          // 通常時
        }

        /// <summary>
        /// デフォルトの戦略データを作成
        /// </summary>
        private static StrategyData CreateDefaultStrategy()
        {
            return new StrategyData
            {
                BasicTactic = "Adaptive",
                AttackCriteria = "Cumulative Probability",
                ContinuousAttackCriteria = "Recent Pattern Focus",
                DefenseCriteria = "Cumulative Probability",
                ContinuousDefenseCriteria = "Counterattack Focus"
            };
        }

        #endregion
    }
}