using System;
using System.Collections.Generic;
using System.Text;

namespace RTSPPusher
{
    public class RTPWapper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SSRC">SSRC</param>
        /// <param name="beginSequenceNumber">起始序列号</param>
        public RTPWapper(uint SSRC, ushort beginSequenceNumber = 1)
        {
            this.SSRC = SSRC;
            this._SequenceNumber = beginSequenceNumber;
        }

        private ushort _SequenceNumber;
        /// <summary>
        /// 序列号
        /// </summary>
        public ushort SequenceNumber
        {
            get
            {
                if (_SequenceNumber >= ushort.MaxValue)
                {
                    _SequenceNumber = 0;
                }
                return _SequenceNumber++;
            }
        }

        /// <summary>
        /// 同步信源(SSRC)标识符Synchronization Source Identifier
        /// </summary>
        public uint SSRC { get; set; }

        /// <summary>
        /// 包裹MP3帧为RTP
        /// </summary>
        /// <param name="mp3Frame">MP3帧(裸流)</param>
        /// <returns></returns>
        public byte[] Wapper(byte[] mp3Frame)
        {
            byte[] sequenceNumberBuffer = BitConverter.GetBytes(SequenceNumber);
            Array.Reverse(sequenceNumberBuffer);

            byte[] timeSpanBuffer = BitConverter.GetBytes((uint)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
            Array.Reverse(timeSpanBuffer);

            byte[] ssrcBuffer = BitConverter.GetBytes(SSRC);
            Array.Reverse(ssrcBuffer);

            //RTP帧, 具体协议请百度RTP协议详解
            List<byte> rtpFrame = new List<byte>();
            rtpFrame.Add(0x80);
            rtpFrame.Add(0x0e);
            rtpFrame.AddRange(sequenceNumberBuffer);
            rtpFrame.AddRange(timeSpanBuffer);
            rtpFrame.AddRange(ssrcBuffer);
            rtpFrame.AddRange(new byte[] { 0, 0, 0, 0 });
            rtpFrame.AddRange(mp3Frame);

            //InterleavedFrame
            List<byte> rtpInterleavedFrame = new List<byte>();
            rtpInterleavedFrame.Add(0x24);
            rtpInterleavedFrame.Add(0x00);
            
            byte[] length = BitConverter.GetBytes((ushort)rtpFrame.Count);
            Array.Reverse(length);
            rtpInterleavedFrame.AddRange(length);

            rtpInterleavedFrame.AddRange(rtpFrame);

            return rtpInterleavedFrame.ToArray();
        }
    }
}
