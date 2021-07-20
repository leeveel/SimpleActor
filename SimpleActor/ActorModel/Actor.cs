using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ActorModel
{

    public class Actor
    {

        public const int TIME_OUT = 10000;
        readonly ActionBlock<WorkWrapper> actionBlock;

        public static object Lockable = new object();
        /// <summary>
        /// Actor当前正在执行的调用链
        /// callchain id of actor current executing
        /// </summary>
        public long curCallChainId;
        public volatile bool CurCanBeInterleaved;
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
                Environment.Exit(0); //only for test (stop application when timeout)
                wrapper.ForceSetResult();
            }
        }

        private void IsNeedEnqueue(MethodInfo method, out bool needEnqueue, out long callChainId)
        {
            //same call chain must be sigle thread
            //multipath (callChainId == curCallChainId) not equal absolutly
            // so no need lock
            callChainId = RuntimeContext.Current;
            if (callChainId <= 0)
            {
                callChainId = Interlocked.Increment(ref idCounter);
                needEnqueue = true;
                return;
            }
            else if (callChainId == curCallChainId)
            {
                needEnqueue = false;
                return;
            }
            needEnqueue = true;
            long curChainId = Volatile.Read(ref curCallChainId);
            if (curChainId > 0)
            {
                lock (Lockable)
                {
                    waitingMap.TryGetValue(curChainId, out var waiting);
                    //Console.WriteLine($"curCallChainId:{curCallChainId} waitingCallChainId:{waiting?.curCallChainId}");
                    if (waiting != null && Volatile.Read(ref waiting.curCallChainId) == callChainId)
                    {
                        if (CurCanBeInterleaved)
                            needEnqueue = false;
                        else
                            throw new DeadlockException("multi call chain dead lock");
                    }
                    else
                    {
                        waitingMap[callChainId] = this;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="work"></param>
        /// <param name="forceEnqueue">
        /// means user not wait the result of Task (such as: _ = actor.SendAsync)
        /// no difference whether you pass it or not when at the beginning of this call chain.
        /// </param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public Task SendAsync(Action work, bool forceEnqueue = false, int timeOut = TIME_OUT)
        {
            long callChainId;
            bool needEnqueue;
            if (forceEnqueue)
            {
                callChainId = Interlocked.Increment(ref idCounter);
                needEnqueue = true;
            }
            else
            {
                IsNeedEnqueue(work.Method, out needEnqueue, out callChainId);
            }
            if (needEnqueue)
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

        public Task<T> SendAsync<T>(Func<T> work, bool forceEnqueue = false, int timeOut = TIME_OUT)
        {
            long callChainId;
            bool needEnqueue;
            if (forceEnqueue)
            {
                callChainId = Interlocked.Increment(ref idCounter);
                needEnqueue = true;
            }
            else
            {
                IsNeedEnqueue(work.Method, out needEnqueue, out callChainId);
            }
            if (needEnqueue)
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

        public Task SendAsync(Func<Task> work, bool forceEnqueue = false, int timeOut = TIME_OUT)
        {
            long callChainId;
            bool needEnqueue;
            if (forceEnqueue)
            {
                callChainId = Interlocked.Increment(ref idCounter);
                needEnqueue = true;
            }
            else
            {
                IsNeedEnqueue(work.Method, out needEnqueue, out callChainId);
            }
            if (needEnqueue)
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

        public Task<T> SendAsync<T>(Func<Task<T>> work, bool forceEnqueue = false, int timeOut = TIME_OUT)
        {
            long callChainId;
            bool needEnqueue;
            if (forceEnqueue)
            {
                callChainId = Interlocked.Increment(ref idCounter);
                needEnqueue = true;
            }
            else
            {
                IsNeedEnqueue(work.Method, out needEnqueue, out callChainId);
            }
            if (needEnqueue)
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
