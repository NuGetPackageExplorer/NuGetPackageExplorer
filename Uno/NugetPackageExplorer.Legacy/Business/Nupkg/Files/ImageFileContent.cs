using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Uno.Extensions;
using Uno.Logging;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace NupkgExplorer.Business.Nupkg.Files
{
	public class ImageFileContent : IFileContent
	{
		public ImageSource Source { get; }

		public ImageFileContent(Stream stream)
		{
			try
			{
				using (var memory = new MemoryStream())
				{
					stream.CopyTo(memory);
					memory.Seek(0, SeekOrigin.Begin);

					var bitmap = new BitmapImage();
#if __WASM__ || __IOS__ || __ANDROID__ || __MACOS__
					_ = bitmap.SetSourceAsync(memory);
#else
					_ = bitmap.SetSourceAsync(memory.AsRandomAccessStream());
#endif

					Source = bitmap;
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to load image from stream", e);
				throw;
			}
		}
	}
}
