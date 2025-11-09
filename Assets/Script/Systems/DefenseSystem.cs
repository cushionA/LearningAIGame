using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// DefenseSystem
// 
// 概要: キャラクターの防御行動を管理するシステムクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// ガードの構え方向変更、ブロッキングの実行を行い、
// 防御状態の変化をStateSystemに通知する。
// 
// 入力元クラス:BattleCharacterController
// 出力先クラス:StateSystem
// 
// その他:
// BaseSystem<DefenseReportInfo>を継承
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Systems
{
    public class DefenseSystem : BaseSystem<DefenseReportInfo>
    {
        /// <summary>
        /// 防御状態変更の報告用のデータ
        /// </summary>
        private DefenseReportInfo _info;

        /// <summary>
        /// 構え方向を変化させるメソッド
        /// </summary>
        public void GuardStanceChange(StanceType stance)
        {
            _info.SetInfo(stance, DefenseReportType.StanceChange);
            NotifyObservers(_info);
        }

        /// <summary>
        /// ブロッキング実行メソッド
        /// </summary>
        public void BlockingStart(StanceType stance)
        {
            moveController.Stop();
            _info.SetInfo(stance, DefenseReportType.BlockingStart);
            NotifyObservers(_info);
        }

    }
}