using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RTSPPusher
{
    class Program
    {
        static void Main(string[] args)
        {
            //打开MP3文件, 请自行修改mp3文件地址.
            FileStream fileStream = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "/chinax.mp3");
            //随便读取一片数据(当然必须比一帧的数据长)
            byte[] fileMp3Buffer = new byte[2048];
            fileStream.Read(fileMp3Buffer, 0, fileMp3Buffer.Length);

            //初始化MP3解码器
            MP3Decoder mP3Decoder = new MP3Decoder();
            //将mp3帧添加到解码器
            mP3Decoder.PushMP3Buffer(fileMp3Buffer);
            //获取mp3的标签帧(由于当前是使用标准Mp3 ID3文件数据, 所以需要获取,以便跳过标签帧. 如果设备采集的帧没有标签帧,则不需要使用该方法)
            mP3Decoder.GetTAGFrame();

            //定义RTSP服务器
            RTSPHelper rtspHelper = new RTSPHelper("192.168.1.163", 554, "chinax");
            //连接RTSP
            rtspHelper.ConnectRTSP();
            //初始化RTP包裹器
            RTPWapper rTPWapper = new RTPWapper((uint)new Random().Next(0, int.MaxValue));

            //每帧播放时长(Thread.Sleep等待时间)
            double framePlayTime = 0;
            //文件是否读取完成(可选, 当前是测试MP3文件)
            bool fileReadOver = false;
            while (true)
            {
                //没读完MP3文件就继续读,并添加到MP3解码器
                if (!fileReadOver)
                {
                    fileReadOver = fileStream.Read(fileMp3Buffer, 0, fileMp3Buffer.Length) == 0;
                    if (fileReadOver)
                    {
                        Console.WriteLine($"文件已经全部读取完成");
                    }
                    else
                    {
                        mP3Decoder.PushMP3Buffer(fileMp3Buffer);
                    }
                }
                
                
                //获取MP3帧
                byte[] frame = mP3Decoder.GetAudioFrame(out framePlayTime);
                if (frame.Length == 0)
                {
                    Console.WriteLine($"MP3所有帧发送完成");
                    break;
                }

                //转换为RTP帧
                byte[] rtpBuffer = rTPWapper.Wapper(frame);


                Console.WriteLine($"发送了rtp帧数据长度: {rtpBuffer.Length}, 剩余待rtp封包数据长度:{mP3Decoder.MP3Buffer.Count}");
                try
                {
                    //发送
                    rtspHelper.RTSPServer.Send(rtpBuffer);
                }
                catch (Exception)
                {
                    //Console.WriteLine($"连接{(DateTime.Now - connectedTime).TotalMilliseconds}毫秒后断开");
                    break;
                }
                //-2是因为逻辑操作耗时, 否则会卡顿
                Thread.Sleep((int)framePlayTime - 2);
            }
            //关闭RTSP
            rtspHelper.CloseRTSP();
            mP3Decoder = null;

            Console.WriteLine("结束");
            Console.ReadLine();
        }
    }
}
