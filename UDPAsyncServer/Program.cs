using System;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace UDPAsyncServer
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            IPAddress serverIP = IPAddress.Parse("127.0.0.1");
            int port = 30009;
            var server = new TestUDPAsyncServer(serverIP, port, 1024, 2000);
            //server.Start();
            Thread workThread = new Thread(() => {
                while (true)
				{
                    server.Update();
				}
			});
            workThread.Start();
			string input;
            while(true)
            {
                input = Console.ReadLine();
                if (input == "start")
                {
                    server.Start();
                }
                else if (input == "restart")
                {
                    server.ReStart();
                }
                else if (input == "stop")
                {
                    server.Stop();
                }
                else if (input == "exit")
                {
                    break;
                }
            }
		}
    }
}
