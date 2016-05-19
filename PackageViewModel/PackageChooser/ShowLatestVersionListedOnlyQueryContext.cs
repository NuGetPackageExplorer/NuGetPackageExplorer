using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NuGet;

namespace PackageExplorerViewModel
{
    internal class ShowLatestVersionsListedOnlyQueryContext<T> : IQueryContext<T> where T : IPackageInfoType
    {
        private readonly int _bufferSize;
        private readonly int _pageSize;
        private readonly Stack<int> _skipHistory = new Stack<int>();
        private readonly IQueryable<T> _source;
        private readonly Lazy<int> _totalItemCount;
        private int _nextSkip;
        private int _skip;

        public ShowLatestVersionsListedOnlyQueryContext(IQueryable<T> source, int pageSize)
        {
            _source = source;
            _bufferSize = pageSize;
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
            _nextSkip = _skip;
            int head = 0;
            for (int i = 0;
                 i < _pageSize && (!_totalItemCount.IsValueCreated || _nextSkip < _totalItemCount.Value);
                 i++)
            {
                while (!_totalItemCount.IsValueCreated || _nextSkip < _totalItemCount.Value)
                {
                    if (buffer == null || head >= buffer.Length)
                    {
                        // read the next batch
                        buffer = _source.Skip(_nextSkip).Take(_bufferSize).ToArray();
                        if (buffer.Length == 0)
                        {
                            // if no item returned, we have reached the end.
                            yield break;
                        }

                        for (int j = 0; j < buffer.Length; j++)
                        {
                            buffer[j].ShowAll = false;
                        }

                        head = 0;
                    }

                    if (buffer[head].IsUnlisted)
                    {
                        head++;
                        _nextSkip++;
                    }
                    else
                    {
                        yield return buffer[head];
                        head++;
                        _nextSkip++;
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