using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json.Linq;

namespace WeekendMerger
{
    public class Mergerer
    {
        private static readonly JObject patchFile = JObject.Parse(@"{""$type"":"".Project.ProjectFile, LuaSTGEditorSharp"",""Attributes"":[{""attrCap"":""Path"",""attrInput"":"""",""EditWindow"":""lstgesFile""}],""AttributeCount"":1}");

        private readonly string directory;

        public Mergerer(string dir)
        {
            directory = dir;
        }

        public void Merge(string weekName)
        {
            string? name = Path.GetFileName(directory);
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"Parameter \"{directory}\" is not a valid directory", nameof(directory));
            }
            using StreamWriter sw = new(Path.Combine(directory, $"{name}.lstgproj"));
            using (StreamReader sr = new(Program.TemplatePath))
            {
                sw.Write(string.Format(sr.ReadToEnd(), weekName));
            }
            foreach (string file in Directory.EnumerateFiles(directory, "*.zip"))
            {
                Console.WriteLine("[Decompress]" + file);
                var folder = Path.Combine(directory, Path.GetFileNameWithoutExtension(file));
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
                Directory.CreateDirectory(folder);
                var zip = ZipPackage.OpenFile(file);
                foreach (var zipfile in zip.Files)
                {
                    var subPath = Path.GetDirectoryName(zipfile);
                    if (subPath != null)
                    {
                        subPath = Path.Combine(folder, subPath);
                        if (!File.Exists(subPath))
                        {
                            Directory.CreateDirectory(subPath);
                        }
                    }
                    using MemoryStream zipStream = (MemoryStream)zip.GetFileStream(zipfile);
                    File.WriteAllBytes(Path.Combine(folder, zipfile), zipStream.ToArray());
                    zipStream.Dispose();
                }
            }
            foreach (string dir in Directory.EnumerateDirectories(directory).Shuffle())
            {
                if (Directory.EnumerateFiles(dir, "*.lstges").Any())
                {
                    SubFileManipulator mani = new(dir);
                    mani.Resolve();
                    JToken clone = patchFile.DeepClone();
                    JToken? jt = clone["Attributes"]?[0];
                    if (jt != null)
                    {
                        string target = Path.Combine(Path.GetFileName(Path.GetDirectoryName(mani.File))
                            ?? throw new ArgumentException($"Parameter \"{mani.File}\" is not a valid directory")
                            , Path.GetFileName(mani.File));
                        jt["attrInput"] = target;
                        sw.WriteLine($"{1},{clone.ToString(Newtonsoft.Json.Formatting.None)}");
                    }
                }
                else
                {
                    IssueTracker.Instance.Report(new MergerException($"Directory \"{dir}\" does not contains any .lstges file"));
                }
            }
        }
    }
}
