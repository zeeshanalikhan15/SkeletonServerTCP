using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace TestServer
{
    class AsynchronousSocketListener
    {
        private readonly AutoResetEvent _listenAgain = new AutoResetEvent(false);
        private Socket _listenSocket;
        private readonly ConcurrentDictionary<int, ClientConnection> _clientConnections = new ConcurrentDictionary<int, ClientConnection>();
        private readonly SessionIdManager _sessionIdManager = new SessionIdManager();
        private readonly Logger _logger = LogManager.GetLogger("AsynchronousSocketListener");
        private readonly IncomingDataProcessor _incomingDataProcessor = new IncomingDataProcessor();
        private bool _stop;

        public void StartListening(IPEndPoint ipEndPoint)
        {
            try
            {
                _logger.Info("Starting Server");
                _incomingDataProcessor.Start();
                _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listenSocket.Bind(ipEndPoint);
                _listenSocket.Listen(100);
                _logger.Info("Started Server");
                while (!_stop)
                {
                    _listenSocket.BeginAccept(AcceptCallback, _listenSocket);
                    _listenAgain.WaitOne();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception:" + ex);
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            _listenAgain.Set();
            var listener = (Socket)ar.AsyncState;
            var handler = listener.EndAccept(ar);
            _logger.Info("Reciving Connection|Socket:" + handler);
            var sessionId = _sessionIdManager.GetRequestId();
            var clientSession = new ClientConnection(handler, sessionId, _incomingDataProcessor);
            clientSession.ClientConnectionClosed += CloseClientConnection;
            _clientConnections.TryAdd(sessionId, clientSession);
            clientSession.Start();
            _logger.Info("Connection Recieved|Socket:" + handler + "|ClientSession:" + sessionId);
        }

        private void CloseClientConnection(int sessionId)
        {
            try
            {
                ClientConnection clientConnection;
                _clientConnections.TryRemove(sessionId, out clientConnection);
                _sessionIdManager.ReleaseRquestId(sessionId);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception:" + ex);
            }
        }

        public void SendMessageToClient(int sessionId, byte[] message)
        {
            try
            {
                ClientConnection clientConnection;
                if (_clientConnections.TryGetValue(sessionId, out clientConnection))
                {
                    clientConnection.Send(message);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception:" + ex);
            }
        }

        public void Stop()
        {
            try
            {
                _logger.Info("Closing Server");
                _stop = true;
                _listenSocket?.Close();
                foreach (var clientConnection in _clientConnections)
                {
                    clientConnection.Value.Stop();
                }
                _clientConnections.Clear();
                _incomingDataProcessor.Stop();
                _logger.Info("Server Closed");
            }
            catch (Exception ex)
            {
                _logger.Error("Exception:" + ex);
            }
        }
    }
}
