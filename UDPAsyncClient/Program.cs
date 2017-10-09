using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace UDPAsyncClient
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 30009);
            var client = new UDPAsyncClient(1024);
            client.RecvDataHandler = (data) => {
                Console.WriteLine(Encoding.UTF8.GetString(data));
            };
            client.Connect(serverEndPoint);

            Thread workThread = new Thread(() => {
				while (true)
				{
					client.Update();
				}
			});
			workThread.Start();

			Console.WriteLine("Enter the message for server");

			string request;
			while(true)
			{
				Console.Write("> ");
				request = Console.ReadLine();
                if(request == "exit"){
                    break;
                }
                byte[] data = Encoding.UTF8.GetBytes(request);
                client.Send(data);
			} 
        }
    }
}
