using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libGis
{
    public class LatLon
    {
        public double lat;
        public double lon;

        // コンストラクタ 
        public LatLon() { }
        public LatLon(double lat, double lon)
        {
            this.lat = lat;
            this.lon = lon;
        }

        // メソッド

        public double GetDistanceTo(LatLon toLatLon) => CalcDistanceBetween(this, toLatLon);

        public double GetDistanceToLine(LatLon A, LatLon B) => CalcDistanceOfPointAndLine(this, A, B);

        public double GetDistanceToPolyline(LatLon[] polyline) => CalcDistanceBetween(this, polyline);

        public double GetDistanceToPolyline(List<LatLon> polyline) => GetDistanceToPolyline(polyline.ToArray());

        public LatLon GetOffsetLatLon(double meterToEast, double meterToNorth) => CalcOffsetLatLon(this, meterToEast, meterToNorth);


        public new string ToString()
        {
            return $"{lon:F7}_{lat:F7}";
        }


        //静的メソッド

        public static double CalcLength(LatLon[] polyline)
        {
            if (polyline == null || polyline.Length < 2)
                return double.MaxValue;

            double length = 0;
            for (int i = 1; i < polyline.Length; i++)
            {
                length += LatLon.CalcDistanceBetween(polyline[i], polyline[i - 1]);
            }

            return length;
        }

        public static double CalcDistanceBetween(LatLon latLonA, LatLon latLonB)
        {
            int Rx = 6378137;
            double E2 = 6.69437999019758E-03;// '第2離心率(e^2)

            double di = latLonA.lat - latLonB.lat;

            double dk = latLonA.lon - latLonB.lon;
            double i = (latLonB.lat + latLonA.lat) / 2;

            double W = Math.Sqrt(1 - E2 * Math.Pow(Math.Sin(i * Math.PI / 180), 2));
            double M = Rx * (1 - E2) / Math.Pow(W, 3);
            double N = Rx / W;

            return Math.Sqrt(Math.Pow((di * Math.PI / 180 * M), 2) + Math.Pow((dk * Math.PI / 180 * N * Math.Cos(i * Math.PI / 180)), 2));
        }

        public static double CalcDistanceBetween(LatLon latLon, LatLon[] polyline)
        {
            if (polyline == null || polyline.Length == 0)
                return double.MaxValue;

            else if (polyline.Length == 1)
                return CalcDistanceBetween(polyline[0], latLon);

            double minDistance = Double.MaxValue;
            double tmp;

            for (int i = 0; i < polyline.Length - 1; i++)
            {
                tmp = latLon.GetDistanceToLine(polyline[i], polyline[i + 1]);
                if (tmp < minDistance)
                {
                    minDistance = tmp;
                }
            }

            return minDistance;

        }

        public static double CalcDistanceOfPointAndLine(LatLon P, LatLon L1, LatLon L2)
        {
            LatLon AofSameLat = new LatLon(P.lat, L1.lon);
            LatLon AofSameLon = new LatLon(L1.lat, P.lon);
            LatLon BofSameLat = new LatLon(P.lat, L2.lon);
            LatLon BofSameLon = new LatLon(L2.lat, P.lon);


            double x1 = CalcDistanceBetween(P, AofSameLat) * Math.Sign(L1.lon - P.lon);
            double y1 = CalcDistanceBetween(P, AofSameLon) * Math.Sign(L1.lat - P.lat);
            double x2 = CalcDistanceBetween(P, BofSameLat) * Math.Sign(L2.lon - P.lon);
            double y2 = CalcDistanceBetween(P, BofSameLon) * Math.Sign(L2.lat - P.lat);

            return CalcDistanceOfPointAndLine(0.0, 0.0, x1, y1, x2, y2);

        }

        private static double CalcDistanceOfPointAndLine(double x0, double y0, double x1, double y1, double x2, double y2)
        {
            //x0,y0 が点
            double a = x2 - x1;
            double b = y2 - y1;
            double a2 = a * a;
            double b2 = b * b;
            double r2 = a2 + b2;
            double tt = -(a * (x1 - x0) + b * (y1 - y0));

            if (tt < 0)
            {
                return Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            }
            if (tt > r2)
            {
                return Math.Sqrt((x2 - x0) * (x2 - x0) + (y2 - y0) * (y2 - 0));
            }

            double f1 = a * (y1 - y0) - b * (x1 - x0);

            return Math.Sqrt((f1 * f1) / r2);
        }

        public static LatLon CalcOffsetLatLon(LatLon latlon, double meterToEast, double meterToNorth)
        {
            double meterXperDgree = CalcDistanceBetween(latlon, new LatLon(latlon.lat, latlon.lon + 1.0));
            double meterYperDgree = CalcDistanceBetween(latlon, new LatLon(latlon.lat + 1.0, latlon.lon));

            return new LatLon(latlon.lat + meterToNorth / meterYperDgree , latlon.lon + meterToEast / meterXperDgree);
        }

        public static LatLon CalcOffsetLatLon(LatLon baseLatLon, LatLon toLatLon, double offsetMeter)
        {
            double lineDistance = LatLon.CalcDistanceBetween(baseLatLon, toLatLon);

            return baseLatLon + (toLatLon - baseLatLon) * offsetMeter / lineDistance;
        }

        public static PolyLinePos CalcNearestPoint(LatLon latlon, LatLon[] polyline)  //開発中
        {
            if (polyline == null || polyline.Length <= 1)
                return null;

            double minDistance = Double.MaxValue;
            
            int nearestIndex = -1;

            //最近傍計算
            for (int i = 0; i < polyline.Length - 1; i++)
            {
                double tmp = latlon.GetDistanceToLine(polyline[i], polyline[i + 1]);
                if (tmp < minDistance)
                {
                    minDistance = tmp;
                    nearestIndex = i;
                }
            }

            //距離・オフセット計算
            double offset = 0;
            for (int i = 0; i < nearestIndex; i++)
            {
                offset += polyline[i].GetDistanceTo(polyline[i + 1]);
            }

            //　小数点以下はいつか作る

            return new PolyLinePos(polyline[nearestIndex], (float)nearestIndex, offset);

        }

        public static LatLon Parse(string s)
        {
            if (s == null)
                return null;

            //エラー処理いつか作る
            string[] latlonStr = s.Split('_');
            return new LatLon(double.Parse(latlonStr[1]), double.Parse(latlonStr[0]));

        }

        public static LatLon[] DouglasPeuker(LatLon[] geometry, double errDist, double maxOffsetLength = double.MaxValue)
        {
            byte[] useFlag = new byte[geometry.Length];
            useFlag[0] = 1;
            useFlag[geometry.Length - 1] = 1;

            LatLon[] tmpGeometry;
            
            while (true) {
                tmpGeometry = geometry.Where((x, i) => useFlag[i] == 1).ToArray();
                
                var mostDistPoint = geometry
                    .Select((latlon, index) => new { latlon, index })
                    .Where(x => useFlag[x.index] == 0)
                    .Select(x => new { d = CalcDistanceBetween(x.latlon, tmpGeometry), i = x.index })
                    .OrderByDescending(x => x.d)
                    .FirstOrDefault();

                if (mostDistPoint == null)
                    break;

                if(mostDistPoint.d < errDist)
                {
                    break;
                }
                else
                {
                    useFlag[mostDistPoint.i] = 1;
                }
            }


            //補完点距離の上限オーバー確認

            List<LatLon> retLatLon = new List<LatLon>();
            LatLon baseLatLon = geometry[0];
            retLatLon.Add(baseLatLon);

            for (int i = 1; i < geometry.Length; i++)
            {

                if (baseLatLon.GetDistanceTo(geometry[i]) > maxOffsetLength)
                {
                    //１つ手前の補完点が利用可能
                    if(useFlag[i-1] == 0)
                    {
                        retLatLon.Add(geometry[i - 1]);
                        baseLatLon = geometry[i - 1];
                        useFlag[i - 1] = 1;
                        i--;
                        continue;

                    }

                    //利用できない場合、補完点を生成

                    LatLon tmpLatLon = LatLon.CalcOffsetLatLon(baseLatLon, geometry[i], maxOffsetLength);

                    retLatLon.Add(tmpLatLon);
                    baseLatLon = tmpLatLon;
                    i--;
                    continue;
                }

                if (useFlag[i] == 1)
                {
                    retLatLon.Add(geometry[i]);
                    baseLatLon = geometry[i];

                }
            }        

            return retLatLon.ToArray();
        
        }


        //演算子オーバーライド

        public static LatLon operator +(LatLon a, LatLon b)
        {
            return new LatLon(a.lat + b.lat, a.lon + b.lon);
        }

        public static LatLon operator -(LatLon a, LatLon b)
        {
            return new LatLon(a.lat - b.lat, a.lon - b.lon);
        }

        public static LatLon operator *(LatLon a, double b)
        {
            return new LatLon(a.lat * b, a.lon * b);
        }

        public static LatLon operator /(LatLon a, double b)
        {
            return new LatLon(a.lat / b, a.lon / b);
        }

    }


    public class PolyLinePos  //開発中
    {
        LatLon latLon; //最近傍点
        float index; //形状点番号
        double shapeOffset; //始点からの距離

        public PolyLinePos(LatLon latlon, float index, double shapeOffset)
        {
            this.latLon = latlon;
            this.index = index;
            this.shapeOffset = shapeOffset;
        }
    }


    public class LatLonZ : LatLon
    {
        public double z;

        public LatLonZ() { }

        public LatLonZ(double lat, double lon, double z) : base(lat, lon)
        {
            this.z = z;
        }

    }
}
