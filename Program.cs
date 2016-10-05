using System;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var dispatcher = new SerialDispather();
            dispatcher.Start();

            dispatcher.Sync(() => {
                Console.WriteLine("sync: 0");
            });

            dispatcher.Sync(() => {
                Console.WriteLine("sync: 1");
            });

            dispatcher.Sync(() => {
                Console.WriteLine("sync: 2");
                dispatcher.Sync(() => {
                    Console.WriteLine("sync: 2-1");
                    dispatcher.Sync(() => {
                        Console.WriteLine("sync: 2-1-1");
                    });
                });
            });

            dispatcher.Async(func);

            Console.WriteLine("end of sync");
                
            dispatcher.Async(() => {
                Console.WriteLine("async: first");
                dispatcher.Sync(() => {
                    Console.WriteLine("sync in async");
                    dispatcher.Async(() => {
                        Console.WriteLine("async in sync in async");
                        dispatcher.Async(() => {
                            Console.WriteLine("async in async in sync in async");
                        });
                    });
                });
            });

            int x = 0;
            dispatcher.Async(() => {
                Console.WriteLine("async: x:=100");
                x = 100;
            });
            
            dispatcher.Async(() => {
                Console.WriteLine(String.Format("async: x={0}", x));
            });
            
            dispatcher.Async(() => {
                Console.WriteLine("async: last");
            });
            
            Console.WriteLine("end of main()");

            // Wait for action "async in async in sync in async" is registered
            System.Threading.Thread.Sleep(100);
            dispatcher.Stop();
        }

        private static void func()
        {
            Console.WriteLine("func()");
        }
    }
}
