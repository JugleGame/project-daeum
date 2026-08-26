using System.Collections.Generic;
using Daeume.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Daeume.Tests.PlayMode
{
    public sealed class KoreanFontTests
    {
        [Test]
        public void Test_KoreanFont_CoversKoreanUiAndAppliesToLoadedText()
        {
            var font = KoreanFontBootstrap.KoreanFont;
            Assert.That(font, Is.Not.Null, "WebGL 빌드에 포함할 한글 폰트를 Resources에서 찾지 못했다.");

            var missing = new List<char>();
            for (var codePoint = 0xAC00; codePoint <= 0xD7A3; codePoint++)
            {
                var character = (char)codePoint;
                if (!font.HasCharacter(character) && missing.Count < 20) missing.Add(character);
            }

            const string uiSymbols = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789←›…·□:/.()!?+-";
            foreach (var character in uiSymbols)
            {
                if (!font.HasCharacter(character) && !missing.Contains(character)) missing.Add(character);
            }

            Assert.That(missing, Is.Empty,
                $"한글 UI에 필요한 glyph가 누락됐다: {string.Join(", ", missing)}");

            var uiObject = new GameObject("KoreanUiText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var worldObject = new GameObject("KoreanWorldText", typeof(TextMesh));
            try
            {
                var uiText = uiObject.GetComponent<Text>();
                var worldText = worldObject.GetComponent<TextMesh>();

                KoreanFontBootstrap.ApplyToLoadedText();

                Assert.That(uiText.font, Is.SameAs(font));
                Assert.That(worldText.font, Is.SameAs(font));
                Assert.That(worldObject.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(font.material));
            }
            finally
            {
                Object.DestroyImmediate(uiObject);
                Object.DestroyImmediate(worldObject);
            }
        }
    }
}
