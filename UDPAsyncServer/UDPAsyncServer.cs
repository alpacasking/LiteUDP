using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace LiteUDP
{
    public class UDPAsyncServer
    {

        protected IPEndPoint mServerEndPoint;
		protected Socket mServerSocket;
        protected UDPArgsPool mSAEPool;
        protected ConcurrentQueue<SocketAsyncEventArgs> RecvQueue;
        public Action<byte[]> RecvDataHandler;

		public UDPAsyncServer(IPAddress ip, int port, int bufferSize, int maxUserCount)
		{
			mServerEndPoint = new IPEndPoint(ip, port);
            mSAEPool = new UDPArgsPool(bufferSize, IOCompleted, maxUserCount / 4, maxUserCount);
            RecvQueue = new ConcurrentQueue<SocketAsyncEventArgs>();
		}

		public virtual void Start()
		{
			mServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			mServerSocket.Bind(mServerEndPoint);
			StartReceiving();
		}

        public virtual void ReStart()
        {
            Stop();
            Start();
        }

		public virtual void Stop()
		{
            mServerSocket.Close();
		}

        public virtual void Update()
        {
            ProcessRecvQueue();
        }

		private void StartReceiving()
		{
            var e = mSAEPool.GetObject();
			try
			{
				if (!mServerSocket.ReceiveFromAsync(e))
					ProcessReceive(e);
			}
			catch (ObjectDisposedException)
			{
                mSAEPool.PutObject(e);
			}
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
				default:
                    mSAEPool.PutObject(args);
					break;
			}
		}

		private void ProcessReceive(SocketAsyncEventArgs args)
		{
			switch (args.SocketError)
			{
				case SocketError.Success:
					if ( args.BytesTransferred > 0)
					{
                        RecvQueue.Enqueue(args);
					}
                    else{
                        mSAEPool.PutObject(args);
                    }
					break;

				case SocketError.OperationAborted:
                    mSAEPool.PutObject(args);
					return;

				default:
					mSAEPool.PutObject(args);
					break;
			}
			StartReceiving();
		}

        public void Send(byte[] data)
        {
            var e = mSAEPool.GetObject();
            //e.RemoteEndPoint = ;
            Buffer.BlockCopy(data, 0, e.Buffer, 0, data.Length);
            e.SetBuffer(0, data.Length);
            mServerSocket.SendToAsync(e);
        }

		private void ProcessSend(SocketAsyncEventArgs args)
		{
			mSAEPool.PutObject(args);
		}

        private void ProcessRecvQueue()
        {
            while(!RecvQueue.IsEmpty){
                SocketAsyncEventArgs e = null;
                bool isSuccess = RecvQueue.TryDequeue(out e);
                if(!isSuccess||e == null){
                    continue;
                }
                int dataLength = e.BytesTransferred;
                byte[] data = new byte[dataLength];
                Buffer.BlockCopy(e.Buffer,e.Offset,data,0,dataLength);
                RecvDataHandler(data);
                mSAEPool.PutObject(e);
            }
        }
    }
}
