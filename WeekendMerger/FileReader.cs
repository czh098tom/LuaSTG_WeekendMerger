using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeekendMerger
{
    public record struct FilePos(int Line, int Level, JObject Content, bool HasBanned)
    {
        public override string ToString()
        {
            return $"{Level},{Content.ToString(Formatting.None)}";
        }
    }

    public class FileReader
    {
        public static IEnumerable<FilePos> EnumerateNodes(string file)
        {
            using StreamReader sr = new(file, Encoding.UTF8);
            int pos = 0;
            bool hasBanned = false;
            int bannedLevel = 0;
            while (!sr.EndOfStream)
            {
                char[]? temp = sr.ReadLine()?.ToCharArray();
                if (temp == null) throw new IOException();
                int i = 0;
                while (temp[i] != ',') i++;
                string des = new(temp, i + 1, temp.Length - i - 1);
                int level = Convert.ToInt32(new string(temp, 0, i));
                JObject obj = JObject.Parse(des);
                if (hasBanned)
                {
                    if (level <= bannedLevel)
                    {
                        if ((bool?)obj["IsBanned"] == true)
                        {
                            hasBanned = true;
                            bannedLevel = level;
                        }
                        else
                        {
                            hasBanned = false;
                        }
                    }
                }
                else
                {
                    if ((bool?)obj["IsBanned"] == true)
                    {
                        hasBanned = true;
                        bannedLevel = level;
                    }
                }
                yield return new FilePos(pos, level, obj, hasBanned);
                pos++;
            }
        }
    }
}
