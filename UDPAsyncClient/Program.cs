using System;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text;

namespace UDPAsyncClient
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 30009);
            var client = new TestUDPAsyncClient(1024);
            client.RecvDataHandler = (data) => {
                Console.WriteLine(Encoding.UTF8.GetString(data));
            };
            //client.Connect(serverEndPoint);

            Thread workThread = new Thread(() => {
				while (true)
				{
                    Thread.Sleep(0);
					client.Update();
				}
			});
			workThread.Start();
			string sendPattern = @"send ?";
            string sendLoopPattern = @"loop ?";
			Console.WriteLine("Enter the message for server");
            string input;
			while(true)
			{
				input = Console.ReadLine();
                if(input == "exit"){
                    break;
                }
                else if (input == "connect")
                {
                    client.Connect(serverEndPoint);
                }
                else if(Regex.IsMatch(input,sendPattern)){
                    string[] splitInput = input.Split(' ');
                    string stringData = string.Empty;
                    for (int i = 1; i < splitInput.Length;i++){
                        stringData += splitInput[i];
                    }
					client.Send(stringData);
                }
				else if (Regex.IsMatch(input, sendLoopPattern))
				{
					string[] splitInput = input.Split(' ');
					string stringData = string.Empty;
					for (int i = 1; i < splitInput.Length; i++)
					{
						stringData += splitInput[i];
					}
					client.SendLoop(stringData);
				}
			
			}

        }
    }
}
