using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libGis
{
    class CmnMapView
    {




        public string WriteKml(LatLon start, LatLon dest, List<LatLon> route, string fileName)
        {

            string kmlString =
            $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
            + "<kml xmlns = \"http://www.opengis.net/kml/2.2\" xmlns:gx = \"http://www.google.com/kml/ext/2.2\" xmlns:kml = \"http://www.opengis.net/kml/2.2\" xmlns:atom = \"http://www.w3.org/2005/Atom\">\n"
            + "<Document>"
            + "<name>KmlFile</name>"
            + "<open>1</open>\n"
            + "<Placemark>"
            + "<name>スタート</name>"
            + $"<Point><coordinates> { start.lon },{ start.lat },0</coordinates></Point>"
            + "</Placemark>\n"

            + "<Placemark>"
            + "<name>目的地</name>"
            + $"<Point><coordinates> {dest.lon},{dest.lat},0</coordinates></Point>"
            + "</Placemark>\n"

            + "<Placemark>"

            + "<name> ルート情報 </name>"


            + "<Style> <LineStyle> <color> ffff5500 </color> <width> 5 </width> </LineStyle> </Style>"
            + "<LineString>"
            + "<tessellate> 1 </tessellate>"
            + "<coordinates>\n";

            foreach (LatLon routePos in route)
            {
                kmlString += $"{routePos.lon},{routePos.lat},0\n";
            }

            kmlString +=
                "</coordinates> </LineString>"
                + "</Placemark></Document></kml>";


            return kmlString;
        }

    }
}
