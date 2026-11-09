using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace KSPGraffitiArtist
{
    // Read-only Windows per-user font discovery. Fonts are never copied into GameData.
    public static class UserFonts
    {
        public static IDictionary<string,string> Discover()
        {
            var fonts=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            if(Environment.OSVersion.Platform!=PlatformID.Win32NT)return fonts;
            try {
                using(RegistryKey key=Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts",false)){
                    if(key==null)return fonts;
                    foreach(string entry in key.GetValueNames()){
                        string path=key.GetValue(entry) as string;
                        if(string.IsNullOrEmpty(path))continue;
                        try{if(!Path.IsPathRooted(path))path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),@"Microsoft\Windows\Fonts",path);}
                        catch(ArgumentException){continue;}
                        string family=ReadFamily(path);
                        if(family!=null && !fonts.ContainsKey(family))fonts.Add(family,path);
                    }
                }
            }catch(System.Security.SecurityException){}catch(UnauthorizedAccessException){}catch(IOException){}
            return fonts;
        }
        static ushort U16(BinaryReader reader)
        {return (ushort)((reader.ReadByte()<<8)|reader.ReadByte());}
        static uint U32(BinaryReader reader)
        {return ((uint)U16(reader)<<16)|U16(reader);}
        public static string ReadFamily(string path)
        {
            try {
                string extension=Path.GetExtension(path);
                if(!string.Equals(extension,".otf",StringComparison.OrdinalIgnoreCase) && !string.Equals(extension,".ttf",StringComparison.OrdinalIgnoreCase))return null;
                using(var stream=File.OpenRead(path))using(var reader=new BinaryReader(stream)){
                    if(stream.Length<12)return null;
                    uint signature=U32(reader);
                    if(signature!=0x4F54544F && signature!=0x00010000)return null;
                    int count=U16(reader);if(count>256 || 12L+16L*count>stream.Length)return null;
                    long nameOffset=0,nameLength=0;
                    for(int i=0;i<count;i++){
                        stream.Position=12+16*i;uint tag=U32(reader);U32(reader);uint offset=U32(reader),length=U32(reader);
                        if(tag==0x6E616D65){nameOffset=offset;nameLength=length;break;}
                    }
                    if(nameLength<6 || nameLength>1024*1024 || nameOffset+nameLength>stream.Length)return null;
                    stream.Position=nameOffset;U16(reader);int records=U16(reader),strings=U16(reader);
                    if(records>4096 || 6L+12L*records>nameLength || strings>nameLength)return null;
                    string best=null;int bestScore=-1;
                    for(int i=0;i<records;i++){
                        stream.Position=nameOffset+6+12*i;
                        int platform=U16(reader),encoding=U16(reader),language=U16(reader),id=U16(reader),length=U16(reader),offset=U16(reader);
                        if((id!=1 && id!=16) || length==0 || length>1024 || strings+offset+(long)length>nameLength)continue;
                        if(platform!=0 && !(platform==3 && (encoding==0 || encoding==1 || encoding==10)))continue;
                        if(length%2!=0)continue;
                        stream.Position=nameOffset+strings+offset;
                        string family=Encoding.BigEndianUnicode.GetString(reader.ReadBytes(length));
                        if(!MarkingData.SafeFontName(family))continue;
                        int score=(id==16?100:0)+(platform==3?20:0)+(language==0x409?10:0);
                        if(score>bestScore){best=family;bestScore=score;}
                    }
                    return best;
                }
            }catch(IOException){return null;}catch(UnauthorizedAccessException){return null;}
             catch(ArgumentException){return null;}catch(NotSupportedException){return null;}
             catch(System.Security.SecurityException){return null;}
        }
    }
}
