using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static Stream CreateFileGroupDescriptorW(string fileName)
        {
            var fileGroupDescriptor = new FILEGROUPDESCRIPTORW() { cItems = 1 };
            var fileDescriptor = new FILEDESCRIPTORW() { cFileName = fileName };
            fileDescriptor.dwFlags |= FD_SHOWPROGRESSUI;

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
            var ptr = Marshal.AllocHGlobal(size);
            var bytes = new byte[size];
            try
            {
                Marshal.StructureToPtr(source, ptr, false);
                // Copy marshalled bytes to buffer
                Marshal.Copy(ptr, bytes, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return bytes;
        }

        public static IEnumerable<KeyValuePair<string, Stream>> GetFileGroupDescriptorW(WindowsIDataObject windowsDataObject)
        {
            if (!(windowsDataObject is ComIDataObject)) yield break;

            var fileNames = GetFileGroupDescriptorWFileNames(windowsDataObject);

            for (var i = 0; i < fileNames.Length; i++)
            {
                var stream = GetStream(windowsDataObject, i);

                yield return new KeyValuePair<string, Stream>(fileNames[i], stream);
            }
        }

        // https://stackoverflow.com/questions/8709076/drag-and-drop-multiple-attached-file-from-outlook-to-c-sharp-window-form
        private static string[] GetFileGroupDescriptorWFileNames(WindowsIDataObject data)
        {
            var fileGroupDescriptorWPointer = IntPtr.Zero;
            try
            {
                var fileGroupDescriptorStream = (MemoryStream)data.GetData(FileGroupDescriptorW);
                var fileGroupDescriptorBytes = fileGroupDescriptorStream.ToArray();

                //copy the file group descriptor into unmanaged memory
                fileGroupDescriptorWPointer = Marshal.AllocHGlobal(fileGroupDescriptorBytes.Length);
                Marshal.Copy(fileGroupDescriptorBytes, 0, fileGroupDescriptorWPointer, fileGroupDescriptorBytes.Length);

                //marshal the unmanaged memory to to FILEGROUPDESCRIPTORW struct
                var fileGroupDescriptor = Marshal.PtrToStructure<FILEGROUPDESCRIPTORW>(fileGroupDescriptorWPointer);

                //create a new array to store file names in of the number of items in the file group descriptor
                var fileNames = new string[fileGroupDescriptor.cItems];

                //get the pointer to the first file descriptor
                var fileDescriptorPointer = (IntPtr)((long)fileGroupDescriptorWPointer + Marshal.SizeOf(fileGroupDescriptor.cItems));

                //loop for the number of files acording to the file group descriptor
                for (var fileDescriptorIndex = 0; fileDescriptorIndex < fileGroupDescriptor.cItems; fileDescriptorIndex++)
                {
                    //marshal the pointer top the file descriptor as a FILEDESCRIPTORW struct and get the file name
                    var fileDescriptor = Marshal.PtrToStructure<FILEDESCRIPTORW>(fileDescriptorPointer);
                    fileNames[fileDescriptorIndex] = fileDescriptor.cFileName;

                    //move the file descriptor pointer to the next file descriptor
                    fileDescriptorPointer = (IntPtr)((long)fileDescriptorPointer + Marshal.SizeOf(fileDescriptor));
                }

                return fileNames;
            }
            finally
            {
                //free unmanaged memory pointer
                Marshal.FreeHGlobal(fileGroupDescriptorWPointer);
            }
        }

        // https://stackoverflow.com/questions/8709076/drag-and-drop-multiple-attached-file-from-outlook-to-c-sharp-window-form
        private static Stream GetStream(WindowsIDataObject windowsObjectData, int index)
        {
            //create a FORMATETC struct to request the data with
            var formatetc = new FORMATETC();
            formatetc.cfFormat = (short)System.Windows.DataFormats.GetDataFormat(FileContents).Id;
            formatetc.dwAspect = DVASPECT.DVASPECT_CONTENT;
            formatetc.lindex = index;
            formatetc.ptd = IntPtr.Zero;
            formatetc.tymed = TYMED.TYMED_ISTREAM | TYMED.TYMED_HGLOBAL;

            try
            {
                var comObjectData = (ComIDataObject)windowsObjectData;

                if (comObjectData.QueryGetData(ref formatetc) <= 0)
                {
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
                    var stats = new System.Runtime.InteropServices.ComTypes.STATSTG();
                    _inner.Stat(out stats, 0);

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
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }

            public override void Flush()
            {
                _inner.Commit(0); // https://msdn.microsoft.com/en-us/library/windows/desktop/aa380320%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<ulong>());
                try
                {
                    if (offset > 0)
                    {
                        buffer = buffer.Skip(offset).ToArray();
                    }

                    _inner.Read(buffer, count, ptr);

                    return (int)Marshal.ReadInt64(ptr);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<ulong>());
                try
                {
                    _inner.Seek(offset, (int)origin, ptr);

                    return Marshal.ReadInt64(ptr);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }

            public override void SetLength(long value)
            {
                _inner.SetSize(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<ulong>());
                try
                {
                    if (offset > 0)
                    {
                        buffer = buffer.Skip(offset).ToArray();
                    }

                    _inner.Write(buffer, count, ptr);

                    var written = (int)Marshal.ReadInt64(ptr);

                    if (written != count)
                    {
                        Write(buffer, written, count - written);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                Marshal.ReleaseComObject(_inner);
            }
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/bb773288(v=vs.85).aspx
        private const uint FD_SHOWPROGRESSUI = 0x00004000;

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
    }
}
