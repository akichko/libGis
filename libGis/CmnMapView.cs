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

namespace libGis
{
    public class CmnMapView
    {

        public string MakeKml(LatLon start, LatLon dest, LatLon[] route)
        {
            StringBuilder kmlString = new System.Text.StringBuilder();

            kmlString.AppendLine($"<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            kmlString.AppendLine("<kml xmlns = \"http://www.opengis.net/kml/2.2\" xmlns:gx = \"http://www.google.com/kml/ext/2.2\" xmlns:kml = \"http://www.opengis.net/kml/2.2\" xmlns:atom = \"http://www.w3.org/2005/Atom\">\n");
            kmlString.AppendLine("<Document>");
            kmlString.AppendLine("<name>KmlFile</name>");
            kmlString.AppendLine("<open>1</open>");
            kmlString.AppendLine("<Placemark>");
            kmlString.AppendLine("<name>Origin</name>");
            kmlString.AppendLine($"<Point><coordinates> { start.lon },{ start.lat },0</coordinates></Point>");
            kmlString.AppendLine("</Placemark>");

            kmlString.AppendLine("<Placemark>");
            kmlString.AppendLine("<name>Destination</name>");
            kmlString.AppendLine($"<Point><coordinates> {dest.lon},{dest.lat},0</coordinates></Point>");
            kmlString.AppendLine("</Placemark>");

            kmlString.AppendLine("<Placemark>");

            kmlString.AppendLine("<name> Route Info </name>");


            kmlString.AppendLine("<Style> <LineStyle> <color> 8000ff00 </color> <width> 10 </width> </LineStyle> </Style>");
            kmlString.AppendLine("<LineString>");
            kmlString.AppendLine("<tessellate> 1 </tessellate>");
            kmlString.AppendLine("<coordinates>");

            foreach (LatLon routePos in route)
            {
                kmlString.AppendLine($"{routePos.lon},{routePos.lat},0");
            }

            kmlString.AppendLine("</coordinates> </LineString>");
            kmlString.AppendLine("</Placemark></Document></kml>");

            return kmlString.ToString();
        }


        public string MakeJson(LatLon start, LatLon dest, LatLon[] route)
        {
            StringBuilder jsonString = new System.Text.StringBuilder();
            jsonString.AppendLine($"{{\"route\":[");

            foreach (LatLon routePos in route.Take(1))
            {
                jsonString.AppendLine($"{{\"latitude\":\"{routePos.lat}\",\"longitude\":\"{routePos.lon}\"}}");
            }

            foreach (LatLon routePos in route.Skip(1))
            {
                jsonString.AppendLine($",{{\"latitude\":\"{routePos.lat}\",\"longitude\":\"{routePos.lon}\"}}");
            }

            jsonString.AppendLine("]}");

            return jsonString.ToString();
        }


    }
}
