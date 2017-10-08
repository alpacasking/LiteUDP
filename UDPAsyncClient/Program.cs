using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UDPAsyncClient
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 30019);
            var client = new UDPAsyncClient(1024);
            client.Connect(serverEndPoint);
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
