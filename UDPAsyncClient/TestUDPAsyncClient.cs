using System;
using System.Text;
using System.Threading;

namespace UDPAsyncClient
{
    public class TestUDPAsyncClient : UDPAsyncClient
    {
        public TestUDPAsyncClient(int bufferSize) : base(bufferSize)
        {
        }

        public void Send(string data){
            base.Send(Encoding.UTF8.GetBytes(data));
        }

        public void SendLoop(string data){
            Thread tempThread = new Thread(() =>
            {
                while(true){
                    Thread.Sleep(10);
                    Send(data);
                }
            });
            tempThread.Start();
        }
    }
}
