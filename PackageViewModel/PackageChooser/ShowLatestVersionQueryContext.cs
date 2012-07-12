using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NuGet;

namespace PackageExplorerViewModel
{
    internal class ShowLatestVersionQueryContext<T> : QueryContextBase<T>, IQueryContext<T> where T : IPackageInfoType
    {
        private readonly int _pageSize;
        private int _pageIndex;

        public ShowLatestVersionQueryContext(IQueryable<T> source, int pageSize) 
            : base(source)
        {
            _pageSize = pageSize;
        }

        private int PageCount
        {
            get { return (TotalItemCount + (_pageSize - 1)) / _pageSize; }
        }

        #region IQueryContext<T> Members

        public int BeginPackage
        {
            get { return Math.Min(TotalItemCount, _pageIndex * _pageSize + 1); }
        }

        public int EndPackage
        {
            get { return Math.Min(TotalItemCount, (_pageIndex + 1) * _pageSize); }
        }

        public IEnumerable<T> GetItemsForCurrentPage()
        {
            var pagedQuery = Source.Skip(_pageIndex * _pageSize).Take(_pageSize);
            var queryResponse = LoadData(pagedQuery);
            foreach (var package in queryResponse)
            {
                package.ShowAll = false;
                yield return package;
            }
        }

        public bool MoveFirst()
        {
            _pageIndex = 0;
            return true;
        }

        public bool MoveNext()
        {
            if (_pageIndex < PageCount - 1)
            {
                _pageIndex++;
                return true;
            }

            return false;
        }

        public bool MovePrevious()
        {
            if (_pageIndex > 0)
            {
                _pageIndex--;
                return true;
            }
            return false;
        }

        public bool MoveLast()
        {
            _pageIndex = PageCount - 1;
            return true;
        }

        #endregion
    }
}