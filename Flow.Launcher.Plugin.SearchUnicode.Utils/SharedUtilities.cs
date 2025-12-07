using System;
using System.Collections.Generic;
using System.Text;

namespace Flow.Launcher.Plugin.SearchUnicode.Utils
{
    /// <summary>
    /// Shared utilities for Flow.Launcher.Plugin.SearchUnicode plugins
    /// </summary>
    public static class SharedUtilities
    {
        /// <author>Mikescher https://stackoverflow.com/a/64236441</author>
        /// <license>CC BY-SA 4.0</license>
        public static IEnumerable<string> SplitArgs(string commandLine)
        {
            var result = new StringBuilder();

            var quoted = false;
            var escaped = false;
            var started = false;
            var allowcaret = false;
            for (int i = 0; i < commandLine.Length; i++)
            {
                var chr = commandLine[i];

                if (chr == '^' && !quoted)
                {
                    if (allowcaret)
                    {
                        result.Append(chr);
                        started = true;
                        escaped = false;
                        allowcaret = false;
                    }
                    else if (i + 1 < commandLine.Length && commandLine[i + 1] == '^')
                    {
                        allowcaret = true;
                    }
                    else if (i + 1 == commandLine.Length)
                    {
                        result.Append(chr);
                        started = true;
                        escaped = false;
                    }
                }
                else if (escaped)
                {
                    result.Append(chr);
                    started = true;
                    escaped = false;
                }
                else if (chr == '"')
                {
                    quoted = !quoted;
                    started = true;
                }
                else if (chr == '\\' && i + 1 < commandLine.Length && commandLine[i + 1] == '"')
                {
                    escaped = true;
                }
                else if (chr == ' ' && !quoted)
                {
                    if (started) yield return result.ToString();
                    result.Clear();
                    started = false;
                }
                else
                {
                    result.Append(chr);
                    started = true;
                }
            }

            if (started) yield return result.ToString();
        }

        public static IList<int> GetMatchingCharacterIndices(IPublicAPI api, string input, IEnumerable<string> patterns)
        {
            var result = new SortedSet<int>();

            if (string.IsNullOrEmpty(input) || patterns is null)
            {
                return new List<int>();
            }

            foreach (var pattern in patterns)
            {
                var matches = api.FuzzySearch(input, pattern).MatchData;

                if (matches is not null)
                {
					result.UnionWith(matches);
				}
            }

            return new List<int>(result);
        }
    }
}
