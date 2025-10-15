using LLMDataArchitect;
using UnityEngine;

namespace LLMDataArchitect.Test
{
    /// <summary>
    /// プロンプト生成クラス
    /// </summary>
    public abstract class PromptGeneratorBase
    {
        /// <summary>
        /// ランダムなテストデータからプロンプトを生成
        /// </summary>
        public abstract string GenerateRandomPrompt();

        /// <summary>
        /// 指定されたデータからプロンプトを生成
        /// </summary>
        public abstract string GeneratePromptByData(LLMInputData inputData);

        /// <summary>
        /// グラマーを生成する
        /// </summary>
        /// <returns></returns>
        public abstract string GenerateGrammar();
    }
}