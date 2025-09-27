using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static LLMDataArchitectTest.ActionTable;
using static StateSystem;
using static UnityEditorInternal.VersionControl.ListControl;

namespace LLMDataArchitectTest
{

    /// <summary>
    /// LLMに入力するデータの構造体
    /// </summary>
    public struct LLMInputData
    {
        /// <summary>
        /// 直近敵が実行したアクションの配列
        /// </summary>
        public ActionState[] RecentActionArray { get; set; }

        /// <summary>
        /// 自分のキャラクターデータ
        /// </summary>
        public CharacterData MyData { get; set; }

        /// <summary>
        /// 敵のキャラクターデータ
        /// </summary>
        public CharacterData EnemyData { get; set; }

        /// <summary>
        /// 行動ログの集積
        /// </summary>
        public ActionProbabilityManager ActionLog { get; set; }

        /// <summary>
        /// 自分がダメージを与えることに成功した状況のログ
        /// </summary>
        public HitSituation[] HitSituations { get; set; }

        /// <summary>
        /// 自分がダメージを受けた状況のログ
        /// </summary>
        public HitSituation[] EnemyHitSituations { get; set; }

        /// <summary>
        /// 前回の判断データ
        /// </summary>
        public StrategyData? LastStrategy { get; set; }

        /// <summary>
        /// この構造体をJSON形式の文字列に変換します。
        /// </summary>
        /// <param name="data">変換するデータ</param>
        /// <param name="indented">インデント付きの整形JSONにするかどうか</param>
        /// <returns>シリアライズされたJSON文字列</returns>
        public static string ToJson(LLMInputData data, bool indented = true)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = indented ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            };

            // 新プロンプト形式のデータ構造（最終版）
            var newPromptData = new
            {
                敵の最近の行動履歴 = data.RecentActionArray,
                敵の今までの行動割合 = new
                {
                    後ろ回避 = data.ActionLog?.BackwardDodgePercentage ?? 0,
                    横回避 = data.ActionLog?.HorizontalDodgePercentage ?? 0,
                    前回避 = data.ActionLog?.ForwardDodgePercentage ?? 0,
                    ガード = data.ActionLog?.GuardPercentage ?? 0,
                    ブロッキング = data.ActionLog?.BlockingPercentage ?? 0,
                    弱攻撃 = data.ActionLog?.LightAttackPercentage ?? 0,
                    強攻撃 = data.ActionLog?.StrongAttackPercentage ?? 0,
                    強攻撃キャンセル = data.ActionLog?.StrongAttackCancelPercentage ?? 0,
                    横回避攻撃 = data.ActionLog?.HorizontalDodgeAttackPercentage ?? 0,
                    前回避攻撃 = data.ActionLog?.ForwardDodgeAttackPercentage ?? 0
                }
            };

