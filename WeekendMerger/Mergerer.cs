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
        private static readonly JObject codeNode = JObject.Parse(@"{""$type"":"".General.Code, LuaSTGEditorSharp"",""Attributes"":[{""attrCap"":""Code"",""attrInput"":"""",""EditWindow"":""code""}],""AttributeCount"":1}");

        private static readonly string codeBeforePatchFile = "if BeforeLoadSubProject then BeforeLoadSubProject(\"{0}\") end";
        private static readonly string codeAfterPatchFile = "if AfterLoadSubProject then AfterLoadSubProject(\"{0}\") end";

        private readonly string directory;
        private readonly string output_directory;
        private readonly bool equal_dir;

        public Mergerer(string dir, string out_dir)
        {
            directory = dir;
            output_directory = out_dir;
            equal_dir = Path.GetFullPath(directory) == Path.GetFullPath(output_directory);
        }

        public Mergerer(string dir)
        {
            directory = dir;
            output_directory = dir;
            equal_dir = Path.GetFullPath(directory) == Path.GetFullPath(output_directory);
        }

        void DeleteFolder(string dir)
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                }
                catch (UnauthorizedAccessException)
                {
                    FileAttributes attributes = File.GetAttributes(file);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        attributes &= ~FileAttributes.ReadOnly;
                        File.SetAttributes(file, attributes);
                        File.Delete(file);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            Directory.Delete(dir, true);
        }

        void CleanOutput()
        {
            if (equal_dir) return;
            if (Directory.Exists(output_directory))
                DeleteFolder(output_directory);
            Directory.CreateDirectory(output_directory);
        }

        void UnpackZip()
        {
            foreach (string file in Directory.EnumerateFiles(directory, "*.zip"))
            {
                Console.WriteLine("[Decompress] " + file);
                var folder = Path.Combine(output_directory, Path.GetFileNameWithoutExtension(file));
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
                Directory.CreateDirectory(folder);
                try
                {
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
                catch (Exception ex)
                {
                    IssueTracker.Instance.Report(new MergerException(file, "Unpack failed", ex));
                    Directory.Delete(folder, true);
                }
            }
        }

        void CopyFolder()
        {
            if (equal_dir) return;
            foreach (var dir in Directory.EnumerateDirectories(directory))
            {
                var dir_path = Path.GetFullPath(dir);
                dir_path = Path.GetDirectoryName(dir_path) ?? dir_path;
                if (Directory.EnumerateFiles(dir, "*.lstges", SearchOption.TopDirectoryOnly).Any())
                {
                    foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
                    {
                        var file_path = Path.GetFullPath(file);
                        var target_path = output_directory + file_path.Replace(dir_path, string.Empty);
                        var target_folder = Path.GetDirectoryName(target_path)
                            ?? Path.Combine(output_directory, Path.GetFileName(dir));
                        if (!Directory.Exists(target_folder))
                            Directory.CreateDirectory(target_folder);
                        File.Copy(file, target_path);
                    }
                }
                else
                {
                    IssueTracker.Instance.Report(new MergerException($"Directory \"{dir}\" does not contains any .lstges file"));
                }
            }
        }

        void InnerMerge(string weekName)
        {
            string? name = Path.GetFileName(output_directory);
            using StreamWriter sw = new(Path.Combine(output_directory, $"{name}.lstgproj"));
            using (StreamReader sr = new(Program.TemplatePath))
            {
                sw.Write(string.Format(sr.ReadToEnd(), weekName));
            }
            foreach (string dir in Directory.EnumerateDirectories(output_directory).Shuffle())
            {
                try
                {
                    if (!Directory.EnumerateFiles(dir, "*.lstges", SearchOption.TopDirectoryOnly).Any())
                    {
                        IssueTracker.Instance.Report(new MergerException(dir, $"Directory \"{dir}\" does not contains any .lstges file"));
                        var files = Directory.EnumerateFiles(dir, "*.lstges", SearchOption.AllDirectories);
                        foreach (var file in files)
                            IssueTracker.Instance.Report(new MergerException(dir, $"Found .lstges file at \"{file}\" but will not use it."));
                        continue;
                    }
                    SubFileManipulator mani = new(dir);
                    mani.Resolve();
                    JToken codeBefore = codeNode.DeepClone();
                    JToken clone = patchFile.DeepClone();
                    JToken codeAfter = codeNode.DeepClone();
                    JToken? jt = clone["Attributes"]?[0];
                    JToken? codeBeforeAttr = codeBefore["Attributes"]?[0];
                    JToken? codeAfterAttr = codeAfter["Attributes"]?[0];
                    string author = Program.GetAuthorFullNameFromDirName(dir);
                    if (jt != null)
                    {
                        string target = Path.Combine(Path.GetFileName(Path.GetDirectoryName(mani.File))
                            ?? throw new ArgumentException($"Parameter \"{mani.File}\" is not a valid directory")
                            , Path.GetFileName(mani.File));
                        jt["attrInput"] = target;
                        if (codeBeforeAttr != null)
                        {
                            codeBeforeAttr["attrInput"] = string.Format(codeBeforePatchFile, author);
                            sw.WriteLine($"{1},{codeBefore.ToString(Newtonsoft.Json.Formatting.None)}");
                        }
                        sw.WriteLine($"{1},{clone.ToString(Newtonsoft.Json.Formatting.None)}");
                        if (codeAfterAttr != null)
                        {
                            codeAfterAttr["attrInput"] = string.Format(codeAfterPatchFile, author);
                            sw.WriteLine($"{1},{codeAfter.ToString(Newtonsoft.Json.Formatting.None)}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    IssueTracker.Instance.Report(new MergerException(dir, ex.Message, ex));
                }
            }
        }

        public void Merge(string weekName)
        {
            if (!Directory.Exists(directory))
            {
                throw new ArgumentException($"Parameter \"{directory}\" is not a valid directory", nameof(directory));
            }
            CleanOutput();
            UnpackZip();
            CopyFolder();
            InnerMerge(weekName);
        }
    }
}
