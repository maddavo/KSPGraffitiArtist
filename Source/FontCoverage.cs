namespace KSPGraffitiArtist
{
    // Font RGB is not ink colour: only alpha describes glyph coverage.
    public static class FontCoverage
    {
        public static uint WhiteWithCoverage(uint rgba)
        {
            return (rgba&0xFF000000u)|0x00FFFFFFu;
        }
        public static uint TintCoverage(uint rgba,byte r,byte g,byte b)
        { return (rgba&0xFF000000u)|((uint)b<<16)|((uint)g<<8)|r; }
    }
}
