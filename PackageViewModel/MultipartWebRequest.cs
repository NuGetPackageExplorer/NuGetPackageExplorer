using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PackageExplorerViewModel
{
    /// <remarks>
    /// Based on the blog post by Travis Illig at http://www.paraesthesia.com/archive/2009/12/16/posting-multipartform-data-using-.net-webrequest.aspx
    /// </remarks>
    internal class MultipartWebRequest
    {
        private const string FormDataTemplate = "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}\r\n";
        private const string FileTemplate = "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n";
        private readonly Dictionary<string, string> _formData;

        private readonly List<PostFileData> _files;

        public MultipartWebRequest()
            : this(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
        {
        }

        public MultipartWebRequest(Dictionary<string, string> formData)
        {
            _formData = formData;
            _files = new List<PostFileData>();
        }

        public void AddFile(FileInfo fileInfo, string fieldName, string contentType = "application/octet-stream")
        {
            _files.Add(new PostFileData
                        {
                            FileInfo = fileInfo,
                            FieldName = fieldName,
                            ContentType = contentType
                        });
        }

        public async Task CreateMultipartRequest(HttpWebRequest request)
        {
            var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.ContentLength = CalculateContentLength(boundary);

            using (var stream = await Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, state: null))
            {
                foreach (var item in _formData)
                {
                    var header = string.Format(CultureInfo.InvariantCulture, FormDataTemplate, boundary, item.Key, item.Value);
                    var headerBytes = Encoding.UTF8.GetBytes(header);
                    stream.Write(headerBytes, 0, headerBytes.Length);
                }

                var newlineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);
                foreach (var file in _files)
                {
                    var header = string.Format(CultureInfo.InvariantCulture, FileTemplate, boundary, file.FieldName, file.FieldName, file.ContentType);
                    var headerBytes = Encoding.UTF8.GetBytes(header);
                    stream.Write(headerBytes, 0, headerBytes.Length);

                    Stream fileStream = file.FileInfo.OpenRead();
                    fileStream.CopyTo(stream, bufferSize: 4 * 1024);
                    fileStream.Close();
                    stream.Write(newlineBytes, 0, newlineBytes.Length);
                }

                var trailer = string.Format(CultureInfo.InvariantCulture, "--{0}--", boundary);
                var trailerBytes = Encoding.UTF8.GetBytes(trailer);
                stream.Write(trailerBytes, 0, trailerBytes.Length);
            }
        }

        private long CalculateContentLength(string boundary)
        {
            long totalContentLength = 0;

            foreach (var item in _formData)
            {
                var header = string.Format(CultureInfo.InvariantCulture, FormDataTemplate, boundary, item.Key, item.Value);
                var headerBytes = Encoding.UTF8.GetBytes(header);

                totalContentLength += headerBytes.Length;
            }

            var newlineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);
            foreach (var file in _files)
            {
                var header = string.Format(CultureInfo.InvariantCulture, FileTemplate, boundary, file.FieldName, file.FieldName, file.ContentType);
                var headerBytes = Encoding.UTF8.GetBytes(header);

                totalContentLength += headerBytes.Length;
                totalContentLength += file.FileInfo.Length;
                totalContentLength += newlineBytes.Length;
            }

            var trailer = string.Format(CultureInfo.InvariantCulture, "--{0}--", boundary);
            var trailerBytes = Encoding.UTF8.GetBytes(trailer);

            totalContentLength += trailerBytes.Length;

            return totalContentLength;
        }

        private sealed class PostFileData
        {
            public FileInfo FileInfo { get; set; }
            public string ContentType { get; set; }
            public string FieldName { get; set; }
        }
    }
}