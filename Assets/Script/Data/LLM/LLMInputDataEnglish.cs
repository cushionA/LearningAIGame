using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static LLMDataArchitect.ActionTableEnglish;


namespace LLMDataArchitect
{
    #region 列挙型

    /// <summary>
    /// 選択する攻撃タイプの列挙（英語版）
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ActionListEnglish : byte
    {
        //-------Defense------------
        BackwardDodge,// 長所：敵の近接攻撃の択に対する安全行動 短所：敵が攻撃をキャンセルした場合に強射撃攻撃が確定する
        HorizontalDodge,// 長所：射撃と上、または横からの近接攻撃をすり抜けられる 短所：ブロッキングされると大ダメージの可能性
        ForwardDodge,//長所：銃撃を避けながら接近できる 短所：近接攻撃を避けられない。
        Guard,// 長所：低コスト 短所:強攻撃、強射撃を防げない
        Blocking,// 長所：強攻撃、弱攻撃を防げる。ハイリスク 短所：エネルギー消費アリ
        //-------Attack------------
        LightAttack,// 特徴：出が早い近接攻撃 長所：隙が小さい。エネルギー消費少ない 短所：威力が低い。ガードされる。ブロッキングされると大ダメージの可能性（難度は高い） 
        HeavyAttack, // 特徴：出が遅い近接攻撃 長所：ガード不可。威力が高い。ブロッキングされても低リスク（低難度） 短所：隙が大きい。エネルギー消費多い。
        HeavyAttackCancel,// 特徴：強攻撃を出さずに止める 長所：隙が小さい。エネルギー消費多い。敵のブロッキングや回避を誘える 短所：攻撃判定が出ない。敵の攻撃頻度が高い場合は無意味
        HorizontalDodgeAttack,
        ForwardDodgeAttack,//長所：銃撃を避けながら接近し攻撃できる 短所：近接攻撃を避けられない。ブロッキングされると大ダメージの可能性
        //-------AIの行動指定にだけ使う（英語版）------------
        LightAttackBlocking,// 長所：強攻撃、弱攻撃を防げる。高リターン 短所：高リスクでエネルギー消費アリ
        HeavyAttackBlocking,// 長所：強攻撃、弱攻撃を防げる。低リスク 短所：エネルギー消費アリ
    }

    #endregion

    /// <summary>
    /// LLMに入力するデータの構造体（英語版）
    /// </summary>
    public struct LLMInputDataEnglish
    {
        /// <summary>
        /// 直近敵が実行したアクションの配列
        /// </summary>
        public ActionListEnglish[] RecentActionArray { get; set; }

        /// <summary>
        /// 自分のキャラクターデータ
        /// </summary>
        public CharacterDataEnglish MyData { get; set; }

        /// <summary>
        /// 敵のキャラクターデータ
        /// </summary>
        public CharacterDataEnglish EnemyData { get; set; }

        /// <summary>
        /// 行動ログの集積
        /// </summary>
        public ActionProbabilityManagerEnglish ActionLog { get; set; }

        /// <summary>
        /// 自分がダメージを与えることに成功した状況のログ
        /// </summary>
        public HitSituationEnglish[] HitSituations { get; set; }

        /// <summary>
        /// 自分がダメージを受けた状況のログ
        /// </summary>
        public HitSituationEnglish[] EnemyHitSituations { get; set; }

        /// <summary>
        /// 前回の判断データ
        /// </summary>
        public StrategyDataEnglish? LastStrategy { get; set; }

        /// <summary>
        /// この構造体をJSON形式の文字列に変換します。
        /// </summary>
        /// <param name="data">変換するデータ</param>
        /// <param name="indented">インデント付きの整形JSONにするかどうか</param>
        /// <returns>シリアライズされたJSON文字列</returns>
        public static string ToJson(LLMInputDataEnglish data, bool indented = true)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = indented ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            };

            // 新プロンプト形式のデータ構造（英語版）
            var newPromptData = new
            {
                enemy_recent_action_history = data.RecentActionArray,
                enemy_action_probabilities = new
                {
                    backward_dodge = data.ActionLog?.BackwardDodgePercentage ?? 0,
                    horizontal_dodge = data.ActionLog?.HorizontalDodgePercentage ?? 0,
                    forward_dodge = data.ActionLog?.ForwardDodgePercentage ?? 0,
                    guard = data.ActionLog?.GuardPercentage ?? 0,
                    blocking = data.ActionLog?.BlockingPercentage ?? 0,
                    light_attack = data.ActionLog?.LightAttackPercentage ?? 0,
                    heavy_attack = data.ActionLog?.StrongAttackPercentage ?? 0,
                    heavy_attack_cancel = data.ActionLog?.StrongAttackCancelPercentage ?? 0,
                    horizontal_dodge_attack = data.ActionLog?.HorizontalDodgeAttackPercentage ?? 0,
                    forward_dodge_attack = data.ActionLog?.ForwardDodgeAttackPercentage ?? 0
                }
            };

