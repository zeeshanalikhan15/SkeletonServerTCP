using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace TestServer
{
    public class SessionIdManager
    {
        private readonly int _seed;
        private int _nextRequestId;
        private readonly int _capacity;
        private readonly ConcurrentDictionary<int, int> _requestIdList;
        private readonly Logger _logger = LogManager.GetLogger("SessionManager");

        public SessionIdManager()
        {
            _nextRequestId = _seed = 1;
            _requestIdList = new ConcurrentDictionary<int, int>();
            _capacity = Int32.MaxValue;
        }

        public int GetRequestId()
        {
            try
            {
                while (!_requestIdList.TryAdd(_nextRequestId, _nextRequestId))
                {

                    if (_nextRequestId >= _capacity)
                    {
                        _nextRequestId = _seed - 1;
                    }
                    _nextRequestId++;
                }

                return _nextRequestId++;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception" + ex);
            }

            return -1;
        }

        public void ReleaseRquestId(int requestId)
        {
            try
            {
                _requestIdList.TryRemove(requestId, out requestId);

            }
            catch (Exception ex)
            {
                _logger.Error("Exception" + ex);
            }
        }

        public void Reset()
        {
            try
            {
                _requestIdList.Clear();
                _nextRequestId = _seed;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception" + ex);
            }
        }

    }
}
