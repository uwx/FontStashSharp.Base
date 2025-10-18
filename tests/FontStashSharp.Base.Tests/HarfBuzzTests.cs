using NUnit.Framework;
using System;

namespace FontStashSharp.Tests
{
	[TestFixture]
	public class HarfBuzzTests
	{
		private class ShaperWrapper
		{
			private readonly HarfBuzzTextShaper _shaper;
			private readonly Func<int, int> _codepointToId;

			public ShaperWrapper()
			{
				_shaper = new HarfBuzzTextShaper();

				var assembly = typeof(HarfBuzzTests).Assembly;

				var fontId = _shaper.RegisterTtfFont(assembly.ReadResourceAsBytes("Resources.DroidSans.ttf"));

				_codepointToId = id => fontId;
			}

			public ShapedText Shape(string text, float size) => _shaper.Shape(text, size, _codepointToId);
		}

		[TestCase("Hello World", 1, TextDirection.LTR)]
		[TestCase("مرحبا", 1, TextDirection.RTL)]
		[TestCase("", 0, TextDirection.LTR)]
		[TestCase("123", 1, TextDirection.LTR)]
		public void BiDiAnalyzer_SingleDirectionText(string text, int expectedRunCount, TextDirection expectedDirection)
		{
			var runs = BiDiAnalyzer.SegmentIntoDirectionalRuns(text);

			Assert.That(runs.Count, Is.EqualTo(expectedRunCount));

			if (expectedRunCount > 0)
			{
				Assert.That(runs[0].Direction, Is.EqualTo(expectedDirection));
				Assert.That(runs[0].Start, Is.EqualTo(0));
				Assert.That(runs[0].Length, Is.EqualTo(text.Length));
			}
		}

		[Test]
		public void BiDiAnalyzer_MixedLtrRtlText()
		{
			// English "Hello" + Arabic "مرحبا"
			var text = "Hello مرحبا";
			var runs = BiDiAnalyzer.SegmentIntoDirectionalRuns(text);

			// Should have 2 runs: LTR for "Hello " and RTL for "مرحبا"
			Assert.That(runs.Count, Is.EqualTo(2));

			Assert.That(runs[0].Direction, Is.EqualTo(TextDirection.LTR));
			Assert.That(runs[0].Start, Is.EqualTo(0));
			Assert.That(runs[1].Direction, Is.EqualTo(TextDirection.RTL));
		}

		[Test]
		public void BiDiAnalyzer_LeadingNeutralCharacters()
		{
			// Spaces before text should be assigned to the following run's direction
			var text = "  Hello";
			var runs = BiDiAnalyzer.SegmentIntoDirectionalRuns(text);

			Assert.That(runs.Count, Is.EqualTo(1));
			Assert.That(runs[0].Direction, Is.EqualTo(TextDirection.LTR));
			Assert.That(runs[0].Start, Is.EqualTo(0));
			Assert.That(runs[0].Length, Is.EqualTo(text.Length));
		}

		[Test]
		public void BiDiAnalyzer_OnlyNeutralCharacters()
		{
			// Text with only neutral characters should default to LTR
			var text = "   ...   ";
			var runs = BiDiAnalyzer.SegmentIntoDirectionalRuns(text);

			Assert.That(runs.Count, Is.EqualTo(1));
			Assert.That(runs[0].Direction, Is.EqualTo(TextDirection.LTR));
		}

		[Test]
		public void TextShaper_EmptyString()
		{
			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape("", 32);

			Assert.That(shaped, Is.Not.Null);
			Assert.That(shaped.Glyphs, Is.Not.Null);
			Assert.That(shaped.Glyphs.Length, Is.EqualTo(0));
			Assert.That(shaped.OriginalText, Is.EqualTo(""));
		}

		[Test]
		public void TextShaper_NullString()
		{
			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape(null, 32);

			Assert.That(shaped, Is.Not.Null);
			Assert.That(shaped.Glyphs, Is.Not.Null);
			Assert.That(shaped.Glyphs.Length, Is.EqualTo(0));
			Assert.That(shaped.OriginalText, Is.EqualTo(""));
		}

		[Test]
		public void TextShaper_SimpleText()
		{
			// Create font system with BiDi disabled but text shaping enabled
			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape("Hello", 32);

			Assert.That(shaped, Is.Not.Null);
			Assert.That(shaped.Glyphs, Is.Not.Null);
			Assert.That(shaped.Glyphs.Length, Is.GreaterThan(0));
			Assert.That(shaped.OriginalText, Is.EqualTo("Hello"));
			Assert.That(shaped.FontSize, Is.EqualTo(32));

			// Each glyph should have valid advance values
			foreach (var glyph in shaped.Glyphs)
			{
				Assert.That(glyph.XAdvance, Is.GreaterThan(0));
			}
		}

		[Test]
		public void TextShaper_WithBiDiEnabled()
		{
			// Create font system with BiDi enabled
			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape("Test", 32);

			Assert.That(shaped, Is.Not.Null);
			Assert.That(shaped.Glyphs, Is.Not.Null);
			Assert.That(shaped.Glyphs.Length, Is.GreaterThan(0));
			Assert.That(shaped.OriginalText, Is.EqualTo("Test"));
		}

		[Test]
		public void TextShaper_SurrogatePair_FormsSingleCluster()
		{
			var text = "😀"; // U+1F600 (surrogate pair)
			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape(text, 32);

			Assert.That(shaped.Glyphs.Length, Is.LessThanOrEqualTo(1), "Emoji surrogate pair should form a single cluster");
		}

		[Test]
		public void TextShaper_EmojiZWJSequence()
		{
			// Family: 👨‍👩‍👧‍👦 (multiple codepoints joined by ZWJ)
			var text = "👨‍👩‍👧‍👦";
			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape(text, 32);

			Assert.That(shaped.Glyphs.Length, Is.LessThan(text.Length),
					"ZWJ sequences should combine into fewer glyphs");
		}

		[Test]
		public void ShapedText_PreservesOriginalText()
		{
			var originalText = "Testing 123";

			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape(originalText, 32);

			Assert.That(shaped.OriginalText, Is.EqualTo(originalText));
		}

		[Test]
		public void ShapedGlyphs_HaveValidClusterIndices()
		{
			var text = "Hello";

			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape(text, 32);

			// All cluster indices should be within the text length
			foreach (var glyph in shaped.Glyphs)
			{
				Assert.That(glyph.Cluster, Is.GreaterThanOrEqualTo(0));
				Assert.That(glyph.Cluster, Is.LessThan(text.Length));
			}
		}
	}
}
