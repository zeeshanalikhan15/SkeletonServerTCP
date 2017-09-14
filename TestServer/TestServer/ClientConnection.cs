using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace TestServer
{
    public delegate void ClientConnectionClosed(int session);

    class ClientConnection
    {
        public readonly Socket Socket;
        private readonly int _session;
        public event ClientConnectionClosed ClientConnectionClosed;
        private readonly Logger _logger = LogManager.GetLogger("ClientConnection");
        private readonly IncomingDataProcessor _incomingDataProcessor;

        public ClientConnection(Socket socket, int session, IncomingDataProcessor incomingDataProcessor)
        {
            Socket = socket;
            _session = session;
            _incomingDataProcessor = incomingDataProcessor;
        }

        public void Start()
        {
            var state = new StateObject();
            Socket.BeginReceive(state.Buffer, 0, StateObject.BufferSize, SocketFlags.None, ReadCallback, state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                var bytesRead = Socket.EndReceive(ar);
                if (bytesRead > 0)
                {
                    _logger.Info("Session:" + _session + "|DataRecieved:" + bytesRead);
                    ReadData(bytesRead, state);
                    DelegateDataToUperLayer(state);
                    Socket.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);
                }
                else
                {
                    if (state.ResultantBuffer.Count > 0)
                    {
                        _logger.Error("ClosingSocket|DataSize:" + state.ResultantBuffer.Count + "|Invalid Data:" + state.Buffer);
                    }
                    Stop();
                }
            }
            catch (Exception ex)
            {
                Stop();
                _logger.Error("Exception:" + ex);
            }
        }

        private void DelegateDataToUperLayer(StateObject state)
        {
            if (state.ResultantBuffer.Count >= StateObject.MaxDataSize)
            {
                _incomingDataProcessor.AddData(_session, state.ResultantBuffer.GetRange(0, StateObject.MaxDataSize).ToList());
                state.ResultantBuffer.RemoveRange(0, StateObject.MaxDataSize);
            }
        }

        private void ReadData(int bytesToRead, StateObject state)
        {
            var data = Program.SubArray(state.Buffer, 0, bytesToRead);
            state.ResultantBuffer.AddRange(data);
        }

        public void Send(byte[] byteData)
        {
            //byte[] byteData = Program.SubArray(data, 0, data.Length);
            Socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), Socket);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);
                _logger.Info("Sent {0} bytes to client.", bytesSent);

                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();

            }
            catch (Exception e)
            {
                Stop();
                _logger.Error(e.ToString());
            }
        }

        public void Stop()
        {
            try
            {
                _logger.Info("ClosingSocket|ClientSession:" + _session);
                Socket.Close();
                Socket.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }
            finally
            {
                ClientConnectionClosed?.Invoke(_session);
            }
        }
    }
}
