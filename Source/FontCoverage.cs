namespace KSPGraffitiArtist
{
    // Font RGB is not ink colour: only alpha describes glyph coverage.
    public static class FontCoverage
    {
        public static uint WhiteWithCoverage(uint rgba)
        {
            return (rgba&0xFF000000u)|0x00FFFFFFu;
        }
    }
}
