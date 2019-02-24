using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using ComIDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using WindowsIDataObject = System.Windows.IDataObject;

namespace PackageExplorer
{
    // code taken from http://dlaa.me/blog/post/9917797
    internal static class NativeDragDrop
    {
        public static readonly string FileGroupDescriptorW = "FileGroupDescriptorW";
        public static readonly string FileContents = "FileContents";

        public static Stream CreateFileGroupDescriptorW(string fileName, DateTimeOffset lastWriteTime, long? fileSize)
        {
            var fileGroupDescriptor = new FILEGROUPDESCRIPTORW() { cItems = 1 };
            var fileDescriptor = new FILEDESCRIPTORW() { cFileName = fileName };
            fileDescriptor.dwFlags |= FD_SHOWPROGRESSUI;

            if (lastWriteTime != default)
            {
                fileDescriptor.dwFlags |= FD_CREATETIME | FD_WRITESTIME;
                var changeTime = lastWriteTime.ToFileTime();
                var changeTimeFileTime = new System.Runtime.InteropServices.ComTypes.FILETIME
                {
                    dwLowDateTime = (int)(changeTime & 0xffffffff),
                    dwHighDateTime = (int)(changeTime >> 32),
                };
                fileDescriptor.ftLastWriteTime = changeTimeFileTime;
                fileDescriptor.ftCreationTime = changeTimeFileTime;
            }

            if (fileSize.HasValue)
            {
                fileDescriptor.dwFlags |= FD_FILESIZE;
                // TODO: remove ! once https://github.com/dotnet/roslyn/issues/33330 is fixed
                fileDescriptor.nFileSizeLow = (uint)(fileSize & 0xffffffff)!;
                fileDescriptor.nFileSizeHigh = (uint)(fileSize >> 32)!;
            }

            var fileGroupDescriptorBytes = StructureBytes(fileGroupDescriptor);
            var fileDescriptorBytes = StructureBytes(fileDescriptor);

            var memoryStream = new MemoryStream();
            memoryStream.Write(fileGroupDescriptorBytes, 0, fileGroupDescriptorBytes.Length);
            memoryStream.Write(fileDescriptorBytes, 0, fileDescriptorBytes.Length);

            return memoryStream;
        }

        private static byte[] StructureBytes<T>(T source) where T : struct
        {
            // Set up for call to StructureToPtr
            var size = Marshal.SizeOf(source.GetType());
            var bytes = new byte[size];

            unsafe
            {
                fixed (byte* p = &MemoryMarshal.GetReference((ReadOnlySpan<byte>)bytes))
                {
                    Marshal.StructureToPtr(source, (IntPtr)p, false);
                }
            }

            return bytes;
        }

        public static IEnumerable<(string FilePath, Stream? Stream)> GetFileGroupDescriptorW(WindowsIDataObject windowsDataObject)
        {
            if (!(windowsDataObject is ComIDataObject))
            {
                yield break;
            }

            var fileNames = GetFileGroupDescriptorWFileNames(windowsDataObject);

            for (var i = 0; i < fileNames.Length; i++)
            {
                Stream? stream = null;
                if (!fileNames[i].IsDirectory)
                {
                    stream = GetStream(windowsDataObject, i);
                }

                yield return (fileNames[i].FileName, stream);
            }
        }

