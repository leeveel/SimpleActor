using System;
using System.Threading.Tasks;

namespace ActorModel
{
    public abstract class WorkWrapper
    {
        public Actor Owner { get; set; }
        public int TimeOut { get; set; }
        public abstract Task DoTask();
        public abstract string GetTrace();
        public abstract void ForceSetResult();
        public long CallChainId { get; set; }
        
        protected void SetContext()
        {
            lock (Actor.Lockable)
            {
                RuntimeContext.SetContext(CallChainId);
                Owner.curCallChainId = CallChainId;
            }
        }

        public void ResetContext()
        {
            Actor.waitingMap.TryRemove(CallChainId, out _);
            //no need reset context, just make sure setcontext before execute workitem 
            //lock (Owner.lockObj)
            //{
            //    DataFlowActor.waitingMap.TryRemove(CallChainId, out _);
            //    Owner.curCallChainId = 0;
            //    RuntimeContext.ResetContext();
            //}
        }
    }

    public class ActionWrapper : WorkWrapper
    {
        public Action Work { private set; get; }
        public TaskCompletionSource<bool> Tcs { private set; get; }

        public ActionWrapper(Action work)
        {
            this.Work = work;
            Tcs = new TaskCompletionSource<bool>();
        }

        public override Task DoTask()
        {
            try
            {
                SetContext();
                Work();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Tcs.TrySetResult(true);
                ResetContext();
            }
            return Task.CompletedTask;
        }

        public override string GetTrace()
        {
            return this.Work.Target + "|" + Work.Method.Name;
        }

        public override void ForceSetResult()
        {
            Tcs.TrySetResult(false);
            ResetContext();
        }
    }

    public class FuncWrapper<T> : WorkWrapper
    {
        public Func<T> Work { private set; get; }
        public TaskCompletionSource<T> Tcs { private set; get; }

        public FuncWrapper(Func<T> work)
        {
            this.Work = work;
            this.Tcs = new TaskCompletionSource<T>();
        }

        public override Task DoTask()
        {
            T ret = default;
            try
            {
                SetContext();
                ret = Work();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Tcs.TrySetResult(ret);
                ResetContext();
            }
            return Task.CompletedTask;
        }

        public override string GetTrace()
        {
            return this.Work.Target + "|" + Work.Method.Name;
        }

        public override void ForceSetResult()
        {
            Tcs.TrySetResult(default);
            ResetContext();
        }
    }

    public class ActionAsyncWrapper : WorkWrapper
    {
        public Func<Task> Work { private set; get; }
        public TaskCompletionSource<bool> Tcs { private set; get; }

        public ActionAsyncWrapper(Func<Task> work)
        {
            this.Work = work;
            Tcs = new TaskCompletionSource<bool>();
        }

        public override async Task DoTask()
        {
            try
            {
                SetContext();
                await Work();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Tcs.TrySetResult(true);
                ResetContext();
            }
        }

        public override string GetTrace()
        {
            return this.Work.Target + "|" + Work.Method.Name;
        }

        public override void ForceSetResult()
        {
            Tcs.TrySetResult(false);
            ResetContext();
        }
    }

    public class FuncAsyncWrapper<T> : WorkWrapper
    {
        public Func<Task<T>> Work { private set; get; }
        public TaskCompletionSource<T> Tcs { private set; get; }

        public FuncAsyncWrapper(Func<Task<T>> work)
        {
            this.Work = work;
            this.Tcs = new TaskCompletionSource<T>();
        }

        public override async Task DoTask()
        {
            T ret = default;
            try
            {
                SetContext();
                ret = await Work();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Tcs.TrySetResult(ret);
                ResetContext();
            }
        }

        public override string GetTrace()
        {
            return this.Work.Target + "|" + Work.Method.Name;
        }

        public override void ForceSetResult()
        {
            Tcs.TrySetResult(default);
            ResetContext();
        }
    }
}
