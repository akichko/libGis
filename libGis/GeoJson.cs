using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Akichko.libGis
{
    public abstract class Json
    {
        protected static JsonSerializerOptions options;

        static Json()
        {
            options = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        }

        public virtual string Serialize() => JsonSerializer.Serialize(this, this.GetType(), options);
    }

    public abstract class GeoJson : Json
    {
        public abstract string type { get; }
        public GjCrs crs;

        //public string type => Enum.GetName(typeof(GeoJsonType), _type);


        //public abstract string Serialize();
        //public virtual string Serialize() => JsonSerializer.Serialize(this, this.GetType(), options);
    }

    public class GjPoint : GeoJson
    {
        public override string type => "Point";
        LatLon latlon;

        public double[] coordinates => new double[]{latlon.lon, latlon.lat};

        public GjPoint(LatLon coordinates)
        {
            this.latlon = coordinates;
        }        
    }


    public class GjMultiPoint : GeoJson
    {
        public override string type => "MultiPoint";
        LatLon[] latlons;

        public double[][] coordinates =>
            latlons.Select(x => new double[] { x.lon, x.lat }).ToArray();

        public GjMultiPoint(LatLon[] latlons)
        {
            this.latlons = latlons;
        }
    }

    public class GjLineString : GeoJson
    {
        public override string type => "LineString";
        LatLon[] latlons;

        public double[][] coordinates =>
            latlons.Select(x => new double[] { x.lon, x.lat }).ToArray();

        public GjLineString(LatLon[] latlons)
        {
            this.latlons = latlons;
        }
    }

    public class GjFeature : GeoJson
    {
        public override string type => "Feature";
        GeoJson geometry;
        Json properties;


        public GjFeature(GeoJson geometry, Json properties = null)
        {
            this.geometry = geometry;
            this.properties = properties;
        }

        public override string Serialize()
        {
            StringBuilder jsonText = new StringBuilder();

            jsonText.AppendLine(@"{ ""type"": ""Feature"", ");
            
            if(properties != null)
            {
                jsonText.AppendLine(@" ""properties"": " + properties.Serialize() + ", " );
            }
            jsonText.AppendLine(@" ""geometry"": " + geometry.Serialize());

            jsonText.AppendLine(@"}");

            return jsonText.ToString();
        }
    }

    public class GjFeatureCollection : GeoJson
    {
        public override string type => "FeatureCollection";
        GjFeature[] features;

        public GjFeatureCollection(GjFeature[] features)
        {
            this.features = features;
        }

        public override string Serialize()
        {
            StringBuilder jsonText = new StringBuilder();

            jsonText.AppendLine(@"{ ""type"": ""FeatureCollection"", ""features"": [");

            features.Take(1).ForEach(x => jsonText.Append(x.Serialize()));
            features.Skip(1).ForEach(x => jsonText.Append($", {x.Serialize()}"));

            jsonText.AppendLine(@"]}");

            return jsonText.ToString();
        }
    }

    public enum GeoJsonType
    {
        Point = 1,
        MultiPoint,
        LineString,
        MultiLineString,
        Polygon,
        MultiPolygon,
        GeometryCollection,
        Feature,
        FeatureCollection
    }

    public class GjCrs
    {

    }
}
