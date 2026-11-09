using System;
using System.Runtime.InteropServices;
using System.Text;

namespace KSPGraffitiArtist
{
    public sealed class RasterText
    {
        public int Width,Height;
        // Tight ink bounds, bottom row first, matching Unity texture coordinates.
        public byte[] Alpha;
        public string Face;
    }
    // Offscreen GDI text only: no windows, font installation, registry writes or extra DLLs.
    public static class WindowsTextRasterizer
    {
        public const int MaxDimension=2048;
        [StructLayout(LayoutKind.Sequential)] struct Rect {public int Left,Top,Right,Bottom;}
        [StructLayout(LayoutKind.Sequential)] struct BitmapInfo
        {
            public uint Size;public int Width,Height;public ushort Planes,BitCount;
            public uint Compression,ImageSize;public int XPels,YPels;public uint Used,Important,Color;
        }
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
        [DllImport("gdi32.dll")] static extern int SetBkMode(IntPtr dc,int mode);
        [DllImport("gdi32.dll")] static extern uint SetTextColor(IntPtr dc,uint color);
        [DllImport("gdi32.dll")] static extern bool GdiFlush();
        [DllImport("gdi32.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]
        static extern IntPtr CreateFontW(int height,int width,int angle,int orientation,int weight,uint italic,uint underline,uint strike,uint charset,uint precision,uint clip,uint quality,uint pitch,string face);
        [DllImport("gdi32.dll",CharSet=CharSet.Unicode,ExactSpelling=true)] static extern int GetTextFaceW(IntPtr dc,int count,StringBuilder face);
        [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr dc,ref BitmapInfo info,uint usage,out IntPtr pixels,IntPtr section,uint offset);
        [DllImport("user32.dll",CharSet=CharSet.Unicode,ExactSpelling=true)] static extern int DrawTextW(IntPtr dc,string text,int length,ref Rect bounds,uint flags);
        const uint NoPrefix=0x800,NoClip=0x100,Calculate=0x400;
        public static RasterText Render(string family,string text,string style)
        {
            if(Environment.OSVersion.Platform!=PlatformID.Win32NT)throw new InvalidOperationException("Windows font rendering is unavailable on this platform.");
            if(!MarkingData.SafeFontName(family) || family.Length>31 || !MarkingData.ValidStyle(style))throw new InvalidOperationException("Unsupported Windows font family or style.");
            if(string.IsNullOrWhiteSpace(text) || text.Length>512)throw new InvalidOperationException("Enter visible text (up to 512 characters).");
            text=text.Replace("\r\n","\n").Replace('\r','\n').Replace("\n","\r\n");
            IntPtr dc=CreateCompatibleDC(IntPtr.Zero),font=IntPtr.Zero,oldFont=IntPtr.Zero,bitmap=IntPtr.Zero,oldBitmap=IntPtr.Zero;
            if(dc==IntPtr.Zero)throw new InvalidOperationException("Cannot create offscreen font context.");
            try {
                Rect measure=new Rect();int pixels=96,padding=0,width=0,height=0;string selectedFace="";
                while(true){
                    font=CreateFontW(-pixels,0,0,0,(style=="Bold"||style=="BoldItalic")?700:400,
                        (style=="Italic"||style=="BoldItalic")?1u:0u,0,0,1,0,0,4,0,family);
                    if(font==IntPtr.Zero)throw new InvalidOperationException("Cannot create Windows font: "+family);
                    oldFont=SelectObject(dc,font);
                    var face=new StringBuilder(128);GetTextFaceW(dc,face.Capacity,face);selectedFace=face.ToString();
                    if(!string.Equals(selectedFace,family,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Windows substituted "+selectedFace+" for "+family+"; refusing to change the font.");
                    measure=new Rect{Right=1000000};
                    if(DrawTextW(dc,text,text.Length,ref measure,Calculate|NoPrefix)<=0)throw new InvalidOperationException("Cannot measure font text.");
                    padding=pixels/2+4;width=measure.Right+2*padding;height=measure.Bottom+2*padding;
                    if(width<=MaxDimension && height<=MaxDimension)break;
                    if(pixels<=8)throw new InvalidOperationException("Text is too large for the font texture; shorten the label.");
                    int next=Math.Max(8,Math.Min(pixels-1,(int)(pixels*.95*Math.Min((double)MaxDimension/width,(double)MaxDimension/height))));
                    SelectObject(dc,oldFont);oldFont=IntPtr.Zero;DeleteObject(font);font=IntPtr.Zero;pixels=next;
                }
                var info=new BitmapInfo{Size=40,Width=width,Height=-height,Planes=1,BitCount=32,ImageSize=(uint)(width*height*4)};
                IntPtr bits;bitmap=CreateDIBSection(dc,ref info,0,out bits,IntPtr.Zero,0);
                if(bitmap==IntPtr.Zero || bits==IntPtr.Zero)throw new InvalidOperationException("Cannot allocate font texture.");
                oldBitmap=SelectObject(dc,bitmap);
                var raw=new byte[width*height*4];Marshal.Copy(raw,0,bits,raw.Length);
                SetBkMode(dc,1);SetTextColor(dc,0xFFFFFF);
                Rect draw=new Rect{Left=padding,Top=padding,Right=padding+measure.Right,Bottom=padding+measure.Bottom};
                if(DrawTextW(dc,text,text.Length,ref draw,NoPrefix|NoClip|1)<=0)throw new InvalidOperationException("Cannot draw Windows font text.");
                GdiFlush();Marshal.Copy(bits,raw,0,raw.Length);
                int left=width,right=-1,top=height,bottom=-1;
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(raw[(y*width+x)*4]!=0){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
                if(right<left)throw new InvalidOperationException("Text contains no visible glyphs in "+family+".");
                var result=new RasterText{Width=right-left+1,Height=bottom-top+1,Face=selectedFace};
                result.Alpha=new byte[result.Width*result.Height];
                for(int y=0;y<result.Height;y++)for(int x=0;x<result.Width;x++)result.Alpha[(result.Height-1-y)*result.Width+x]=raw[((top+y)*width+left+x)*4];
                return result;
            }finally{
                if(oldBitmap!=IntPtr.Zero)SelectObject(dc,oldBitmap);
                if(bitmap!=IntPtr.Zero)DeleteObject(bitmap);
                if(oldFont!=IntPtr.Zero)SelectObject(dc,oldFont);
                if(font!=IntPtr.Zero)DeleteObject(font);
                DeleteDC(dc);
            }
        }
    }
}
