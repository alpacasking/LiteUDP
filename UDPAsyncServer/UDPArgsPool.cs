﻿using System;
using System.Net;
using System.Net.Sockets;

namespace LiteUDP
{
    public class UDPArgsPool:ObjectPool<SocketAsyncEventArgs>
    {
		private int mBufferSize;
		private EventHandler<SocketAsyncEventArgs> mIOCompleted;

        public UDPArgsPool(int bufferSize,EventHandler<SocketAsyncEventArgs> IOCompleted,int initialCount, int maxCapacity)
            :base(maxCapacity)
        {
			mBufferSize = bufferSize;
			mIOCompleted = IOCompleted;
            mCreateFunc = CreateSAE;
			PutSAEs(initialCount);
        }

        private void PutSAEs(int count){
            for (int i = 0; i < count;i++){
                PutObject(CreateSAE());
            }
        }

        private SocketAsyncEventArgs CreateSAE()
        {
			var e = new SocketAsyncEventArgs();
			e.RemoteEndPoint = new IPEndPoint(0L, 0);
			e.SetBuffer(new byte[mBufferSize], 0, mBufferSize);
			e.Completed += mIOCompleted;
			e.AcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			return e;
        }
    }
}