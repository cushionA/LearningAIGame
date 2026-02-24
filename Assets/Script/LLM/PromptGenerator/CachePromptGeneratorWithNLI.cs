using System;
using System.Text;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// CachePromptGeneratorWithNLI
// 
// 概要: 自然言語指示（NLI: Natural Language Instruction）機能付きプロンプト生成クラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// CachePromptGeneratorを継承し、プレイスタイルや状況対応の自然言語指示を
// プロンプトに挿入する機能を追加。訓練データの多様化やゲーム内での
// プレイヤー指示に対応可能。
// 
// 自然言語指示タイプ:
// 
// ■ ルールベース系（具体的な条件指示）
// - AggressiveFinisher: 攻撃的 + 早期決着重視
// - AggressiveDisruptor: 攻撃的 + 敵を崩す
// - DefensiveSurvivor: 防御的 + 生存最優先
// - DefensiveCounter: 防御的 + 反撃機会待ち
// - BalancedAdaptive: バランス型 + 状況適応
// - AnalyticalLearner: 分析型 + 敵パターン学習
// - EnduranceManager: 持久型 + エネルギー管理
// 
// ■ 自然言語系（LLMの判断に委ねる抽象的指示）
// - CorneredBeast: 追い詰められるほど攻撃的に
// - Finisher: 敵HPが減るほど攻撃的に
// - FrontRunner: リード時は安全に
// - PatternBreaker: 予測不能に動く
// - MomentumRider: 流れに乗る/変える
// - StaminaManager: エネルギー意識
// - CounterPuncher: 反撃重視
// - Berserker: 常時攻撃的
// - Tactician: 慎重・確実
// - WaterFlow: 状況適応（柔軟）
// 
// 改善履歴:
// v2.0 - 自然言語系10タイプを小型LLM(2-3B)向けに最適化
//        - 比喩・格言を排除し直接的指示文に変更
//        - 出力フィールド名(BasicTactic等)をプロンプト内に埋め込み
//        - 条件→行動の "When X → Y" 形式で統一
//        - トークン効率改善（各タイプ約10-15%削減）
// 
// 使用方法:
// var generator = new CachePromptGeneratorWithNLI();
// string prompt = generator.GeneratePromptByData(inputData, NaturalLanguageInstructionType.CorneredBeast);
//=====================================================================================================================

namespace LLMDataArchitect.Test
{
    #region 自然言語指示タイプ列挙体

    /// <summary>
    /// 自然言語指示のタイプを定義
    /// </summary>
    public enum NaturalLanguageInstructionType
    {
        /// <summary>デフォルト（自然言語指示なし）- 従来のプロンプト動作</summary>
        None = 0,

        // =====================================================
        // ルールベース系（具体的な条件指示）
        // =====================================================

        /// <summary>攻撃的 + 早期決着重視</summary>
        AggressiveFinisher,

        /// <summary>攻撃的 + 敵を崩す</summary>
        AggressiveDisruptor,

        /// <summary>防御的 + 生存最優先</summary>
        DefensiveSurvivor,

        /// <summary>防御的 + 反撃機会待ち</summary>
        DefensiveCounter,

        /// <summary>バランス型 + 状況適応</summary>
        BalancedAdaptive,

        /// <summary>分析型 + 敵パターン学習重視</summary>
        AnalyticalLearner,

        /// <summary>持久型 + エネルギー管理重視</summary>
        EnduranceManager,

        // =====================================================
        // 自然言語系（LLMの判断に委ねる抽象的指示）
        // =====================================================

        /// <summary>追い詰められるほど攻撃的に</summary>
        CorneredBeast,

        /// <summary>敵HPが減るほど攻撃的に</summary>
        Finisher,

        /// <summary>リード時は安全に</summary>
        FrontRunner,

        /// <summary>予測不能に動く</summary>
        PatternBreaker,

        /// <summary>流れに乗る/変える</summary>
        MomentumRider,

        /// <summary>エネルギー意識</summary>
        StaminaManager,

        /// <summary>反撃重視</summary>
        CounterPuncher,

        /// <summary>常時攻撃的</summary>
        Berserker,

        /// <summary>慎重・確実</summary>
        Tactician,

        /// <summary>状況適応（柔軟）</summary>
        WaterFlow
    }

    #endregion

