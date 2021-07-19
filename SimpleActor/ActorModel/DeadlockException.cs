using System;

namespace ActorModel
{
    public class DeadlockException : Exception
    {

        public DeadlockException() { }

        public DeadlockException(string msg) : base(msg)
        {
            
        }

    }
}
