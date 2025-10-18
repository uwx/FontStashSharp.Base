using System;

namespace FontStashSharp
{
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
		/// <param name="fontSystem">The font system containing font sources</param>
		/// <param name="text">The text to shape</param>
		/// <param name="fontSize">The font size</param>
		/// <param name="codepointToId">Function that maps codepoint to font id</param>
		/// <returns>Shaped text with glyph information</returns>
		ShapedText Shape(string text, float fontSize, Func<int, int> codepointToId);
	}
}
