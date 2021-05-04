using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libGis
{


    //public abstract class GisTile : CmnTile
    //{
    //    public GisTile() { }

    //    public GisTile(uint tileid)
    //    {
    //        tileInfo = new GisTileCode(tileid);


    //        objDic = new Dictionary<UInt16, CmnObjGroup>();
    //}

    //    override public CmnTile CreateTile(uint tileId)
    //    {
    //        return new GisTile(tileId);
    //    }

    //    public abstract int UpdateObjGroup(UInt32 objType, CmnObjGroup objGroup);
    //}


    public class GisTileCode : CmnTileCode //, IView
    {
        const byte ConstDefaultLevel = 2;
        const byte ConstMaxLevel = 7;

        //コンストラクタ
        public GisTileCode()
        {
            this.TileId = 0xFFFFFFFF;
        }
        public GisTileCode(uint tileId)
        {
            this.TileId = tileId;
        }

        //オーバーライドプロパティ
        public override byte DefaultLevel => ConstDefaultLevel;
        public override byte MaxLevel => ConstMaxLevel;


        /* オーバーライドメソッド *****************************************************************************/

        //ID -> XYL
        public override byte CalcTileLv(uint tileId) => GisTileCode.S_CalcLv(tileId);
        public override int CalcTileX(uint tileId) => GisTileCode.S_CalcX(tileId);
        public override int CalcTileY(uint tileId) => GisTileCode.S_CalcY(tileId);

        //XYL -> ID
        public override uint CalcTileId(int x, int y, byte level = ConstDefaultLevel)
        {
            return S_CalcTileId((UInt16)x, (UInt16)y, level);
        }

        // XYL -> LatLon
        public override double CalcTileLon(int tileX, byte tileLv)
        {
            return (tileX << tileLv) / Math.Pow(2, 15) * 360.0;
        }

        public override double CalcTileLat(int tileY, byte tileLv)
        {
            return (tileY << tileLv) / Math.Pow(2, 14) * 180.0;
        }

        // LatLon -> XYL
        public override int CalcTileX(double lon, byte level)
        {
            if (lon < 0) lon += 360.0;

            UInt16 x = (UInt16)((lon / 360.0) * Math.Pow(2, 15));
            x = (UInt16)(x >> level);
            return (int)x;
        }

        public override int CalcTileY(double lat, byte level)
        {
            if (lat < 0) lat += 180.0;

            UInt16 y = (UInt16)((lat / 180.0) * Math.Pow(2, 14));
            y = (UInt16)(y >> level);
            return (int)y;

        }


        //LatLon -> ID
        //public override uint CalcTileId(LatLon latlon, byte level = ConstDefaultLevel)
        //{
        //    return SCalcTileId(latlon, level);
        //}

        //public override LatLon CalcLatLon(uint tileId, ERectPos tilePos = ERectPos.Center)
        //{
        //    return SCalcLatLon(this.tileId, tilePos);
        //}

        //追加メソッド
        //public double CalcDistanceTo(uint tileId)
        //{
        //    return S_CalcTileDistance(this.TileId, tileId);
        //}


        /* Staticメソッド *****************************************************************************/
        public static UInt16 S_CalcX(uint tileId)
        {
            return (UInt16)((tileId & 0x1fffc000) >> 14);
        }

        public static UInt16 S_CalcY(uint tileId)
        {
            return (UInt16)((tileId & 0x00003fff));
        }

        public static byte S_CalcLv(uint tileId)
        {
            return (byte)((tileId & 0xe0000000) >> 29);
        }

        public static uint S_CalcTileId(UInt16 x, UInt16 y, byte level = ConstDefaultLevel)
        {
            return ((uint)level << 29) | ((uint)x << 14) | (uint)y;
        }

        // Staticが必要な場面があるかも
        public static uint S_CalcTileId(LatLon latlon, byte level = ConstDefaultLevel)
        {
            if (latlon.lon < -180.0 || latlon.lon > 180.0 || latlon.lat < -90.0 || latlon.lat > 90.0 || level > ConstMaxLevel)
                return 0xffffffff;

            double tmpLon = latlon.lon;
            double tmpLat = latlon.lat;

            if (tmpLon < 0) tmpLon += 360.0;
            if (tmpLat < 0) tmpLat += 180.0;

            UInt16 x = (UInt16)((latlon.lon / 360.0) * Math.Pow(2, 15));
            x = (UInt16)(x >> level);

            UInt16 y = (UInt16)((latlon.lat / 180.0) * Math.Pow(2, 14));
            y = (UInt16)(y >> level);

            return S_CalcTileId(x, y, level);
        }


        public static double S_CalcTileDistance(uint tileIdA, uint tileIdB)
        {
            return LatLon.CalcDistanceBetween(S_CalcLatLon(tileIdA), S_CalcLatLon(tileIdB));

        }



        public static LatLon S_CalcLatLon(uint tileId, ERectPos tilePos = ERectPos.Center)
        {
            double dx;
            double dy;

            int tileX = S_CalcX(tileId);
            int tileY = S_CalcY(tileId);
            byte tileLv = S_CalcLv(tileId);

            switch (tilePos)
            {
                case ERectPos.SouthWest:
                    dx = tileX << tileLv;
                    dy = tileY << tileLv;
                    break;
                case ERectPos.SouthEast:
                    dx = (tileX + 1) << tileLv;
                    dy = tileY << tileLv;
                    break;
                case ERectPos.NorthWest:
                    dx = tileX << tileLv;
                    dy = (tileY + 1) << tileLv;
                    break;
                case ERectPos.NorthEast:
                    dx = (tileX + 1) << tileLv;
                    dy = (tileY + 1) << tileLv;
                    break;
                case ERectPos.Center:
                    dx = ((tileX << tileLv) + ((tileX + 1) << tileLv)) * 0.5;
                    dy = ((tileY << tileLv) + ((tileY + 1) << tileLv)) * 0.5;
                    break;
                default:
                    return null;
            }

            double tmpLon = dx / Math.Pow(2, 15) * 360.0;
            double tmpLat = dy / Math.Pow(2, 14) * 180.0;

            return new LatLon(tmpLat, tmpLon);
        }


        //
        //public static List<uint> S_CalcTileIdAround(uint tileId, int distanceX, int distanceY)
        //{
        //    IEnumerable<int> rangeX = Enumerable.Range(S_CalcX(tileId) - distanceX, distanceX * 2 + 1);
        //    IEnumerable<int> rangeY = Enumerable.Range(S_CalcX(tileId) - distanceY, distanceY * 2 + 1);

        //    List<uint> retList = new List<uint>();
        //    foreach (var x in rangeX)
        //    {
        //        foreach (var y in rangeY)
        //        {
        //            retList.Add(GisTileCode.S_CalcTileId((ushort)x, (ushort)y));
        //        }

        //    }

        //    return retList;

        //}

        //public static List<uint> CalcTileEllipse(uint tileIdA, uint tileIdB, double ratio)
        //{
        //    List<uint> retList = new List<uint>();

        //    if (tileIdA == tileIdB)
        //    {
        //        return S_CalcTileIdAround(tileIdA, 1, 1);
        //    }

        //    //大き目に取る
        //    //TileXY tA = new TileXY(tileIdA);
        //    //TileXY tB = new TileXY(tileIdB);

        //    uint aX = S_CalcX(tileIdA);
        //    uint aY = S_CalcY(tileIdA);
        //    uint bX = S_CalcX(tileIdB);
        //    uint bY = S_CalcY(tileIdB);

        //    int diffX = Math.Abs((int)(aX - bX));
        //    int diffY = Math.Abs((int)(aY - bY));

        //    int lengthXY = diffX + diffY;

        //    int minX = (int)Math.Min(aX, bX);
        //    int minY = (int)Math.Min(aY, bY);

        //    IEnumerable<int> rangeX = Enumerable.Range(minX - lengthXY, diffX + lengthXY * 2 + 1);
        //    IEnumerable<int> rangeY = Enumerable.Range(minY - lengthXY, diffY + lengthXY * 2 + 1);

        //    int debugX = rangeX.Count();
        //    int deBugY = rangeY.Count();
        //    foreach (var x in rangeX)
        //    {
        //        foreach (var y in rangeY)
        //        {
        //            retList.Add(S_CalcTileId((ushort)x, (ushort)y));
        //        }

        //    }

        //    //選別
        //    double baseLength = S_CalcTileDistance(tileIdA, tileIdB) * ratio;

        //    retList = retList.Where(x =>
        //        S_CalcTileDistance(x, tileIdA) + S_CalcTileDistance(x, tileIdB) <= baseLength
        //    ).ToList();



        //    return retList;

        //}

    }






}