            return JsonConvert.SerializeObject(newPromptData, settings);
        }

        /// <summary>
        /// 特定のテスト状況に応じたデータを生成
        /// </summary>
        public static LLMInputDataEnglish CreateForTestSituation(TestSituationTypeEnglish situationType)
        {
            var rand = new Random();

            // 状況に応じた基本設定
            CharacterDataEnglish myData, enemyData;
            CreateSituationBasedCharacters(situationType, out myData, out enemyData, rand);

            // 敵の行動履歴（状況に応じて変更）
            ActionListEnglish[] recentActions = CreateRecentActions(situationType, rand);

            // 効果的だった攻撃履歴の生成
            var hitSituations = CreateEffectiveAttackHistory(rand);

            // 危険だった防御履歴の生成
            var enemyHitSituations = CreateDangerousDefenseHistory(rand);

            // 敵の行動確率
            var actionLog = CreateActionProbabilities(situationType, rand);

            // 前回戦略
            var lastStrategy = CreateDefaultStrategy();

            return new LLMInputDataEnglish
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
        private static void CreateSituationBasedCharacters(TestSituationTypeEnglish situationType,
            out CharacterDataEnglish myData, out CharacterDataEnglish enemyData, Random rand)
        {
            switch (situationType)
            {
                case TestSituationTypeEnglish.Advantage:
                    myData = new CharacterDataEnglish
                    {
                        Hp = 180,
                        MaxHp = 200,
                        Energy = 85,
                        MaxEnergy = 100,
                        Position = new Vector3(60f, 0f, 100f),
                        WeakAttackRange = 1.95f,
                        StrongAttackRange = 4.77f,
                        WeakAttackDamage = 12.8f,
                        StrongAttackDamage = 17.5f
                    };
                    enemyData = new CharacterDataEnglish
                    {
                        Hp = 85,
                        MaxHp = 200,
                        Energy = 45,
                        MaxEnergy = 100,
                        Position = new Vector3(35f, 0f, 87.5f),
                        WeakAttackRange = 2.62f,
                        StrongAttackRange = 3.39f,
                        WeakAttackDamage = 10.6f,
                        StrongAttackDamage = 22.3f
                    };
                    break;

                case TestSituationTypeEnglish.Even:
                    myData = new CharacterDataEnglish
                    {
                        Hp = 150,
                        MaxHp = 200,
                        Energy = 60,
                        MaxEnergy = 100,
                        Position = new Vector3(60f, 0f, 100f),
                        WeakAttackRange = 1.95f,
                        StrongAttackRange = 4.77f,
                        WeakAttackDamage = 12.8f,
                        StrongAttackDamage = 17.5f
                    };
                    enemyData = new CharacterDataEnglish
                    {
                        Hp = 145,
                        MaxHp = 200,
                        Energy = 65,
                        MaxEnergy = 100,
                        Position = new Vector3(42f, 0f, 91f),
                        WeakAttackRange = 2.62f,
                        StrongAttackRange = 3.39f,
                        WeakAttackDamage = 10.6f,
                        StrongAttackDamage = 22.3f
                    };
                    break;

                case TestSituationTypeEnglish.Disadvantage:
                    myData = new CharacterDataEnglish
                    {
                        Hp = 65,
                        MaxHp = 200,
                        Energy = 35,
                        MaxEnergy = 100,
                        Position = new Vector3(60f, 0f, 100f),
                        WeakAttackRange = 1.95f,
                        StrongAttackRange = 4.77f,
                        WeakAttackDamage = 12.8f,
                        StrongAttackDamage = 17.5f
                    };
                    enemyData = new CharacterDataEnglish
                    {
                        Hp = 175,
                        MaxHp = 200,
                        Energy = 80,
                        MaxEnergy = 100,
                        Position = new Vector3(52f, 0f, 96f),
                        WeakAttackRange = 2.62f,
                        StrongAttackRange = 3.39f,
                        WeakAttackDamage = 10.6f,
                        StrongAttackDamage = 22.3f
                    };
                    break;

                case TestSituationTypeEnglish.LowEnergy:
                    myData = new CharacterDataEnglish
                    {
                        Hp = 140,
                        MaxHp = 200,
                        Energy = 15,
                        MaxEnergy = 100,
                        Position = new Vector3(60f, 0f, 100f),
                        WeakAttackRange = 1.95f,
                        StrongAttackRange = 4.77f,
                        WeakAttackDamage = 12.8f,
                        StrongAttackDamage = 17.5f
                    };
                    enemyData = new CharacterDataEnglish
                    {
                        Hp = 155,
                        MaxHp = 200,
                        Energy = 70,
                        MaxEnergy = 100,
                        Position = new Vector3(45f, 0f, 92.5f),
                        WeakAttackRange = 2.62f,
                        StrongAttackRange = 3.39f,
                        WeakAttackDamage = 10.6f,
                        StrongAttackDamage = 22.3f
                    };
                    break;

                case TestSituationTypeEnglish.CriticalHP:
                    myData = new CharacterDataEnglish
                    {
                        Hp = 35,
                        MaxHp = 200,
                        Energy = 55,
                        MaxEnergy = 100,
                        Position = new Vector3(60f, 0f, 100f),
                        WeakAttackRange = 1.95f,
                        StrongAttackRange = 4.77f,
                        WeakAttackDamage = 12.8f,
                        StrongAttackDamage = 17.5f
                    };
                    enemyData = new CharacterDataEnglish
                    {
                        Hp = 120,
                        MaxHp = 200,
                        Energy = 60,
                        MaxEnergy = 100,
                        Position = new Vector3(52f, 0f, 96f),
                        WeakAttackRange = 2.62f,
                        StrongAttackRange = 3.39f,
                        WeakAttackDamage = 10.6f,
                        StrongAttackDamage = 22.3f
                    };
                    break;

                default:
                    throw new ArgumentException($"Unknown situation type: {situationType}");
            }
        }

        /// <summary>
        /// 状況に応じた敵の行動履歴生成
        /// </summary>
        private static ActionListEnglish[] CreateRecentActions(TestSituationTypeEnglish situationType, Random rand)
        {
            switch (situationType)
            {
                case TestSituationTypeEnglish.Advantage:
                    return new[] { ActionListEnglish.BackwardDodge, ActionListEnglish.Guard, ActionListEnglish.HorizontalDodge, ActionListEnglish.LightAttackBlocking, ActionListEnglish.BackwardDodge };

                case TestSituationTypeEnglish.Even:
                    return new[] { ActionListEnglish.LightAttack, ActionListEnglish.HorizontalDodge, ActionListEnglish.HeavyAttack, ActionListEnglish.ForwardDodge, ActionListEnglish.LightAttack };

                case TestSituationTypeEnglish.Disadvantage:
                    return new[] { ActionListEnglish.HeavyAttack, ActionListEnglish.ForwardDodgeAttack, ActionListEnglish.LightAttack, ActionListEnglish.HorizontalDodgeAttack, ActionListEnglish.HeavyAttack };

                case TestSituationTypeEnglish.LowEnergy:
                    return new[] { ActionListEnglish.LightAttack, ActionListEnglish.HeavyAttackCancel, ActionListEnglish.ForwardDodge, ActionListEnglish.LightAttack, ActionListEnglish.HeavyAttackCancel };

                case TestSituationTypeEnglish.CriticalHP:
                    return new[] { ActionListEnglish.HeavyAttack, ActionListEnglish.ForwardDodgeAttack, ActionListEnglish.HeavyAttack, ActionListEnglish.LightAttack, ActionListEnglish.ForwardDodgeAttack };

                default:
                    throw new ArgumentException($"Unknown situation type: {situationType}");
            }
        }

        /// <summary>
        /// 効果的だった攻撃履歴の生成（最も高ダメージを記録）
        /// </summary>
        private static HitSituationEnglish[] CreateEffectiveAttackHistory(Random rand)
        {
            // 強攻撃が効果的だったシナリオ（敵の後ろ回避時に高ダメージ）
            return new[]
            {
                new HitSituationEnglish
                {
                    SituationType = ActionListEnglish.HeavyAttack,
                    EnemyActionType = ActionListEnglish.BackwardDodge,
                    GetDamage = 14.2f + (float)rand.NextDouble() * 10f // 14-24ダメージ
                },
                new HitSituationEnglish
                {
                    SituationType = ActionListEnglish.HeavyAttack,
                    EnemyActionType = ActionListEnglish.BackwardDodge,
                    GetDamage = 22.4f + (float)rand.NextDouble() * 5f // 22-27ダメージ
                },
                new HitSituationEnglish
                {
                    SituationType = ActionListEnglish.HeavyAttack,
                    EnemyActionType = ActionListEnglish.BackwardDodge,
                    GetDamage = 24.5f + (float)rand.NextDouble() * 3f // 24-27ダメージ
                }
            };
        }

        /// <summary>
        /// 危険だった防御履歴の生成（最も被ダメージが大きかった状況）
        /// </summary>
        private static HitSituationEnglish[] CreateDangerousDefenseHistory(Random rand)
        {
            // ガード中に敵の強攻撃で大ダメージを受けたシナリオ
            return new[]
            {
                new HitSituationEnglish
                {
                    SituationType = ActionListEnglish.LightAttackBlocking,
                    EnemyActionType = ActionListEnglish.LightAttack,
                    GetDamage = 0.5f + (float)rand.NextDouble() * 2f // 0.5-2.5の軽微ダメージ
                },
                new HitSituationEnglish
                {
                    SituationType = ActionListEnglish.LightAttackBlocking,
                    EnemyActionType = ActionListEnglish.LightAttack,
                    GetDamage = 1.5f + (float)rand.NextDouble() * 2f // 1.5-3.5の軽微ダメージ
                },
                new HitSituationEnglish
                {
                    SituationType = ActionListEnglish.LightAttackBlocking,
                    EnemyActionType = ActionListEnglish.LightAttack,
                    GetDamage = 1.3f + (float)rand.NextDouble() * 2f // 1.3-3.3の軽微ダメージ
                }
            };
        }

        /// <summary>
        /// 状況に応じた敵の行動確率生成
        /// </summary>
        private static ActionProbabilityManagerEnglish CreateActionProbabilities(TestSituationTypeEnglish situationType, Random rand)
        {
            var actionLog = new ActionProbabilityManagerEnglish();

            switch (situationType)
            {
                case TestSituationTypeEnglish.Advantage:
                    // 敵が守備的
                    actionLog.BackwardDodgePercentage = 0.15f;
                    actionLog.GuardPercentage = 0.1f;
                    actionLog.LightAttackPercentage = 0.15f;
                    actionLog.StrongAttackPercentage = 0.1f;
                    break;

                case TestSituationTypeEnglish.Disadvantage:
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
        public static LLMInputDataEnglish CreateRandom(int recentActionCount = 5, int hitCount = 3)
        {
            var rand = new Random();
            var situationType = (TestSituationTypeEnglish)rand.Next(Enum.GetValues(typeof(TestSituationTypeEnglish)).Length);
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
        public static LLMInputDataEnglish CreateCustom(
            int myHp, int myMaxHp, int enemyHp, int enemyMaxHp,
            int myEnergy, int myMaxEnergy, int enemyEnergy, int enemyMaxEnergy,
            ActionListEnglish[] recentEnemyActions = null)
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
                var actions = Enum.GetValues(typeof(ActionListEnglish)).Cast<ActionListEnglish>().ToArray();
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

            return new LLMInputDataEnglish
            {
                RecentActionArray = recentEnemyActions,
                MyData = new CharacterDataEnglish
                {
                    Hp = myHp,
                    MaxHp = myMaxHp,
                    Energy = myEnergy,
                    MaxEnergy = myMaxEnergy,
                    Position = myPos,
                    WeakAttackRange = 1.95f,
                    StrongAttackRange = 4.77f,
                    WeakAttackDamage = 12.8f,
                    StrongAttackDamage = 17.5f
                },
                EnemyData = new CharacterDataEnglish
                {
                    Hp = enemyHp,
                    MaxHp = enemyMaxHp,
                    Energy = enemyEnergy,
                    MaxEnergy = enemyMaxEnergy,
                    Position = enemyPos,
                    WeakAttackRange = 2.62f,
                    StrongAttackRange = 3.39f,
                    WeakAttackDamage = 10.6f,
                    StrongAttackDamage = 22.3f
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
        private static TestSituationTypeEnglish DetermineSituationType(float myHpRatio, float enemyHpRatio, float myEnergyRatio)
        {
            if (myHpRatio < 0.3f)
                return TestSituationTypeEnglish.CriticalHP;
            if (myEnergyRatio < 0.3f)
                return TestSituationTypeEnglish.LowEnergy;

            var hpDiff = (myHpRatio - enemyHpRatio) * 100f;
            if (hpDiff >= 20f)
                return TestSituationTypeEnglish.Advantage;
            if (hpDiff <= -20f)
                return TestSituationTypeEnglish.Disadvantage;
            return TestSituationTypeEnglish.Even;
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
        private static ActionProbabilityManagerEnglish CreateRealisticActionLogForSituation(float myHpRatio, float enemyHpRatio, float myEnergyRatio)
        {
            var actionLog = new ActionProbabilityManagerEnglish();

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
        private static HitSituationEnglish[] GenerateContextualHitSituations(bool isAdvantage, int count, Random rand)
        {
            var situations = new List<HitSituationEnglish>();

            for (int i = 0; i < count; i++)
            {
                if (isAdvantage) // 優勢時はより多くダメージを与えている
                {
                    situations.Add(new HitSituationEnglish
                    {
                        SituationType = ActionListEnglish.HeavyAttack,
                        EnemyActionType = ActionListEnglish.BackwardDodge,
                        GetDamage = (float)(rand.NextDouble() * 15 + 12) // 12-27ダメージ
                    });
                }
                else // 劣勢時は少ないダメージ
                {
                    situations.Add(new HitSituationEnglish
                    {
                        SituationType = ActionListEnglish.LightAttack,
                        EnemyActionType = ActionListEnglish.Guard,
                        GetDamage = (float)(rand.NextDouble() * 8 + 3) // 3-11ダメージ
                    });
                }
            }

            return situations.ToArray();
        }

        /// <summary>
        /// 戦況に応じた被ダメージ状況を生成
        /// </summary>
        private static HitSituationEnglish[] GenerateContextualEnemyHitSituations(bool isDisadvantage, int count, Random rand)
        {
            var situations = new List<HitSituationEnglish>();

            for (int i = 0; i < count; i++)
            {
                if (isDisadvantage) // 劣勢時はより多くダメージを受けている
                {
                    situations.Add(new HitSituationEnglish
                    {
                        SituationType = ActionListEnglish.Guard,
                        EnemyActionType = ActionListEnglish.HeavyAttack,
                        GetDamage = (float)(rand.NextDouble() * 20 + 15) // 15-35ダメージ
                    });
                }
                else // 優勢時は少ない被ダメージ
                {
                    situations.Add(new HitSituationEnglish
                    {
                        SituationType = ActionListEnglish.LightAttackBlocking,
                        EnemyActionType = ActionListEnglish.LightAttack,
                        GetDamage = (float)(rand.NextDouble() * 3 + 0.5f) // 0.5-3.5ダメージ
                    });
                }
            }

            return situations.ToArray();
        }

        /// <summary>
        /// デフォルトの戦略データを作成
        /// </summary>
        private static StrategyDataEnglish CreateDefaultStrategy()
        {
            return new StrategyDataEnglish
            {
                Conclusion = "Adopt a balanced approach to respond.",
                Reasoning = "Safety-first tactics are appropriate for the current situation.",
                BasicTactics = "Adaptive",
                ActionTable = new ActionTableEnglish
                {
                    EnemyAttackStance = "Guard",
                    EnemyStandbyState = "Light Attack",
                    MySlightAdvantage = "Light Attack",
                    MyAdvantage = "Heavy Attack",
                    MySlightDisadvantage = "Guard",
                    MyDisadvantage = "Backward Dodge",
                    MyHeavyAttackHit = "Light Attack",
                    EnemyHeavyAttackHit = "Backward Dodge"
                }
            };
        }
    }

    /// <summary>
    /// Vector3用のJSONコンバーター（英語版）
    /// </summary>
    public class Vector3JsonConverterEnglish : JsonConverter<Vector3>
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
    /// キャラクターのデータ（英語版）
    /// </summary>
    public class CharacterDataEnglish
    {
        /// <summary>
        /// 体力
        /// </summary>
        public int Hp { get; set; }

        /// <summary>
        /// 最大体力
        /// </summary>
        public int MaxHp { get; set; }

        /// <summary>
        /// 現在のエネルギー
        /// </summary>
        public int Energy { get; set; }

        /// <summary>
        /// 最大エネルギー
        /// </summary>
        public int MaxEnergy { get; set; }

        // 以下のプロパティはJSONに含めたくない場合は[JsonIgnore]を追加
        /// <summary>
        /// 位置
        /// </summary>
        [JsonIgnore] // JSONシリアライズ時に無視
        [JsonConverter(typeof(Vector3JsonConverterEnglish))]
        public Vector3 Position { get; set; }

        /// <summary>
        /// 弱攻撃のリーチ
        /// </summary>
        [JsonIgnore] // JSONシリアライズ時に無視
        public float WeakAttackRange { get; set; }

        /// <summary>
        /// 強攻撃の届くリーチ
        /// </summary>
        [JsonIgnore] // JSONシリアライズ時に無視
        public float StrongAttackRange { get; set; }

        /// <summary>
        /// 弱攻撃のダメージ
        /// </summary>
        [JsonIgnore] // JSONシリアライズ時に無視
        public float WeakAttackDamage { get; set; }

        /// <summary>
        /// 強攻撃のダメージ
        /// </summary>
        [JsonIgnore] // JSONシリアライズ時に無視
        public float StrongAttackDamage { get; set; }
    }


    /// <summary>
    /// AIキャラクターの各アクションの実行確率を管理するクラス（英語版）
    /// プロンプトで使用される確率名に対応
    /// </summary>
    public class ActionProbabilityManagerEnglish
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
        public ActionProbabilityManagerEnglish()
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
    /// 攻撃ヒット時の状況をまとめる（英語版）
    /// </summary>
    public struct HitSituationEnglish
    {
        /// <summary>
        /// ヒットした時の状況
        /// </summary>
        public ActionListEnglish SituationType { get; set; }

        /// <summary>
        /// ヒット時の敵の行動
        /// </summary>
        public ActionListEnglish EnemyActionType { get; set; }

        /// <summary>
        /// 与えたダメージ
        /// </summary>
        public float GetDamage { get; set; }
    }

    /// <summary>
    /// 新プロンプト対応の戦略データ（英語版）
    /// </summary>
    public class StrategyDataEnglish
    {
        /// <summary>
        /// 戦略的な結論（行動方針）
        /// </summary>
        [JsonProperty("conclusion")]
        public string? Conclusion { get; set; }

        /// <summary>
        /// 結論に至った理由
        /// </summary>
        [JsonProperty("reasoning")]
        public string? Reasoning { get; set; }

        /// <summary>
        /// 基本戦術
        /// </summary>
        [JsonProperty("basic_tactics")]
        public string? BasicTactics { get; set; }

        /// <summary>
        /// 状況ごとの行動テーブル
        /// </summary>
        [JsonProperty("action_table")]
        public ActionTableEnglish? ActionTable { get; set; }

        /// <summary>
        /// サンプルデータを生成するファクトリーメソッド
        /// </summary>
        public static StrategyDataEnglish CreateSample()
        {
            return new StrategyDataEnglish
            {
                Conclusion = "Prioritize axis shifting and defense, counterattack with light attacks when openings appear.",
                Reasoning = "Since the enemy creates large openings when aiming for heavy attacks, we can counterattack at those timings. Additionally, aggressive advances carry high risks, so defense and evasion should be the foundation.",
                BasicTactics = "Adaptive",
                ActionTable = new ActionTableEnglish
                {
                    EnemyAttackStance = "Guard",
                    EnemyStandbyState = "Light Attack",
                    MySlightAdvantage = "Light Attack",
                    MyAdvantage = "Heavy Attack",
                    MySlightDisadvantage = "Guard",
                    MyDisadvantage = "Backward Dodge",
                    MyHeavyAttackHit = "Light Attack",
                    EnemyHeavyAttackHit = "Backward Dodge"
                }
            };
        }
    }

    /// <summary>
    /// 新プロンプト対応の行動テーブル（英語版）
    /// </summary>
    public class ActionTableEnglish
    {
        [JsonProperty("enemy_attack_stance")]
        public string? EnemyAttackStance { get; set; }

        [JsonProperty("enemy_standby_state")]
        public string? EnemyStandbyState { get; set; }

        [JsonProperty("my_slight_advantage")]
        public string? MySlightAdvantage { get; set; }

        [JsonProperty("my_advantage")]
        public string? MyAdvantage { get; set; }

        [JsonProperty("my_slight_disadvantage")]
        public string? MySlightDisadvantage { get; set; }

        [JsonProperty("my_disadvantage")]
        public string? MyDisadvantage { get; set; }

        [JsonProperty("my_heavy_attack_hit")]
        public string? MyHeavyAttackHit { get; set; }

        [JsonProperty("enemy_heavy_attack_hit")]
        public string? EnemyHeavyAttackHit { get; set; }

        /// <summary>
        /// デフォルトの行動テーブルを作成
        /// </summary>
        public static ActionTableEnglish CreateDefault()
        {
            return new ActionTableEnglish
            {
                EnemyAttackStance = "Guard",
                EnemyStandbyState = "Light Attack",
                MySlightAdvantage = "Light Attack",
                MyAdvantage = "Heavy Attack",
                MySlightDisadvantage = "Guard",
                MyDisadvantage = "Backward Dodge",
                MyHeavyAttackHit = "Light Attack",
                EnemyHeavyAttackHit = "Backward Dodge"
            };
        }

        /// <summary>
        /// 攻撃的な行動テーブルを作成（優勢時用）
        /// </summary>
        public static ActionTableEnglish CreateAggressive()
        {
            return new ActionTableEnglish
            {
                EnemyAttackStance = "Heavy Attack",
                EnemyStandbyState = "Heavy Attack",
                MySlightAdvantage = "Heavy Attack",
                MyAdvantage = "Heavy Attack",
                MySlightDisadvantage = "Light Attack",
                MyDisadvantage = "Light Attack",
                MyHeavyAttackHit = "Heavy Attack",
                EnemyHeavyAttackHit = "Heavy Attack"
            };
        }

        /// <summary>
        /// 守備的な行動テーブルを作成（劣勢時用）
        /// </summary>
        public static ActionTableEnglish CreateDefensive()
        {
            return new ActionTableEnglish
            {
                EnemyAttackStance = "Backward Dodge",
                EnemyStandbyState = "Guard",
                MySlightAdvantage = "Guard",
                MyAdvantage = "Light Attack",
                MySlightDisadvantage = "Backward Dodge",
                MyDisadvantage = "Backward Dodge",
                MyHeavyAttackHit = "Guard",
                EnemyHeavyAttackHit = "Backward Dodge"
            };
        }

        /// <summary>
        /// エネルギー節約重視の行動テーブルを作成（エネルギー不足時用）
        /// </summary>
        public static ActionTableEnglish CreateEnergySaving()
        {
            return new ActionTableEnglish
            {
                EnemyAttackStance = "Guard",
                EnemyStandbyState = "Guard",
                MySlightAdvantage = "Guard",
                MyAdvantage = "Light Attack",
                MySlightDisadvantage = "Guard",
                MyDisadvantage = "Guard",
                MyHeavyAttackHit = "Guard",
                EnemyHeavyAttackHit = "Guard"
            };
        }

        /// <summary>
        /// 回避重視の行動テーブルを作成（体力危険時用）
        /// </summary>
        public static ActionTableEnglish CreateEvasive()
        {
            return new ActionTableEnglish
            {
                EnemyAttackStance = "Backward Dodge",
                EnemyStandbyState = "Backward Dodge",
                MySlightAdvantage = "Backward Dodge",
                MyAdvantage = "Horizontal Dodge",
                MySlightDisadvantage = "Backward Dodge",
                MyDisadvantage = "Backward Dodge",
                MyHeavyAttackHit = "Backward Dodge",
                EnemyHeavyAttackHit = "Backward Dodge"
            };
        }

        /// <summary>
        /// 状況に応じた行動テーブルを作成
        /// </summary>
        /// <param name="situationType">テスト状況の種類</param>
        /// <returns>適切な行動テーブル</returns>
        public static ActionTableEnglish CreateForSituation(TestSituationTypeEnglish situationType)
        {
            return situationType switch
            {
                TestSituationTypeEnglish.Advantage => CreateAggressive(),
                TestSituationTypeEnglish.Disadvantage => CreateDefensive(),
                TestSituationTypeEnglish.LowEnergy => CreateEnergySaving(),
                TestSituationTypeEnglish.CriticalHP => CreateEvasive(),
                _ => CreateDefault()
            };
        }

        /// <summary>
        /// TestSituationType列挙型（英語版）
        /// </summary>
        public enum TestSituationTypeEnglish
        {
            Advantage,      // 自分有利
            Even,           // 互角
            Disadvantage,   // 敵有利
            LowEnergy,      // エネルギー危機
            CriticalHP      // 体力危機
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
                "Backward Dodge", "Horizontal Dodge", "Forward Dodge", "Guard", "Blocking",
                "Light Attack", "Heavy Attack", "Heavy Attack Cancel", "Horizontal Dodge Attack", "Forward Dodge Attack",
                "Light Attack Blocking", "Heavy Attack Blocking"
            };

            CheckAction(nameof(EnemyAttackStance), EnemyAttackStance, validActions, errors);
            CheckAction(nameof(EnemyStandbyState), EnemyStandbyState, validActions, errors);
            CheckAction(nameof(MySlightAdvantage), MySlightAdvantage, validActions, errors);
            CheckAction(nameof(MyAdvantage), MyAdvantage, validActions, errors);
            CheckAction(nameof(MySlightDisadvantage), MySlightDisadvantage, validActions, errors);
            CheckAction(nameof(MyDisadvantage), MyDisadvantage, validActions, errors);
            CheckAction(nameof(MyHeavyAttackHit), MyHeavyAttackHit, validActions, errors);
            CheckAction(nameof(EnemyHeavyAttackHit), EnemyHeavyAttackHit, validActions, errors);

            return errors;
        }

        private void CheckAction(string fieldName, string? action, HashSet<string> validActions, List<string> errors)
        {
            if (string.IsNullOrEmpty(action))
            {
                errors.Add($"{fieldName} is not set.");
            }
            else if (!validActions.Contains(action))
            {
                errors.Add($"{fieldName} value '{action}' is not a valid action.");
            }
        }

        /// <summary>
        /// 行動テーブルの統計情報を取得
        /// </summary>
        /// <returns>統計情報</returns>
        public ActionTableStatsEnglish GetStats()
        {
            var actions = new[] { EnemyAttackStance, EnemyStandbyState, MySlightAdvantage, MyAdvantage,
                             MySlightDisadvantage, MyDisadvantage, MyHeavyAttackHit, EnemyHeavyAttackHit };

            var stats = new ActionTableStatsEnglish();

            foreach (var action in actions.Where(a => !string.IsNullOrEmpty(a)))
            {
                switch (action)
                {
                    case "Light Attack":
                    case "Heavy Attack":
                    case "Heavy Attack Cancel":
                    case "Horizontal Dodge Attack":
                    case "Forward Dodge Attack":
                    case "Light Attack Blocking":
                    case "Heavy Attack Blocking":
                        stats.AttackActionsCount++;
                        break;
                    case "Backward Dodge":
                    case "Horizontal Dodge":
                    case "Forward Dodge":
                    case "Guard":
                    case "Blocking":
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
    /// 行動テーブルの統計情報（英語版）
    /// </summary>
    public class ActionTableStatsEnglish
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
                    return "Aggressive";
                if (DefenseRatio > 0.6f)
                    return "Defensive";
                return "Balanced";
            }
        }

        public override string ToString()
        {
            return $"Attack:{AttackActionsCount} Defense:{DefenseActionsCount} " +
                   $"Attack Rate:{AttackRatio:P0} Defense Rate:{DefenseRatio:P0} " +
                   $"Tendency:{TacticTendency}";
        }
    }

    /// <summary>
    /// プロンプト分析用の計算結果クラス（英語版）
    /// </summary>
    public class BattleAnalysisResultEnglish
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
            $"My HP {MyHpPercentage:F0}% Enemy HP {EnemyHpPercentage:F0}% Diff {HpDifference:+0;-0;0}P Energy {MyEnergyPercentage:F0}%";

        /// <summary>
        /// LLMInputDataから分析結果を計算
        /// </summary>
        public static BattleAnalysisResultEnglish AnalyzeFromInputData(LLMInputDataEnglish inputData)
        {
            var result = new BattleAnalysisResultEnglish();

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
                result.EffectiveAttack = $"{maxDamageHit.SituationType} (when enemy {maxDamageHit.EnemyActionType}, {maxDamageHit.GetDamage:F1} damage)";
            }

            // 危険だった防御の分析
            if (inputData.EnemyHitSituations != null && inputData.EnemyHitSituations.Length > 0)
            {
                var maxDamageReceived = inputData.EnemyHitSituations.OrderByDescending(h => h.GetDamage).First();
                result.DangerousDefense = $"{maxDamageReceived.SituationType} (when enemy {maxDamageReceived.EnemyActionType}, {maxDamageReceived.GetDamage:F1} damage taken)";
            }

            // 敵の攻撃傾向分析
            if (inputData.ActionLog != null)
            {
                var attackPercentage = inputData.ActionLog.LightAttackPercentage + inputData.ActionLog.StrongAttackPercentage;
                var defensePercentage = inputData.ActionLog.GuardPercentage + inputData.ActionLog.BackwardDodgePercentage;

                if (attackPercentage > 0.4f)
                    result.EnemyAttackTendency = "Aggressive type (prefers active attacks)";
                else if (defensePercentage > 0.3f)
                    result.EnemyAttackTendency = "Defensive type (emphasizes defense)";
                else
                    result.EnemyAttackTendency = "Balanced type (balances offense and defense)";
            }

            // 戦術判定ルール適用
            if (result.HpDifference >= 20f && result.MyEnergyPercentage >= 50f)
                result.TacticType = "Aggressive";
            else if (result.HpDifference <= -20f || result.MyEnergyPercentage <= 30f)
                result.TacticType = "Defensive";
            else
                result.TacticType = "Adaptive";

            return result;
        }
    }
}