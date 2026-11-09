using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using KSPGraffitiArtist;
class WindowsFontTests
{
    static int count;
    [DllImport("user32.dll")] static extern uint GetGuiResources(IntPtr process,uint kind);
    static void Check(bool value,string name){count++;if(!value)throw new Exception(name);}
    static string Hash(RasterText text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(text.Alpha));}
    static void Refused(Action action,string name){bool refused=false;try{action();}catch(InvalidOperationException){refused=true;}Check(refused,name);}
    static int Main(string[] args)
    {
        if(Environment.OSVersion.Platform!=PlatformID.Win32NT){Console.WriteLine("SKIP: Windows raster checks require Windows.");return 0;}
        try{
            RasterText arial=WindowsTextRasterizer.Render("Arial","LABEL 5","Regular");
            Check(arial.Face=="Arial"&&arial.Alpha.Length==arial.Width*arial.Height,"real Windows font returned with bounded texture");
            Check(arial.Alpha.Any(a=>a==0)&&arial.Alpha.Any(a=>a==255)&&arial.Alpha.Any(a=>a>0&&a<255),"actual raster has background, solid ink and antialiased edges");
            Refused(()=>WindowsTextRasterizer.Render("NoSuchGraffitiFontXYZ","5","Regular"),"refuse Windows family substitution");
            Refused(()=>WindowsTextRasterizer.Render("Arial","   ","Regular"),"blank label rejected");
            Refused(()=>WindowsTextRasterizer.Render("Arial","5","Outline"),"invalid style rejected");
            RasterText bounded=WindowsTextRasterizer.Render("Arial",new string('i',512),"Regular");
            Check(bounded.Width<=2048&&bounded.Height<=2048,"long label uses bounded raster dimensions");
            if(UserFonts.Discover().ContainsKey("Countdown")){
                RasterText countdown=WindowsTextRasterizer.Render("Countdown","LABEL 5","Regular");
                Check(countdown.Face=="Countdown","actual GDI selected Countdown rather than Arial");
                Check(Hash(countdown)!=Hash(arial),"actual Countdown pixels differ from Arial");
                Check(countdown.Alpha.Any(a=>a==0)&&countdown.Alpha.Any(a=>a==255)&&countdown.Alpha.Any(a=>a>0&&a<255),"Countdown glyph holes/background and fractional edge coverage preserved");
                foreach(string style in new[]{"Bold","Italic","BoldItalic"}){
                    RasterText styled=WindowsTextRasterizer.Render("Countdown","LABEL 5",style);
                    Check(styled.Face=="Countdown"&&styled.Alpha.Any(a=>a>0),"Countdown renders style "+style);
                }
                RasterText multiline=WindowsTextRasterizer.Render("Countdown","LABEL\n5","Regular");
                Check(multiline.Height>countdown.Height,"Countdown multiline layout retained");
                int left=multiline.Width,right=0;
                for(int y=0;y<multiline.Height;y++){
                    bool ink=false;
                    for(int x=0;x<multiline.Width;x++)if(multiline.Alpha[y*multiline.Width+x]>0){left=Math.Min(left,x);right=Math.Max(right,x);ink=true;}
                    if(!ink)break;
                }
                Check(Math.Abs((left+right)/2.0-(multiline.Width-1)/2.0)<5,"shorter multiline label is centred beneath the wider line");
                RasterText crlf=WindowsTextRasterizer.Render("Countdown","LABEL\r\n5","Regular");
                Check(Hash(crlf)==Hash(multiline),"Windows and saved newline forms give identical pixels");
                if(args.Length>0)using(var file=new BinaryWriter(File.Create(args[0]))){
                    file.Write(Encoding.ASCII.GetBytes("P5\n"+countdown.Width+" "+countdown.Height+"\n255\n"));
                    for(int y=countdown.Height-1;y>=0;y--)file.Write(countdown.Alpha,y*countdown.Width,countdown.Width);
                }
            }
            uint before=GetGuiResources(System.Diagnostics.Process.GetCurrentProcess().Handle,0);
            for(int i=0;i<20;i++)WindowsTextRasterizer.Render("Arial","HAB 03","BoldItalic");
            uint after=GetGuiResources(System.Diagnostics.Process.GetCurrentProcess().Handle,0);
            Check(after<=before+2,"repeated offscreen renders release GDI handles");
            Console.WriteLine("PASS: "+count+" real Windows font-raster checks; Unity upload/rendering not executed.");return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
