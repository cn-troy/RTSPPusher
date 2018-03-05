using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RTSPPusher
{
    /// <summary>
    /// MP3解码, 由于本系统中MP3帧使用RAM传输, 所以不能直接在该类中直接使用文件访问.
    /// 暂只支持ID3版本
    /// </summary>
    public class MP3Decoder
    {


        public class MP3AudioFrameHeader
        {
            /// <summary>
            /// 同步信息: 11位 所有位均为1，第1字节恒为FF。
            /// </summary>
            public string Sync { get; set; }

            /// <summary>
            /// 版本: 2位 00-MPEG 2.5   01-未定义     10-MPEG 2     11-MPEG 1
            /// </summary>
            public string Version { get; set; }
            /// <summary>
            /// 层: 2位 00-未定义      01-Layer 3     10-Layer 2      11-Layer 1
            /// </summary>
            public string Layer { get; set; }
            /// <summary>
            /// CRC校验: 1位 0-校验        1-不校验
            /// </summary>
            public string CRC { get; set; }
            /// <summary>
            /// 位率: 4位
            /// </summary>
            public string BitrateIndex { get; set; }
            /// <summary>
            /// 采样频率: 2位 
            /// 对于MPEG-1：  00-44.1kHz    01-48kHz    10-32kHz      11-未定义
            /// 对于MPEG-2：  00-22.05kHz   01-24kHz    10-16kHz      11-未定义
            /// 对于MPEG-2.5： 00-11.025kHz 01-12kHz    10-8kHz       11-未定义
            /// </summary>
            public string SamplingFrequency { get; set; }
            /// <summary>
            /// 帧长调节: 1位 用来调整文件头长度，0-无需调整，1-调整
            /// </summary>
            public string Padding { get; set; }
            /// <summary>
            /// 保留字
            /// </summary>
            public string Reserved { get; set; }
            /// <summary>
            /// 声道模式: 2位 00-立体声Stereo    01-Joint Stereo    10-双声道        11-单声道
            /// </summary>
            public string Mode { get; set; }
            /// <summary>
            /// 扩充模式
            /// </summary>
            public string ModeExtension { get; set; }
            /// <summary>
            /// 版权: 1位 0-不合法   1-合法
            /// </summary>
            public string Copyright { get; set; }
            /// <summary>
            /// 原版标志: 1位 0-非原版   1-原版
            /// </summary>
            public string Original { get; set; }
            /// <summary>
            /// 强调模式: 2位
            /// 用于声音经降噪压缩后再补偿的分类，很少用到，今后也可能不会用。
            /// 00-未定义     01-50/15ms     10-保留       11-CCITT J.17
            /// </summary>
            public string Emphasis { get; set; }

            public MP3AudioFrameHeader(byte[] mp3HeaderBuffer)
            {
                Array.Reverse(mp3HeaderBuffer);
                uint mp3Header = BitConverter.ToUInt32(mp3HeaderBuffer, 0);
                string mp3HeaderBinString = Convert.ToString(mp3Header, 2);
                //Console.WriteLine(mp3HeaderBinString);

                Sync = mp3HeaderBinString.Substring(0, 11);
                //Console.WriteLine(Sync);

                Version = mp3HeaderBinString.Substring(11, 2);
                //Console.WriteLine(Version);

                Layer = mp3HeaderBinString.Substring(13, 2);
                //Console.WriteLine(Layer);

                CRC = mp3HeaderBinString.Substring(15, 1);
                //Console.WriteLine(CRC);

                //位率
                BitrateIndex = mp3HeaderBinString.Substring(16, 4);
                //Console.WriteLine(BitrateIndex);

                //采样频率
                SamplingFrequency = mp3HeaderBinString.Substring(20, 2);
                //Console.WriteLine(SamplingFrequency);

                Padding = mp3HeaderBinString.Substring(22, 1);
                //Console.WriteLine(Padding);

                Reserved = mp3HeaderBinString.Substring(23, 1);
                //Console.WriteLine(Reserved);

                Mode = mp3HeaderBinString.Substring(24, 2);
                //Console.WriteLine(Mode);

                //扩充模式
                ModeExtension = mp3HeaderBinString.Substring(26, 2);
                //Console.WriteLine(ModeExtension);

                Copyright = mp3HeaderBinString.Substring(28, 1);
                //Console.WriteLine(Copyright);

                Original = mp3HeaderBinString.Substring(29, 1);
                //Console.WriteLine(Original);

                Emphasis = mp3HeaderBinString.Substring(30, 2);
                //Console.WriteLine(Emphasis);
            }
        }

        //private FileStream MP3Stream { get; set; }

        public List<byte> MP3Buffer { get; set; } = new List<byte>();

        public int TAGFrameLength { get; set; }

        public byte[] TAGFrame { get; set; }

        /// <summary>
        /// 比特率kbps字典
        /// </summary>
        Dictionary<string, Dictionary<string, Dictionary<string, int>>> KBPSDictionary = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>()
        {
            //11 -> MPEG 1, V1
            { "11", new Dictionary<string, Dictionary<string, int>>(){
                //11 -> Layer 1, L1
                { "11", new Dictionary<string, int>(){
                    { "0001", 32},
                    { "0010", 64},
                    { "0011", 96},
                    { "0100", 128},
                    { "0101", 160},
                    { "0110", 192},
                    { "0111", 224},
                    { "1000", 256},
                    { "1001", 288},
                    { "1010", 320},
                    { "1011", 352},
                    { "1100", 384},
                    { "1101", 416},
                    { "1110", 448},
                } },
                //10 -> Layer 2, L2
                { "10", new Dictionary<string, int>(){
                    { "0001", 32},
                    { "0010", 48},
                    { "0011", 56},
                    { "0100", 64},
                    { "0101", 80},
                    { "0110", 96},
                    { "0111", 112},
                    { "1000", 128},
                    { "1001", 160},
                    { "1010", 192},
                    { "1011", 224},
                    { "1100", 256},
                    { "1101", 320},
                    { "1110", 384},
                } },
                //01 -> Layer 3, L3
                { "01", new Dictionary<string, int>(){
                    { "0001", 32},
                    { "0010", 40},
                    { "0011", 48},
                    { "0100", 56},
                    { "0101", 64},
                    { "0110", 80},
                    { "0111", 96},
                    { "1000", 112},
                    { "1001", 128},
                    { "1010", 160},
                    { "1011", 192},
                    { "1100", 224},
                    { "1101", 256},
                    { "1110", 320},
                } }
            } },
            //10 -> MPEG 2, V2
            { "10", new Dictionary<string, Dictionary<string, int>>(){
                //11 -> Layer 1, L1
                { "11", new Dictionary<string, int>(){
                    { "0001", 32},
                    { "0010", 64},
                    { "0011", 96},
                    { "0100", 128},
                    { "0101", 160},
                    { "0110", 192},
                    { "0111", 224},
                    { "1000", 256},
                    { "1001", 288},
                    { "1010", 320},
                    { "1011", 352},
                    { "1100", 384},
                    { "1101", 416},
                    { "1110", 448},
                } },
                //10 -> Layer 2, L2
                { "10", new Dictionary<string, int>(){
                    { "0001", 32},
                    { "0010", 48},
                    { "0011", 56},
                    { "0100", 64},
                    { "0101", 80},
                    { "0110", 96},
                    { "0111", 112},
                    { "1000", 128},
                    { "1001", 160},
                    { "1010", 192},
                    { "1011", 224},
                    { "1100", 256},
                    { "1101", 320},
                    { "1110", 384},
                } },
                //01 -> Layer 3, L3
                { "01", new Dictionary<string, int>(){
                    { "0001", 8},
                    { "0010", 16},
                    { "0011", 24},
                    { "0100", 32},
                    { "0101", 64},
                    { "0110", 80},
                    { "0111", 56},
                    { "1000", 64},
                    { "1001", 128},
                    { "1010", 160},
                    { "1011", 112},
                    { "1100", 128},
                    { "1101", 256},
                    { "1110", 320},
                } }
            } },
            //00 -> MPEG 2.5, V2
            { "00", new Dictionary<string, Dictionary<string, int>>(){
                //11 -> Layer 1, L1
                { "11", new Dictionary<string, int>(){
                    { "0001", 32},
                    { "0010", 48},
                    { "0011", 56},
                    { "0100", 64},
                    { "0101", 80},
                    { "0110", 96},
                    { "0111", 112},
                    { "1000", 128},
                    { "1001", 144},
                    { "1010", 160},
                    { "1011", 176},
                    { "1100", 192},
                    { "1101", 224},
                    { "1110", 256},
                } },
                //10 -> Layer 2, L2
                { "10", new Dictionary<string, int>(){
                    { "0001", 8},
                    { "0010", 16},
                    { "0011", 24},
                    { "0100", 32},
                    { "0101", 40},
                    { "0110", 48},
                    { "0111", 56},
                    { "1000", 64},
                    { "1001", 80},
                    { "1010", 96},
                    { "1011", 112},
                    { "1100", 128},
                    { "1101", 144},
                    { "1110", 160},
                } },
                //01 -> Layer 3, L3
                { "01", new Dictionary<string, int>(){
                    { "0001", 8},
                    { "0010", 16},
                    { "0011", 24},
                    { "0100", 32},
                    { "0101", 40},
                    { "0110", 48},
                    { "0111", 56},
                    { "1000", 64},
                    { "1001", 80},
                    { "1010", 96},
                    { "1011", 112},
                    { "1100", 128},
                    { "1101", 144},
                    { "1110", 160},
                } }
            } },
        };

        /// <summary>
        /// 采样频率kHz字典
        /// </summary>
        Dictionary<string, Dictionary<string, double>> KHZDictionary = new Dictionary<string, Dictionary<string, double>>()
        {
            //11 -> MPEG 1, V1
            { "11", new Dictionary<string, double>(){
                //采样频率 00
                { "00", 44.1},
                //采样频率 01
                { "01", 48},
                //采样频率 10
                { "10", 32}
            } },
            //10 -> MPEG 2, V2
            { "10", new Dictionary<string, double>(){
                { "00", 22.05},
                { "01", 24},
                { "10", 16}
            } },
            //00 -> MPEG 2.5, V2
            { "00", new Dictionary<string, double>(){
                { "00", 11.025},
                { "01", 12},
                { "10", 8}
            } },
        };

        /// <summary>
        /// 每帧采样数字典
        /// </summary>
        Dictionary<string, Dictionary<string, int>> FrameCountDictionary = new Dictionary<string, Dictionary<string, int>>()
        {
            //11 -> MPEG 1, V1
            { "11", new Dictionary<string, int>(){
                //11 -> Layer 1, L1
                { "11", 384},
                //10 -> Layer 2, L2
                { "10", 1152},
                //01 -> Layer 3, L3
                { "01", 1152}
            } },
            //10 -> MPEG 2, V2
            { "10", new Dictionary<string, int>(){
                //11 -> Layer 1, L1
                { "11", 384},
                //10 -> Layer 2, L2
                { "10", 1152},
                //01 -> Layer 3, L3
                { "01", 576}
            } },
            //00 -> MPEG 2.5, V2
            { "00", new Dictionary<string, int>(){
                //11 -> Layer 1, L1
                { "11", 384},
                //10 -> Layer 2, L2
                { "10", 1152},
                //01 -> Layer 3, L3
                { "01", 576}
            } },
        };


        public void PushMP3Buffer(byte[] buffer)
        {
            this.MP3Buffer.AddRange(buffer);
        }

        /// <summary>
        /// 获取标签帧长度
        /// </summary>
        /// <returns></returns>
        private void GetTAGFrameLength()
        {
            //必须为"ID3"否则认为标签不存在
            byte[] mp3Header = MP3Buffer.GetRange(0, 3).ToArray();
            
            //MP3Stream.Read(mp3Header, 0, mp3Header.Length);

            if (Encoding.ASCII.GetString(mp3Header) != "ID3")
            {
                throw new NotImplementedException("暂只能识别ID3的MP3音频");
            }
            //版本号 ID3V2.3 就记录 3
            byte ver = MP3Buffer[3];
            //副版本号此版本记录为
            byte Revision = MP3Buffer[4];
            //存放标志的字节,这个版本只定义了三位
            byte flag = MP3Buffer[5];

            //标签大小,包括标签头的 10 个字节和所有的标签帧的大小
            byte[] size = MP3Buffer.GetRange(6, 4).ToArray();
            //MP3Stream.Read(size, 0, size.Length);

            MP3Buffer.RemoveRange(0, 10);

            TAGFrameLength = (size[0] & 0x7F) * 0x200000 + (size[1] & 0x7F) * 0x400 + (size[2] & 0x7F) * 0x80
                         + (size[3] & 0x7F);
        }

        /// <summary>
        /// 获取标签帧
        /// </summary>
        /// <returns></returns>
        public byte[] GetTAGFrame()
        {
            GetTAGFrameLength();

            byte[] frameBuffer = MP3Buffer.GetRange(0, TAGFrameLength).ToArray();
            MP3Buffer.RemoveRange(0, TAGFrameLength);
            //MP3Stream.Read(frameBuffer, 0, frameBuffer.Length);
            return frameBuffer;
        }


        /// <summary>
        /// 获取数据帧长度
        /// </summary>
        /// <returns></returns>
        private int GetAuidoFrameLength(out double playTime)
        {
            playTime = 0;
            
            byte[] headerBuffer = MP3Buffer.GetRange(0, 4).ToArray();
            //MP3Stream.Read(headerBuffer, 0, headerBuffer.Length);
            ////读取位置回到ff fb
            //MP3Stream.Position = MP3Stream.Position - 4;

            if (headerBuffer[0] != 0xFF)
            {
                Console.WriteLine("没有读取到FF头");

                return 0;
            }

            MP3AudioFrameHeader header = new MP3AudioFrameHeader(headerBuffer);

            int length = 0;
            //每帧采样数
            int frameCount = FrameCountDictionary[header.Version][header.Layer];
            //比特率
            int kbps = KBPSDictionary[header.Version][header.Layer][header.BitrateIndex];
            //采样率
            double khz = KHZDictionary[header.Version][header.SamplingFrequency];

            switch (header.Layer)
            {
                //11 -> Layer 1, L1
                case "11":
                    break;
                //10 -> Layer 2, L2
                //01 -> Layer 3, L3
                case "10":
                case "01":
                    length = (int)Math.Floor(((frameCount / 8 * kbps) / khz)) + (header.Padding == "0" ? 0 : 1);
                    playTime = frameCount / khz;
                    break;
                default:
                    break;
            }

            return length;
        }

        public byte[] GetAudioFrame(out double playTime)
        {
            playTime = 0;
            if (MP3Buffer.Count == 0)
            {
                return new byte[0];
            }
            int audioFrameLength = GetAuidoFrameLength(out playTime);
            if (audioFrameLength == 0)
            {
                return new byte[0];
            }
            byte[] audioBuffer = MP3Buffer.GetRange(0, audioFrameLength).ToArray();

            //MP3Stream.Read(audioBuffer, 0, audioFrameLength);
            MP3Buffer.RemoveRange(0, audioFrameLength);
            return audioBuffer;
        }


    }
}