        // https://stackoverflow.com/questions/8709076/drag-and-drop-multiple-attached-file-from-outlook-to-c-sharp-window-form
        private static (string FileName, bool IsDirectory)[] GetFileGroupDescriptorWFileNames(WindowsIDataObject data)
        {
            var fileGroupDescriptorStream = (MemoryStream)data.GetData(FileGroupDescriptorW);
            ReadOnlySpan<byte> fileGroupDescriptorBytes = fileGroupDescriptorStream.ToArray();

            ref var fileGroupDescriptorPtr = ref MemoryMarshal.GetReference(fileGroupDescriptorBytes);
            var fileGroupDescriptor = Unsafe.As<byte, FILEGROUPDESCRIPTORW>(ref fileGroupDescriptorPtr);

            var fileNames = new (string, bool)[fileGroupDescriptor.cItems];
            unsafe
            {
                fixed (byte* pStart = &Unsafe.Add(ref fileGroupDescriptorPtr, Marshal.SizeOf<FILEGROUPDESCRIPTORW>()))
                {
                    var fileDescriptorRowPtr = pStart;
                    for (var fileDescriptorIndex = 0; fileDescriptorIndex < fileGroupDescriptor.cItems; fileDescriptorIndex++)
                    {
                        var fileDescriptor = Marshal.PtrToStructure<FILEDESCRIPTORW>((IntPtr)fileDescriptorRowPtr);

                        var isDirectory = false;
                        if ((fileDescriptor.dwFlags & FD_ATTRIBUTES) == FD_ATTRIBUTES)
                        {
                            if ((fileDescriptor.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY)
                            {
                                isDirectory = true;
                            }
                        }

                        fileNames[fileDescriptorIndex] = (fileDescriptor.cFileName, isDirectory);

                        fileDescriptorRowPtr += Marshal.SizeOf<FILEDESCRIPTORW>();
                    }
                }
            }
            return fileNames;
        }

        // https://stackoverflow.com/questions/8709076/drag-and-drop-multiple-attached-file-from-outlook-to-c-sharp-window-form
        private static Stream? GetStream(WindowsIDataObject windowsObjectData, int index)
        {
            //create a FORMATETC struct to request the data with
            var formatetc = new FORMATETC
            {
                cfFormat = (short)System.Windows.DataFormats.GetDataFormat(FileContents).Id,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = index,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_ISTREAM | TYMED.TYMED_HGLOBAL
            };

            try
            {
                var comObjectData = (ComIDataObject)windowsObjectData;

                //create STGMEDIUM to output request results into
                var medium = new STGMEDIUM();

                //using the Com IDataObject interface get the data using the defined FORMATETC
                comObjectData.GetData(ref formatetc, out medium);

                //retrieve the data depending on the returned store type
                switch (medium.tymed)
                {
                    case TYMED.TYMED_ISTREAM:
                        //to handle a IStream it needs to be read into a managed byte and
                        //returned as a Stream

                        //marshal the returned pointer to a IStream object
                        var iStream = (IStream)Marshal.GetObjectForIUnknown(medium.unionmember);
                        Marshal.Release(medium.unionmember);

                        //get the STATSTG of the IStream to determine how many bytes are in it
                        var iStreamStat = new System.Runtime.InteropServices.ComTypes.STATSTG();
                        iStream.Stat(out iStreamStat, 0); // this will throw for folders

                        //wrapped the IStream in a Stream
                        return new IStreamWrapper(iStream, iStreamStat);

                    case TYMED.TYMED_HGLOBAL:
                        var stream = windowsObjectData.GetData(FileContents);

                        return stream as Stream;
                }
            }
            catch { }

            return null;
        }

        private class IStreamWrapper : Stream
        {
            private readonly IStream _inner;
            private System.Runtime.InteropServices.ComTypes.STATSTG? _stats;

            public IStreamWrapper(IStream istream)
            {
                _inner = istream;
            }

            public IStreamWrapper(IStream istream, System.Runtime.InteropServices.ComTypes.STATSTG stats)
            {
                _inner = istream;
                _stats = stats;
            }

            private System.Runtime.InteropServices.ComTypes.STATSTG GetStats()
            {
                if (_stats == null)
                {
                    _inner.Stat(out var stats, 0);

                    _stats = stats;
                }
                return _stats.Value;
            }

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => true;

            public override long Length => GetStats().cbSize;

            public override long Position
            {
                get;
                set;
            }

            public override void Flush()
            {
                _inner.Commit(0); // https://msdn.microsoft.com/en-us/library/windows/desktop/aa380320%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
            }

            public override unsafe int Read(byte[] buffer, int offset, int count)
            {
                if (offset > 0)
                {
                    buffer = buffer.Skip(offset).ToArray();
                }

                ulong lng = 0;
                var p = &lng;

                _inner.Read(buffer, count, (IntPtr)p);

                Position += count;

                return (int)lng;
            }

            public override unsafe long Seek(long offset, SeekOrigin origin)
            {
                long lng = 0;
                var p = &lng;

                _inner.Seek(offset, (int)origin, (IntPtr)p);

                Position = offset + (int)origin;

                return lng;
            }

            public override void SetLength(long value)
            {
                _inner.SetSize(value);
            }

            public override unsafe void Write(byte[] buffer, int offset, int count)
            {
                if (offset > 0)
                {
                    buffer = buffer.Skip(offset).ToArray();
                }

                long result = 0;
                var p = &result;

                _inner.Write(buffer, count, (IntPtr)p);

                var written = (int)result;

                Position += written;

                if (written != count)
                {
                    Write(buffer, written, count - written);
                }

            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                Marshal.ReleaseComObject(_inner);
            }
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/bb773288(v=vs.85).aspx
        private const uint FD_ATTRIBUTES = 0x00000004;
        private const uint FD_CREATETIME = 0x00000008;
        private const uint FD_WRITESTIME = 0x00000020;
        private const uint FD_FILESIZE = 0x00000040;
        private const uint FD_SHOWPROGRESSUI = 0x00004000;

        // https://msdn.microsoft.com/en-us/library/windows/desktop/gg258117(v=vs.85).aspx
        private const int FILE_ATTRIBUTE_DIRECTORY = 0x10;

#pragma warning disable IDE1006 // Naming Styles
        // https://msdn.microsoft.com/en-us/library/windows/desktop/bb773290(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct FILEGROUPDESCRIPTORW
        {
            public uint cItems;
            // Followed by 0 or more FILEDESCRIPTORs
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/bb773288(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct FILEDESCRIPTORW
        {
            public uint dwFlags;
            public Guid clsid;
            public int sizelcx;
            public int sizelcy;
            public int pointlx;
            public int pointly;
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
        }
#pragma warning restore IDE1006 // Naming Styles
    }
}
