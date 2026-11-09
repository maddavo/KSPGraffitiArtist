using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KSPGraffitiArtist
{
    public static class PaintFonts
    {
        static Font builtin;
        static string[] names;
        static IDictionary<string,string> files;
        static readonly Dictionary<string,Font> cache=new Dictionary<string,Font>(StringComparer.OrdinalIgnoreCase);
        static readonly HashSet<string> bitmapFonts=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public static string[] Names
        {
            get {
                if(names==null){
                    files=UserFonts.Discover();
                    names=new[]{"Arial"}.Concat(Font.GetOSInstalledFontNames()).Concat(files.Keys).Where(MarkingData.SafeFontName)
                        .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n=>n,StringComparer.OrdinalIgnoreCase).ToArray();
                }
                return names;
            }
        }
        public static bool Available(string name)
        { return Names.Contains(name,StringComparer.OrdinalIgnoreCase); }
        public static Font Get(string name)
        {
            if(name=="Arial" || !Available(name)) {
                if(builtin==null)builtin=Resources.GetBuiltinResource<Font>("Arial.ttf");
                return builtin;
            }
            Font font;
            if(cache.TryGetValue(name,out font))return font;
            if(bitmapFonts.Contains(name))return null;
            if(cache.Count+bitmapFonts.Count>=64)throw new InvalidOperationException("Font limit reached for this flight. Reuse a selected font or return to the space centre.");
            string path;
            bool registered=files.TryGetValue(name,out path);
            if(registered){try{font=new Font(path.Replace('\\','/'));}catch(ArgumentException){font=null;}}
            else font=Font.CreateDynamicFontFromOSFont(name,96);
            if(font==null || !font.dynamic){
                if(font!=null)UnityEngine.Object.Destroy(font);
                if(registered && Environment.OSVersion.Platform==PlatformID.Win32NT){bitmapFonts.Add(name);Debug.LogWarning("[GraffitiArtist] Native dynamic font unavailable for "+name+"; using Windows text coverage.");return null;}
                throw new InvalidOperationException("Cannot load font: "+name);
            }
            cache.Add(name,font);return font;
        }
        public static FontStyle Style(string name)
        {
            switch(name){case "Bold":return FontStyle.Bold;case "Italic":return FontStyle.Italic;
                case "BoldItalic":return FontStyle.BoldAndItalic;default:return FontStyle.Normal;}
        }
        public static void Clear()
        { foreach(Font font in cache.Values)UnityEngine.Object.Destroy(font);cache.Clear();bitmapFonts.Clear();Refresh(); }
        public static void Refresh(){names=null;files=null;}
    }
}
