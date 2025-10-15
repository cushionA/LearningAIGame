//==============================================ファイルヘッダ=========================================================
// FixedLengthList
// 
// 概要: 指定した個数だけ直近のデータを保持する固定長リスト（リングバッファ）
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// [FixedLengthList<T>]
// - 容量を超えた場合、最も古いデータから上書きされるリングバッファ方式
// - ジェネリック型でどんなデータ型でも格納可能
// - 動的配列のようなメモリ再確保が不要で、パフォーマンスが安定
// 
// 主要メソッド:
// - Add(T item): 要素を追加（容量超過時は最古データを上書き）
// - GetInOrder(): 追加順に並んだ要素を配列で取得
// - GetSpan(): 現在の内部配列をSpanで取得（高速だが順序は保証されない）
// - Clear(): リストをクリア
// - this[int index]: インデックスアクセス（古い順に0から）
// 
// プロパティ:
// - Count: 現在格納されている要素数
// - Capacity: 最大容量
// - IsFull: 容量いっぱいか
// 
// 使用例:
// var log = new FixedLengthList<string>(5);
// log.Add("A"); log.Add("B"); log.Add("C");
// foreach(var item in log.GetInOrder()) { Debug.Log(item); } // A, B, C
// 
// 入力元クラス: CombatSystem各種
// 出力先クラス: UIシステム、デバッグシステム
// 
// その他:
// - GetSpan()は高速だが要素の順序は保証されない（内部配列をそのまま返す）
// - GetInOrder()は要素を追加順に並べ替えるため少しコストがかかる
// - ログ記録、履歴管理、移動平均計算などに適している
//=====================================================================================================================

using System;
using UnityEngine;

namespace LearningAIGame.CombatSystem.Utilities
{
    /// <summary>
    /// 指定した個数だけ直近のログを保持する固定長リスト
    /// 容量を超えた場合は古いデータから上書きされる(リングバッファ方式)
    /// </summary>
    public class FixedLengthList<T>
    {
        // 実際のデータを格納する配列
        private readonly T[] _items;

        // 次に書き込む位置(リングバッファの書き込みヘッド)
        private int _head;

        // 現在格納されている要素数
        private int _count;

        /// <summary>
        /// 現在格納されている要素数を取得
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// リストの最大容量を取得
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// リストが容量いっぱいかどうか
        /// </summary>
        public bool IsFull => _count == Capacity;

        /// <summary>
        /// 固定長リストを初期化
        /// </summary>
        /// <param name="capacity">保持する最大要素数</param>
        public FixedLengthList(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentException("容量は1以上である必要があります", nameof(capacity));
            }

            Capacity = capacity;
            _items = new T[capacity];
            _head = 0;
            _count = 0;
        }

        /// <summary>
        /// 固定長リストを初期化
        /// </summary>
        /// <param name="source">保持する最大要素数</param>
        public FixedLengthList(T[] source)
        {
            if (source.Length <= 0)
            {
                throw new ArgumentException("容量は1以上である必要があります", nameof(source));
            }

            Capacity = source.Length;
            _items = source;
            _head = 0;
            _count = source.Length;
        }

        /// <summary>
        /// リストに要素を追加
        /// 容量を超えた場合は最も古い要素を上書き
        /// </summary>
        /// <param name="item">追加する要素</param>
        public void Add(T item)
        {
            _items[_head] = item;
            _head = (_head + 1) % Capacity;

            // まだ容量いっぱいでなければカウントを増やす
            if (_count < Capacity)
            {
                _count++;
            }
        }

        /// <summary>
        /// 指定したインデックスの要素を取得
        /// インデックス0が最も古い要素、Count-1が最新の要素
        /// </summary>
        /// <param name="index">要素のインデックス（0からCount-1）</param>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                {
                    throw new IndexOutOfRangeException($"インデックスが範囲外です: {index} (有効範囲: 0-{_count - 1})");
                }

                // リングバッファの開始位置を計算
                int startIndex = IsFull ? _head : 0;
                int actualIndex = (startIndex + index) % Capacity;
                return _items[actualIndex];
            }
        }

        /// <summary>
        /// 追加順に並んだ要素を配列で取得
        /// 古い要素から新しい要素の順に並ぶ
        /// </summary>
        /// <returns>追加順に並んだ要素の配列</returns>
        public T[] GetInOrder()
        {
            if (_count == 0)
            {
                return Array.Empty<T>();
            }

            T[] result = new T[_count];

            if (IsFull)
            {
                // 容量いっぱいの場合: _headから末尾、次に先頭から_head直前まで
                int firstPartLength = Capacity - _head;
                Array.Copy(_items, _head, result, 0, firstPartLength);
                Array.Copy(_items, 0, result, firstPartLength, _head);
            }
            else
            {
                // まだ容量いっぱいでない場合: 先頭から_countまで
                Array.Copy(_items, 0, result, 0, _count);
            }

            return result;
        }

        /// <summary>
        /// 現在格納されている要素をSpanとして取得
        /// 注意: リングバッファのため、要素の順序は追加順とは限らない
        /// 高速アクセスが必要で順序が重要でない場合に使用
        /// </summary>
        /// <returns>格納されている要素のSpan</returns>
        public Span<T> AsSpan()
        {
            return _items.AsSpan(0, _count);
        }

        /// <summary>
        /// 最新の要素を取得
        /// </summary>
        /// <returns>最新の要素</returns>
        public T GetLatest()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("リストが空です");
            }

            int latestIndex = (_head - 1 + Capacity) % Capacity;
            return _items[latestIndex];
        }

        /// <summary>
        /// 最も古い要素を取得
        /// </summary>
        /// <returns>最も古い要素</returns>
        public T GetOldest()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("リストが空です");
            }

            int oldestIndex = IsFull ? _head : 0;
            return _items[oldestIndex];
        }

        /// <summary>
        /// リストをクリアして初期状態に戻す
        /// </summary>
        public void Clear()
        {
            Array.Clear(_items, 0, _items.Length);
            _head = 0;
            _count = 0;
        }
    }
}