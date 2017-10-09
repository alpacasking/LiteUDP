﻿using System;
using System.Net;
using System.Net.Sockets;
using LiteUDP;
namespace UDPAsyncServer
{
    public class KCPClientSession
    {
        public uint Conv { get; set; }
        public EndPoint ClientEndPoint { get; set; }
        private KCP mKcp = null;
        public Action<KCPClientSession, byte[], int> KCPOutput { get; set; }
        public Action<KCPClientSession, byte[], int> RecvDataHandler { get; set; }
        private bool mNeedUpdateFlag = false;
        private UInt32 mNextUpdateTime;

        public KCPClientSession(uint conv)
        {
            initKCP(conv);
            Conv = conv;
        }

        void initKCP(UInt32 conv)
        {
            mKcp = new KCP(conv, (byte[] buf, int size) =>
            {
                KCPOutput(this, buf, size);
            });
            // fast mode.
            mKcp.NoDelay(1, 10, 2, 1);
            mKcp.WndSize(128, 128);
        }


        public void Send(byte[] buf)
        {
            mKcp.Send(buf, buf.Length);
            mNeedUpdateFlag = true;
        }

        public void Update()
        {
            UpdateKCP(Helper.iclock());
        }

        public void processRecvQueue(SocketAsyncEventArgs e)
        {
            mKcp.Input(e.Buffer);

            mNeedUpdateFlag = true;

            for (var size = mKcp.PeekSize(); size > 0; size = mKcp.PeekSize())
            {
                byte[] buffer = new byte[size];
                if (mKcp.Recv(buffer) > 0)
                {
                    RecvDataHandler(this, buffer, size);
                }
            }
        }

        private void UpdateKCP(UInt32 current)
        {
            if (mKcp == null)
                return;
            if (mNeedUpdateFlag || current >= mNextUpdateTime)
            {
                mKcp.Update(current);
                mNextUpdateTime = mKcp.Check(current);
                mNeedUpdateFlag = false;
            }
        }

        private void ResetKCP()
        {
            initKCP(Conv);
        }
    }
}