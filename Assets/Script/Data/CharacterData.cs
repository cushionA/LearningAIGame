using Newtonsoft.Json;
using System;
using System.Diagnostics;
using UnityEngine;
//==============================================ファイルヘッダ=========================================================
// CharacterData
// 
// 概要: キャラクターの体力・エネルギー管理とLLM報告用の基礎データクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// [プロパティ]
// - Hp / MaxHp: 現在体力と最大体力
// - Energy / MaxEnergy: 現在エネルギーと最大エネルギー
// - IsEnergyExhaust: エネルギー枯渇状態（JsonIgnore）
// - IsDead: 死亡判定（JsonIgnore）
// 
// [メソッド]
// - TakeDamage(int): ダメージを受けてHPを減少
// - ConsumeEnergy(int): エネルギーを消費、0以下で枯渇状態に
// - RecoverEnergyByRate(float): 割合でエネルギー回復、最大値到達で枯渇解除
// 
// 入力元クラス: StateSystem
// 出力先クラス: LLMシステム（JSON形式でシリアライズ）
// 
// その他:
// JsonIgnore属性により、内部判定用プロパティはLLM出力から除外
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Data
{
    /// <summary>
    /// キャラクターの基礎データ
    /// LLMの判断で使用する情報をまとめる
    /// </summary>
    public class CharacterData
    {
        /// <summary>
        /// 現在の体力
        /// </summary>
        public int Hp { get; set; }

        /// <summary>
        /// 最大体力
        /// </summary>
        public int MaxHp { get; set; }

        /// <summary>
        /// 現在のエネルギー
        /// </summary>
        public float Energy { get; set; }

        /// <summary>
        /// 最大エネルギー
        /// </summary>
        public float MaxEnergy { get; set; }

        /// <summary>
        /// エネルギー切れかどうかを返すプロパティ
        /// JsonIgnore属性を付与して、シリアライズ時に無視されるようにする
        /// </summary>
        [JsonIgnore]
        public bool IsEnergyExhaust { get; private set; }

        /// <summary>
        /// 死んでいるかを返すプロパティ
        /// 真なら死亡
        /// </summary>
        [JsonIgnore]
        public bool IsDead { get { return Hp <= 0; } }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public CharacterData()
        {

        }

        /// <summary>
        /// 最大体力と最大エネルギーを指定するコンストラクタ
        /// </summary>
        /// <param name="maxHp"></param>
        /// <param name="maxEnergy"></param>
        public CharacterData(int maxHp, int maxEnergy)
        {
            MaxHp = maxHp;
            Hp = maxHp;
            MaxEnergy = maxEnergy;
            Energy = maxEnergy;
        }

        /// <summary>
        /// ダメージを受ける
        /// </summary>
        public void TakeDamage(int amount)
        {
            Hp = Math.Max(0, Hp - amount);
        }

        /// <summary>
        /// エネルギーを消費する
        /// </summary>
        public void ConsumeEnergy(int amount)
        {
            Energy = Math.Max(0, Energy - amount);
            if (Energy <= 0)
            {
                IsEnergyExhaust = true;
            }
        }

        /// <summary>
        /// 割合でエネルギーを回復する
        /// </summary>
        public void RecoverEnergyByRate(float ratio)
        {
            float recoverAmount = MaxEnergy * ratio * 0.01f;
            Energy = Math.Min(MaxEnergy, Energy + recoverAmount);

            // UnityEngine.Debug.Log($"エネルギー回復: ratio={ratio}, recoverAmount={recoverAmount},Max:{MaxEnergy}");

            // エネルギー枯渇時、最大まで回復すれば枯渇解除
            if (Energy >= MaxEnergy)
            {
                Energy = MaxEnergy;
                IsEnergyExhaust = false;
            }
        }
    }
}