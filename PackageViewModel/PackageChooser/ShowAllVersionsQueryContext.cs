using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace PackageExplorerViewModel
{
    internal class ShowAllVersionsQueryContext<T> : IQueryContext<T>
    {
        private readonly int _bufferSize;
        private readonly IEqualityComparer<T> _comparer;
        private readonly int _pageSize;
        private readonly Stack<int> _skipHistory = new Stack<int>();
        private readonly IQueryable<T> _source;
        private readonly Lazy<int> _totalItemCount;
        private int _nextSkip;
        private int _skip;

        public ShowAllVersionsQueryContext(IQueryable<T> source, int pageSize, int bufferSize,
                                           IEqualityComparer<T> comparer)
        {
            _source = source;
            _bufferSize = bufferSize;
            _comparer = comparer;
            _pageSize = pageSize;
            _totalItemCount = new Lazy<int>(_source.Count);
        }

        private int PageIndex
        {
            get { return _skipHistory.Count; }
        }

        #region IQueryContext<T> Members

        public int BeginPackage
        {
            get { return Math.Min(_skip + 1, EndPackage); }
        }

        public int EndPackage
        {
            get { return _nextSkip; }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public int TotalItemCount
        {
            get
            {
                try
                {
                    return _totalItemCount.Value;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public IEnumerable<T> GetItemsForCurrentPage()
        {
            T[] buffer = null;
            int skipCursor = _nextSkip = _skip;
            int head = 0;
            for (int i = 0;
                 i < _pageSize && (!_totalItemCount.IsValueCreated || _nextSkip < _totalItemCount.Value);
                 i++)
            {
                bool firstItem = true;
                T lastItem = default(T);
                while (!_totalItemCount.IsValueCreated || _nextSkip < _totalItemCount.Value)
                {
                    if (buffer == null || head >= buffer.Length)
                    {
                        // read the next batch
                        buffer = _source.Skip(skipCursor).Take(_bufferSize).ToArray();
                        if (buffer.Length == 0)
                        {
                            // if no item returned, we have reached the end.
                            yield break;
                        }

                        head = 0;
                        skipCursor += buffer.Length;
                    }

                    if (firstItem || _comparer.Equals(buffer[head], lastItem))
                    {
                        yield return buffer[head];
                        lastItem = buffer[head];
                        head++;
                        firstItem = false;
                        _nextSkip++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public bool MoveFirst()
        {
            _skipHistory.Clear();
            _skip = _nextSkip = 0;
            return true;
        }

        public bool MoveNext()
        {
            if (_nextSkip != _skip && _nextSkip < TotalItemCount)
            {
                _skipHistory.Push(_skip);
                _skip = _nextSkip;
                return true;
            }

            return false;
        }

        public bool MovePrevious()
        {
            if (PageIndex > 0)
            {
                _nextSkip = _skip;
                _skip = _skipHistory.Pop();
                return true;
            }
            return false;
        }

        public bool MoveLast()
        {
            return false;
        }

        #endregion
    }
}