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

    //    public abstract int UpdateObjGroup(UInt16 objType, CmnObjGroup objGroup);
    //}


    public  class GisTileCode : CmnTileCode //, IView
    {
        const byte ConstDefaultLevel = 2;

        //コンストラクタ
        public GisTileCode()
        {
            this.tileId = 0xFFFFFFFF;
        }
        public GisTileCode(uint tileId)
        {
            this.tileId = tileId;
        }

        //オーバーライドプロパティ
        override public byte DefaultLevel
        {
            get { return ConstDefaultLevel; }
        }


        //オーバーライドメソッド
        override public byte CalcTileLv(uint tileId)
        {
            return GisTileCode.GetLv(tileId);
        }

        override public int CalcTileX(uint tileId)
        {
           return GisTileCode.GetX(tileId); 
        }

        override public int CalcTileY(uint tileId)
        {
            return GisTileCode.GetY(tileId);
        }

        override public uint CalcTileId(int x, int y, byte level = ConstDefaultLevel)
        {
            return SCalcTileId((UInt16)x, (UInt16)y, level);
        }


        override public uint CalcTileId(LatLon latlon, byte level = ConstDefaultLevel)
        {
            return SCalcTileId(latlon, level);
        }


        override public LatLon CalcLatLon(uint tileId, ERectPos tilePos = ERectPos.Center)
        {
            return SCalcLatLon(this.tileId, tilePos);
        }


        //追加メソッド
        public double CalcDistanceTo(uint tileId)
        {
            return SCalcTileDistance(this.tileId, tileId);
        }


        //Staticメソッド
        public static UInt16 GetX(uint tileId)
        {
            return (UInt16)((tileId & 0x1fffc000) >> 14);
        }

        public static UInt16 GetY(uint tileId)
        {
            return (UInt16)((tileId & 0x00003fff));
        }

        public static byte GetLv(uint tileId)
        {
            return (byte)((tileId & 0xe0000000) >> 29);
        }

        public static uint SCalcTileId(UInt16 x, UInt16 y, byte level= ConstDefaultLevel)
        {
            return ((uint)level << 29) | ((uint)x << 14) | (uint)y;
        }

        public static uint SCalcTileId(LatLon latlon, byte level = ConstDefaultLevel)
        {
            if (latlon.lon < -180.0 || latlon.lon > 180.0 || latlon.lat < -90.0 || latlon.lat > 90.0 || level > 6)
                return 0xffffffff;

            double tmpLon = latlon.lon;
            double tmpLat = latlon.lat;

            if (tmpLon < 0) tmpLon += 360.0;
            if (tmpLat < 0) tmpLat += 180.0;

            UInt16 x = (UInt16)((latlon.lon / 360.0) * Math.Pow(2, 15));
            x = (UInt16)(x >> level);

            UInt16 y = (UInt16)((latlon.lat / 180.0) * Math.Pow(2, 14));
            y = (UInt16)(y >> level);

            return SCalcTileId(x, y, level);
        }


        public static double SCalcTileDistance(uint tileIdA, uint tileIdB)
        {
            return LatLon.CalcDistanceBetween(SCalcLatLon(tileIdA), SCalcLatLon(tileIdB));

        }



        public static LatLon SCalcLatLon(uint tileId, ERectPos tilePos = ERectPos.Center)
        {
            double dx;
            double dy;

            int tileX = GetX(tileId);
            int tileY = GetY(tileId);
            byte tileLv = GetLv(tileId);

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
        public static List<uint> CalcTileIdAround(uint tileId, int distanceX, int distanceY)
        {
            IEnumerable<int> rangeX = Enumerable.Range(GetX(tileId) - distanceX, distanceX * 2 + 1);
            IEnumerable<int> rangeY = Enumerable.Range(GetY(tileId) - distanceY, distanceY * 2 + 1);

            List<uint> retList = new List<uint>();
            foreach (var x in rangeX)
            {
                foreach (var y in rangeY)
                {
                    retList.Add(GisTileCode.SCalcTileId((ushort)x, (ushort)y));
                }

            }

            return retList;

        }

        public static List<uint> CalcTileEllipse(uint tileIdA, uint tileIdB, double ratio)
        {
            List<uint> retList = new List<uint>();

            if (tileIdA == tileIdB)
            {
                return CalcTileIdAround(tileIdA, 1, 1);
            }

            //大き目に取る
            //TileXY tA = new TileXY(tileIdA);
            //TileXY tB = new TileXY(tileIdB);

            uint aX = GetX(tileIdA);
            uint aY = GetY(tileIdA);
            uint bX = GetX(tileIdB);
            uint bY = GetY(tileIdB);

            int diffX = Math.Abs((int)(aX - bX));
            int diffY = Math.Abs((int)(aY - bY));

            int lengthXY = diffX + diffY;

            int minX = (int)Math.Min(aX, bX);
            int minY = (int)Math.Min(aY, bY);

            IEnumerable<int> rangeX = Enumerable.Range(minX - lengthXY, diffX + lengthXY * 2 + 1);
            IEnumerable<int> rangeY = Enumerable.Range(minY - lengthXY, diffY + lengthXY * 2 + 1);

            int debugX = rangeX.Count();
            int deBugY = rangeY.Count();
            foreach (var x in rangeX)
            {
                foreach (var y in rangeY)
                {
                    retList.Add(SCalcTileId((ushort)x, (ushort)y));
                }

            }

            //選別
            double baseLength = SCalcTileDistance(tileIdA, tileIdB) * ratio;

            retList = retList.Where(x =>
                SCalcTileDistance(x, tileIdA) + SCalcTileDistance(x, tileIdB) <= baseLength
            ).ToList();



            return retList;

        }
    }




    public class Polyline
    {
        public LatLon[] geometry;
    }

    public class Lane : Polyline { }

    public class Link : Polyline { }


    public class MapPoint
    {
        public LatLon latLon;
    }





}
