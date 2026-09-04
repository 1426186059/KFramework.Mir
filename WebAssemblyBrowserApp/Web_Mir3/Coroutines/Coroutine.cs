using System;
using System.Collections;
using System.Diagnostics;

namespace Coroutines
{
    /// <summary>
    /// 一个正在运行的协程句柄（等价于 Unity 的 Coroutine）。
    /// 由 CoroutineManager 每帧推进一格（一次 MoveNext），与 Unity 的协程调度一致：
    /// 每帧每个协程最多前进一个 yield 边界，下一帧才会继续，因此长任务不会冻结主线程。
    /// 可对以下 yield 值作出响应：
    ///   - null / WaitForEndOfFrame / WaitForFixedUpdate : 下一帧继续
    ///   - WaitForSeconds(s)                              : 约 s 秒后继续
    ///   - WaitWhile(pred) / WaitUntil(pred)             : 条件满足前每帧轮询
    ///   - 另一个 Coroutine                              : 等待该协程完成后继续（链式，Unity 经典写法）
    /// </summary>
    public sealed class Coroutine
    {
        private readonly IEnumerator _enumerator;
        private object _pending;     // 当前挂起的 yield 值（null / YieldInstruction / 另一个 Coroutine）
        private long _resumeTicks;   // WaitForSeconds 的恢复时间戳

        public bool IsDone { get; internal set; }

        internal IEnumerator Routine => _enumerator;

        internal Coroutine(IEnumerator enumerator)
        {
            _enumerator = enumerator;
            _pending = null;
            IsDone = false;
        }

        /// <summary>当前挂起的指令是否已就绪，可以前进一格。</summary>
        internal bool IsReady(long nowTicks)
        {
            object p = _pending;
            if (p == null) return true;                              // yield return null -> 下一帧
            if (p is WaitForEndOfFrame) return true;                // 下一帧
            if (p is WaitForFixedUpdate) return true;               // 下一帧
            if (p is WaitForSeconds) return nowTicks >= _resumeTicks;
            if (p is WaitWhile ww) return !ww.Predicate();
            if (p is WaitUntil wu) return wu.Predicate();
            if (p is Coroutine child) return child.IsDone;          // 链式等待
            return true;                                            // 未知 yield -> 当作 null
        }

        /// <summary>前进一格（一次 MoveNext）。返回 false 表示协程已结束。</summary>
        internal bool Advance(long nowTicks)
        {
            if (IsDone) return false;
            if (!_enumerator.MoveNext()) { IsDone = true; return false; }
            object cur = _enumerator.Current;
            _pending = cur;
            if (cur is WaitForSeconds wfs)
                _resumeTicks = nowTicks + (long)(wfs.Seconds * Stopwatch.Frequency);
            return true;
        }
    }
}
