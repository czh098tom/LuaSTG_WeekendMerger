// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;

namespace WeekendMerger
{
    public class Program
    {
        public static string TemplatePath { get; private set; } = "weekend_template";
        private static Dictionary<string, string> authorDict = new();

        public static string GetAuthorFullName(string name)
        {
            string lowername = name.ToLower();
            if (authorDict.ContainsKey(lowername))
            {
                return authorDict[lowername];
            }
            return name;
        }

        public static void Main(string[] args)
        {
            ArgsResolver ar;
            try
            {
                ar = new(new ArgsGrouper(args));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " type -h for more info.");
                return;
            }
            bool help = ar.IsHelp;
            if (help)
            {
                Console.WriteLine("WeekendMerger [path][-n name][-j json][-h]");
                Console.WriteLine();
                Console.WriteLine("\tpath\t\tPath of the target");
                Console.WriteLine();
                Console.WriteLine("\t-n\tname\tIf given, rename the stages and icon shown for specific weekend.");
                Console.WriteLine("\t\t\tDefault is X in directory name X_weekend or simple for X.");
                Console.WriteLine();
                Console.WriteLine("\t-o\tdir\tIf given, use this path to save and build the merged project.");
                Console.WriteLine();
                Console.WriteLine("\t-j\tjson\tIf given, use path in [json] for the table of characters.");
                Console.WriteLine();
                Console.WriteLine("\t-h\t\tShow help.");
                Console.WriteLine();
            }
            if (string.IsNullOrEmpty(ar.Path)) return;
            string dir = ar.Path;
            string output_dir = ar.OutputPath ?? dir;
            string nameCHPath = ar.NameJSONPath ?? "nameCH.json";
            string weekName = ar.WeekName ?? Regex.Match(Path.GetFileName(dir), @"(?<=_[Ww]eekend)?(.*)").Value;
            using (StreamReader sr = new(nameCHPath))
            {
                Dictionary<string, string>? dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(sr.ReadToEnd());
                if (dict != null)
                {
                    authorDict = dict;
                }
            }
            var m = new Mergerer(dir, output_dir);
            try
            {
                m.Merge(weekName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.WriteLine();
            IssueTracker.Instance.Show();
        }
    }
}