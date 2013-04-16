using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NuGet;

namespace PackageExplorerViewModel
{
    internal class ShowAllVersionsQueryContext<T> : QueryContextBase<T>, IQueryContext<T> where T : IPackageInfoType
    {
        private readonly int _bufferSize;
        private readonly IEqualityComparer<T> _equalityComparer;
        private readonly Comparison<T> _comparer;
        private readonly int _pageSize;
        private readonly Stack<int> _skipHistory = new Stack<int>();
        private int _nextSkip;
        private int _skip;

        public ShowAllVersionsQueryContext(
            IQueryable<T> source, 
            int pageSize, 
            int bufferSize, 
            IEqualityComparer<T> equalityComparer,
            Comparison<T> comparer)
            : base(source)
        {
            _bufferSize = bufferSize;
            _equalityComparer = equalityComparer;
            _comparer = comparer;
            _pageSize = pageSize;
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

        public IEnumerable<T> GetItemsForCurrentPage()
        {
            T[] buffer = null;
            var groupItems = new List<T>();

            int skipCursor = _nextSkip = _skip;
            int head = 0;
            for (int i = 0;
                 i < _pageSize && (!TotalItemCountReady || _nextSkip < TotalItemCount);
                 i++)
            {
                bool firstItem = true;
                T lastItem = default(T);
                while (!TotalItemCountReady || _nextSkip < TotalItemCount)
                {
                    if (buffer == null || head >= buffer.Length)
                    {
                        // read the next batch
                        var pagedQuery = Source.Skip(skipCursor).Take(_bufferSize);
                        buffer = LoadData(pagedQuery).ToArray();
                        if (buffer.Length == 0)
                        {
                            // if no item returned, we have reached the end.
                            yield break;
                        }

                        for (int j = 0; j < buffer.Length; j++)
                        {
                            buffer[j].ShowAll = true;
                        }

                        head = 0;
                        skipCursor += buffer.Length;
                    }

                    if (firstItem || _equalityComparer.Equals(buffer[head], lastItem))
                    {
                        groupItems.Add(buffer[head]);
                        lastItem = buffer[head];                            
                        firstItem = false;

                        head++;
                        _nextSkip++;
                    }
                    else
                    {
                        if (groupItems.Count > 0)
                        {
                            groupItems.Sort(_comparer);
                            foreach (T item in groupItems)
                            {
                                yield return item;
                            }
                            groupItems.Clear();
                        }

                        break;
                    }
                }
            }

            if (groupItems.Count > 0)
            {
                groupItems.Sort(_comparer);
                foreach (T item in groupItems)
                {
                    yield return item;
                }
                groupItems.Clear();
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