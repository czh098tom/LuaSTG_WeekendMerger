using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeekendMerger
{
    public class StageResolver : FileResolverBase
    {
        private static readonly string sceneUFO = @"{0},{{""$type"":"".Advanced.UnidentifiedNode, LuaSTGEditorSharp"",""Attributes"":[{{""$type"":"".DependencyAttrItem, "",""attrCap"":""Type"",""attrInput"":""week_stage"",""EditWindow"":""userDefinedNodeDefinition""}},{{""attrCap"":""Name"",""attrInput"":""{1}"",""EditWindow"":""""}}],""AttributeCount"":2}}";

        public StageResolver(string file, string dir) : base(file, dir) { }

        public override void Resolve()
        {
            string authorName = GetAuthorName();

            LinkedList<List<FilePos>> modification = new();
            HashSet<int> modifiedLine = new();
            bool find = false;
            int targetLevel = -1;
            foreach (FilePos v in FileReader.EnumerateNodes(File))
            {
                if (!find)
                {
                    JObject obj = v.Content;
                    if ((string?)obj["$type"] == ".Stage.StageGroup, " && !v.HasBanned)
                    {
                        find = true;
                        targetLevel = v.Level;
                        modification.AddLast(new List<FilePos>(10) { v });
                        modifiedLine.Add(v.Line);
                    }
                }
                else
                {
                    if (v.Level <= targetLevel)
                    {
                        find = false;
                    }
                    else
                    {
                        if (modification.Last == null) throw new InvalidOperationException("Unknown Error.");
                        modification.Last.Value.Add(v);
                        modifiedLine.Add(v.Line);
                    }
                }
            }

            StringBuilder sb = new();
            foreach (FilePos v in FileReader.EnumerateNodes(File))
            {
                if (!modifiedLine.Contains(v.Line))
                {
                    sb.AppendLine(v.ToString());
                }
            }

            List<FilePos>? mo = modification.First?.Value;
            sb.AppendFormat(sceneUFO, 1, authorName);
            sb.AppendLine();
            if (mo == null) throw new MergerException(File, "No StageGroup node was found.");
            int levelRef = mo[0].Level;
            bool begin = false;
            for (int i = 1; i < mo.Count; i++)
            {
                if (!begin)
                {
                    if (((string?)mo[i].Content["$type"]) == ".Stage.Stage, " && !mo[i].HasBanned)
                    {
                        begin = true;
                    }
                }
                else
                {
                    //+1 and -1 because
                    sb.AppendLine((mo[i] with { Level = mo[i].Level - levelRef }).ToString());
                    if (((string?)mo[i].Content["$type"]) == ".Stage.Stage, " && !mo[i].HasBanned)
                    {
                        IssueTracker.Instance.Report(new MergerException(File, "Multiple Stage node was found. the latter has been removed."));
                    }
                }
            }

            Write(sb);

            if (modification.Count > 1)
            {
                IssueTracker.Instance.Report(new MergerException(File, "Multiple StageGroup node was found. the latter has been removed."));
            }

            Console.WriteLine($"[Stage] Modified {File}.");
        }
    }
}