﻿using System;

namespace ActorModel
{


    /// <summary>
    /// Can Be Interleaved when Multi Call Chain Deadlock
    /// 当多条调用链死锁的时候可以被交错
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class InterleaveWhenDeadlock : Attribute { }


}
