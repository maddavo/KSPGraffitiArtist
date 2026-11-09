using System;
using System.Collections.Generic;
using System.Globalization;

namespace KSPGraffitiArtist
{
    // Save-local world anchors; terrain coordinates remain doubles, not scene positions.
    public sealed class WorldSurfaceData
    {
        public const int MaxSurfaces=128;
        public string Kind="terrain",Body="",Id=Guid.NewGuid().ToString("N");
        public double Latitude,Longitude,Altitude;
        public readonly MarkingCollection Marks=new MarkingCollection();
        public bool Valid()
        {
            Guid id;
            return (Kind=="terrain" || Kind=="kk") && MarkingData.SafeFontName(Body) && Guid.TryParse(Id,out id)
                && !double.IsNaN(Latitude) && !double.IsNaN(Longitude) && !double.IsNaN(Altitude)
                && Math.Abs(Latitude)<=90 && Math.Abs(Longitude)<=180 && Math.Abs(Altitude)<=10000000;
        }
        public Dictionary<string,string> Encode()
        {
            if(!Valid())throw new InvalidOperationException("Invalid world paint anchor");
            return new Dictionary<string,string>{{"schema","1"},{"kind",Kind},{"body",Body},{"id",Id},
                {"latitude",Latitude.ToString("R",CultureInfo.InvariantCulture)},
                {"longitude",Longitude.ToString("R",CultureInfo.InvariantCulture)},
                {"altitude",Altitude.ToString("R",CultureInfo.InvariantCulture)}};
        }
        public static bool TryDecode(IDictionary<string,string> values,out WorldSurfaceData data)
        {
            data=null;if(values==null)return false;
            try {
                if(values["schema"]!="1")return false;
                var result=new WorldSurfaceData{Kind=values["kind"],Body=values["body"],Id=values["id"],
                    Latitude=double.Parse(values["latitude"],CultureInfo.InvariantCulture),
                    Longitude=double.Parse(values["longitude"],CultureInfo.InvariantCulture),
                    Altitude=double.Parse(values["altitude"],CultureInfo.InvariantCulture)};
                if(!result.Valid())return false;data=result;return true;
            }catch(ArgumentException){return false;}catch(KeyNotFoundException){return false;}
             catch(FormatException){return false;}catch(OverflowException){return false;}
        }
    }
}
