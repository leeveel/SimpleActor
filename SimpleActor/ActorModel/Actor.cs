using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ActorModel
{
    public class Actor
    {
        public const int TIME_OUT = 3000;
        readonly ActionBlock<WorkWrapper> actionBlock;

        public static object Lockable = new object();
        /// <summary>
        /// Actor当前正在执行的调用链
        /// callchain id of actor current executing
        /// </summary>
        public long curCallChainId;   
        /// <summary>
        /// key:调用链 ---  value:正在等待的Actor
        /// key:callchainId ---- value:Actor of curreent callChain is waiting for
        /// </summary>
        public static ConcurrentDictionary<long, Actor> waitingMap = new ConcurrentDictionary<long, Actor>();

        private static long idCounter=1;
        private static long actorIdCounter = 0;
        public long ActorId { get; }
        public Actor()
        {
            ActorId = Interlocked.Increment(ref actorIdCounter);
            var ops = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = 1,
            };
            actionBlock = new ActionBlock<WorkWrapper>(InnerRun, ops);
        }

        static async Task InnerRun(WorkWrapper wrapper)
        {
            var task = wrapper.DoTask();
            var res = await task.WaitAsync(TimeSpan.FromMilliseconds(wrapper.TimeOut));
            if (res)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("wrapper time out:" + wrapper.GetTrace());
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Read(); //only for test (stop application when timeout)
                wrapper.ForceSetResult();
            }
        }

        private void IsNeedEqueue(bool allowCallChainReentrancy, out bool needEqueue, out long callChainId)
        {
            lock (Lockable)
            {
                //needEqueue = false;
                callChainId = RuntimeContext.Current;
                if (!allowCallChainReentrancy)
                {
                    callChainId = Interlocked.Increment(ref idCounter);
                    needEqueue = true;
                    return;
                }

                if (callChainId <= 0)
                {
                    callChainId = Interlocked.Increment(ref idCounter);
                    needEqueue = true;
                }
                else if (callChainId == curCallChainId)
                {
                    needEqueue = false;
                    return;
                }

                if (curCallChainId > 0)
                {
                    waitingMap.TryGetValue(curCallChainId, out var waiting);
                    //Console.WriteLine($"curCallChainId:{curCallChainId} waitingCallChainId:{waiting?.curCallChainId}");
                    if (waiting != null && waiting.curCallChainId == callChainId)
                    {
                        needEqueue = false;
                    }
                    else
                    {
                        waitingMap[callChainId] = this;
                        needEqueue = true;
                    }
                }
                else
                {
                    needEqueue = true;
                }
            }
        }

        public Task SendAsync(Action work, bool allowCallChainReentrancy = true, int timeOut = TIME_OUT)
        {
            IsNeedEqueue(allowCallChainReentrancy, out bool needEqueue, out long callChainId);
            if (needEqueue)
            {
                ActionWrapper at = new ActionWrapper(work);
                at.Owner = this;
                at.TimeOut = timeOut;
                at.CallChainId = callChainId;
                actionBlock.SendAsync(at);
                return at.Tcs.Task;
            }
            else
            {
                Console.WriteLine("Directly execute");
                work();
                return Task.CompletedTask;
            }
        }

        public Task<T> SendAsync<T>(Func<T> work, bool allowCallChainReentrancy = true, int timeOut = TIME_OUT)
        {
            IsNeedEqueue(allowCallChainReentrancy, out bool needEqueue, out long callChainId);
            if (needEqueue)
            {
                FuncWrapper<T> at = new FuncWrapper<T>(work);
                at.Owner = this;
                at.TimeOut = timeOut;
                at.CallChainId = callChainId;
                actionBlock.SendAsync(at);
                return at.Tcs.Task;
            }
            else
            {
                Console.WriteLine("Directly execute");
                return Task.FromResult(work());
            }
        }

        public Task SendAsync(Func<Task> work, bool allowCallChainReentrancy = true, int timeOut = TIME_OUT)
        {
            IsNeedEqueue(allowCallChainReentrancy, out bool needEqueue, out long callChainId);
            if (needEqueue)
            {
                ActionAsyncWrapper at = new ActionAsyncWrapper(work);
                at.Owner = this;
                at.TimeOut = timeOut;
                at.CallChainId = callChainId;
                actionBlock.SendAsync(at);
                return at.Tcs.Task;
            }
            else
            {
                Console.WriteLine("Directly execute");
                return work();
            }
        }

        public Task<T> SendAsync<T>(Func<Task<T>> work, bool allowCallChainReentrancy = true, int timeOut = TIME_OUT)
        {
            IsNeedEqueue(allowCallChainReentrancy, out bool needEqueue, out long callChainId);
            if (needEqueue)
            {
                FuncAsyncWrapper<T> at = new FuncAsyncWrapper<T>(work);
                at.Owner = this;
                at.TimeOut = timeOut;
                at.CallChainId = callChainId;
                actionBlock.SendAsync(at);
                return at.Tcs.Task;
            }
            else
            {
                Console.WriteLine("Directly execute");
                return work();
            }
        }

    }
}
