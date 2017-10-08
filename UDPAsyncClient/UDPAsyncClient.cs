using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

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

		public void Send(byte[] data)
		{
			// 送信するデータをバッファーに追加する。
			mSendSAE.SetBuffer(data, 0, data.Length);
			// ソケットを使用して非同期の送信要求を行う。
			mUDPSocket.SendToAsync(mSendSAE);
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
			switch (args.SocketError)
			{
				case SocketError.Success:
					if (args.BytesTransferred > 0)
					{
                        byte[] data = new byte[args.BytesTransferred];
                        Buffer.BlockCopy(args.Buffer,args.Offset,data,0,args.BytesTransferred);
						recvQueue.Enqueue(data);
                        //Console.WriteLine("ProcessReceive");
					}
                    break;
			}
			if (!mUDPSocket.ReceiveFromAsync(mReceiveSAE))
				ProcessReceive(mReceiveSAE);
		}

		private void ProcessSend(SocketAsyncEventArgs args)
		{
            //Console.WriteLine("ProcessSend");
		}

		public virtual void Update()
		{
			ProcessRecvQueue();
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
				RecvDataHandler(data);
			}
		}
	}
}
