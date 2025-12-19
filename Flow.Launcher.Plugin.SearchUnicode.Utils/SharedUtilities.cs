using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

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

        /// <summary>
        /// Sets text to the clipboard with retry logic to handle clipboard access errors.
        /// This method retries up to 10 times with exponential backoff when the clipboard is locked.
        /// </summary>
        /// <param name="text">The text to set to the clipboard</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool SetClipboardTextSafe(string text)
        {
            const int maxRetries = 10;
            const int initialDelayMs = 10;
            const int CLIPBRD_E_CANT_OPEN = unchecked((int)0x800401D0);
            
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    System.Windows.Clipboard.SetText(text);
                    return true;
                }
                catch (COMException ex) when (ex.HResult == CLIPBRD_E_CANT_OPEN)
                {
                    if (i == maxRetries - 1)
                    {
                        // Last retry failed, give up
                        return false;
                    }
                    
                    // Wait with exponential backoff before retrying
                    // Using Thread.Sleep is appropriate here as this is called from synchronous Action callbacks
                    Thread.Sleep(initialDelayMs * (1 << i));
                }
                catch
                {
                    // For other exceptions, don't retry
                    return false;
                }
            }
            
            return false;
        }
    }
}
