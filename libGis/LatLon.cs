/*============================================================================
MIT License

Copyright (c) 2021 akichko

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
============================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akichko.libGis
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

        public (double x, double y) GetOffsetXY(LatLon toLatLon) => CalcOffsetXY(this, toLatLon);

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
            (double sX, double sY) = CalcOffsetXY(P, L1);
            (double eX, double eY) = CalcOffsetXY(P, L2);

            return CalcDistanceOfPointAndLine(0.0, 0.0, sX, sY, eX, eY);
        }

        public static double CalcOffsetOfPointAndLine(LatLon P, LatLon L1, LatLon L2)
        {
            (double sX, double sY) = CalcOffsetXY(P, L1);
            (double eX, double eY) = CalcOffsetXY(P, L2);

            double ratio = CalcOffsetRatioOfPointAndLine(0.0, 0.0, sX, sY, eX, eY);
            return CalcDistanceBetween(L1, L2) * ratio;
        }

        public static double CalcOffsetRatioOfPointAndLine(LatLon P, LatLon L1, LatLon L2)
        {
            (double sX, double sY) = CalcOffsetXY(P, L1);
            (double eX, double eY) = CalcOffsetXY(P, L2);

            double ratio = CalcOffsetRatioOfPointAndLine(0.0, 0.0, sX, sY, eX, eY);
            return ratio;
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

     
        public static (double x, double y) CalcOffsetXY(LatLon B, LatLon T) // B: base, T:target
        {
            LatLon TofLatB = new LatLon(B.lat, T.lon);
            LatLon TofLonB = new LatLon(T.lat, B.lon);

            double x = CalcDistanceBetween(B, TofLatB) * Math.Sign(T.lon - B.lon);
            double y = CalcDistanceBetween(B, TofLonB) * Math.Sign(T.lat - B.lat);

            return (x, y);

        }

        public static PolyLinePos CalcNearestPoint(LatLon latlon, LatLon[] polyline)
        {
            if (latlon == null || polyline == null || polyline.Length <= 1)
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

            //補間点内計算
            double lastOffsetRatio = CalcOffsetRatioOfPointAndLine(latlon, polyline[nearestIndex], polyline[nearestIndex + 1]);


            double lastOffset = CalcDistanceBetween(polyline[nearestIndex], polyline[nearestIndex + 1]) * lastOffsetRatio;
            //double lastOffset = CalcOffsetOfPointAndLine(latlon, polyline[nearestIndex], polyline[nearestIndex + 1]);

            offset += lastOffset;
            LatLon nearestLatLon = CalcOffsetLatLon(polyline[nearestIndex], polyline[nearestIndex + 1], lastOffset);

            return new PolyLinePos(nearestLatLon, (float)(nearestIndex + lastOffsetRatio), offset);

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


        //private静的メソッド

        private static double CalcDistanceOfPointAndLine(double pX, double pY, double sX, double sY, double eX, double eY)
        {
            //xP,yP が点
            double seX = eX - sX;
            double seY = eY - sY;
            double seX2 = seX * seX;
            double seY2 = seY * seY;
            double r2 = seX2 + seY2;
            double tt = -(seX * (sX - pX) + seY * (sY - pY));

            if (tt < 0) // P < S
            {
                return Math.Sqrt((sX - pX) * (sX - pX) + (sY - pY) * (sY - pY));
            }
            else if (tt > r2) // E < P
            {
                return Math.Sqrt((eX - pX) * (eX - pX) + (eY - pY) * (eY - pY));
            }
            else // S < P < E
            {
                double f1 = seX * (sY - pY) - seY * (sX - pX);
                return Math.Sqrt((f1 * f1) / r2);
            }
        }

        private static double CalcOffsetRatioOfPointAndLine(double pX, double pY, double sX, double sY, double eX, double eY)
        {
            //xP,yP が点
            double seX = eX - sX;
            double seY = eY - sY;
            double seX2 = seX * seX;
            double seY2 = seY * seY;
            double r2 = seX2 + seY2;
            double tt = -(seX * (sX - pX) + seY * (sY - pY));

            if (tt < 0) // P < S
            {
                return 0.0;
            }
            else if (tt > r2) // E < P
            {
                return 1.0;
            }
            else // S < P < E
            {
                double spX = pX - sX;
                double spY = pY - sY;
                return ((seX * spX) + (seY * spY)) / ((seX * seX) + (seY * seY));
            }
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
        public LatLon latLon; //最近傍点
        public float index; //形状点番号
        public double shapeOffset; //始点からの距離

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

    public class XYd
    {
        double x;
        double y;
    }
}
