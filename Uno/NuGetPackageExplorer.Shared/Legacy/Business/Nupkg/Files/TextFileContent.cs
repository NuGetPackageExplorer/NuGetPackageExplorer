using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NupkgExplorer.Business.Nupkg.Files
{
	public class TextFileContent : IFileContent
	{
		public TextFileContent(Stream stream)
		{
			using (var reader = new StreamReader(stream))
			{
				Text = reader.ReadToEnd();
			}
		}

		public string Text { get; }
	}
}
