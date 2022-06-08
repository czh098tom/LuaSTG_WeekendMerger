using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeekendMerger
{
    public class MergerException : Exception
    {
        public string SourceFilePath { get; private set; }

        public MergerException(string src) : base()
        {
            SourceFilePath = src;
        }

        public MergerException(string src, string? message) : base(message)
        {
            SourceFilePath = src;
        }

        public MergerException(string src, string? message, Exception? innerException) : base(message, innerException)
        {
            SourceFilePath = src;
        }
    }
}
