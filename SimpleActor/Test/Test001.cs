using ActorModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    public class Test001
    {

        private volatile int id = 0;
        public async Task Test()
        {

            var a = new Actor();
            var b = new Actor();
            var c = new Actor();
            var d = new Actor();

            /**********************/
            /*  single path a->b->a  */
            /*********************/

            //for (int i = 0; i < 1000; i++)
            //{
            //    _ = a.SendAsync(async () =>
            //    {
            //        id++;
            //        Console.WriteLine("method a " + id);
            //        await Task.Delay(1);
            //        await b.SendAsync(async () =>
            //        {
            //            id++;
            //            Console.WriteLine("method b " + id);
            //            await Task.Delay(1);
            //            await a.SendAsync(() =>
            //            {
            //                id++;
            //                Console.WriteLine("method a back " + id);
            //            });
            //        });
            //    });
            //}

            /**********************/
            /* two path a->b; b->a  */
            /*********************/
            //for (int j = 0; j < 1000; j++)
            //{
            //    for (int i = 0; i < 100; i++)
            //    {
            //        _ = a.SendAsync(async () =>
            //        {
            //            id = Interlocked.Increment(ref id);
            //            Console.WriteLine("1 method a " + id);
            //            await Task.Delay(1);
            //            await b.SendAsync(() =>
            //            {
            //                id = Interlocked.Increment(ref id);
            //                Console.WriteLine("1 method b " + id);
            //            });
            //        }, true); // forceEnqueue: no difference whether you pass it or not when at the beginning of this call chain.

            //        _ = b.SendAsync(async () =>
            //        {
            //            id = Interlocked.Increment(ref id);
            //            Console.WriteLine("2 method b " + id);
            //            await Task.Delay(1);
            //            await a.SendAsync(() =>
            //            {
            //                id = Interlocked.Increment(ref id);
            //                Console.WriteLine("2 method a " + id);
            //            });
            //        });
            //    }
            //    await Task.Delay(5000);
            //    Console.WriteLine("--------------------------------------------------:" + j);
            //}

            /****************************/
            /* multipath: b->c; b->a; a->b  */
            /***************************/

            var random = new Random();
            for (int j = 0; j < 1000000; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    _ = b.SendAsync(async () =>
                    {
                        Console.WriteLine("1---method b");
                        await Task.Delay(random.Next(1, 10));
                        await c.SendAsync(async () =>
                        {
                            Console.WriteLine("1---method c");
                            await Task.Delay(random.Next(10, 30));
                            Console.WriteLine("1---method c end");
                        });
                    }); //1 // forceEnqueue: no difference whether you pass it or not when at the beginning of this call chain.

                    _ = b.SendAsync(async () =>
                    {
                        Console.WriteLine("2---method b");
                        await Task.Delay(random.Next(1, 10));
                        await a.SendAsync(async () =>
                        {
                            Console.WriteLine("2---method a");
                            await Task.Delay(random.Next(10, 30));
                        });
                    });//2

                    await Task.Delay(10);
                    _ = a.SendAsync(async () =>
                    {
                        Console.WriteLine("3---method a");
                        await b.SendAsync(async () =>
                        {
                            Console.WriteLine("3---method b");
                            await Task.Delay(random.Next(1, 10));
                        });
                    });//3
                }

                await Task.Delay(1500);
                Console.WriteLine("--------------------------------------------------:" + j);
            }


        }

    }
}
