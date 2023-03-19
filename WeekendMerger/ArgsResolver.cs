using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeekendMerger
{
    public class ArgsResolver
    {
        public string? Path { get; private set; }
        public string? OutputPath { get; private set; }
        public bool IsHelp { get; private set; }
        public string? NameJSONPath { get; private set; }
        public string? WeekName { get; private set; }

        public ArgsResolver(ArgsGrouper grouper)
        {
            if (grouper.MainParams.Count > 0)
            {
                Path = grouper.MainParams[0];
            }
            if (grouper.AdditionalParams.ContainsKey("h"))
            {
                IsHelp = true;
            }
            if (grouper.AdditionalParams.ContainsKey("j"))
            {
                if (grouper.AdditionalParams["j"].Count > 0)
                {
                    NameJSONPath = grouper.AdditionalParams["j"][0];
                }
                else
                {
                    throw new ArgumentException($"Insufficient argument for \"{nameof(NameJSONPath)}\".");
                }
            }
            if (grouper.AdditionalParams.ContainsKey("n"))
            {
                if (grouper.AdditionalParams["n"].Count > 0)
                {
                    WeekName = grouper.AdditionalParams["n"][0];
                }
                else
                {
                    throw new ArgumentException($"Insufficient argument for \"{nameof(WeekName)}\".");
                }
            }
            if (grouper.AdditionalParams.ContainsKey("o"))
            {
                if (grouper.AdditionalParams["o"].Count > 0)
                {
                    OutputPath = grouper.AdditionalParams["o"][0];
                }
                else
                {
                    throw new ArgumentException($"Insufficient argument for \"{nameof(OutputPath)}\".");
                }
            }
        }
    }
}
