using System;
using System.Net;
using System.Text;

namespace UDPAsyncServer
{
    public class TestUDPAsyncServer:UDPAsyncServer
    {
        public TestUDPAsyncServer(IPAddress ip, int port, int bufferSize, int maxUserCount)
            :base(ip, port,bufferSize,maxUserCount)
        {
            RecvDataHandler = OnPacket;
        }

        public void OnPacket(byte[] data){
            Console.Write("OnPacket:"+Encoding.UTF8.GetString(data,0,data.Length)+"\n>");
            //Send(data);
        }
    }
}
