using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System;

namespace Common
{
    public sealed class NetworkHelper
    {
        private static readonly NetworkHelper instance;

        static NetworkHelper()
        {
            instance = new NetworkHelper();
        }

        private NetworkHelper()
        {

        }

        public static NetworkHelper Instance()
        {
            return instance;
        }

        /// <summary>
        /// 检查网络是否连通，有延迟
        /// </summary>
        /// <param name="connectionDescription"></param>
        /// <param name="reservedValue"></param>
        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(out int connectionDescription, int reservedValue);


        /// <summary>
        /// 判断本地网络是否连接
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            int dwFlag ;
            if (!InternetGetConnectedState(out dwFlag, 0))
            {
                Console.WriteLine("网络连接不可用");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检测网络连接状态
        /// </summary>
        /// <param name="url"></param>
        /// <param name="errorMessage"></param>
        public bool CheckServeStatus(string url, out string errorMessage)
        {
            int pingFailureCount;
            errorMessage = string.Empty;
            if (EcgConfigXML.GetInstance().EnablePing)
            {
                List<long> roundtripTimes;
                if (!IsConnected())
                {
                    Console.WriteLine("网络未连接，请检测网络！");
                    errorMessage = "网络未连接，请检测网络！";
                    return false;
                }

                var pingResult = PingTest(url, out pingFailureCount, out roundtripTimes);

                if (!pingResult)
                {
                    if ((double)pingFailureCount / url.Length >= 0.3)
                    {
                        errorMessage = "网络不稳定，请检查网络！";
                        Console.WriteLine("网络不稳定，请检查网络！");
                        return false;
                    }
                }
                else
                {
                    if (pingFailureCount > 0)
                    {
                        errorMessage = "网络不稳定，请检查网络！";
                        Console.WriteLine("网络不稳定，请检查网络！");
                        return false;
                    }

                    if (roundtripTimes.Any(item => item > 300))
                    {
                        errorMessage = "网络不稳定，请检查网络！";
                        Console.WriteLine("网络不稳定，请检查网络！");
                        return false;
                    }
                }

                return true;
            }
            return true;
        }


        /// <summary>
        /// Ping命令检测网络是否畅通
        /// </summary>
        /// <param name="urls">URL数据</param>
        /// <param name="errorCount">ping时连接失败个数</param>
        /// <param name="roundtripTimes"></param>
        /// <returns></returns>
        private bool PingTest(string urls,out int errorCount,out List<long> roundtripTimes)
        {
            var isconn = true;
            var ping = new Ping();
            roundtripTimes = new List<long>();
            errorCount = 0;
            try
            {
                for (var i=0;i<4;i++)
                {
                    var pr = ping.Send(urls);
                    if (pr != null)
                    {
                        roundtripTimes.Add(pr.RoundtripTime);
                        if (pr.Status != IPStatus.Success)
                        {
                            isconn = false;
                            Console.WriteLine("Ping " + urls + "  " + pr.Status);
                            errorCount++;
                        }
                    } 
                }
            }
            catch
            {
                isconn = false;
                errorCount = urls.Length;
            }
            
            return isconn;
        }
    }
}
