using System;
using System.Collections.Generic;

namespace Daeume.Core
{
    public static class StringTable
    {
        private static readonly Dictionary<string, string> Korean = new(StringComparer.Ordinal)
        {
            ["prompt.memory"] = "기억 살펴보기",
            ["prompt.continue"] = "계속",
            ["hud.health"] = "체력",
            ["hud.chase"] = "도망치세요",
            ["memory.stage01.title"] = "남겨진 기억",
            ["memory.stage01.01"] = "익숙한 목소리가 벽 너머에서 희미하게 들린다.",
            ["memory.stage01.02"] = "붙잡으려 할수록 풍경은 더 빠르게 일그러진다.",
            ["memory.stage01.03"] = "이 기억은 나를 기다린 것이 아니라 붙들고 있었다."
        };

        public static string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            return Korean.TryGetValue(key, out var value) ? value : $"[{key}]";
        }

        public static bool TryGet(string key, out string value) => Korean.TryGetValue(key ?? string.Empty, out value);

        public static void Register(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("String table key is required.", nameof(key));
            Korean[key] = value ?? string.Empty;
        }
    }
}
