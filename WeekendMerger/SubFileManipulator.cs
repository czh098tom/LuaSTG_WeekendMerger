using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WeekendMerger
{
    public class SubFileManipulator : FileResolverBase
    {
        public SubFileManipulator(string dir) : base(Directory.EnumerateFiles(dir, "*.lstges").First(), dir) { }

        public override void Resolve()
        {
            IssueTracker.Instance.RunTracking(new StageResolver(File, Dir));
            IssueTracker.Instance.RunTracking(new ResourceResolver(File, Dir));
            IssueTracker.Instance.RunTracking(new QuestionResolver(File, Dir));
        }
    }
}
