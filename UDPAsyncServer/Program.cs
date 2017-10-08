using System;
using System.Net;
using System.Threading.Tasks;

namespace LiteUDP
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            IPAddress serverIP = IPAddress.Parse("127.0.0.1");
            int port = 30019;
            var server = new TestUDPAsyncServer(serverIP, port, 1024, 10);
            //server.Start();
			Task task = new Task(() => {
                while (true)
				{
                    server.Update();
				}
			});
            task.Start();
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
