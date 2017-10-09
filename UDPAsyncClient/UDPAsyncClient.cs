using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using LiteUDP;

namespace UDPAsyncClient
{
    public class UDPAsyncClient
    {
		// このクラスの有効期間中の各呼び出しに使用される、キャッシュされた Socket オブジェクト。
		Socket mUDPSocket = null;

		// 非同期ソケット メソッドで使用するデータ バッファーの最大サイズ。
		private int mBufferSize = 1024;
		private IPEndPoint mServerEndPoint;
        private SocketAsyncEventArgs mSendSAE;
        private SocketAsyncEventArgs mReceiveSAE;

        private ConcurrentQueue<byte[]> recvQueue = new ConcurrentQueue<byte[]>();
        public Action<byte[]> RecvDataHandler;

        private KCP mKcp;
        private bool mNeedUpdateFlag = false;
		private UInt32 mNextUpdateTime;


		public UDPAsyncClient(int bufferSize)
		{
			// AddressFamily.InterNetwork - ソケットは IP version 4 アドレス指定方式を
			// 使用してアドレスを解決する。
			// SocketType.Dgram - データグラム (メッセージ) パケットをサポートするソケット
			// PrototcolType.Udp - ユーザー データグラム プロトコル (UDP)
			mUDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mUDPSocket.Bind(new IPEndPoint(IPAddress.Any,0));
            mBufferSize = bufferSize;
		}

        public void Connect(IPEndPoint serverEndPoint)
        {
            mServerEndPoint = serverEndPoint;

			mSendSAE = new SocketAsyncEventArgs();
            mSendSAE.RemoteEndPoint = mServerEndPoint;
			mSendSAE.Completed += IOCompleted;
            SendHandshake();
			// SocketAsyncEventArgs コンテキスト オブジェクトを作成する。
			mReceiveSAE = new SocketAsyncEventArgs();
			mReceiveSAE.RemoteEndPoint = mServerEndPoint;
			// データを受信するためのバッファーを設定する。
			mReceiveSAE.SetBuffer(new Byte[mBufferSize], 0, mBufferSize);
            // Completed イベントのインライン イベント ハンドラー。
            // 注: メソッドを自己完結させるため、このイベント ハンドラーはインラインで実装される。
            mReceiveSAE.Completed += IOCompleted;

			if (!mUDPSocket.ReceiveFromAsync(mReceiveSAE))
				ProcessReceive(mReceiveSAE);
		}

		public void Close()
		{
			mUDPSocket.Close();
		}

		public void RawSend(byte[] data,int size)
		{
			// 送信するデータをバッファーに追加する。
			mSendSAE.SetBuffer(data, 0, size);
			// ソケットを使用して非同期の送信要求を行う。
			mUDPSocket.SendToAsync(mSendSAE);
		}

        public void Send(byte[] data)
        {
            mKcp.Send(data,data.Length);
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
			if (!mUDPSocket.ReceiveFromAsync(mReceiveSAE))
				ProcessReceive(mReceiveSAE);
		}

		private void ProcessSend(SocketAsyncEventArgs args)
		{
            Console.WriteLine("Packet Send:" + args.BytesTransferred + " bytes");
		}

		public virtual void Update()
		{
			ProcessRecvQueue();

            if(mKcp == null ){
                return;
            }
            var current = Helper.iclock();
			if (mNeedUpdateFlag || current >= mNextUpdateTime)
            {
				mKcp.Update(current);
                mNextUpdateTime = mKcp.Check(current);
				mNeedUpdateFlag = false;
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

                if(Helper.IsHandshakeDataRight(data, 0, data.Length)){
                    UInt32 conv = 0;
                    KCP.ikcp_decode32u(data, Helper.HandshakeHeadData.Length, ref conv);
                    init_kcp(conv);
                    Console.WriteLine("Handshake Success");
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
            RawSend(Helper.HandshakeHeadData,Helper.HandshakeHeadData.Length);
        }
	}
}
