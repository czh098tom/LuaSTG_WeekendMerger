using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WeekendMerger
{
    public class ZipPackage : IDisposable
    {
        readonly ZipFile Zip;

        public string Password
        {
            set => Zip.Password = value;
        }

        public string Comment
        {
            get => Zip.ZipFileComment;
            set => Zip.SetComment(value);
        }

        IEnumerable<ZipEntry> ZipEntries
        {
            get
            {
                List<ZipEntry> files = new();
                for (int i = 0; i < Zip.Count; i++)
                {
                    files.Add(Zip[i]);
                }
                return files;
            }
        }

        public string[] Files => ZipEntries.Where(x => x.IsFile).Select(x => x.Name).ToArray();

        public Stream GetFileStream(string file)
        {
            var entry = Zip.GetEntry(file);
            var stream = new MemoryStream();
            Stream input = Zip.GetInputStream(entry);
            byte[] buffer = new byte[2048];
            int length;
            while ((length = input.Read(buffer, 0, 2048)) > 0)
                stream.Write(buffer, 0, length);
            stream.Seek(0, SeekOrigin.Begin);
            input.Close();
            return stream;
        }

        public void SaveTo(string path, string? password = null)
        {
            using var file = new ZipFile(path);
            if (password != null)
                file.Password = password;
            file.BeginUpdate();
            foreach (ZipEntry entry in Zip)
                file.Add(entry);
            file.CommitUpdate();
            file.Close();
        }

        #region 构造
        ZipPackage(ZipFile file) {
            Zip = file;
        }

        ~ZipPackage()
            => Dispose();

        public void Dispose()
        {
            Zip?.Close();
            GC.SuppressFinalize(this);
        }

        public static ZipPackage OpenFile(string path, string? password = null)
        {
            var package = new ZipPackage(new ZipFile(path));
            if (password != null)
                package.Password = password;
            return package;
        }

        public static ZipPackage OpenStream(Stream stream, string? password = null)
        {
            var package = new ZipPackage(new ZipFile(stream));
            if (password != null)
                package.Password = password;
            return package;
        }

        public static ZipPackage Create(string? password = null)
        {
            var package = new ZipPackage(ZipFile.Create(new MemoryStream()));
            if (password != null)
                package.Password = password;
            return package;
        }
        #endregion
    }
}
