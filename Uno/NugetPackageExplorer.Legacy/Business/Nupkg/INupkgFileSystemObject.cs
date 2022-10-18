using System;
using System.Collections.Generic;
using System.Text;

namespace NupkgExplorer.Business.Nupkg
{
	public interface INupkgFileSystemObject
	{
		string Name { get; }

		string FullName { get; }
	}
}
