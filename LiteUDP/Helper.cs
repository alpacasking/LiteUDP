using System;

namespace LiteUDP
{
    public class Helper
    {
		private static readonly DateTime utc_time = new DateTime(1970, 1, 1);

		public static UInt32 iclock()
		{
			return (UInt32)(Convert.ToInt64(DateTime.UtcNow.Subtract(utc_time).TotalMilliseconds) & 0xffffffff);
		}

		//because data on Ethernet need to be bigger than 46 byte,and
		//ip header = 20 bytes
		//udp header = 8 bytes
		//so we need the handshake data to be bigger than 46-20-8=18bytes
		public static byte[] HandshakeHeadData = {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF ,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF ,0xFF, 0xFF};

		public static bool IsHandshakeDataRight(byte[] buffer, int offset, int size)
		{
            if (size < HandshakeHeadData.Length)
			{
				return false;
			}
			for (int i = 0; i < HandshakeHeadData.Length; i++)
			{
				if (buffer[offset + i] != HandshakeHeadData[i])
				{
					return false;
				}
			}
			return true;
		}
    }
}
