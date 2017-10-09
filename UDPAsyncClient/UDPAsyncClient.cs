using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using LiteUDP;

namespace UDPAsyncClient
{
    public class UDPAsyncClient
    {
		Socket mUDPSocket = null;

		private int mBufferSize = 1024;
		private IPEndPoint mServerEndPoint;
        private SocketAsyncEventArgs mSendSAE;
        private SocketAsyncEventArgs mReceiveSAE;

        private ConcurrentQueue<byte[]> recvQueue = new ConcurrentQueue<byte[]>();
        public Action<byte[]> RecvDataHandler;

        private KCP mKcp;
        private bool mNeedUpdateFlag = false;
		private UInt32 mNextUpdateTime;

        private ClientStatus status = ClientStatus.Disconnected;
		private UInt32 mConnectStartTime;
		private UInt32 mLastSendConnectTime;

		private const UInt32 CONNECT_TIMEOUT = 5000;
		private const UInt32 RESEND_CONNECT = 500;

		public UDPAsyncClient(int bufferSize)
		{
			mUDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mUDPSocket.Bind(new IPEndPoint(IPAddress.Any,0));
            mBufferSize = bufferSize;
		}

        public void Connect(IPEndPoint serverEndPoint)
        {
            mServerEndPoint = serverEndPoint;

			mSendSAE = new SocketAsyncEventArgs();
            mSendSAE.RemoteEndPoint = mServerEndPoint;
            mSendSAE.SetBuffer(new Byte[mBufferSize], 0, mBufferSize);
			mSendSAE.Completed += IOCompleted;
			mReceiveSAE = new SocketAsyncEventArgs();
			mReceiveSAE.RemoteEndPoint = mServerEndPoint;
			mReceiveSAE.SetBuffer(new Byte[mBufferSize], 0, mBufferSize);
            mReceiveSAE.Completed += IOCompleted;

            mConnectStartTime = Helper.iclock();
			SendHandshake();

			if (!mUDPSocket.ReceiveFromAsync(mReceiveSAE))
				ProcessReceive(mReceiveSAE);
		}

		public void Close()
		{
			mUDPSocket.Close();
            status = ClientStatus.Disconnected;
		}

		public void RawSend(byte[] data,int size)
		{
            Buffer.BlockCopy(data, 0, mSendSAE.Buffer, 0, size);
			mSendSAE.SetBuffer(0, size);
			mUDPSocket.SendToAsync(mSendSAE);
		}

        public void Send(byte[] data)
        {
            mKcp.Send(data);
        }

		private void IOCompleted(object sender, SocketAsyncEventArgs args)
		{
			switch (args.LastOperation)
			{
				case SocketAsyncOperation.ReceiveFrom:
					ProcessReceive(args);
					break;
				case SocketAsyncOperation.SendTo:
					ProcessSend(args);
					break;
			}
		}

		private void ProcessReceive(SocketAsyncEventArgs args)
		{
            Console.WriteLine("Packet Receive:"+args.BytesTransferred+" bytes");
			switch (args.SocketError)
			{
				case SocketError.Success:
					if (args.BytesTransferred > 0)
					{
                        byte[] data = new byte[args.BytesTransferred];
                        Buffer.BlockCopy(args.Buffer,args.Offset,data,0,args.BytesTransferred);
                        recvQueue.Enqueue(data);
					}
                    break;
			}
            args.SetBuffer(0,args.Buffer.Length);
			if (!mUDPSocket.ReceiveFromAsync(args))
				ProcessReceive(args);
		}

		private void ProcessSend(SocketAsyncEventArgs args)
		{
			args.SetBuffer(0, args.Buffer.Length);
            Console.WriteLine("Packet Send:" + args.BytesTransferred + " bytes");
		}

        public virtual void Update()
        {
			var current = Helper.iclock();
			switch (status)
            {
                case ClientStatus.InConnect:
					if (IsConnectTimeout(current))
					{
                        status = ClientStatus.Disconnected;
                        Console.WriteLine("Connect Timeout");
						return;
					}
                    if (IsRehandshake(current))
					{
						mLastSendConnectTime = current;
                        SendHandshake();
					}
                    ProcessConnectQueue();
					break;
                case ClientStatus.Connected:
                    ProcessRecvQueue();
                    if (mKcp == null)
                    {
                        return;
                    }
                    if (mNeedUpdateFlag || current >= mNextUpdateTime)
                    {
                        mKcp.Update(current);
                        mNextUpdateTime = mKcp.Check(current);
                        mNeedUpdateFlag = false;
                    }
                    break;
                case ClientStatus.Disconnected:
                    break;
            }
		}

		private void ProcessConnectQueue()
		{
            if (!recvQueue.IsEmpty)
			{
				byte[] data = null;
				bool isSuccess = recvQueue.TryDequeue(out data);
				if (!isSuccess || data == null)
				{
					return;
				}

				if (Helper.IsHandshakeDataRight(data, 0, data.Length))
				{
					UInt32 conv = 0;
					KCP.ikcp_decode32u(data, Helper.HandshakeHeadData.Length, ref conv);
					init_kcp(conv);
					status = ClientStatus.Connected;
					Console.WriteLine("Handshake Success");
				}
			}
		}


		private void ProcessRecvQueue()
		{
			while (!recvQueue.IsEmpty)
			{
				byte[] data = null;
				bool isSuccess = recvQueue.TryDequeue(out data);
				if (!isSuccess || data == null)
				{
					continue;
				}
                mKcp.Input(data);
				mNeedUpdateFlag = true;

				for (var size = mKcp.PeekSize(); size > 0; size = mKcp.PeekSize())
				{
					var packet = new byte[size];
					if (mKcp.Recv(packet) > 0)
					{
				        RecvDataHandler(packet);
					}
				}
				
			}
		}


		private void init_kcp(UInt32 conv)
		{
			mKcp = new KCP(conv, (byte[] buf, int size) => {
				RawSend(buf,size);
			});

			// fast mode.
			mKcp.NoDelay(1, 10, 2, 1);
			mKcp.WndSize(128, 128);
		}

        private void SendHandshake()
        {
            mLastSendConnectTime = Helper.iclock();
            RawSend(Helper.HandshakeHeadData,Helper.HandshakeHeadData.Length);
            status = ClientStatus.InConnect;
        }

		private bool IsConnectTimeout(UInt32 current)
		{
			return current - mConnectStartTime > CONNECT_TIMEOUT;
		}

		private bool IsRehandshake(UInt32 current)
		{
			return current - mLastSendConnectTime > RESEND_CONNECT;
		}
	}
}
