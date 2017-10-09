using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using LiteUDP;

namespace UDPAsyncServer
{
    public class UDPAsyncServer
    {

        protected IPEndPoint mServerEndPoint;
		protected Socket mServerSocket;
        protected UDPArgsPool mSAEPool;
        protected ConcurrentQueue<SocketAsyncEventArgs> RecvQueue;
        public Action<byte[]> RecvDataHandler;

        protected  ConcurrentDictionary<uint, KCPClientSession> mSessions;

		public UDPAsyncServer(IPAddress ip, int port, int bufferSize, int maxUserCount)
		{
			mServerEndPoint = new IPEndPoint(ip, port);
            mSAEPool = new UDPArgsPool(bufferSize, IOCompleted, maxUserCount / 4, maxUserCount);
            RecvQueue = new ConcurrentQueue<SocketAsyncEventArgs>();
			mSessions = new ConcurrentDictionary<uint, KCPClientSession>();
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
            foreach(var vk in mSessions){
                vk.Value.Update();
            }
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
            Console.WriteLine("Packet Receive:" + args.BytesTransferred + " bytes");
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

        public void SendWithSession(KCPClientSession session, byte[] data, int size)
        {
			var e = mSAEPool.GetObject();
            e.RemoteEndPoint = session.ClientEndPoint;
			Buffer.BlockCopy(data, 0, e.Buffer, 0, size);
			e.SetBuffer(0, size);
			mServerSocket.SendToAsync(e);
        }

        public void RecvDataHandlerWithSession(KCPClientSession session, byte[] data, int size)
        {
            byte[] newData = new byte[size];
            Buffer.BlockCopy(data,0,newData,0,size);
			RecvDataHandler(newData);
		}

		private void ProcessSend(SocketAsyncEventArgs args)
		{
			Console.WriteLine("Packet Send:" + args.BytesTransferred + " bytes");
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
				if (e.BytesTransferred == 0)
				{
                    mSAEPool.PutObject(e);
					continue;
				}
                //handshake with {0,0,0,0} at the first
                if (Helper.IsHandshakeDataRight(e.Buffer, e.Offset, e.BytesTransferred))
                {
                    uint conv = (uint)mSessions.Count+1;
					var newSession = new KCPClientSession(conv);
                    newSession.ClientEndPoint = e.RemoteEndPoint;
                    newSession.KCPOutput = SendWithSession;
                    newSession.RecvDataHandler = RecvDataHandlerWithSession;
                    mSessions.TryAdd(conv, newSession);
                    byte[] handshakeRespone = new byte[e.BytesTransferred+4];
                    Buffer.BlockCopy(e.Buffer,e.Offset,handshakeRespone,0,e.BytesTransferred);
                    KCP.ikcp_encode32u(handshakeRespone,e.BytesTransferred,conv);
                    e.SetBuffer(handshakeRespone,0,handshakeRespone.Length);
                    mServerSocket.SendToAsync(e);
                    Console.WriteLine("Handshake from:"+e.RemoteEndPoint.ToString());
                }
				else
				{
                    uint conv = 0;
					KCP.ikcp_decode32u(e.Buffer, e.Offset, ref conv);
                    KCPClientSession session = null;
                    mSessions.TryGetValue(conv, out session);
					if (session != null)
					{
                        session.processRecvQueue(e);
					}
                    mSAEPool.PutObject(e);
				}

			}
        }
    }
}
