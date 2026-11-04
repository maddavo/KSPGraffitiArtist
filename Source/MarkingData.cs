using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace KSPGraffitiArtist
{
    // Unity-free record codec: the same code is exercised by the offline executable.
    public sealed class MarkingData
    {
        public const int MaxPerPart = 32;
        public string Id = Guid.NewGuid().ToString("N");
        public string Kind = "text", Text = "WORKSHOP", Image = "", Font = "Arial";
        public float X, Y, Z, NX, NY, NZ = 1, UX, UY = 1, UZ;
        public float Width = 1, Rotation, R = 1, G = 1, B = 1;
        public MarkingData Copy() { return (MarkingData)MemberwiseClone(); }
        public static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        public static float Wrapped(float angle) { return (angle % 360 + 360) % 360; }
        public bool Valid()
        {
            Guid parsed;
            if (!Guid.TryParseExact(Id, "N", out parsed) || (Kind != "text" && Kind != "image")) return false;
            if (Text == null || Text.Length > 512 || Image == null || Font != "Arial") return false;
            if (Kind == "text" && string.IsNullOrWhiteSpace(Text)) return false;
            if (Kind == "image" && !SafeImageName(Image)) return false;
            float[] values = { X,Y,Z,NX,NY,NZ,UX,UY,UZ,Width,Rotation,R,G,B };
            foreach (float value in values) if (!Finite(value)) return false;
            float normal = NX*NX+NY*NY+NZ*NZ, up = UX*UX+UY*UY+UZ*UZ;
            if (Math.Abs(X)>10000 || Math.Abs(Y)>10000 || Math.Abs(Z)>10000) return false;
            return normal > .99f && normal < 1.01f && up > .99f && up < 1.01f
                && Math.Abs(NX*UX+NY*UY+NZ*UZ)<.01f && Width>=.1f && Width<=4f
                && Rotation>=0 && Rotation<360 && R>=0 && R<=1 && G>=0 && G<=1 && B>=0 && B<=1;
        }
        public Dictionary<string,string> Encode()
        {
            if (!Valid()) throw new InvalidOperationException("Invalid marking");
            var d = new Dictionary<string,string>();
            d["schema"]="1"; d["id"]=Id; d["kind"]=Kind; d["font"]=Font;
            d["text64"]=Convert.ToBase64String(Encoding.UTF8.GetBytes(Text)); d["image"]=Image;
            string[] names={"x","y","z","nx","ny","nz","ux","uy","uz","width","rotation","r","g","b"};
            float[] values={X,Y,Z,NX,NY,NZ,UX,UY,UZ,Width,Rotation,R,G,B};
            for(int i=0;i<names.Length;i++) d[names[i]]=values[i].ToString("R",CultureInfo.InvariantCulture);
            return d;
        }
        public static bool TryDecode(IDictionary<string,string> d, out MarkingData result)
        {
            result=null;
            if(d==null)return false;
            foreach(string value in d.Values)if(value==null)return false;
            try {
                if(d["schema"]!="1" || d["text64"].Length>4096) return false;
                var m = new MarkingData();
                m.Id=d["id"]; m.Kind=d["kind"]; m.Font=d["font"]; m.Image=d["image"];
                m.Text=new UTF8Encoding(false,true).GetString(Convert.FromBase64String(d["text64"]));
                m.X=Number(d,"x"); m.Y=Number(d,"y"); m.Z=Number(d,"z");
                m.NX=Number(d,"nx"); m.NY=Number(d,"ny"); m.NZ=Number(d,"nz");
                m.UX=Number(d,"ux"); m.UY=Number(d,"uy"); m.UZ=Number(d,"uz");
                m.Width=Number(d,"width"); m.Rotation=Number(d,"rotation");
                m.R=Number(d,"r"); m.G=Number(d,"g"); m.B=Number(d,"b");
                if(!m.Valid())return false;
                result=m; return true;
            } catch(ArgumentException) { return false; }
              catch(FormatException) { return false; }
              catch(OverflowException) { return false; }
              catch(KeyNotFoundException) { return false; }
        }
        static float Number(IDictionary<string,string> d,string key)
        { return float.Parse(d[key],NumberStyles.Float,CultureInfo.InvariantCulture); }
        public static bool SafeImageName(string name)
        {
            if(string.IsNullOrEmpty(name) || name.Length>128 || name=="." || name=="..") return false;
            // Basenames only, including on hosts whose path separator differs from Windows.
            if(name.IndexOfAny(new char[]{'/', '\\', ':', '\0'})>=0 || name.IndexOfAny(Path.GetInvalidFileNameChars())>=0) return false;
            return name.EndsWith(".png",StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class MarkingCollection
    {
        readonly List<MarkingData> records=new List<MarkingData>();
        public IList<MarkingData> Snapshot()
        {
            var result=new List<MarkingData>();foreach(MarkingData m in records)result.Add(m.Copy());return result.AsReadOnly();
        }
        public void Clear(){records.Clear();}
        public bool Restore(MarkingData record)
        {
            if(record==null || !record.Valid() || records.Count>=MarkingData.MaxPerPart || records.Exists(m=>m.Id==record.Id))return false;
            records.Add(record.Copy());return true;
        }
        public bool Commit(MarkingData draft)
        {
            if(draft==null || !draft.Valid())return false;
            int index=records.FindIndex(m=>m.Id==draft.Id);
            if(index<0)return Restore(draft);
            records[index]=draft.Copy();return true;
        }
        public void Erase(string id){records.RemoveAll(m=>m.Id==id);}
    }
}