    /// <summary>
    /// 自然言語指示機能付きプロンプト生成クラス
    /// CachePromptGeneratorを継承し、NLI機能を追加
    /// </summary>
    public class CachePromptGeneratorWithNLI : CachePromptGenerator
    {
        #region 定数

        /// <summary>自然言語指示セクションのヘッダー</summary>
        private const string k_NLISectionHeader = "## 6. Player Instruction";

        #endregion

        #region 現在の指示タイプ

        /// <summary>現在設定されている自然言語指示タイプ</summary>
        private NaturalLanguageInstructionType _currentInstructionType = NaturalLanguageInstructionType.None;

        /// <summary>
        /// 現在の自然言語指示タイプを取得・設定
        /// </summary>
        public NaturalLanguageInstructionType CurrentInstructionType
        {
            get => _currentInstructionType;
            set => _currentInstructionType = value;
        }

        #endregion

        #region コンストラクタ

        /// <summary>
        /// デフォルトコンストラクタ（指示なし）
        /// </summary>
        public CachePromptGeneratorWithNLI() : base()
        {
            _currentInstructionType = NaturalLanguageInstructionType.None;
        }

        /// <summary>
        /// 指示タイプを指定するコンストラクタ
        /// </summary>
        /// <param name="instructionType">使用する自然言語指示タイプ</param>
        public CachePromptGeneratorWithNLI(NaturalLanguageInstructionType instructionType) : base()
        {
            _currentInstructionType = instructionType;
        }

        #endregion

        #region プロンプト生成メソッド

        /// <summary>
        /// 戦闘データと指示タイプからプロンプトを生成（オーバーロード）
        /// </summary>
        /// <param name="inputData">戦闘入力データ</param>
        /// <param name="instructionType">自然言語指示タイプ</param>
        /// <returns>生成されたプロンプト</returns>
        public string GeneratePromptByData(LLMInputData inputData, NaturalLanguageInstructionType instructionType)
        {
            // 基底クラスのプロンプト生成
            string basePrompt = base.GeneratePromptByData(inputData);

            // 指示なしの場合はそのまま返す
            if (instructionType == NaturalLanguageInstructionType.None)
            {
                return basePrompt;
            }

            // 自然言語指示セクションを生成して追加
            string nliSection = GenerateNLISection(instructionType);

            var finalPrompt = new StringBuilder();
            finalPrompt.Append(basePrompt);
            finalPrompt.AppendLine(nliSection);

            return finalPrompt.ToString();
        }

        /// <summary>
        /// 現在設定されている指示タイプでプロンプトを生成（オーバーライド）
        /// </summary>
        /// <param name="inputData">戦闘入力データ</param>
        /// <returns>生成されたプロンプト</returns>
        public override string GeneratePromptByData(LLMInputData inputData)
        {
            return GeneratePromptByData(inputData, _currentInstructionType);
        }

        /// <summary>
        /// ランダムなテストプロンプトを生成（指示タイプ指定可能）
        /// </summary>
        /// <param name="instructionType">自然言語指示タイプ</param>
        /// <returns>生成されたプロンプト</returns>
        public string GenerateRandomPrompt(NaturalLanguageInstructionType instructionType)
        {
            var randomSituation = (TestSituationType)UnityEngine.Random.Range(0, 5);
            var inputData = LLMInputData.CreateForTestSituation(randomSituation);
            return GeneratePromptByData(inputData, instructionType);
        }

        /// <summary>
        /// ランダムな指示タイプでランダムプロンプトを生成
        /// 訓練データ多様化用
        /// </summary>
        /// <returns>生成されたプロンプト</returns>
        public string GenerateRandomPromptWithRandomNLI()
        {
            // None以外のランダムな指示タイプを選択
            var instructionTypes = (NaturalLanguageInstructionType[])Enum.GetValues(typeof(NaturalLanguageInstructionType));
            int randomIndex = UnityEngine.Random.Range(1, instructionTypes.Length); // 0(None)を除外
            var randomInstructionType = instructionTypes[randomIndex];

            return GenerateRandomPrompt(randomInstructionType);
        }

        #endregion

        #region 自然言語指示セクション生成

