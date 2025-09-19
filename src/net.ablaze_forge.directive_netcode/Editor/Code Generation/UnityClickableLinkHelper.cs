using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Editor.CodeGen
{
    /// <summary>
    /// Provides clickable links in the Unity console that jump directly to source code locations.
    /// Builds an index of C# files, types, and methods so diagnostics can show links like
    /// <c>Assets/Scripts/MyClass.cs:42</c>.
    /// </summary>
    internal static class UnityClickableLinkHelper
    {
        /// <summary>
        /// Represents an indexed method within a type, storing its name, parameter count, and line number.
        /// </summary>
        private class IndexedMethod
        {
            public string MethodName;
            public int ParameterCount;
            public int LineNumber;
        }

        /// <summary>
        /// Represents an indexed type, including its source file path and contained methods.
        /// </summary>
        private class IndexedType
        {
            public string TypeName;
            public string RelativePath;
            public List<IndexedMethod> Methods = new();
        }

        /// <summary>
        /// Maps type names to their indexed type information (including file path and methods).
        /// </summary>
        private static Dictionary<string, IndexedType> _typeIndex;

        /// <summary>
        /// Caches generated clickable links for methods to avoid recomputing them.
        /// </summary>
        private static Dictionary<MethodInfo, string> _methodLinkCache;

        /// <summary>
        /// Regex used to detect class, struct, or interface declarations.
        /// </summary>
        private static readonly Regex TypeRegex =
            new(@"\b(class|struct|interface)\s+(?<name>\w+)", RegexOptions.Compiled);

        /// <summary>
        /// Regex used to detect method declarations (simplified, not full C# grammar).
        /// </summary>
        private static readonly Regex MethodRegex =
            new(@"\b(?:public|private|protected|internal)?\s*(?:static\s+)?[^\s]+\s+(?<name>\w+)\s*\(",
                RegexOptions.Compiled);

        /// <summary>
        /// Builds an index of all C# files in the Unity <c>Assets</c> folder,
        /// mapping types and methods to their source file and line numbers.
        /// </summary>
        public static void WarmupFileCache()
        {
            if (_typeIndex != null) return;

            System.Diagnostics.Stopwatch stopwatch = new();
            stopwatch.Start();

            _typeIndex = new Dictionary<string, IndexedType>(StringComparer.Ordinal);
            _methodLinkCache = new Dictionary<MethodInfo, string>();

            var files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                string relativePath = "Assets" + file.Substring(Application.dataPath.Length);
                string[] lines = File.ReadAllLines(file);

                string currentType = null;
                IndexedType currentIndexedType = null;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    // Detect type declarations
                    var typeMatch = TypeRegex.Match(line);
                    if (typeMatch.Success)
                    {
                        currentType = typeMatch.Groups["name"].Value;
                        currentIndexedType = new IndexedType
                        {
                            TypeName = currentType,
                            RelativePath = relativePath
                        };
                        _typeIndex[currentType] = currentIndexedType;
                    }

                    // Detect method declarations for the current type
                    if (currentType != null && currentIndexedType != null)
                    {
                        var methodMatch = MethodRegex.Match(line);
                        if (methodMatch.Success)
                        {
                            string methodName = methodMatch.Groups["name"].Value;
                            int paramCount = CountParameters(line);

                            currentIndexedType.Methods.Add(new IndexedMethod
                            {
                                MethodName = methodName,
                                ParameterCount = paramCount,
                                LineNumber = i + 1
                            });
                        }
                    }
                }
            }

            stopwatch.Stop();

            Debug.Log($"UnityClickableLinkHelper: Indexed {_typeIndex.Count} types across {files.Length} files.");
            Debug.Log($"Indexing took {stopwatch.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// Clears the cached type and method index, forcing a rebuild on the next call to <see cref="WarmupFileCache"/>.
        /// </summary>
        public static void ClearFileCache()
        {
            _typeIndex = null;
            _methodLinkCache = null;
        }

        /// <summary>
        /// Gets a Unity console clickable link (<c>&lt;a href&gt;</c>) pointing to the file and line number
        /// of the given <paramref name="method"/> if it was found during indexing.
        /// </summary>
        /// <param name="method">The reflection method to look up.</param>
        /// <returns>A clickable link to the source file, or a fallback message if not found.</returns>
        public static string GetUnityClickableLink(MethodInfo method)
        {
            if (_typeIndex == null)
                return $"(no index warmed up for {method.DeclaringType.FullName}.{method.Name})";

            if (_methodLinkCache.TryGetValue(method, out var cached))
                return cached;

            if (_typeIndex.TryGetValue(method.DeclaringType.Name, out var indexedType))
            {
                var paramCount = method.GetParameters().Length;

                foreach (var m in indexedType.Methods)
                {
                    if (m.MethodName == method.Name && m.ParameterCount == paramCount)
                    {
                        string link =
                            $"<a href=\"{indexedType.RelativePath}\" line=\"{m.LineNumber}\">{indexedType.RelativePath}:{m.LineNumber}</a>";
                        _methodLinkCache[method] = link;
                        return link;
                    }
                }
            }

            string fallback =
                $"(source not found for {method.DeclaringType.FullName}.{method.Name})";
            _methodLinkCache[method] = fallback;
            return fallback;
        }

        /// <summary>
        /// Counts the number of parameters in a method signature line using a simple string-based parse.
        /// </summary>
        private static int CountParameters(string line)
        {
            int openParen = line.IndexOf('(');
            int closeParen = line.IndexOf(')', openParen + 1);
            if (openParen < 0 || closeParen < 0) return 0;

            string inside = line.Substring(openParen + 1, closeParen - openParen - 1).Trim();
            if (string.IsNullOrEmpty(inside)) return 0;

            return inside.Split(',').Length;
        }
    }
}
