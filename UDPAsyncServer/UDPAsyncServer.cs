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
        protected ConcurrentQueue<uint> mDisposeSessionConvs;

		public UDPAsyncServer(IPAddress ip, int port, int bufferSize, int maxUserCount)
		{
			mServerEndPoint = new IPEndPoint(ip, port);
            mSAEPool = new UDPArgsPool(bufferSize, IOCompleted, maxUserCount / 4, maxUserCount);
            RecvQueue = new ConcurrentQueue<SocketAsyncEventArgs>();
			mSessions = new ConcurrentDictionary<uint, KCPClientSession>();
            mDisposeSessionConvs = new ConcurrentQueue<uint>();
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
                KCPClientSession session = vk.Value;
                session.Update();
                if(session.IsDead()){
                    mDisposeSessionConvs.Enqueue(session.Conv);
                }
            }
            ProcessDisposeSession();
        }

        private void ProcessDisposeSession()
        {
            while (!mDisposeSessionConvs.IsEmpty)
            {
                UInt32 conv = 0;
                bool isSuccess = mDisposeSessionConvs.TryDequeue(out conv);
                if (!isSuccess)
                {
                    continue;
                }
                KCPClientSession tempSession = null;
                mSessions.TryRemove(conv, out tempSession);
                Console.WriteLine("Dead Session:"+tempSession.ClientEndPoint.ToString());
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
            if(!mServerSocket.SendToAsync(e)){
                ProcessSend(e);
            }
			
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
 
                    KCP.ikcp_encode32u(e.Buffer,e.BytesTransferred,conv);
                    e.SetBuffer(0,e.BytesTransferred+4);
                    if(!mServerSocket.SendToAsync(e)){
                        ProcessSend(e);
                    }
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
