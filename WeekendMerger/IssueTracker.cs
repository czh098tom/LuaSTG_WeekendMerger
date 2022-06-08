using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeekendMerger
{
    public class IssueTracker
    {
        public static IssueTracker Instance { get; private set; } = new IssueTracker();

        private readonly List<MergerException> exceptions = new();

        public void RunTracking(FileResolverBase fileResolver)
        {
            try
            {
                fileResolver.Resolve();
            }
            catch (MergerException ex)
            {
                exceptions.Add(ex);
            }
            catch (Exception ex)
            {
                exceptions.Add(new MergerException(fileResolver.File, $"[{fileResolver.GetType().Name}]{ex.Message}", ex));
            }
        }

        public void Report(MergerException ex)
        {
            exceptions.Add(ex);
        }

        public void Show()
        {
            foreach (MergerException ex in exceptions)
            {
                Console.WriteLine($"[{ex.SourceFilePath}]: {ex.Message}");
            }
        }
    }
}
