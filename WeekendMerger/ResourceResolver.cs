using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json.Linq;

namespace WeekendMerger
{
    public class ResourceResolver : FileResolverBase
    {
        public static readonly HashSet<string> resTypes = new()
        {
            ".General.AddFile, LuaSTGEditorSharp",
            ".General.Patch, LuaSTGEditorSharp",
            ".Audio.LoadBGM, ",
            ".Audio.LoadSE, ",
            ".Boss.SetBossWalkImageSystem, ",
            ".Graphics.LoadAnimation, ",
            ".Graphics.LoadFX, ",
            ".Graphics.LoadImage, ",
            ".Graphics.LoadImageGroup, ",
            ".Graphics.LoadParticle, ",
        };

        public ResourceResolver(string file, string dir) : base(file, dir) { }

        public override void Resolve()
        {
            StringBuilder sb = new();
            foreach (FilePos fp in FileReader.EnumerateNodes(File))
            {
                string? type = (string?)(fp.Content["$type"]);
                if (!string.IsNullOrEmpty(type))
                {
                    if (resTypes.Contains(type) && !fp.HasBanned)
                    {
                        JToken? attrPath = fp.Content["Attributes"]?[0];
                        if (attrPath != null)
                        {
                            string? path = (string?)attrPath["attrInput"];
                            if (!string.IsNullOrEmpty(path))
                            {
                                var ps = path.Split("|");
                                foreach (string p in ps)
                                {
                                    string fileName = Path.GetFileName(p);
                                    string target = Path.Combine(Dir, fileName);
                                    try
                                    {
                                        string source = "";
                                        source = Directory.EnumerateFiles(Dir, fileName, SearchOption.AllDirectories).First();
                                        if (source != target)
                                        {
                                            if (System.IO.File.Exists(target))
                                            {
                                                IssueTracker.Instance.Report(new MergerException(File, $"Naming conflict occurs for {target}"));
                                            }
                                            else
                                            {
                                                System.IO.File.Copy(source, target, false);
                                                System.IO.File.Delete(source);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        IssueTracker.Instance.Report(new MergerException(File, $"Cannot find resource named {fileName}.", ex));
                                    }
                                }
                                attrPath["attrInput"] = string.Join('|', ps.Select(x => Path.GetFileName(x)));
                            }
                        }
                    }
                }
                sb.AppendLine(fp.ToString());
            }

            using FileStream fs = new(File, FileMode.Create, FileAccess.Write);
            using StreamWriter sw = new(fs, Encoding.UTF8);
            sw.Write(sb);

            Console.WriteLine($"[Resource] Modified {File}.");
        }
    }
}
