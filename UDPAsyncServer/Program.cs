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
            var server = new TestUDPAsyncServer(serverIP, port, 1024, 10);
            //server.Start();
            Thread workThread = new Thread(() => {
                while (true)
				{
                    server.Update();
				}
			});
            workThread.Start();
			string input;
			do
			{
				Console.Write("> ");
				input = Console.ReadLine();
				switch (input)
				{
                    case "start":
                        server.Start();
                        break;
                    case "restart":
                        server.ReStart();
                        break;
					case "stop":           
						server.Stop();
						break;
				}
			} while (!string.Equals(input, "exit"));

		}
    }
}
