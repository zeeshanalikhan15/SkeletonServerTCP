using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Configuration;
using NLog;

namespace TestServer
{
    public class Server
    {
        private static Server _serverInstance;
        private readonly AsynchronousSocketListener _asynchronousSocketListener;
        private readonly Logger _logger = LogManager.GetLogger("Server");

        private Server()
        {
            _asynchronousSocketListener = new AsynchronousSocketListener();
        }

        public static  Server GetInstance()
        {
            return _serverInstance ?? (_serverInstance = new Server());
        }

        public void Start()
        {
            try
            {
                var ip = ConfigurationManager.AppSettings["ServerIP"];
                var port = ConfigurationManager.AppSettings["Port"];
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(ip), Convert.ToInt32(port));
                _asynchronousSocketListener.StartListening(localEndPoint);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception" + ex);
            }
        }
        
        public void Stop()
        {
            _asynchronousSocketListener.Stop();
        }
    }
}
