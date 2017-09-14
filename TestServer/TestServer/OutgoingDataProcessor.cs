using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace TestServer
{
    class OutgoingDataProcessor
    {
        private readonly AsynchronousSocketListener _asynchronousSocketListener;
        private static readonly BlockingCollection<KeyValuePair<int, List<byte>>> _dataToBeSent = new BlockingCollection<KeyValuePair<int, List<byte>>>();
        private readonly Thread _processorThread;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private static readonly Logger _logger = LogManager.GetLogger("OutgoingDataProcessor");

        public OutgoingDataProcessor(AsynchronousSocketListener asynchronousSocketListener)
        {
            _asynchronousSocketListener = asynchronousSocketListener;
            _cancellationTokenSource = new CancellationTokenSource();
            _processorThread = new Thread(OutgoingDataProcessingThread);
        }

        public void Start()
        {
            _processorThread.Start();
        }

        public static void SendDataToClient(int session, List<byte> data)
        {
            if (data != null)
            {
                AddData(session, data);
            }
        }

        private static void AddData(int session, List<byte> data)
        {
            _logger.Info("DataToSend|ClientSession:" + session + "|Data:" + Convert.ToString(data));
            _dataToBeSent.TryAdd(new KeyValuePair<int, List<byte>>(session, data));
        }

        private void OutgoingDataProcessingThread()
        {
            _logger.Info("Entering OutgoingDataProcessingThread");
            while (true)
            {
                try
                {
                    var dataPair = _dataToBeSent.Take(_cancellationTokenSource.Token);
                    _logger.Info("SendingData|Client:" + dataPair.Key + "|Data: " + dataPair.Value);
                    _asynchronousSocketListener.SendMessageToClient(dataPair.Key, dataPair.Value.ToArray());
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error("Exception" + ex);
                }
            }
            _logger.Info("Exiting OutgoingDataProcessingThread");
        }

        public void Stop()
        {
            try
            {
                _logger.Info("Stopping OutgoingDataProcessor");
                _dataToBeSent?.Dispose();
                _cancellationTokenSource?.Cancel();
                Thread.Sleep(2000);
                _processorThread?.Abort();
                _logger.Info("Stopped OutgoingDataProcessor");
            }
            catch (Exception ex)
            {
                _logger.Error("Exception" + ex);
            }
        }
    }
}
