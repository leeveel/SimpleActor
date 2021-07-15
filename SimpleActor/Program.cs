using Test;
using System;

namespace SimpleActor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("-----------------start----------------------");

            var t1 = new Test001();
            _ = t1.Test();

            Console.ReadLine();
        }
    }
}