        /// <summary>
        /// 指示タイプに応じた自然言語指示セクションを生成
        /// </summary>
        /// <param name="instructionType">自然言語指示タイプ</param>
        /// <returns>生成された指示セクション</returns>
        private string GenerateNLISection(NaturalLanguageInstructionType instructionType)
        {
            return instructionType switch
            {
                // ルールベース系
                NaturalLanguageInstructionType.AggressiveFinisher => GenerateAggressiveFinisherSection(),
                NaturalLanguageInstructionType.AggressiveDisruptor => GenerateAggressiveDisruptorSection(),
                NaturalLanguageInstructionType.DefensiveSurvivor => GenerateDefensiveSurvivorSection(),
                NaturalLanguageInstructionType.DefensiveCounter => GenerateDefensiveCounterSection(),
                NaturalLanguageInstructionType.BalancedAdaptive => GenerateBalancedAdaptiveSection(),
                NaturalLanguageInstructionType.AnalyticalLearner => GenerateAnalyticalLearnerSection(),
                NaturalLanguageInstructionType.EnduranceManager => GenerateEnduranceManagerSection(),

                // 自然言語系
                NaturalLanguageInstructionType.CorneredBeast => GenerateCorneredBeastSection(),
                NaturalLanguageInstructionType.Finisher => GenerateFinisherSection(),
                NaturalLanguageInstructionType.FrontRunner => GenerateFrontRunnerSection(),
                NaturalLanguageInstructionType.PatternBreaker => GeneratePatternBreakerSection(),
                NaturalLanguageInstructionType.MomentumRider => GenerateMomentumRiderSection(),
                NaturalLanguageInstructionType.StaminaManager => GenerateStaminaManagerSection(),
                NaturalLanguageInstructionType.CounterPuncher => GenerateCounterPuncherSection(),
                NaturalLanguageInstructionType.Berserker => GenerateBerserkerSection(),
                NaturalLanguageInstructionType.Tactician => GenerateTacticianSection(),
                NaturalLanguageInstructionType.WaterFlow => GenerateWaterFlowSection(),

                _ => string.Empty
            };
        }

        #endregion

        #region ルールベース系指示生成