            return JsonConvert.SerializeObject(newPromptData, settings);
        }

        /// <summary>
        /// 特定のテスト状況に応じたデータを生成
        /// </summary>
        public static LLMInputData CreateForTestSituation(TestSituationType situationType)
        {
            var rand = new Random();

            // 状況に応じた基本設定
            CharacterData myData, enemyData;
            CreateSituationBasedCharacters(situationType, out myData, out enemyData, rand);

            // 敵の行動履歴（状況に応じて変更）
            ActionState[] recentActions = CreateRecentActions(situationType, rand);

            // 効果的だった攻撃履歴の生成
            var hitSituations = CreateEffectiveAttackHistory(rand);

            // 危険だった防御履歴の生成
            var enemyHitSituations = CreateDangerousDefenseHistory(rand);

            // 敵の行動確率
            var actionLog = CreateActionProbabilities(situationType, rand);

            // 前回戦略
            var lastStrategy = CreateDefaultStrategy();

            return new LLMInputData
            {
                RecentActionArray = recentActions,
                MyData = myData,
                EnemyData = enemyData,
                ActionLog = actionLog,
                HitSituations = hitSituations,
                EnemyHitSituations = enemyHitSituations,
                LastStrategy = lastStrategy
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
                HitType = ActionState.後ろ回避,
                // float計算を外し、intの範囲で生成します
                GetDamage = 14 + rand.Next(1, 11) // 15-24ダメージ
            },
            new HitSituation
            {
                HitState = ActionState.強攻撃,
                HitType = ActionState.後ろ回避,
                GetDamage = 22 + rand.Next(1, 6) // 23-27ダメージ
            },
            new HitSituation
            {
                HitState = ActionState.強攻撃,
                HitType = ActionState.後ろ回避,
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
                HitType = ActionState.弱攻撃,
                // float計算を外し、intの範囲で生成します
                GetDamage = rand.Next(1, 3) // 1-2の軽微ダメージ
            },
            new HitSituation
            {
                HitState = ActionState.弱攻撃ブロッキング,
                HitType = ActionState.弱攻撃,
                GetDamage = 1 + rand.Next(1, 3) // 2-3の軽微ダメージ
            },
            new HitSituation
            {
                HitState = ActionState.弱攻撃ブロッキング,
                HitType = ActionState.弱攻撃,
                GetDamage = 1 + rand.Next(1, 3) // 2-3の軽微ダメージ
            }
        };
        }

        /// <summary>
        /// 状況に応じた敵の行動確率生成
        /// </summary>
        private static ActionProbabilityManager CreateActionProbabilities(TestSituationType situationType, Random rand)
        {
            var actionLog = new ActionProbabilityManager();

            switch (situationType)
            {
                case TestSituationType.優勢:
                    // 敵が守備的
                    actionLog.BackwardDodgePercentage = 0.15f;
                    actionLog.GuardPercentage = 0.1f;
                    actionLog.LightAttackPercentage = 0.15f;
                    actionLog.StrongAttackPercentage = 0.1f;
                    break;

                case TestSituationType.劣勢:
                    // 敵が攻撃的
                    actionLog.LightAttackPercentage = 0.3f;
                    actionLog.StrongAttackPercentage = 0.25f;
                    actionLog.ForwardDodgeAttackPercentage = 0.15f;
                    break;

                default:
                    // 標準的な確率
                    actionLog.InitializeBasicProbabilities();
                    break;
            }

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
        /// カスタムデータを生成（PromptGenerator用）
        /// </summary>
        /// <param name="myHp">自分の体力</param>
        /// <param name="myMaxHp">自分の最大体力</param>
        /// <param name="enemyHp">敵の体力</param>
        /// <param name="enemyMaxHp">敵の最大体力</param>
        /// <param name="myEnergy">自分のエネルギー</param>
        /// <param name="myMaxEnergy">自分の最大エネルギー</param>
        /// <param name="enemyEnergy">敵のエネルギー</param>
        /// <param name="enemyMaxEnergy">敵の最大エネルギー</param>
        /// <param name="recentEnemyActions">敵の最近の行動</param>
        /// <returns>カスタマイズされた入力データ</returns>
        public static LLMInputData CreateCustom(
            int myHp, int myMaxHp, int enemyHp, int enemyMaxHp,
            int myEnergy, int myMaxEnergy, int enemyEnergy, int enemyMaxEnergy,
            ActionState[] recentEnemyActions = null)
        {
            var rand = new Random();

            // データ整合性チェック
            myHp = Math.Clamp(myHp, 1, myMaxHp);
            enemyHp = Math.Clamp(enemyHp, 1, enemyMaxHp);
            myEnergy = Math.Clamp(myEnergy, 0, myMaxEnergy);
            enemyEnergy = Math.Clamp(enemyEnergy, 0, enemyMaxEnergy);

            // 最近の敵の行動が指定されていない場合はランダムに生成
            if (recentEnemyActions == null || recentEnemyActions.Length == 0)
            {
                var actions = Enum.GetValues(typeof(ActionState)).Cast<ActionState>().ToArray();
                recentEnemyActions = Enumerable.Range(0, 5)
                    .Select(_ => actions[rand.Next(actions.Length)])
                    .ToArray();
            }

            // 戦況判定
            var myHpRatio = (float)myHp / myMaxHp;
            var enemyHpRatio = (float)enemyHp / enemyMaxHp;
            var myEnergyRatio = (float)myEnergy / myMaxEnergy;

            var situationType = DetermineSituationType(myHpRatio, enemyHpRatio, myEnergyRatio);

            // 位置データ（戦況に応じて調整）
            var distance = GetReasonableDistance(myHpRatio, enemyHpRatio, myEnergyRatio);
            var myPos = new Vector3(60, 0, 100);
            var enemyPos = new Vector3(60 - distance, 0, 100 - distance / 2);

            return new LLMInputData
            {
                RecentActionArray = recentEnemyActions,
                MyData = new CharacterData
                {
                    Hp = myHp,
                    MaxHp = myMaxHp,
                    Energy = myEnergy,
                    MaxEnergy = myMaxEnergy
                },
                EnemyData = new CharacterData
                {
                    Hp = enemyHp,
                    MaxHp = enemyMaxHp,
                    Energy = enemyEnergy,
                    MaxEnergy = enemyMaxEnergy
                },
                ActionLog = CreateRealisticActionLogForSituation(myHpRatio, enemyHpRatio, myEnergyRatio),
                HitSituations = GenerateContextualHitSituations(myHpRatio > enemyHpRatio, 3, rand),
                EnemyHitSituations = GenerateContextualEnemyHitSituations(myHpRatio < enemyHpRatio, 3, rand),
                LastStrategy = CreateDefaultStrategy()
            };
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
        /// 戦況に応じたリアルな行動ログを作成
        /// </summary>
        private static ActionProbabilityManager CreateRealisticActionLogForSituation(float myHpRatio, float enemyHpRatio, float myEnergyRatio)
        {
            var actionLog = new ActionProbabilityManager();

            // 基本確率を設定
            actionLog.InitializeBasicProbabilities();

            // 戦況に応じて確率を調整
            if (myHpRatio > enemyHpRatio * 1.2f) // 優勢時：敵が守備的
            {
                actionLog.BackwardDodgePercentage = 0.15f;
                actionLog.GuardPercentage = 0.10f;
                actionLog.LightAttackPercentage = 0.15f;
                actionLog.StrongAttackPercentage = 0.10f;
            }
            else if (myHpRatio < enemyHpRatio * 0.8f) // 劣勢時：敵が攻撃的
            {
                actionLog.ForwardDodgePercentage = 0.20f;
                actionLog.LightAttackPercentage = 0.30f;
                actionLog.StrongAttackPercentage = 0.25f;
                actionLog.ForwardDodgeAttackPercentage = 0.15f;
            }
            else // 拮抗時：標準的な確率
            {
                actionLog.LightAttackPercentage = 0.25f;
                actionLog.StrongAttackPercentage = 0.20f;
                actionLog.ForwardDodgePercentage = 0.15f;
                actionLog.BlockingPercentage = 0.10f;
            }

            return actionLog;
        }

        /// <summary>
        /// 戦況に応じたヒット状況を生成
        /// </summary>
        private static HitSituation[] GenerateContextualHitSituations(bool isAdvantage, int count, Random rand)
        {
            var situations = new List<HitSituation>();

            for (int i = 0; i < count; i++)
            {
                if (isAdvantage) // 優勢時はより多くダメージを与えている
                {
                    situations.Add(new HitSituation
                    {
                        HitState = ActionState.強攻撃,
                        HitType = ActionState.後ろ回避,
                        GetDamage = (float)(rand.NextDouble() * 15 + 12) // 12-27ダメージ
                    });
                }
                else // 劣勢時は少ないダメージ
                {
                    situations.Add(new HitSituation
                    {
                        HitState = ActionState.弱攻撃,
                        HitType = ActionState.ガード,
                        GetDamage = (float)(rand.NextDouble() * 8 + 3) // 3-11ダメージ
                    });
                }
            }

            return situations.ToArray();
        }

        /// <summary>
        /// 戦況に応じた被ダメージ状況を生成
        /// </summary>
        private static HitSituation[] GenerateContextualEnemyHitSituations(bool isDisadvantage, int count, Random rand)
        {
            var situations = new List<HitSituation>();

            for (int i = 0; i < count; i++)
            {
                if (isDisadvantage) // 劣勢時はより多くダメージを受けている
                {
                    situations.Add(new HitSituation
                    {
                        HitState = ActionState.ガード,
                        HitType = ActionState.強攻撃,
                        GetDamage = (float)(rand.NextDouble() * 20 + 15) // 15-35ダメージ
                    });
                }
                else // 優勢時は少ない被ダメージ
                {
                    situations.Add(new HitSituation
                    {
                        HitState = ActionState.弱攻撃ブロッキング,
                        HitType = ActionState.弱攻撃,
                        GetDamage = (float)(rand.NextDouble() * 3 + 0.5f) // 0.5-3.5ダメージ
                    });
                }
            }

            return situations.ToArray();
        }

        /// <summary>
        /// デフォルトの戦略データを作成
        /// </summary>
        private static StrategyData CreateDefaultStrategy()
        {
            return new StrategyData
            {
                結論 = "バランス重視で対応する。",
                理由 = "現在の状況では安全第一の戦術が適している。",
                基本戦術 = "対応型",
                行動テーブル = new ActionTable
                {
                    敵攻撃体勢 = "ガード",
                    敵待機状態 = "弱攻撃",
                    自分微有利状況 = "弱攻撃",
                    自分有利状況 = "強攻撃",
                    自分微不利状況 = "ガード",
                    自分不利状況 = "後ろ回避",
                    自分強攻撃ヒット = "弱攻撃",
                    敵強攻撃ヒット = "後ろ回避"
                }
            };
        }
    }

    /// <summary>
    /// Vector3用のJSONコンバーター
    /// </summary>
    public class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException("Expected StartObject token");

            float x = 0, y = 0, z = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string propertyName = reader.Value?.ToString()?.ToLowerInvariant() ?? "";
                    reader.Read();

                    switch (propertyName)
                    {
                        case "x":
                            x = Convert.ToSingle(reader.Value);
                            break;
                        case "y":
                            y = Convert.ToSingle(reader.Value);
                            break;
                        case "z":
                            z = Convert.ToSingle(reader.Value);
                            break;
                    }
                }
            }

            return new Vector3(x, y, z);
        }

        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.X);
            writer.WritePropertyName("y");
            writer.WriteValue(value.Y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.Z);
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// AIキャラクターの各アクションの実行確率を管理するクラス
    /// プロンプトで使用される確率名に対応
    /// </summary>
    public class ActionProbabilityManager
    {
        /// <summary>
        /// 後ろ回避の実行確率
        /// </summary>
        public float BackwardDodgePercentage { get; set; }

        /// <summary>
        /// 横回避の実行確率（左右統合）
        /// </summary>
        public float HorizontalDodgePercentage { get; set; }

        /// <summary>
        /// 前回避の実行確率
        /// </summary>
        public float ForwardDodgePercentage { get; set; }

        /// <summary>
        /// ガードの実行確率
        /// </summary>
        public float GuardPercentage { get; set; }

        /// <summary>
        /// ブロッキングの実行確率
        /// </summary>
        public float BlockingPercentage { get; set; }

        /// <summary>
        /// 弱攻撃の実行確率
        /// </summary>
        public float LightAttackPercentage { get; set; }

        /// <summary>
        /// 強攻撃の実行確率
        /// </summary>
        public float StrongAttackPercentage { get; set; }

        /// <summary>
        /// 強攻撃キャンセルの実行確率
        /// </summary>
        public float StrongAttackCancelPercentage { get; set; }

        /// <summary>
        /// 横回避攻撃の実行確率
        /// </summary>
        public float HorizontalDodgeAttackPercentage { get; set; }

        /// <summary>
        /// 前回避攻撃の実行確率
        /// </summary>
        public float ForwardDodgeAttackPercentage { get; set; }

        /// <summary>
        /// コンストラクタ。基本確率で初期化
        /// </summary>
        public ActionProbabilityManager()
        {
            InitializeBasicProbabilities();
        }

        /// <summary>
        /// 基本的な確率で初期化
        /// </summary>
        public void InitializeBasicProbabilities()
        {
            BackwardDodgePercentage = 0.05f;
            HorizontalDodgePercentage = 0.05f;
            ForwardDodgePercentage = 0.15f;
            GuardPercentage = 0.05f;
            BlockingPercentage = 0.05f;
            LightAttackPercentage = 0.25f;
            StrongAttackPercentage = 0.20f;
            StrongAttackCancelPercentage = 0.05f;
            HorizontalDodgeAttackPercentage = 0.10f;
            ForwardDodgeAttackPercentage = 0.10f;
        }
    }

    /// <summary>
    /// 攻撃ヒット時の状況をまとめる
    /// </summary>
    public struct HitSituation
    {
        /// <summary>
        /// ヒットした時の状態（自身の行動）
        /// </summary>
        public ActionState HitState { get; set; }

        /// <summary>
        /// ヒット時の敵の行動（敵の攻撃・行動）
        /// </summary>
        public ActionState HitType { get; set; }

        /// <summary>
        /// 与えた/受けたダメージ
        /// </summary>
        // 2. GetDamage の型を float に修正し、より正確なダメージ計算に対応
        public float GetDamage { get; set; }

        // 3. コンストラクタを完成させる
        public HitSituation(ActionState hitState, ActionState attackType, float damage)
        {
            // プロパティに引数を代入
            HitState = hitState;
            HitType = attackType;
            GetDamage = damage;
        }

        /// <summary>
        /// ダメージ報告情報からヒット情報を作成する
        /// </summary>
        /// <param name="reportInfo"></param>
        public HitSituation(in DamageReportInfo reportInfo, ActionState hitState)
        {
            // プロパティに引数を代入
            HitState = hitState;
            HitType = reportInfo.attackType == AttackType.WeakAttack ? ActionState.弱攻撃 : ActionState.強攻撃;
            GetDamage = reportInfo.damage;
        }
    }

    /// <summary>
    /// 新プロンプト対応の戦略データ
    /// </summary>
    public class StrategyData
    {
        /// <summary>
        /// 戦略的な結論（行動方針）
        /// </summary>
        [JsonProperty("結論")]
        public string? 結論 { get; set; }

        /// <summary>
        /// 結論に至った理由
        /// </summary>
        [JsonProperty("理由")]
        public string? 理由 { get; set; }

        /// <summary>
        /// 基本戦術
        /// </summary>
        [JsonProperty("基本戦術")]
        public string? 基本戦術 { get; set; }

        /// <summary>
        /// 状況ごとの行動テーブル
        /// </summary>
        [JsonProperty("行動テーブル")]
        public ActionTable? 行動テーブル { get; set; }

        /// <summary>
        /// サンプルデータを生成するファクトリーメソッド
        /// </summary>
        public static StrategyData CreateSample()
        {
            return new StrategyData
            {
                結論 = "軸ずらしと防御を優先し、隙が出たら弱攻撃で反撃する戦術を取る。",
                理由 = "敵が強攻撃を狙うと隙が大きいため、そのタイミングを見て反撃できる。また、無理に前進するとリスクが高いため、防御と回避を基本とする。",
                基本戦術 = "対応型",
                行動テーブル = new ActionTable
                {
                    敵攻撃体勢 = "ガード",
                    敵待機状態 = "弱攻撃",
                    自分微有利状況 = "弱攻撃",
                    自分有利状況 = "強攻撃",
                    自分微不利状況 = "ガード",
                    自分不利状況 = "後ろ回避",
                    自分強攻撃ヒット = "弱攻撃",
                    敵強攻撃ヒット = "後ろ回避"
                }
            };
        }
    }

    /// <summary>
    /// 新プロンプト対応の行動テーブル
    /// </summary>
    public class ActionTable
    {
        [JsonProperty("敵攻撃体勢")]
        public string? 敵攻撃体勢 { get; set; }

        [JsonProperty("敵待機状態")]
        public string? 敵待機状態 { get; set; }

        [JsonProperty("自分微有利状況")]
        public string? 自分微有利状況 { get; set; }

        [JsonProperty("自分有利状況")]
        public string? 自分有利状況 { get; set; }

        [JsonProperty("自分微不利状況")]
        public string? 自分微不利状況 { get; set; }

        [JsonProperty("自分不利状況")]
        public string? 自分不利状況 { get; set; }

        [JsonProperty("自分強攻撃ヒット")]
        public string? 自分強攻撃ヒット { get; set; }

        [JsonProperty("敵強攻撃ヒット")]
        public string? 敵強攻撃ヒット { get; set; }

        /// <summary>
        /// デフォルトの行動テーブルを作成
        /// </summary>
        public static ActionTable CreateDefault()
        {
            return new ActionTable
            {
                敵攻撃体勢 = "ガード",
                敵待機状態 = "弱攻撃",
                自分微有利状況 = "弱攻撃",
                自分有利状況 = "強攻撃",
                自分微不利状況 = "ガード",
                自分不利状況 = "後ろ回避",
                自分強攻撃ヒット = "弱攻撃",
                敵強攻撃ヒット = "後ろ回避"
            };
        }

        /// <summary>
        /// 攻撃的な行動テーブルを作成（優勢時用）
        /// </summary>
        public static ActionTable CreateAggressive()
        {
            return new ActionTable
            {
                敵攻撃体勢 = "強攻撃",
                敵待機状態 = "強攻撃",
                自分微有利状況 = "強攻撃",
                自分有利状況 = "強攻撃",
                自分微不利状況 = "弱攻撃",
                自分不利状況 = "弱攻撃",
                自分強攻撃ヒット = "強攻撃",
                敵強攻撃ヒット = "強攻撃"
            };
        }

        /// <summary>
        /// 守備的な行動テーブルを作成（劣勢時用）
        /// </summary>
        public static ActionTable CreateDefensive()
        {
            return new ActionTable
            {
                敵攻撃体勢 = "後ろ回避",
                敵待機状態 = "ガード",
                自分微有利状況 = "ガード",
                自分有利状況 = "弱攻撃",
                自分微不利状況 = "後ろ回避",
                自分不利状況 = "後ろ回避",
                自分強攻撃ヒット = "ガード",
                敵強攻撃ヒット = "後ろ回避"
            };
        }

        /// <summary>
        /// エネルギー節約重視の行動テーブルを作成（エネルギー不足時用）
        /// </summary>
        public static ActionTable CreateEnergySaving()
        {
            return new ActionTable
            {
                敵攻撃体勢 = "ガード",
                敵待機状態 = "ガード",
                自分微有利状況 = "ガード",
                自分有利状況 = "弱攻撃",
                自分微不利状況 = "ガード",
                自分不利状況 = "ガード",
                自分強攻撃ヒット = "ガード",
                敵強攻撃ヒット = "ガード"
            };
        }

        /// <summary>
        /// 回避重視の行動テーブルを作成（体力危険時用）
        /// </summary>
        public static ActionTable CreateEvasive()
        {
            return new ActionTable
            {
                敵攻撃体勢 = "後ろ回避",
                敵待機状態 = "後ろ回避",
                自分微有利状況 = "後ろ回避",
                自分有利状況 = "横回避",
                自分微不利状況 = "後ろ回避",
                自分不利状況 = "後ろ回避",
                自分強攻撃ヒット = "後ろ回避",
                敵強攻撃ヒット = "後ろ回避"
            };
        }

        /// <summary>
        /// 状況に応じた行動テーブルを作成
        /// </summary>
        /// <param name="situationType">テスト状況の種類</param>
        /// <returns>適切な行動テーブル</returns>
        public static ActionTable CreateForSituation(TestSituationType situationType)
        {
            return situationType switch
            {
                TestSituationType.優勢 => CreateAggressive(),
                TestSituationType.劣勢 => CreateDefensive(),
                TestSituationType.エネルギー不足 => CreateEnergySaving(),
                TestSituationType.体力危険 => CreateEvasive(),
                _ => CreateDefault()
            };
        }

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
        /// 行動テーブルを検証（全ての行動が有効な選択肢かチェック）
        /// </summary>
        /// <returns>検証結果のメッセージ</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var validActions = new HashSet<string>
        {
            "後ろ回避", "横回避", "前回避", "ガード", "ブロッキング",
            "弱攻撃", "強攻撃", "強攻撃キャンセル", "横回避攻撃", "前回避攻撃",
            "弱攻撃ブロッキング", "強攻撃ブロッキング"
        };

            CheckAction(nameof(敵攻撃体勢), 敵攻撃体勢, validActions, errors);
            CheckAction(nameof(敵待機状態), 敵待機状態, validActions, errors);
            CheckAction(nameof(自分微有利状況), 自分微有利状況, validActions, errors);
            CheckAction(nameof(自分有利状況), 自分有利状況, validActions, errors);
            CheckAction(nameof(自分微不利状況), 自分微不利状況, validActions, errors);
            CheckAction(nameof(自分不利状況), 自分不利状況, validActions, errors);
            CheckAction(nameof(自分強攻撃ヒット), 自分強攻撃ヒット, validActions, errors);
            CheckAction(nameof(敵強攻撃ヒット), 敵強攻撃ヒット, validActions, errors);

            return errors;
        }

        private void CheckAction(string fieldName, string? action, HashSet<string> validActions, List<string> errors)
        {
            if (string.IsNullOrEmpty(action))
            {
                errors.Add($"{fieldName} が設定されていません。");
            }
            else if (!validActions.Contains(action))
            {
                errors.Add($"{fieldName} の値 '{action}' は有効な行動ではありません。");
            }
        }

        /// <summary>
        /// 行動テーブルの統計情報を取得
        /// </summary>
        /// <returns>統計情報</returns>
        public ActionTableStats GetStats()
        {
            var actions = new[] { 敵攻撃体勢, 敵待機状態, 自分微有利状況, 自分有利状況,
                             自分微不利状況, 自分不利状況, 自分強攻撃ヒット, 敵強攻撃ヒット };

            var stats = new ActionTableStats();

            foreach (var action in actions.Where(a => !string.IsNullOrEmpty(a)))
            {
                switch (action)
                {
                    case "弱攻撃":
                    case "強攻撃":
                    case "強攻撃キャンセル":
                    case "横回避攻撃":
                    case "前回避攻撃":
                    case "弱攻撃ブロッキング":
                    case "強攻撃ブロッキング":
                        stats.AttackActionsCount++;
                        break;
                    case "後ろ回避":
                    case "横回避":
                    case "前回避":
                    case "ガード":
                    case "ブロッキング":
                        stats.DefenseActionsCount++;
                        break;
                }
            }

            stats.TotalActions = actions.Count(a => !string.IsNullOrEmpty(a));
            stats.AttackRatio = stats.TotalActions > 0 ? (float)stats.AttackActionsCount / stats.TotalActions : 0f;
            stats.DefenseRatio = stats.TotalActions > 0 ? (float)stats.DefenseActionsCount / stats.TotalActions : 0f;

            return stats;
        }
    }

    /// <summary>
    /// 行動テーブルの統計情報
    /// </summary>
    public class ActionTableStats
    {
        /// <summary>
        /// 攻撃系行動の数
        /// </summary>
        public int AttackActionsCount { get; set; }

        /// <summary>
        /// 防御系行動の数
        /// </summary>
        public int DefenseActionsCount { get; set; }

        /// <summary>
        /// 総行動数
        /// </summary>
        public int TotalActions { get; set; }

        /// <summary>
        /// 攻撃行動の割合
        /// </summary>
        public float AttackRatio { get; set; }

        /// <summary>
        /// 防御行動の割合
        /// </summary>
        public float DefenseRatio { get; set; }

        /// <summary>
        /// 戦術傾向の判定
        /// </summary>
        public string TacticTendency
        {
            get
            {
                if (AttackRatio > 0.6f)
                    return "攻撃的";
                if (DefenseRatio > 0.6f)
                    return "守備的";
                return "バランス型";
            }
        }

        public override string ToString()
        {
            return $"攻撃:{AttackActionsCount} 防御:{DefenseActionsCount} " +
                   $"攻撃率:{AttackRatio:P0} 防御率:{DefenseRatio:P0} " +
                   $"傾向:{TacticTendency}";
        }
    }

    /// <summary>
    /// プロンプト分析用の計算結果クラス
    /// </summary>
    public class BattleAnalysisResult
    {
        /// <summary>
        /// 自分のHP割合
        /// </summary>
        public float MyHpPercentage { get; set; }

        /// <summary>
        /// 敵のHP割合
        /// </summary>
        public float EnemyHpPercentage { get; set; }

        /// <summary>
        /// 体力差（自分% - 敵%）
        /// </summary>
        public float HpDifference { get; set; }

        /// <summary>
        /// 自分のエネルギー割合
        /// </summary>
        public float MyEnergyPercentage { get; set; }

        /// <summary>
        /// 効果的だった攻撃の説明
        /// </summary>
        public string EffectiveAttack { get; set; } = "";

        /// <summary>
        /// 危険だった防御の説明
        /// </summary>
        public string DangerousDefense { get; set; } = "";

        /// <summary>
        /// 敵の攻撃傾向
        /// </summary>
        public string EnemyAttackTendency { get; set; } = "";

        /// <summary>
        /// 決定された戦術タイプ
        /// </summary>
        public string TacticType { get; set; } = "";

        /// <summary>
        /// 計算結果の文字列表現
        /// </summary>
        public string CalculationSummary =>
            $"自分HP{MyHpPercentage:F0}% 敵HP{EnemyHpPercentage:F0}% 差{HpDifference:+0;-0;0}P エネルギー{MyEnergyPercentage:F0}%";

        /// <summary>
        /// LLMInputDataから分析結果を計算
        /// </summary>
        public static BattleAnalysisResult AnalyzeFromInputData(LLMInputData inputData)
        {
            var result = new BattleAnalysisResult();

            // HP割合計算
            result.MyHpPercentage = (float)inputData.MyData.Hp / inputData.MyData.MaxHp * 100f;
            result.EnemyHpPercentage = (float)inputData.EnemyData.Hp / inputData.EnemyData.MaxHp * 100f;
            result.HpDifference = result.MyHpPercentage - result.EnemyHpPercentage;

            // エネルギー割合計算
            result.MyEnergyPercentage = (float)inputData.MyData.Energy / inputData.MyData.MaxEnergy * 100f;

            // 効果的だった攻撃の分析
            if (inputData.HitSituations != null && inputData.HitSituations.Length > 0)
            {
                var maxDamageHit = inputData.HitSituations.OrderByDescending(h => h.GetDamage).First();
                result.EffectiveAttack = $"{maxDamageHit.HitState}（敵{maxDamageHit.HitType}時に{maxDamageHit.GetDamage:F1}ダメージ）";
            }

            // 危険だった防御の分析
            if (inputData.EnemyHitSituations != null && inputData.EnemyHitSituations.Length > 0)
            {
                var maxDamageReceived = inputData.EnemyHitSituations.OrderByDescending(h => h.GetDamage).First();
                result.DangerousDefense = $"{maxDamageReceived.HitState}（敵{maxDamageReceived.HitType}時に{maxDamageReceived.GetDamage:F1}被ダメージ）";
            }

            // 敵の攻撃傾向分析
            if (inputData.ActionLog != null)
            {
                var attackPercentage = inputData.ActionLog.LightAttackPercentage + inputData.ActionLog.StrongAttackPercentage;
                var defensePercentage = inputData.ActionLog.GuardPercentage + inputData.ActionLog.BackwardDodgePercentage;

                if (attackPercentage > 0.4f)
                    result.EnemyAttackTendency = "攻撃型（積極的な攻撃を好む）";
                else if (defensePercentage > 0.3f)
                    result.EnemyAttackTendency = "守備型（防御を重視する）";
                else
                    result.EnemyAttackTendency = "バランス型（攻守のバランスを取る）";
            }

            // 戦術判定ルール適用
            if (result.HpDifference >= 20f && result.MyEnergyPercentage >= 50f)
                result.TacticType = "攻撃型";
            else if (result.HpDifference <= -20f || result.MyEnergyPercentage <= 30f)
                result.TacticType = "防御型";
            else
                result.TacticType = "対応型";

            return result;
        }
    }
}