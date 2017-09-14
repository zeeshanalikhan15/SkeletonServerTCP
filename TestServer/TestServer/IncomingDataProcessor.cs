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
    class IncomingDataProcessor
    {
        private readonly BlockingCollection<KeyValuePair<int, List<byte>>> _dataToBeProcessed;
        private readonly Thread _processorThread;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Logger _logger = LogManager.GetLogger("IncomingDataProcessor");

        public IncomingDataProcessor()
        {
            _dataToBeProcessed = new BlockingCollection<KeyValuePair<int, List<byte>>>();
            _cancellationTokenSource = new CancellationTokenSource();
            _processorThread = new Thread(IncomingDataProcessingThread);
        }

        public void Start()
        {
            _processorThread.Start();
        }

        public  void AddData(int session, List<byte> data)
        {
            _logger.Info("RecievedData|ClientSession:" + session + "|Data:" + Convert.ToString(data));
            _dataToBeProcessed.TryAdd(new KeyValuePair<int, List<byte>>(session, data));
        }

        private  void IncomingDataProcessingThread()
        {
            _logger.Info("Entering IncomingDataProcessingThread");
            while (true)
            {
                try
                {
                    var dataPair = _dataToBeProcessed.Take(_cancellationTokenSource.Token);
                    _logger.Info("ProcessingRecievedData|Client:" + dataPair.Key + "|Data: "+ dataPair.Value);
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
            _logger.Info("Exiting IncomingDataProcessingThread");
        }

        public void Stop()
        {
            try
            {
                _logger.Info("Stopping IncomingDataProcessor");
                _dataToBeProcessed?.Dispose();
                _cancellationTokenSource?.Cancel();
                Thread.Sleep(2000);
                _processorThread?.Abort();
                _logger.Info("Stopped IncomingDataProcessor");
            }
            catch (Exception ex)
            {
                _logger.Error("Exception" + ex);
            }
        }
    }
}
