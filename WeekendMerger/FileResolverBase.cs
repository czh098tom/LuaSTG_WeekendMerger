using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace WeekendMerger
{
    public abstract class FileResolverBase
    {
        public string File { get; protected set; }
        public string Dir { get; protected set; }

        public FileResolverBase(string file, string dir)
        {
            File = file;
            Dir = dir;
        }

        protected string GetAuthorName()
        {
            string? dirName = Path.GetFileName(Dir);
            if (string.IsNullOrEmpty(dirName)) throw new ArgumentException($"Parameter \"{Dir}\" is not a valid directory", nameof(Dir));
            return Regex.Match(dirName, @"(?<=_weekend_)(.*)", RegexOptions.IgnoreCase).Value;
        }

        public abstract void Resolve();

        protected void Write(StringBuilder sb)
        {
            using FileStream fs = new(File, FileMode.Create, FileAccess.Write);
            using StreamWriter sw = new(fs, Encoding.UTF8);
            sw.Write(sb);
        }
    }
}
