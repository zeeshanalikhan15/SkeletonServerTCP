using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace TestServer
{
    class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var logger = LogManager.GetLogger("Program");
                logger.Info("Starting Server");
                var server = Server.GetInstance();
                server.Start();

                logger.Info("Press Any key to terminate");
                Console.ReadKey();
                server.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}
