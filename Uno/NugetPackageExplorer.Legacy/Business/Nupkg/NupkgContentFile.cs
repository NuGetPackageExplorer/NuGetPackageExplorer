using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using NupkgExplorer.Business.Nupkg.Files;
using Uno.Extensions;
using Uno.Logging;

namespace NupkgExplorer.Business.Nupkg
{
	public class NupkgContentFile : INupkgFileSystemObject
	{
		public string Name { get; }

		public string FullName { get; }

		public long Length { get; }

		public IFileContent Content => _content.Value;

		private readonly Lazy<IFileContent> _content;
		private readonly ZipArchiveEntry _entry;

		public NupkgContentFile(ZipArchiveEntry entry)
		{
			_entry = entry;
			_content = new Lazy<IFileContent>(LoadContent);

			Name = entry.Name;
			FullName = entry.FullName;
			Length = entry.Length;
		}

		IFileContent LoadContent()
		{
			using (var stream = _entry.Open())
			{
				try
				{
                    return Path.GetExtension(Name) switch
                    {
                        ".md" or ".xml" => new TextFileContent(stream),
                        ".png" => new ImageFileContent(stream),
                        ".dll" => new AssemblyFileContent(stream),
                        _ => new TextFileContent(stream),
                    };
                    ;
				}
				catch (Exception e)
				{
					this.Log().Error($"Failed to parse file content: {FullName}", e);
					throw;
				}
			}
		}
	}
}
