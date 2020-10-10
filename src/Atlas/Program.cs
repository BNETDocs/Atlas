using Atlas.Bot;

using System;
using System.Reflection;

namespace Atlas
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(DateTime.Now.ToString("ddd, dd MMM yyyy, h:mm tt")); // Mon, 23 Dec 2019, 4:32 AM

            Assembly version = Assembly.GetEntryAssembly();
            Console.WriteLine(version.GetName().Name + " v" + version.GetName().Version.ToString());

            Configuration.State = new Configuration();
            Configuration.State.Save();

            foreach (Bot.Instance instance in Configuration.State.Instances)
            {
                Console.WriteLine("[Instance] " + instance.Name);
            }

            while (true)
            {
                System.Threading.Thread.Yield();
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
