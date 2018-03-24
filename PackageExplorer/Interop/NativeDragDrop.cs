using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PackageExplorer
{
    // code taken from http://dlaa.me/blog/post/9917797
    internal static class NativeDragDrop
    {
        public static readonly string FileGroupDescriptorW = "FileGroupDescriptorW";
        public static readonly string FileContents = "FileContents";

        public static Stream CreateFileGroupDescriptorW(string fileName)
        {
            var fileGroupDescriptor = new FILEGROUPDESCRIPTOR() { cItems = 1 };
            var fileDescriptor = new FILEDESCRIPTOR() { cFileName = fileName };

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

        // https://msdn.microsoft.com/en-us/library/windows/desktop/bb773290%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
        [StructLayout(LayoutKind.Sequential)]
        private struct FILEGROUPDESCRIPTOR
        {
            public UInt32 cItems;
            // Followed by 0 or more FILEDESCRIPTORs
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/bb773288%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct FILEDESCRIPTOR
        {
            public UInt32 dwFlags;
            public Guid clsid;
            public Int32 sizelcx;
            public Int32 sizelcy;
            public Int32 pointlx;
            public Int32 pointly;
            public UInt32 dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public UInt32 nFileSizeHigh;
            public UInt32 nFileSizeLow;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
        }
    }
}
