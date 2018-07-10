using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace Client
{
    class Program
    {
        //版本2：使用Redis的客户端管理器（对象池）
        public static IRedisClientsManager redisClientManager = new PooledRedisClientManager(
        new[]
        {
            // 读写链接

            //如果是Redis集群则配置多个{IP地址:端口号}即可
            "127.0.0.1:6379","10.0.0.1:6379","10.0.0.2:6379","10.0.0.3:6379"
        }, new[]
        {
            // 只读链接
            //如果是Redis集群则配置多个{IP地址:端口号}即可
            "127.0.0.1:6379","10.0.0.1:6379","10.0.0.2:6379","10.0.0.3:6379"
        }, 10);
        //从池中获取Redis客户端实例
        public static IRedisClient redisClient = redisClientManager.GetClient();

        static void Main(string[] args)
        {
            #region 客户端添加消息
            redisClient.EnqueueItemOnList("Log", "1111");
            redisClient.EnqueueItemOnList("Log", "222");
            redisClient.EnqueueItemOnList("Log", "333");
            #endregion

            #region 服务器端扫码消息
            ThreadPool.QueueUserWorkItem(o =>
               {
                   while (true)
                   {
                       try
                       {
                           if (redisClient.GetListCount("Log") > 0)
                           {
                               string log = redisClient.DequeueItemFromList("Log");
                               if (!string.IsNullOrEmpty(log))
                               {
                                   Console.WriteLine(log);
                               }
                           }
                           else
                           {
                               Thread.Sleep(1000); //为避免CPU空转，在队列为空时休息1秒
                           }
                       }
                       catch (Exception ex)
                       {
                           redisClient.EnqueueItemOnList("Log", ex.ToString());
                       }
                   }
               });
            #endregion

            Console.ReadLine();
        }
    }
}
