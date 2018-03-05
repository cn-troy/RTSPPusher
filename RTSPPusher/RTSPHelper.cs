using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RTSPPusher
{
    /// <summary>
    /// RTSP帮助类
    /// </summary>
    public class RTSPHelper
    {
        /// <summary>
        /// 初始化构造函数
        /// </summary>
        /// <param name="serverIP">RTSP服务器IP</param>
        /// <param name="serverPort">RTSP服务器端口</param>
        /// <param name="urlFlag">RTSP地址标识</param>
        public RTSPHelper(string serverIP, int serverPort, string urlFlag)
        {
            this.UrlFlag = urlFlag;
            this.ServerIP = serverIP;
            this.ServerPort = serverPort;

            //TCP连接RTSP服务器
            RTSPServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            RTSPServer.Connect(new IPEndPoint(IPAddress.Parse(serverIP), serverPort));
        }

        /// <summary>
        /// 服务器IP
        /// </summary>
        public string ServerIP { get; set; }
        /// <summary>
        /// 服务器端口
        /// </summary>
        public int ServerPort { get; set; }
        /// <summary>
        /// 地址标识
        /// </summary>
        public string UrlFlag { get; set; }

        /// <summary>
        /// 服务器Socket
        /// </summary>
        public Socket RTSPServer { get; set; }


        /// <summary>
        /// 用户代理
        /// </summary>
        private string UserAgent { get; set; } = "KTSF-RTSP-Pubsher v0.1";

        /// <summary>
        /// SETUP后RTSP服务器分配的Session
        /// </summary>
        private string Session { get; set; }

        /// <summary>
        /// OPTIONS
        /// </summary>
        /// <returns></returns>
        private bool OPTIONS()
        {
            string OPTIONS = $@"OPTIONS rtsp://{ServerIP}:{ServerPort}/{UrlFlag} RTSP/1.0
CSeq:1 
User-Agent:{UserAgent}

";
            Console.WriteLine($"发送{OPTIONS}");
            RTSPServer.Send(Encoding.ASCII.GetBytes(OPTIONS));

            byte[] receiveBuffer = new byte[4056];
            int receiveCount = RTSPServer.Receive(receiveBuffer);

            if (receiveCount <= 0)
            {
                Console.WriteLine("OPTIONS 没有返回数据");
                return false;
            }

            byte[] data = new byte[receiveCount];
            Array.Copy(receiveBuffer, 0, data, 0, receiveCount);
            string optionsResponse = Encoding.ASCII.GetString(data);

            Console.WriteLine($"Options回复:{optionsResponse}");
            return true;
        }

        /// <summary>
        /// ANNOUNCE
        /// </summary>
        /// <returns></returns>
        private bool ANNOUNCE()
        {
            string ANNOUNCEContent = $@"v=0
o=- 0 0 IN IP4 0.0.0.0
s={UrlFlag}
c=IN IP4 {ServerIP}
t=0 0
a=tool:libc6
m=audio 0 RTP/AVP 14
b=AS:320
a=control:streamid=0";

            //长度+2是因为Content前后各有一个换行
            string ANNOUNCE = $@"ANNOUNCE rtsp://{ServerIP}:{ServerPort}/{UrlFlag} RTSP/1.0
Content-Type: application/sdp
CSeq: 2
User-Agent:{UserAgent}
Content-Length:{Encoding.ASCII.GetByteCount(ANNOUNCEContent) + 2}

{ANNOUNCEContent}
";
            Console.WriteLine($"发送{ANNOUNCE}");

            RTSPServer.Send(Encoding.ASCII.GetBytes(ANNOUNCE));

            byte[] receiveBuffer = new byte[4056];
            int receiveCount = RTSPServer.Receive(receiveBuffer);

            if (receiveCount <= 0)
            {
                Console.WriteLine("ANNOUNCE 没有返回数据");
                return false;
            }

            byte[] data = new byte[receiveCount];
            Array.Copy(receiveBuffer, 0, data, 0, receiveCount);
            string ANNOUNCEResponse = Encoding.ASCII.GetString(data);

            Console.WriteLine($"ANNOUNCE回复:{ANNOUNCEResponse}");
            return true;
        }

        /// <summary>
        /// SETUP
        /// </summary>
        /// <returns></returns>
        private bool SETUP()
        {
            //此处注意..使用了@不转义字符串, 并且RTSP指定每行都需要使用\r\n换行, 最后使用两个\r\n, 前面不要输入Tab, 否则将报错
            string SETUP = $@"SETUP rtsp://{ServerIP}:{ServerPort}/{UrlFlag}/streamid=0 RTSP/1.0
CSeq:3
Transport:RTP/AVP/TCP;unicast;interleaved=0-1;mode=record
User-Agent:{UserAgent}

";
            Console.WriteLine($"发送{SETUP}");
            RTSPServer.Send(Encoding.ASCII.GetBytes(SETUP));

            byte[] receiveBuffer = new byte[4056];
            int receiveCount = RTSPServer.Receive(receiveBuffer);

            if (receiveCount <= 0)
            {
                Console.WriteLine("SETUP 没有返回数据");
                return false;
            }

            byte[] data = new byte[receiveCount];
            Array.Copy(receiveBuffer, 0, data, 0, receiveCount);
            string SETUPResponse = Encoding.ASCII.GetString(data);

            Console.WriteLine($"SETUP回复:{SETUPResponse}");

            StringReader stringReader = new StringReader(SETUPResponse);
            string line = "";
            while ((line = stringReader.ReadLine()) != null)
            {
                if (line.StartsWith("Session"))
                {
                    this.Session = line.Split(":")[1].TrimStart().TrimEnd();
                }
            }

            return true;
        }

        /// <summary>
        /// RECORD
        /// </summary>
        /// <returns></returns>
        private bool RECORD()
        {
            if (string.IsNullOrEmpty(Session))
            {
                return false;
            }

            string RECORD = $@"RECORD rtsp://{ServerIP}:{ServerPort}/{UrlFlag} RTSP/1.0
Range: npt=0.000-
CSeq:4
User-Agent:{UserAgent}
Session: {Session}

";
            Console.WriteLine($"发送{RECORD}");
            RTSPServer.Send(Encoding.ASCII.GetBytes(RECORD));

            byte[] receiveBuffer = new byte[4056];
            int receiveCount = RTSPServer.Receive(receiveBuffer);

            if (receiveCount <= 0)
            {
                Console.WriteLine("RECORD 没有返回数据");
                return false;
            }

            byte[] data = new byte[receiveCount];
            Array.Copy(receiveBuffer, 0, data, 0, receiveCount);
            string RECORDResponse = Encoding.ASCII.GetString(data);

            Console.WriteLine($"RECORD回复:{RECORDResponse}");

            return true;
        }

        /// <summary>
        /// TEARDOWN
        /// </summary>
        /// <returns></returns>
        private bool TEARDOWN()
        {
            string TEARDOWN = $@"TEARDOWN rtsp://{ServerIP}:{ServerPort}/{UrlFlag} RTSP/1.0
CSeq: 5
User-Agent:{UserAgent}
Session:{Session}

";
            Console.WriteLine($"发送{TEARDOWN}");
            RTSPServer.Send(Encoding.ASCII.GetBytes(TEARDOWN));

            byte[] receiveBuffer = new byte[4056];
            int receiveCount = RTSPServer.Receive(receiveBuffer);

            if (receiveCount <= 0)
            {
                Console.WriteLine("TEARDOWN 没有返回数据");
                return false;
            }

            byte[] data = new byte[receiveCount];
            Array.Copy(receiveBuffer, 0, data, 0, receiveCount);
            string RECORDResponse = Encoding.ASCII.GetString(data);

            Console.WriteLine($"TEARDOWN:{RECORDResponse}");

            RTSPServer.Close();

            return true;
        }

        /// <summary>
        /// 连接RTSP服务器
        /// </summary>
        /// <returns></returns>
        public bool ConnectRTSP()
        {
            bool success = OPTIONS();
            if (!success)
            {
                return false;
            }

            success = ANNOUNCE();
            if (!success)
            {
                return false;
            }

            success = SETUP();
            if (!success)
            {
                return false;
            }

            success = RECORD();
            if (!success)
            {
                return false;
            }

            return success;
        }

        /// <summary>
        /// 断开RTSP服务器连接
        /// </summary>
        /// <returns></returns>
        public bool CloseRTSP()
        {
            return TEARDOWN(Session);
        }
    }
}