        /// <summary>
        /// AggressiveFinisher: 攻撃的 + 早期決着重視
        /// </summary>
        private string GenerateAggressiveFinisherSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**PRIORITY DIRECTIVE: FINISH THE BATTLE QUICKLY**");
            sb.AppendLine();
            sb.AppendLine("The player wants to end this fight as fast as possible.");
            sb.AppendLine();
            sb.AppendLine("**Tactical Priorities:**");
            sb.AppendLine("- Strongly prefer 'Aggressive' BasicTactic");
            sb.AppendLine("- Use 'Return Priority' for AttackCriteria to maximize damage per hit");
            sb.AppendLine("- Accept higher risk trades if they lead to faster victory");
            sb.AppendLine("- Only switch to defensive if HP drops below 20%");
            sb.AppendLine("- Avoid 'Endurance' and 'Energy Efficiency' - they slow victory");
            sb.AppendLine();
            sb.AppendLine("**Decision Bias:**");
            sb.AppendLine("- When in ADVANTAGE: Push aggressively, do not play safe");
            sb.AppendLine("- When EVENLY MATCHED: Take calculated risks to gain advantage");
            sb.AppendLine("- When in DISADVANTAGE: Consider high-risk reversal tactics");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// AggressiveDisruptor: 攻撃的 + 敵を崩す
        /// </summary>
        private string GenerateAggressiveDisruptorSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**PRIORITY DIRECTIVE: DISRUPT ENEMY RHYTHM**");
            sb.AppendLine();
            sb.AppendLine("The player wants to break the enemy's patterns and create chaos.");
            sb.AppendLine();
            sb.AppendLine("**Tactical Priorities:**");
            sb.AppendLine("- Prefer 'Disruptive' or 'Aggressive' BasicTactic");
            sb.AppendLine("- Use 'Feint Focus' and 'Dispersion Focus' to confuse enemy");
            sb.AppendLine("- Mix attack patterns unpredictably");
            sb.AppendLine("- Use 'Evasive Counter Priority' for aggressive defense");
            sb.AppendLine("- Avoid repetitive or predictable tactics");
            sb.AppendLine();
            sb.AppendLine("**Decision Bias:**");
            sb.AppendLine("- Frequently change AttackCriteria and DefenseCriteria");
            sb.AppendLine("- Prioritize variety over optimization");
            sb.AppendLine("- When enemy shows strong patterns, specifically counter them");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// DefensiveSurvivor: 防御的 + 生存最優先
        /// </summary>
        private string GenerateDefensiveSurvivorSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**PRIORITY DIRECTIVE: SURVIVAL IS TOP PRIORITY**");
            sb.AppendLine();
            sb.AppendLine("The player wants to minimize damage taken at all costs.");
            sb.AppendLine();
            sb.AppendLine("**Tactical Priorities:**");
            sb.AppendLine("- Strongly prefer 'Defensive' or 'Endurance' BasicTactic");
            sb.AppendLine("- Use 'Risk Avoidance' for DefenseCriteria");
            sb.AppendLine("- Use 'Speed Priority' for AttackCriteria - quick safe hits");
            sb.AppendLine("- Never trade HP unless absolutely necessary");
            sb.AppendLine("- Preserve HP even if it means slower victory");
            sb.AppendLine();
            sb.AppendLine("**Decision Bias:**");
            sb.AppendLine("- When in ADVANTAGE: Maintain lead safely, no risky plays");
            sb.AppendLine("- When EVENLY MATCHED: Prioritize not losing over winning");
            sb.AppendLine("- When in DISADVANTAGE: Focus on surviving, wait for opportunities");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// DefensiveCounter: 防御的 + 反撃機会待ち
        /// </summary>
        private string GenerateDefensiveCounterSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**PRIORITY DIRECTIVE: WAIT FOR COUNTER OPPORTUNITIES**");
            sb.AppendLine();
            sb.AppendLine("The player wants to defend solidly and strike back at optimal moments.");
            sb.AppendLine();
            sb.AppendLine("**Tactical Priorities:**");
            sb.AppendLine("- Prefer 'Defensive' or 'Adaptive' BasicTactic");
            sb.AppendLine("- Use 'Counterattack Focus' for DefenseCriteria");
            sb.AppendLine("- Use 'Return Priority' for AttackCriteria (high damage when you do attack)");
            sb.AppendLine("- Be patient - wait for enemy mistakes");
            sb.AppendLine("- Parry and block to create counter windows");
            sb.AppendLine();
            sb.AppendLine("**Decision Bias:**");
            sb.AppendLine("- Focus on reading enemy attack patterns");
            sb.AppendLine("- Prefer reactive defense over evasion");
            sb.AppendLine("- When counter succeeds, follow up aggressively");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// BalancedAdaptive: バランス型 + 状況適応
        /// </summary>
        private string GenerateBalancedAdaptiveSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**PRIORITY DIRECTIVE: ADAPT TO THE SITUATION**");
            sb.AppendLine();
            sb.AppendLine("The player wants flexible tactics that respond to battle conditions.");
            sb.AppendLine();
            sb.AppendLine("**Tactical Priorities:**");
            sb.AppendLine("- Prefer 'Adaptive' BasicTactic as baseline");
            sb.AppendLine("- Use 'Cumulative Probability' for data-driven decisions");
            sb.AppendLine("- Adjust aggression based on HP and Energy differentials");
            sb.AppendLine("- Switch tactics when current approach shows 'Weak Effect'");
            sb.AppendLine("- Balance offense and defense based on situation");
            sb.AppendLine();
            sb.AppendLine("**Decision Bias:**");
            sb.AppendLine("- When in ADVANTAGE: Slightly aggressive, but don't overcommit");
            sb.AppendLine("- When EVENLY MATCHED: Pure adaptation based on feedback");
            sb.AppendLine("- When in DISADVANTAGE: Shift defensive, look for openings");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// AnalyticalLearner: 分析型 + 敵パターン学習
        /// </summary>
        private string GenerateAnalyticalLearnerSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**PRIORITY DIRECTIVE: ANALYZE AND LEARN ENEMY PATTERNS**");
            sb.AppendLine();
            sb.AppendLine("The player wants to study the enemy and exploit discovered weaknesses.");
            sb.AppendLine();
            sb.AppendLine("**Tactical Priorities:**");
            sb.AppendLine("- Use 'Recent Pattern Focus' to track enemy behavior changes");
            sb.AppendLine("- Probe with varied attacks to gather data");
            sb.AppendLine("- Use 'Feint Focus' occasionally to test enemy reactions");
            sb.AppendLine("- Once pattern identified, exploit it consistently");
            sb.AppendLine("- Prioritize information gathering over immediate damage");
            sb.AppendLine();
            sb.AppendLine("**Decision Bias:**");
            sb.AppendLine("- Pay close attention to 'Enemy Attack Patterns' section");
            sb.AppendLine("- Adjust criteria based on enemy's most frequent moves");
            sb.AppendLine("- Change approach when enemy pattern shifts");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// EnduranceManager: 持久型 + エネルギー管理
        /// </summary>
        private string GenerateEnduranceManagerSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**PRIORITY DIRECTIVE: MANAGE RESOURCES FOR LONG BATTLE**");
            sb.AppendLine();
            sb.AppendLine("The player expects a prolonged fight and wants to manage energy carefully.");
            sb.AppendLine();
            sb.AppendLine("**Tactical Priorities:**");
            sb.AppendLine("- Prefer 'Endurance' BasicTactic");
            sb.AppendLine("- Use 'Energy Efficiency' for AttackCriteria");
            sb.AppendLine("- Monitor Energy differential closely");
            sb.AppendLine("- Avoid high-cost moves when Energy is below 30");
            sb.AppendLine("- Outlast the opponent through superior resource management");
            sb.AppendLine();
            sb.AppendLine("**Decision Bias:**");
            sb.AppendLine("- When Energy advantage: Can afford slightly more aggressive play");
            sb.AppendLine("- When Energy disadvantage: Conserve, use low-cost options");
            sb.AppendLine("- When enemy Energy is depleted: Capitalize on their weakness");
            sb.AppendLine();
            return sb.ToString();
        }

