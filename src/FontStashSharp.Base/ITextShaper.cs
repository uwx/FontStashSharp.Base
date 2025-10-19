using FontStashSharp.Interfaces;
using System;

namespace FontStashSharp
{
	public struct TextShaperCodePointInfo
	{
		/// <summary>
		/// Text Shaper Font Id
		/// </summary>
		public int FontId;

		/// <summary>
		/// Font Source
		/// </summary>
		public IFontSource FontSource;

		public TextShaperCodePointInfo(int fontId, IFontSource fontSource)
		{
			FontId = fontId;
			FontSource = fontSource ?? throw new ArgumentNullException(nameof(fontSource));
		}
	}


	public interface ITextShaper
	{
		/// <summary>
		/// Registers a ttf font
		/// </summary>
		/// <param name="data"></param>
		/// <returns>Assigned id</returns>
		int RegisterTtfFont(byte[] data);

		/// <summary>
		/// Disposes and removes font from the text shaper
		/// </summary>
		/// <param name="id"></param>
		void RemoveFont(int id);

		/// <summary>
		/// Shape text using HarfBuzz
		/// </summary>
		/// <param name="text">The text to shape</param>
		/// <param name="fontSize">The font size</param>
		/// <param name="codePointInfoGetter">Function that maps codepoint to font id</param>
		/// <returns>Shaped text with glyph information</returns>
		ShapedText Shape(string text, float fontSize, Func<int, TextShaperCodePointInfo> codePointInfoGetter);
	}
}
