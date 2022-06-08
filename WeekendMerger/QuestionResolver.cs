using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WeekendMerger
{
    public class QuestionResolver : FileResolverBase
    {
        private static string scBefore = @"{0},{{""$type"":"".Boss.BossSCBeforeStart, "",""Attributes"":[]}}";
        private static string scUFO = @"{0},{{""$type"":"".Advanced.UnidentifiedNode, LuaSTGEditorSharp"",""Attributes""" +
            @":[{{""$type"":"".DependencyAttrItem, "",""attrCap"":""Type"",""attrInput"":""week_designer_and_questions""" +
            @",""EditWindow"":""userDefinedNodeDefinition""}},{{""attrCap"":""Designer"",""attrInput"":""{1}"",""EditWindow""" +
            @":""""}},{{""attrCap"":""Q1"",""attrInput"":""{2}"",""EditWindow"":""bool""}},{{""attrCap"":""Q2"",""attrInput""" +
            @":""{3}"",""EditWindow"":""bool""}},{{""attrCap"":""Q3"",""attrInput"":""{4}"",""EditWindow"":""bool""}}],""" +
            @"AttributeCount"":5}}";

        public QuestionResolver(string file, string dir) : base(file, dir) { }

        enum State
        {
            None, InSC, InSCBefore
        }

        public override void Resolve()
        {
            FilePos? prev = null;
            Dictionary<int, bool[]> questions = new();
            foreach (FilePos fp in FileReader.EnumerateNodes(File))
            {
                try
                {
                    if ((string?)prev?.Content?["$type"] == ".General.Comment, LuaSTGEditorSharp" && prev?.HasBanned == false &&
                        (string?)fp.Content?["$type"] == ".Boss.BossSpellCard, " && fp.HasBanned == false)
                    {
                        string? s = (string?)prev.Value.Content?["Attributes"]?[0]?["attrInput"];
                        if (!string.IsNullOrEmpty(s))
                        {
                            string[] vs = s.Split('&');
                            int[] arr = s.Split('&').Select(x => Convert.ToInt32(x)).ToArray();
                            bool[] b = new bool[3];
                            for (int i = 0; i < arr.Length; i++)
                            {
                                if (arr[i] <= 3)
                                {
                                    b[arr[i] - 1] = true;
                                }
                            }
                            questions.Add(fp.Line, b);
                        }
                    }
                }
                catch (Exception e)
                {
                    IssueTracker.Instance.Report(new MergerException(File, $"Comment format is incorrect. {e.Message}", e));
                }
                prev = fp;
            }

            StringBuilder sb = new StringBuilder();
            int level = 0;
            int scKey = 0;
            State state = State.None;
            foreach (FilePos fp in FileReader.EnumerateNodes(File))
            {
                switch (state)
                {
                    case State.None:
                        sb.AppendLine(fp.ToString());
                        if (questions.ContainsKey(fp.Line))
                        {
                            state = State.InSC;
                            scKey = fp.Line;
                            level = fp.Level;
                        }
                        break;
                    case State.InSC:
                        if (fp.Level <= level)
                        {
                            bool[] q = questions[scKey];
                            state = State.None;
                            sb.AppendFormat(scBefore, level + 1);
                            sb.AppendLine();
                            sb.AppendFormat(scUFO, level + 2, Program.GetAuthorFullName(GetAuthorName())
                                , q[0].ToString().ToLower(), q[1].ToString().ToLower(), q[2].ToString().ToLower());
                            sb.AppendLine();
                        }
                        else
                        {
                            if ((string?)fp.Content?["$type"] == ".Boss.BossSCBeforeStart, " && fp.HasBanned == false)
                            {
                                state = State.InSCBefore;
                                level = fp.Level;
                            }
                        }
                        sb.AppendLine(fp.ToString());
                        break;
                    case State.InSCBefore:
                        if (fp.Level <= level)
                        {
                            bool[] q = questions[scKey];
                            state = State.None;
                            sb.AppendFormat(scUFO, level + 1, Program.GetAuthorFullName(GetAuthorName())
                                , q[0].ToString().ToLower(), q[1].ToString().ToLower(), q[2].ToString().ToLower());
                            sb.AppendLine();
                        }
                        sb.AppendLine(fp.ToString());
                        break;
                }
            }

            Write(sb);

            Console.WriteLine($"[Question] Modified {File}.");
        }
    }
}