        #endregion

        #region 自然言語系指示生成（v2.0 最適化版）

        // =========================================================================
        // 自然言語系 v2.0 設計原則:
        // 1. コア原則を1文で明示（LLMが最優先で解釈する部分）
        // 2. 条件→推奨方向を "When X → Y" 形式で統一
        // 3. 出力フィールド名(BasicTactic, AttackCriteria等)を自然に織り込み
        // 4. 比喩・格言を排除、小型LLM(2-3B)で確実に解釈できる直接的表現
        // 5. 各タイプ3-4条件文に圧縮してトークン効率を最適化
        // =========================================================================

        /// <summary>
        /// CorneredBeast: 追い詰められるほど攻撃的に
        /// 判断軸: 自分のHP残量 → リスク許容度の反比例スケーリング
        /// </summary>
        private string GenerateCorneredBeastSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**Style: Cornered Beast** - Risk tolerance increases as your HP decreases.");
            sb.AppendLine();
            sb.AppendLine("When your HP is high (>60%): prefer Defensive or Adaptive tactics, prioritize safety and Risk Avoidance.");
            sb.AppendLine("When your HP is moderate (30-60%): shift toward Aggressive tactics, accept trades if damage dealt > damage taken.");
            sb.AppendLine("When your HP is critical (<30%): commit fully to Aggressive tactics with Return Priority attacks. Defense no longer matters - maximize damage output before defeat.");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// Finisher: 敵HPが減るほど攻撃的に
        /// 判断軸: 敵のHP残量 → 攻撃強度の正比例スケーリング
        /// </summary>
        private string GenerateFinisherSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**Style: Finisher** - Aggression scales with how close the enemy is to defeat.");
            sb.AppendLine();
            sb.AppendLine("When enemy HP is high (>60%): use Adaptive or balanced tactics, gather information about enemy patterns.");
            sb.AppendLine("When enemy HP is moderate (30-60%): shift to Aggressive tactics, increase pressure with Speed Priority attacks.");
            sb.AppendLine("When enemy HP is low (<30%): fully commit to Aggressive tactics with Return Priority. Press the advantage hard - do not let the enemy recover or adapt.");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// FrontRunner: リード時は安全に
        /// 判断軸: HP差分 → リスク管理の方向性
        /// </summary>
        private string GenerateFrontRunnerSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**Style: Front Runner** - Play safer when winning, riskier when losing.");
            sb.AppendLine();
            sb.AppendLine("When your HP > enemy HP by 20+: prefer Defensive or Endurance tactics with Risk Avoidance. Let the enemy take risks and make mistakes.");
            sb.AppendLine("When HP difference is small (<20): use Adaptive tactics, respond to the situation without unnecessary risk.");
            sb.AppendLine("When your HP < enemy HP by 20+: shift to Aggressive tactics. You must take risks to close the gap.");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// PatternBreaker: 予測不能に動く
        /// 判断軸: 前回の選択 → 必ず異なる選択を行う
        /// </summary>
        private string GeneratePatternBreakerSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**Style: Pattern Breaker** - Never repeat the same approach. Maximize unpredictability.");
            sb.AppendLine();
            sb.AppendLine("Always change AttackCriteria and DefenseCriteria from the previous turn. Do not select the same values twice in a row.");
            sb.AppendLine("Prefer Disruptive or Adaptive BasicTactic. Use Feint Focus and Dispersion Focus frequently to confuse the enemy.");
            sb.AppendLine("When the enemy shows strong patterns in one direction: exploit that pattern. When your own approach gets \"Weak Effect\" feedback: switch immediately.");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// MomentumRider: 流れに乗る/変える
        /// 判断軸: 前回のPerformance Feedback結果 → 維持/変更の判断
        /// </summary>
        private string GenerateMomentumRiderSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**Style: Momentum Rider** - Repeat what succeeds, change what fails.");
            sb.AppendLine();
            sb.AppendLine("When previous Performance is \"Highly Successful\" or \"Successful\": keep the same BasicTactic and criteria. Do not change a winning approach.");
            sb.AppendLine("When previous Performance is \"Failure\" or \"Major Failure\": change BasicTactic and at least one criteria. Try a different approach.");
            sb.AppendLine("When previous Performance is \"Even\": make small adjustments to one criteria while keeping BasicTactic the same.");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// StaminaManager: エネルギー意識
        /// 判断軸: 自分vs敵のエネルギー比較 → 戦術選択
        /// </summary>
        private string GenerateStaminaManagerSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**Style: Stamina Manager** - All decisions should consider energy levels first.");
            sb.AppendLine();
            sb.AppendLine("When your Energy > enemy Energy: use Aggressive or Disruptive tactics to force the enemy into expensive actions. You can afford longer exchanges.");
            sb.AppendLine("When Energy levels are similar: prefer Adaptive tactics with Energy Efficiency for attacks. Conserve resources.");
            sb.AppendLine("When your Energy < enemy Energy: switch to Endurance tactics with Energy Efficiency. Avoid high-cost actions. Wait for the enemy to waste energy.");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// CounterPuncher: 反撃重視
        /// 判断軸: 敵の攻撃頻度 → 反撃機会の最大化
        /// </summary>
        private string GenerateCounterPuncherSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**Style: Counter Puncher** - Defend first, then punish enemy mistakes with strong counterattacks.");
            sb.AppendLine();
            sb.AppendLine("Prefer Defensive or Adaptive BasicTactic. Prioritize Counterattack Focus or Evasive Counter Priority for defense.");
            sb.AppendLine("For attacks, prefer Return Priority to maximize damage on each counter opportunity. Quality over quantity.");
            sb.AppendLine("When the enemy is aggressive (attacking frequently): this is ideal. Stay patient, focus on defense criteria, and punish every opening.");
            sb.AppendLine("When the enemy is passive: shift slightly toward Adaptive to create your own opportunities.");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// Berserker: 常時攻撃的
        /// 判断軸: なし（固定ルール） → Aggressiveを常に選択
        /// </summary>
        private string GenerateBerserkerSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**Style: Berserker** - Always choose Aggressive tactics regardless of HP or Energy situation.");
            sb.AppendLine();
            sb.AppendLine("BasicTactic should always be Aggressive. Never select Defensive or Endurance.");
            sb.AppendLine("Prefer Return Priority or Speed Priority for attacks to maximize pressure. Accept damage trades if you deal more than you take.");
            sb.AppendLine("For defense, use Evasive Counter Priority or Counterattack Focus - even defense should create attack opportunities.");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// Tactician: 慎重・確実
        /// 判断軸: リスク最小化 → 確認済みの機会のみ行動
        /// </summary>
        private string GenerateTacticianSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**Style: Tactician** - Minimize risk. Only act on confirmed opportunities.");
            sb.AppendLine();
            sb.AppendLine("Prefer Defensive or Adaptive BasicTactic. Avoid Aggressive unless you have both HP and Energy advantage.");
            sb.AppendLine("For attacks, prefer Speed Priority or Cumulative Probability - choose reliable options over high-risk ones.");
            sb.AppendLine("For defense, prefer Risk Avoidance or Cumulative Probability. Consistent small gains are better than risky big plays.");
            sb.AppendLine("When enemy shows repeated patterns: use Recent Pattern Focus to exploit predictable behavior safely.");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// WaterFlow: 状況適応（柔軟）
        /// 判断軸: 現在の優劣 + 前回戦術の効果 → 総合的な即応判断
        /// BalancedAdaptiveとの差別化: BAは「パラメータへの適応」、WFは「戦術効果への即応」
        /// </summary>
        private string GenerateWaterFlowSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine(k_NLISectionHeader);
            sb.AppendLine();
            sb.AppendLine("**Style: Water Flow** - Adapt every decision to both the current situation AND the effectiveness of previous tactics.");
            sb.AppendLine();
            sb.AppendLine("When in advantage (HP lead + Energy lead): use current tactics but shift slightly toward Aggressive.");
            sb.AppendLine("When in disadvantage: change approach completely. Switch BasicTactic and criteria that showed \"Weak Effect\".");
            sb.AppendLine("When evenly matched: use Adaptive BasicTactic with Recent Pattern Focus to find exploitable weaknesses.");
            sb.AppendLine("Key rule: never keep the same full strategy for more than 2 turns if results are not improving.");
            sb.AppendLine();
            return sb.ToString();
        }

        #endregion

        #region ユーティリティメソッド

        /// <summary>
        /// 指示タイプの説明を取得（UI表示用）
        /// </summary>
        /// <param name="instructionType">自然言語指示タイプ</param>
        /// <returns>日本語説明文</returns>
        public static string GetInstructionDescription(NaturalLanguageInstructionType instructionType)
        {
            return instructionType switch
            {
                NaturalLanguageInstructionType.None => "指示なし（標準動作）",

                // ルールベース系
                NaturalLanguageInstructionType.AggressiveFinisher => "攻撃的：早期決着を狙う",
                NaturalLanguageInstructionType.AggressiveDisruptor => "攻撃的：敵のリズムを崩す",
                NaturalLanguageInstructionType.DefensiveSurvivor => "防御的：生存を最優先",
                NaturalLanguageInstructionType.DefensiveCounter => "防御的：反撃機会を待つ",
                NaturalLanguageInstructionType.BalancedAdaptive => "バランス型：状況に適応",
                NaturalLanguageInstructionType.AnalyticalLearner => "分析型：敵パターンを学習",
                NaturalLanguageInstructionType.EnduranceManager => "持久型：エネルギー管理重視",

                // 自然言語系
                NaturalLanguageInstructionType.CorneredBeast => "追い詰められるほど攻撃的に",
                NaturalLanguageInstructionType.Finisher => "敵HPが減るほど攻撃的に",
                NaturalLanguageInstructionType.FrontRunner => "リード時は安全に",
                NaturalLanguageInstructionType.PatternBreaker => "予測不能に動く",
                NaturalLanguageInstructionType.MomentumRider => "流れに乗る/変える",
                NaturalLanguageInstructionType.StaminaManager => "エネルギー意識",
                NaturalLanguageInstructionType.CounterPuncher => "反撃重視",
                NaturalLanguageInstructionType.Berserker => "常時攻撃的",
                NaturalLanguageInstructionType.Tactician => "慎重・確実",
                NaturalLanguageInstructionType.WaterFlow => "状況適応（柔軟）",

                _ => "不明"
            };
        }

        /// <summary>
        /// 指示タイプの短縮名を取得（ログ用）
        /// </summary>
        /// <param name="instructionType">自然言語指示タイプ</param>
        /// <returns>短縮名</returns>
        public static string GetInstructionShortName(NaturalLanguageInstructionType instructionType)
        {
            return instructionType switch
            {
                NaturalLanguageInstructionType.None => "NONE",

                // ルールベース系
                NaturalLanguageInstructionType.AggressiveFinisher => "AGG_FIN",
                NaturalLanguageInstructionType.AggressiveDisruptor => "AGG_DIS",
                NaturalLanguageInstructionType.DefensiveSurvivor => "DEF_SRV",
                NaturalLanguageInstructionType.DefensiveCounter => "DEF_CNT",
                NaturalLanguageInstructionType.BalancedAdaptive => "BAL_ADP",
                NaturalLanguageInstructionType.AnalyticalLearner => "ANL_LRN",
                NaturalLanguageInstructionType.EnduranceManager => "END_MGR",

                // 自然言語系
                NaturalLanguageInstructionType.CorneredBeast => "CORNERED",
                NaturalLanguageInstructionType.Finisher => "FINISHER",
                NaturalLanguageInstructionType.FrontRunner => "FRONT_RUN",
                NaturalLanguageInstructionType.PatternBreaker => "PAT_BRK",
                NaturalLanguageInstructionType.MomentumRider => "MOMENTUM",
                NaturalLanguageInstructionType.StaminaManager => "STAMINA",
                NaturalLanguageInstructionType.CounterPuncher => "CNT_PUNCH",
                NaturalLanguageInstructionType.Berserker => "BERSERKER",
                NaturalLanguageInstructionType.Tactician => "TACTICIAN",
                NaturalLanguageInstructionType.WaterFlow => "WATER",

                _ => "UNK"
            };
        }

        /// <summary>
        /// 全ての指示タイプを取得（UI用）
        /// </summary>
        /// <returns>指示タイプの配列</returns>
        public static NaturalLanguageInstructionType[] GetAllInstructionTypes()
        {
            return (NaturalLanguageInstructionType[])Enum.GetValues(typeof(NaturalLanguageInstructionType));
        }

        /// <summary>
        /// None以外の指示タイプを取得（ランダム選択用）
        /// </summary>
        /// <returns>None以外の指示タイプの配列</returns>
        public static NaturalLanguageInstructionType[] GetActiveInstructionTypes()
        {
            var allTypes = GetAllInstructionTypes();
            var activeTypes = new NaturalLanguageInstructionType[allTypes.Length - 1];
            int index = 0;
            foreach (var type in allTypes)
            {
                if (type != NaturalLanguageInstructionType.None)
                {
                    activeTypes[index++] = type;
                }
            }
            return activeTypes;
        }

        /// <summary>
        /// 自然言語系の指示タイプのみを取得
        /// </summary>
        /// <returns>自然言語系の指示タイプの配列</returns>
        public static NaturalLanguageInstructionType[] GetNaturalLanguageTypes()
        {
            return new NaturalLanguageInstructionType[]
            {
                NaturalLanguageInstructionType.CorneredBeast,
                NaturalLanguageInstructionType.Finisher,
                NaturalLanguageInstructionType.FrontRunner,
                NaturalLanguageInstructionType.PatternBreaker,
                NaturalLanguageInstructionType.MomentumRider,
                NaturalLanguageInstructionType.StaminaManager,
                NaturalLanguageInstructionType.CounterPuncher,
                NaturalLanguageInstructionType.Berserker,
                NaturalLanguageInstructionType.Tactician,
                NaturalLanguageInstructionType.WaterFlow
            };
        }

        /// <summary>
        /// ルールベース系の指示タイプのみを取得
        /// </summary>
        /// <returns>ルールベース系の指示タイプの配列</returns>
        public static NaturalLanguageInstructionType[] GetRuleBasedTypes()
        {
            return new NaturalLanguageInstructionType[]
            {
                NaturalLanguageInstructionType.AggressiveFinisher,
                NaturalLanguageInstructionType.AggressiveDisruptor,
                NaturalLanguageInstructionType.DefensiveSurvivor,
                NaturalLanguageInstructionType.DefensiveCounter,
                NaturalLanguageInstructionType.BalancedAdaptive,
                NaturalLanguageInstructionType.AnalyticalLearner,
                NaturalLanguageInstructionType.EnduranceManager
            };
        }

        /// <summary>
        /// 指示タイプが自然言語系かどうかを判定
        /// </summary>
        /// <param name="instructionType">自然言語指示タイプ</param>
        /// <returns>自然言語系の場合true</returns>
        public static bool IsNaturalLanguageType(NaturalLanguageInstructionType instructionType)
        {
            return instructionType switch
            {
                NaturalLanguageInstructionType.CorneredBeast => true,
                NaturalLanguageInstructionType.Finisher => true,
                NaturalLanguageInstructionType.FrontRunner => true,
                NaturalLanguageInstructionType.PatternBreaker => true,
                NaturalLanguageInstructionType.MomentumRider => true,
                NaturalLanguageInstructionType.StaminaManager => true,
                NaturalLanguageInstructionType.CounterPuncher => true,
                NaturalLanguageInstructionType.Berserker => true,
                NaturalLanguageInstructionType.Tactician => true,
                NaturalLanguageInstructionType.WaterFlow => true,
                _ => false
            };
        }

        #endregion
    }
}