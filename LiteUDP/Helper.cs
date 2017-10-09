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


		public static byte[] HandshakeHeadData = { 0, 0, 0, 0 };

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
