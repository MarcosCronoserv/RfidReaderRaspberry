using System;
using System.Collections.Generic;
using System.Threading;

namespace RfidReaderRaspberry
{
    /// <summary>
    /// Contador thread-safe de tags RFID.
    /// Compativel com Mono no Raspberry Pi.
    /// </summary>
    public class TagCounter
    {
        private readonly Dictionary<string, int> _tagCounts;
        private readonly object _lock = new object();
        private int _totalReads;

        public TagCounter()
        {
            _tagCounts = new Dictionary<string, int>();
            _totalReads = 0;
        }

        public int UniqueTagCount
        {
            get
            {
                lock (_lock)
                {
                    return _tagCounts.Count;
                }
            }
        }

        public int TotalReads
        {
            get { return Thread.VolatileRead(ref _totalReads); }
        }

        public void AddTag(string epc)
        {
            lock (_lock)
            {
                if (_tagCounts.ContainsKey(epc))
                {
                    _tagCounts[epc]++;
                }
                else
                {
                    _tagCounts[epc] = 1;
                }
            }
            Interlocked.Increment(ref _totalReads);
        }

        public void AddTags(IEnumerable<string> epcs)
        {
            int count = 0;
            lock (_lock)
            {
                foreach (string epc in epcs)
                {
                    if (_tagCounts.ContainsKey(epc))
                    {
                        _tagCounts[epc]++;
                    }
                    else
                    {
                        _tagCounts[epc] = 1;
                    }
                    count++;
                }
            }
            Interlocked.Add(ref _totalReads, count);
        }

        public void Clear()
        {
            lock (_lock)
            {
                _tagCounts.Clear();
            }
            Interlocked.Exchange(ref _totalReads, 0);
        }

        public Dictionary<string, int> GetSnapshot()
        {
            lock (_lock)
            {
                return new Dictionary<string, int>(_tagCounts);
            }
        }

        public double GetAverageReadsPerTag()
        {
            lock (_lock)
            {
                if (_tagCounts.Count == 0)
                    return 0;
                return (double)_totalReads / _tagCounts.Count;
            }
        }
    }
}
