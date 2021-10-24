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
using System.Drawing;

namespace Akichko.libGis
{
    public interface ICmnTileCodeApi
    {

        byte DefaultLevel { get; }
        byte MinLevel { get; }
        byte MaxLevel { get; }

        /* 抽象メソッド ********************************************************/

        //XYL => ID
        uint CalcTileId(int x, int y, byte level);

        //ID => XYL
        int CalcTileX(uint tileId);
        int CalcTileY(uint tileId);
        byte CalcTileLv(uint tileId);

        //XYL => LatLon
        //public double CalcTileLon(int tileX, byte level);
        //public double CalcTileLat(int tileY, byte level);

        //LatLon => XYL
        //protected int CalcTileX(double lon, byte level);
        //protected int CalcTileY(double lat, byte level);

        /* 通常メソッド **********************************************************/

        /* タイルコード変換 */

        //ID => LatLon
        LatLon CalcLatLon(uint tileId, ERectPos tilePos = ERectPos.Center);

        //XYL => LatLon
        LatLon CalcLatLon(TileXYL xyl);

        //LatLon => ID
        uint CalcTileId(LatLon latlon, byte level);

        uint CalcTileId(LatLon latlon);

        //XYL => ID        
        uint CalcTileId(int x, int y);

        //public uint CalcTileId(TileXYL xyl);

        //ID => XYL
        TileXYL CalcTileXYL(uint tileId);

        //LatLon => XYL
        TileXYL CalcTileXYL(LatLon latlon, byte level);

        TileXYL CalcTileXYL(LatLon latlon);


        /* タイル演算 */
        uint CalcOffsetTileId(uint baseTileId, int offsetX, int offsetY);

        double CalcTileLengthX(uint tileId);
        double CalcTileLengthY(uint tileId);


        double CalcTileDistance(uint tileIdA, uint tileIdB);

        double CalcTileMinDistance(uint tileIdA, uint tileIdB);

        List<uint> CalcTileIdAround(uint tileId, int tileRangeX, int tileRangeY);

        List<uint> CalcTileIdAround(LatLon latlon, double radius, byte level);

        TileXY CalcTileOffset(uint baseTileId, uint tileId);

        TileXY CalcTileAbsOffset(uint tileIdA, uint tileIdB);


        List<uint> CalcTileEllipse(uint tileIdA, uint tileIdB, double ratio);


    }


    /* Tileコード *************************************************************/
    public abstract class CmnTileCode : ICmnTileCodeApi
    {
        //プロパティ
        public virtual uint TileId { get; protected set; }
        public virtual byte Lv => CalcTileLv(TileId);
        public virtual int X => CalcTileX(TileId);
        public virtual int Y => CalcTileY(TileId);

        /* 抽象メソッド ********************************************************/

        public abstract byte DefaultLevel { get; }
        public abstract byte MinLevel { get; }
        public abstract byte MaxLevel { get; }

        //XYL => ID
        public abstract uint CalcTileId(int x, int y, byte level);

        //ID => XYL
        public abstract int CalcTileX(uint tileId);
        public abstract int CalcTileY(uint tileId);
        public abstract byte CalcTileLv(uint tileId);

        //XYL => LatLon
        public abstract double CalcTileLon(int tileX, byte level);
        public abstract double CalcTileLat(int tileY, byte level);

        //LatLon => XYL
        protected abstract int CalcTileX(double lon, byte level);
        protected abstract int CalcTileY(double lat, byte level);

        /* 通常メソッド **********************************************************/

        /* タイルコード変換 */

        //ID => LatLon
        public virtual LatLon CalcLatLon(uint tileId, ERectPos tilePos = ERectPos.Center)
        {
            TileXYL xyl = CalcTileXYL(tileId);

            switch (tilePos)
            {
                case ERectPos.SouthWest:

                    return new LatLon(CalcTileLat(xyl.y, xyl.lv), CalcTileLat(xyl.x, xyl.lv));

                case ERectPos.SouthEast:

                    return new LatLon(CalcTileLat(xyl.y + 1, xyl.lv), CalcTileLat(xyl.x, xyl.lv));

                case ERectPos.NorthWest:

                    return new LatLon(CalcTileLat(xyl.y, xyl.lv), CalcTileLat(xyl.x + 1, xyl.lv));

                case ERectPos.NorthEast:

                    return new LatLon(CalcTileLat(xyl.y + 1, xyl.lv), CalcTileLat(xyl.x + 1, xyl.lv));

                case ERectPos.Center:

                    LatLon tmpSW = new LatLon(CalcTileLat(xyl.y, xyl.lv), CalcTileLat(xyl.x, xyl.lv));
                    LatLon tmpNE = new LatLon(CalcTileLat(xyl.y + 1, xyl.lv), CalcTileLat(xyl.x + 1, xyl.lv));

                    return (tmpSW + tmpNE) / 2.0;

                default:
                    throw new NotImplementedException();
            }

        }

        //XYL => LatLon
        public virtual LatLon CalcLatLon(TileXYL xyl)
        {
            return new LatLon(CalcTileLat(xyl.y, xyl.lv), CalcTileLon(xyl.x, xyl.lv));
        }

        //LatLon => ID
        public virtual uint CalcTileId(LatLon latlon, byte level)
        {
            if (latlon.lon < -180.0 || latlon.lon > 180.0 || latlon.lat < -90.0 || latlon.lat > 90.0 || level > MaxLevel)
                return 0xffffffff;

            //double tmpLon = latlon.lon;
            //double tmpLat = latlon.lat;

            //if (tmpLon < 0) tmpLon += 360.0;
            //if (tmpLat < 0) tmpLat += 180.0;

            int tileX = CalcTileX(latlon.lon, level);
            int tileY = CalcTileY(latlon.lat, level);

            return CalcTileId(tileX, tileY, level);
        }

        public virtual uint CalcTileId(LatLon latlon) => CalcTileId(latlon, DefaultLevel);

        //XYL => ID        
        public virtual uint CalcTileId(int x, int y) => CalcTileId(x, y, DefaultLevel);

        public virtual uint CalcTileId(TileXYL xyl) => CalcTileId(xyl.x, xyl.y, xyl.lv);

        //ID => XYL
        public virtual TileXYL CalcTileXYL(uint tileId)
        {
            TileXYL ret = new TileXYL();
            ret.x = CalcTileX(tileId);
            ret.y = CalcTileY(tileId);
            ret.lv = CalcTileLv(tileId);

            return ret;
        }

        //LatLon => XYL
        public virtual TileXYL CalcTileXYL(LatLon latlon, byte level)
        {
            int x = CalcTileX(latlon.lon, level);
            int y = CalcTileY(latlon.lat, level);
            byte lv = level;

            return new TileXYL(x, y, lv);
        }

        public virtual TileXYL CalcTileXYL(LatLon latlon) => CalcTileXYL(latlon, DefaultLevel);


        /* タイル演算 */
        public virtual uint CalcOffsetTileId(uint baseTileId, int offsetX, int offsetY)
        {
            return CalcTileId(CalcTileX(baseTileId) + offsetX, CalcTileY(baseTileId) + offsetY, CalcTileLv(baseTileId));
        }

        public virtual double CalcTileLengthX(uint tileId)
        {
            return LatLon.CalcDistanceBetween(CalcLatLon(tileId), CalcLatLon(CalcOffsetTileId(tileId, 1, 0)));
        }
        public virtual double CalcTileLengthY(uint tileId)
        {
            return LatLon.CalcDistanceBetween(CalcLatLon(tileId), CalcLatLon(CalcOffsetTileId(tileId, 0, 1)));
        }


        public virtual double CalcTileDistance(uint tileIdA, uint tileIdB)
        {
            return LatLon.CalcDistanceBetween(CalcLatLon(tileIdA), CalcLatLon(tileIdB));

        }

        public virtual double CalcTileMinDistance(uint tileIdA, uint tileIdB)
        {
            int tileXA = CalcTileX(tileIdA);
            int tileXB = CalcTileX(tileIdB);
            int tileYA = CalcTileY(tileIdA);
            int tileYB = CalcTileY(tileIdB);

            ERectPos posTileA = ERectPos.SouthWest;
            ERectPos posTileB = ERectPos.SouthWest;

            if (tileXA < tileXB)
            {
                posTileA = ERectPos.SouthEast;
                if (tileYA < tileYB)
                    posTileA = ERectPos.NorthEast;
                else if (tileYA > tileYB)
                    posTileB = ERectPos.NorthWest;
            }
            else if (tileXB < tileXA)
            {
                posTileB = ERectPos.SouthEast;
                if (tileYB < tileYA)
                    posTileB = ERectPos.NorthEast;
                else if (tileYB > tileYA)
                    posTileA = ERectPos.NorthWest;
            }
            else //(tileXA == tileXB)
            {
                if (tileYA < tileYB)
                    posTileA = ERectPos.NorthWest;
                else if (tileYA > tileYB)
                    posTileB = ERectPos.NorthWest;
            }

            return LatLon.CalcDistanceBetween(CalcLatLon(tileIdA, posTileA), CalcLatLon(tileIdB, posTileB));
        }

        public virtual List<uint> CalcTileIdAround(uint tileId, int tileRangeX, int tileRangeY)
        {
            List<uint> retList = new List<uint>();
            int tileX = CalcTileX(tileId);
            int tileY = CalcTileY(tileId);

            for (int x = CalcTileX(tileId) - tileRangeX; x <= tileX + tileRangeX; x++)
            {
                for (int y = tileY - tileRangeY; y <= tileY + tileRangeY; y++)
                {
                    retList.Add(CalcTileId(x, y));
                }
            }

            return retList;
        }

        public virtual List<uint> CalcTileIdAround(LatLon latlon, double radius, byte level) //実際は円ではなく矩形判定
        {
            List<uint> retList = new List<uint>();

            TileXYL xylSW = CalcTileXYL(latlon.GetOffsetLatLon(-radius, -radius), level);
            TileXYL xylNE = CalcTileXYL(latlon.GetOffsetLatLon(radius, radius), level);

            for (int x = xylSW.x; x <= xylNE.x; x++)
            {
                for (int y = xylSW.y; y <= xylNE.y; y++)
                {
                    retList.Add(CalcTileId(x, y, level));
                }
            }

            return retList;
        }

        public virtual TileXY CalcTileOffset(uint baseTileId, uint tileId)
        {
            int offsetX = CalcTileX(tileId) - CalcTileX(baseTileId);
            int offsetY = CalcTileY(tileId) - CalcTileY(baseTileId);

            return new TileXY(offsetX, offsetY);
        }

        public virtual TileXY CalcTileAbsOffset(uint tileIdA, uint tileIdB)
        {
            int offsetX = Math.Abs(CalcTileX(tileIdA) - CalcTileX(tileIdB));
            int offsetY = Math.Abs(CalcTileY(tileIdA) - CalcTileY(tileIdB));

            return new TileXY(offsetX, offsetY);
        }


        public virtual List<uint> CalcTileEllipse(uint tileIdA, uint tileIdB, double ratio)
        {
            List<uint> retList = new List<uint>();

            if (tileIdA == tileIdB)
            {
                return CalcTileIdAround(tileIdA, 1, 1);
            }

            //大きめに取る
            //TileXY tA = new TileXY(tileIdA);
            //TileXY tB = new TileXY(tileIdB);

            int aX = CalcTileX(tileIdA);
            int aY = CalcTileY(tileIdA);
            int bX = CalcTileX(tileIdB);
            int bY = CalcTileY(tileIdB);

            int diffX = Math.Abs((int)(aX - bX));
            int diffY = Math.Abs((int)(aY - bY));

            int lengthXY = Math.Max(diffX, diffY);

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
                    retList.Add(CalcTileId(x, y));
                }
            }

            //選別
            double baseLength = CalcTileDistance(tileIdA, tileIdB);
            double minBaseLength = (CalcTileLengthX(tileIdA) + CalcTileLengthY(tileIdB)) * 3;
            if (baseLength < minBaseLength)
            {
                baseLength = minBaseLength;
            }
            baseLength *= ratio;

            retList = retList.Where(x =>
                CalcTileDistance(x, tileIdA) + CalcTileDistance(x, tileIdB) <= baseLength
            ).ToList();

            return retList;
        }


        //追加メソッド
        //public override TileXYL CalcTileXYL(LatLon latlon) => CalcTileXYL(latlon, Lv);

        public virtual uint CalcOffsetTileId(Int16 offsetX, Int16 offsetY)
        {
            return CalcTileId(X + offsetX, Y + offsetY, Lv);
        }

        public virtual TileXY CalcTileOffsetTo(uint tileId) => CalcTileOffset(this.TileId, tileId);

        public virtual LatLon[] GetGeometry()
        {
            LatLon[] ret = new LatLon[5];
            ret[0] = CalcLatLon(TileId, ERectPos.SouthWest);
            ret[1] = CalcLatLon(TileId, ERectPos.SouthEast);
            ret[2] = CalcLatLon(TileId, ERectPos.NorthEast);
            ret[3] = CalcLatLon(TileId, ERectPos.NorthWest);
            ret[4] = CalcLatLon(TileId, ERectPos.SouthWest);

            return ret;
        }

    }

    //public struct CmnTileOffset
    //{
    //    public Int16 offsetX;
    //    public Int16 offsetY;
    //}



    public class TileXY
    {
        public int x;
        public int y;

        public TileXY() { }

        public TileXY(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public class TileXYL : TileXY
    {
        public byte lv;

        public TileXYL() { }

        public TileXYL(int x, int y, byte lv) : base(x, y)
        {
            this.lv = lv;
        }


    }


    /* オブジェクト *************************************************************/

    public abstract class CmnObj : IViewObj
    {
        /* 抽象プロパティ =====================================================*/

        public abstract UInt64 Id { get; }

        public abstract UInt32 Type { get; } //ビットフラグ形式(廃止予定)

        /* 仮想プロパティ =====================================================*/

        public virtual UInt16 SubType => 0xffff; //値が小さいほど重要。データ切り捨ての閾値に利用

        public virtual LatLon[] Geometry => null;

        public virtual LatLon Location => null; //未対応

        public virtual UInt16 Index { get; set; }　//現状はメモリを消費する実装

        public virtual double Length => Geometry != null ? LatLon.CalcLength(Geometry) : 0;

        public virtual bool Enable => true; //論理削除など



        /* 経路計算用 ----------------------------------------------------------*/

        public virtual int Cost => Geometry != null ? (int)Length : 10; //override推奨

        public virtual DirectionCode Oneway => DirectionCode.None;

        public virtual bool IsOneway => Oneway == DirectionCode.None ? false : true;

        /* 抽象メソッド：現状なし */

        /* 仮想メソッド =====================================================*/

        public virtual List<CmnObjRef> GetObjAllRefList() { return new List<CmnObjRef>(); } //現状はnull返却禁止

        public virtual List<CmnObjRef> GetObjAllRefList(CmnTile tile, DirectionCode direction = DirectionCode.None) { return new List<CmnObjRef>(); } //現状はnull返却禁止

        public virtual List<CmnObjHdlRef> GetObjRefHdlList(int refType, CmnTile tile, DirectionCode direction = DirectionCode.Positive) { return new List<CmnObjHdlRef>(); } //現状はnull返却禁止

        public virtual double GetDistance(LatLon latlon) => latlon.GetDistanceToPolyline(Geometry);


        public virtual LatLon GetCenterLatLon()
        {
            if (Geometry == null)
                return null;

            double lon = (Geometry.Select(x => x.lon).Min() + Geometry.Select(x => x.lon).Max()) / 2.0;
            double lat = (Geometry.Select(x => x.lat).Min() + Geometry.Select(x => x.lat).Max()) / 2.0;

            return new LatLon(lat, lon);
        }


        /* 必要に応じて継承クラスを返却するようにオーバーライド */
        public virtual CmnObjHandle ToCmnObjHandle(CmnTile tile, DirectionCode direction = DirectionCode.None)
        {
            return new CmnObjHandle(tile, this, direction);
        }


        public virtual bool CheckTimeStamp(int timeStamp) => true;

        /* 描画用 ----------------------------------------------------------*/

        public virtual List<AttrItemInfo> GetAttributeListItem(CmnTile tile, bool detail = false)
        {
            List<AttrItemInfo> listItem = new List<AttrItemInfo>();

            //基本属性
            listItem.Add(new AttrItemInfo(new string[] { "ObjType", $"{Type}" }, null));
            listItem.Add(new AttrItemInfo(new string[] { "SubType", $"{SubType}" }, null));
            listItem.Add(new AttrItemInfo(new string[] { "Id", $"{Id}" }, new AttrTag(0, null, null)));

            listItem.AddRange(GetAttributeListItemGeometry(tile, detail));


            return listItem;
        }

        public virtual List<AttrItemInfo> GetAttributeListItemGeometry(CmnTile tile, bool detail = false)
        {
            List<AttrItemInfo> listItem = new List<AttrItemInfo>();

            //形状詳細表示
            if (detail)
            {
                for (int i = 0; i < Geometry.Length; i++)
                {
                    listItem.Add(new AttrItemInfo(new string[] { $"geometry[{i}]", $"({Geometry[i].ToString()})" }, new AttrTag(0, null, Geometry[i])));
                }
            }
            //簡易表示
            else
            {
                listItem.Add(new AttrItemInfo(new string[] { $"geometry[0]", $"({Geometry[0].ToString()})" }, new AttrTag(0, null, Geometry[0])));
                listItem.Add(new AttrItemInfo(new string[] { $"geometry[{Geometry.Length - 1}]", $"({Geometry[Geometry.Length - 1].ToString()})" }, new AttrTag(0, null, Geometry[Geometry.Length - 1])));

            }
            return listItem;
        }

        public virtual int ExeCallbackFunc(CmnTile tile, CbGetObjFunc cbGetObjFuncForDraw)
        {
            //Graphic, ViewParam が課題           
            return cbGetObjFuncForDraw(this.ToCmnObjHandle(tile));
        }


        public virtual LatLon[] GetGeometry(DirectionCode direction)
        {
            if (direction == DirectionCode.Negative)
                return Geometry.Reverse().ToArray();
            else
                return Geometry;
        }

        public virtual DirectionCode GetDirection(CmnObj obj, bool from)
        {
            //objとの関連から方向取得
            return DirectionCode.None;
        }
    }

    delegate bool Judge<T>(T obj);

    //属性表示用
    public class AttrItemInfo
    {
        public string[] attrStr;
        public int group;
        public Object tag;

        public AttrItemInfo(string[] attrStr, Object tag)
        {
            this.attrStr = attrStr;
            this.tag = tag;
        }
        public AttrItemInfo(string[] attrStr)
        {
            this.attrStr = attrStr;
        }
    }


    /* オブジェクトグループ ****************************************************/

    public abstract class CmnObjGroup
    {
        public UInt32 Type { get; }

        public bool isDrawable = true;
        public bool isDrawReverse = false;
        public bool isGeoSearchable = true;
        public bool isIdSearchable = true;

        public abstract CmnObj[] ObjArray { get; }
        public abstract IEnumerable<CmnObj> Objs { get; }
        public UInt16 loadedSubType = 0;

        //コンストラクタ

        public CmnObjGroup(UInt32 type)
        {
            Type = type;
        }


        //抽象メソッド

        public abstract CmnObj GetObj(UInt16 objIndex);

        public virtual CmnObj GetObj(UInt64 objId) //全走査。２分木探索等したい場合はオーバーライド
        {
            if (!isIdSearchable)
                return null;

            return Objs
                ?.Where(x => x.Id == objId)
                .FirstOrDefault();
        }



        public IEnumerable<CmnObj> GetObjs(Func<CmnObj, bool> selector)
        {
            List<CmnObj> retList = new List<CmnObj>();
            foreach(var obj in Objs)
            {
                if (selector(obj))
                {
                    retList.Add(obj);
                }
            }
            return retList;
        }


        public virtual void SetObjArray(CmnObj[] objArray)
        {
            throw new NotImplementedException();
        }


        public virtual IEnumerable<CmnObj> GetIEnumerableObjs(bool reverse = false)
        {
            if (reverse)
                return Objs.Reverse();

            return Objs;
        }

        /* メソッド */

        public virtual IEnumerable<CmnObj> GetIEnumerableDrawObjs(Filter<ushort> subTypeFilter)
        {
            if (!isDrawable)
                return null;

            return GetIEnumerableObjs(isDrawReverse)
                ?.Where(x => subTypeFilter?.CheckPass(x.SubType) ?? true);
        }

        public bool IsContentsLoaded(ushort subType)
        {
            if (loadedSubType >= subType)
                return true;
            else
                return false;
        }



        //public virtual CmnObjDistance GetNearestObj(LatLon latlon, UInt16 maxSubType = 0xFFFF, UInt16 minSubType = 0)
        //{
        //    if (!isGeoSearchable)
        //        return null;

        //    CmnObjDistance nearestObjDistance = GetIEnumerableObjs()
        //        ?.Where(x => (x.SubType >= minSubType) && (x.SubType <= maxSubType))
        //        .Select(x => new CmnObjDistance(x, x.GetDistance(latlon)))
        //        .OrderBy(x => x.distance)
        //        .FirstOrDefault();

        //    if (nearestObjDistance == null || nearestObjDistance.distance == double.MaxValue)
        //        return null;

        //    return nearestObjDistance;

        //}


        //public virtual CmnObjDistance GetNearestObj(LatLon latlon, ReqSubType reqSubType)
        //{
        //    if (!isGeoSearchable)
        //        return null;

        //    CmnObjDistance nearestObjDistance = GetIEnumerableObjs()
        //        ?.Where(x => (x.SubType >= reqSubType.minSubType) && (x.SubType <= reqSubType.maxSubType))
        //        .Select(x => new CmnObjDistance(x, x.GetDistance(latlon)))
        //        .OrderBy(x => x.distance)
        //        .FirstOrDefault();

        //    if (nearestObjDistance == null || nearestObjDistance.distance == double.MaxValue)
        //        return null;

        //    return nearestObjDistance;

        //}

        public virtual CmnObjDistance GetNearestObj(LatLon latlon, Filter<ushort> subTypeFilter, int timeStamp)
        {
            if (!isGeoSearchable)
                return null;

            CmnObjDistance nearestObjDistance = GetIEnumerableObjs()
                ?.Where(x => subTypeFilter?.CheckPass(x.SubType) ?? true)
                .Where(x => x.CheckTimeStamp(timeStamp))
                .Select(x => new CmnObjDistance(x, x.GetDistance(latlon)))
                .OrderBy(x => x.distance)
                .FirstOrDefault();

            if (nearestObjDistance == null || nearestObjDistance.distance == double.MaxValue)
                return null;

            return nearestObjDistance;

        }

        //不要ならoverrideで無効化
        public virtual void SetIndex() { }

        //public virtual void ExeDrawFunc(CmnTile tile, CbGetObjFunc cbDrawFunc, UInt16 subType = 0xFFFF)
        //{
        //    if (!isDrawable)
        //        return;
 
        //    if (isDrawReverse)
        //        GetIEnumerableObjs()?.Where(x => x.SubType <= subType).Reverse().ToList().ForEach(x => x.ExeCallbackFunc(tile, cbDrawFunc));
        //    else
        //        GetIEnumerableObjs()?.Where(x => x.SubType <= subType).ToList().ForEach(x => x.ExeCallbackFunc(tile, cbDrawFunc));

        //}


        //public virtual void ExeDrawFunc(CmnTile tile, CbGetObjFunc cbDrawFunc, Filter<ushort> subTypeFilter)
        //{
        //    if (!isDrawable)
        //        return;

        //    GetIEnumerableObjs(isDrawReverse)
        //        ?.Where(x => subTypeFilter?.CheckPass(x.SubType) ?? true)
        //        .ToList()
        //        .ForEach(x => x.ExeCallbackFunc(tile, cbDrawFunc));

        //}

        public virtual void AddObj(CmnObj obj)
        {
            throw new NotImplementedException();
        }
    }



    public class CmnObjGroupArray : CmnObjGroup
    {
        public CmnObj[] objArray;

        public CmnObjGroupArray(UInt32 type) : base(type) { }

        public CmnObjGroupArray(UInt32 type, CmnObj[] objArray, UInt16 loadedSubType) : base(type)
        {
            this.loadedSubType = loadedSubType;
            this.objArray = objArray;
        }

        public override CmnObj[] ObjArray => objArray;

        public override IEnumerable<CmnObj> Objs => objArray;
        
        public override CmnObj GetObj(UInt16 objIndex)
        {
            if (objArray == null || objIndex >= objArray.Length)
                return null;
            
            return objArray[objIndex];
        }

        //public virtual CmnObjDistance GetNearestObj(LatLon latlon, UInt16 maxSubType = 0xFFFF)
        //{
        //    if (!isGeoSearchable)
        //        return null;

        //    CmnObjDistance nearestObjDistance;

        //    nearestObjDistance = objArray
        //        ?.Where(x => x.SubType <= maxSubType)
        //        .Select(x => new CmnObjDistance(x, x.GetDistance(latlon)))
        //        .OrderBy(x => x.distance)
        //        .FirstOrDefault();


        //    if (nearestObjDistance == null || nearestObjDistance.distance == double.MaxValue)
        //        return null;

        //    return nearestObjDistance;

        //}


        //不要ならoverrideで無効化
        public override void SetIndex()
        {
            if (objArray == null)
                return;

            for (ushort i = 0; i < objArray.Length; i++)
            {
                objArray[i].Index = i;
            }
        }

        //public virtual void ExeDrawFunc(CmnTile tile, CbGetObjFunc cbDrawFunc, UInt16 subType = 0xFFFF)
        //{
        //    if (isDrawable)
        //    {
        //        if (isDrawReverse)
        //            objArray?.Where(x => x.SubType <= subType).Reverse().ToList().ForEach(x => x.ExeCallbackFunc(tile, cbDrawFunc));
        //        else
        //            objArray?.Where(x => x.SubType <= subType).ToList().ForEach(x => x.ExeCallbackFunc(tile, cbDrawFunc));
        //    }

        //}


        public override void SetObjArray(CmnObj[] objArray)
        {
            this.objArray = objArray;
        }
    }


    public class CmnObjGroupList : CmnObjGroup
    {
        public List<CmnObj> objList;

        public CmnObjGroupList(UInt32 type) : base(type) { }
        public CmnObjGroupList(UInt32 type, CmnObj[] objArray, UInt16 loadedSubType) : base(type)
        {
            this.loadedSubType = loadedSubType;
            this.objList = objArray.ToList();
        }

        public override CmnObj[] ObjArray => objList?.ToArray();
        public override IEnumerable<CmnObj> Objs => objList;

        public override CmnObj GetObj(UInt16 objIndex)
        {
            if (objList == null || objIndex >= objList.Count)
                return null;
            else
                return objList[objIndex];
        }

        //public virtual CmnObjDistance GetNearestObj(LatLon latlon, UInt16 maxSubType = 0xFFFF)
        //{
        //    if (!isGeoSearchable)
        //        return null;

        //    CmnObjDistance nearestObjDistance;

        //    nearestObjDistance = objList
        //        ?.Where(x => x.SubType <= maxSubType)
        //        .Select(x => new CmnObjDistance(x, x.GetDistance(latlon)))
        //        .OrderBy(x => x.distance)
        //        .FirstOrDefault();

        //    if (nearestObjDistance == null || nearestObjDistance.distance == double.MaxValue)
        //        return null;

        //    return nearestObjDistance;

        //}


        //不要ならoverrideで無効化
        public override void SetIndex()
        {
            if (objList == null)
                return;

            for (ushort i = 0; i < objList.Count; i++)
            {
                objList[i].Index = i;
            }

        }

        //public virtual void ExeDrawFunc(CmnTile tile, CbGetObjFunc cbDrawFunc, UInt16 subType = 0xFFFF)
        //{
        //    if (isDrawable)
        //    {
        //        if (isDrawReverse)
        //            objList?.Where(x => x.SubType <= subType).Reverse().ToList().ForEach(x => x.ExeCallbackFunc(tile, cbDrawFunc));
        //        else
        //            objList?.Where(x => x.SubType <= subType).ToList().ForEach(x => x.ExeCallbackFunc(tile, cbDrawFunc));
        //    }

        //}


        public override void AddObj(CmnObj obj)
        {
            objList.Add(obj);
            obj.Index = (UInt16)(objList.Count - 1);
        }

    }

    /* タイル ******************************************************************/

    public abstract class CmnTile : CmnObj
    {
        public CmnTileCode tileCode;
        protected Dictionary<UInt32, CmnObjGroup> objGroupDic;

        //プロパティ
        public CmnTileCode TileCode => tileCode;
        override public UInt64 Id => (UInt64)tileCode.TileId;

        public uint TileId => tileCode.TileId;
        public int X => tileCode.X;
        public int Y => tileCode.Y;
        public int Lv => tileCode.Lv;
        override public LatLon[] Geometry => tileCode.GetGeometry();


        public CmnTile()
        {
            objGroupDic = new Dictionary<UInt32, CmnObjGroup>();

            //継承先ではtileInfoをnewすること
        }

        // 抽象メソッド

        public abstract CmnTile CreateTile(uint tileId);



        //必要に応じてオーバーライド
        public virtual int UpdateObjGroup(CmnObjGroup objGroup) /* abstract ?? */
        {
            UInt32 objType = objGroup.Type;
            //上書き
            objGroupDic[objType] = objGroup;

            return 0;
        }


        //必要に応じてオーバーライド
        public virtual int UpdateObjGroupList(List<CmnObjGroup> objGroupList)
        {
            objGroupList.ForEach(x => this.UpdateObjGroup(x));

            return 0;
        }


        //通常メソッド


        public virtual CmnObjGroup GetObjGroup(UInt32 objType)
        {
            if (objGroupDic.ContainsKey(objType))
                return objGroupDic[objType];
            else
                return null;
        }

        private IEnumerable<CmnObjGroup> GetObjGroupList()
        {
            return objGroupDic
                .Select(x => x.Value)
                .Where(x => x != null);
                    //.ToList();
        }

        public IEnumerable<CmnObjGroup> GetObjGroupList(Filter<uint> objTypefilter = null)
        {
            return objGroupDic
                .Where(x => objTypefilter?.CheckPass(x.Key) ?? true)
                .Select(x => x.Value)
                .Where(x => x != null);
                //.ToList();
        }



        public virtual CmnObj[] GetObjArray(UInt32 objType)
        {
            return GetObjGroup(objType)?.ObjArray;
        }

        //非推奨。GetObjHandle推奨
        //public CmnObj GetObj(UInt32 objType, UInt64 objId)
        //{
        //    return GetObjGroup(objType)?.GetObj(objId);
        //}

        ////非推奨。GetObjHandle推奨
        //public CmnObj GetObj(UInt32 objType, UInt16 objIndex)
        //{
        //    return GetObjGroup(objType)?.GetObj(objIndex);
        //}

        public virtual CmnObjHandle GetObjHandle(UInt32 objType, UInt64 objId)
        {
            return GetObjGroup(objType)?.GetObj(objId)?.ToCmnObjHandle(this);
        }

        public virtual CmnObjHandle GetObjHandle(UInt32 objType, UInt16 objIndex)
        {
            return GetObjGroup(objType)?.GetObj(objIndex)?.ToCmnObjHandle(this);
        }

        public virtual CmnObjHandle GetObjHandle(UInt32 objType, Func<CmnObj, bool> selector)
        {
            return GetObjGroup(objType)?.GetObjs(selector).FirstOrDefault()?.ToCmnObjHandle(this);
        }

        public virtual IEnumerable<CmnObjHandle> GetObjHandles(UInt32 objType, Func<CmnObj, bool> selector)
        {
            return GetObjGroup(objType)?.GetObjs(selector).Select(x=>x.ToCmnObjHandle(this));
        }

        //public virtual CmnObjHdlDistance GetNearestObj(LatLon latlon, UInt32 objType = 0xFFFFFFFF, UInt16 maxSubType = 0xFFFF)
        //{
        //    //?必要か要精査
        //    var ret = GetObjGroupList(objType)
        //        ?.Select(x => x?.GetNearestObj(latlon, maxSubType)?.ToCmnObjHdlDistance(this))
        //        .Where(x => x != null)
        //        .OrderBy(x => x.distance)
        //        .FirstOrDefault();

        //    return ret;
        //}

        //public virtual CmnObjHdlDistance GetNearestObj(LatLon latlon, ReqType reqType)
        //{
        //    return GetObjGroup(reqType.type)?.GetNearestObj(latlon, reqType)?.ToCmnObjHdlDistance(this);
        //}


        //public virtual CmnObjHdlDistance GetNearestObj(LatLon latlon, ReqType[] reqTypeArray)
        //{
        //    var ret = reqTypeArray
        //        .Select(x=> GetNearestObj(latlon, x))
        //        .Where(x => x != null)
        //        .OrderBy(x => x.distance)
        //        .FirstOrDefault();

        //    return ret;
        //}

        public virtual CmnObjHdlDistance GetNearestObj(LatLon latlon, uint objType, Filter<ushort> subTypeFilter, int timeStamp)
        {
            return GetObjGroup(objType)?.GetNearestObj(latlon, subTypeFilter, timeStamp)?.ToCmnObjHdlDistance(this);
        }

        public virtual CmnObjHdlDistance GetNearestObj(LatLon latlon, CmnObjFilter filter, int timeStamp)
        {

            var ret = GetObjGroupList(filter)
                .Select(x => x.GetNearestObj(latlon, filter?.GetSubFilter(x.Type), timeStamp)?.ToCmnObjHdlDistance(this))
                .Where(x => x != null)
                .OrderBy(x => x.distance)
                .FirstOrDefault();

            return ret;
        }

        //描画用

        /* 削除予定 */
        //public virtual void ExeDrawFunc(CbGetObjFunc cbDrawFunc, UInt32 objType = 0xFFFFFFFF, UInt16 subType = 0xFFFF)
        //{
        //    GetObjGroupList(objType).ForEach(x => x?.ExeDrawFunc(this, cbDrawFunc, subType));
        //    //cbDrawFunc(Type, SubType, getGeometry());
        //}

        //public virtual void ExeDrawFunc(CbGetObjFunc cbDrawFunc, Dictionary<uint, ushort> typeDic)
        //{
        //    foreach(var x in typeDic)
        //    {
        //        GetObjGroup(x.Key)?.ExeDrawFunc(this, cbDrawFunc, x.Value);
        //    }
        //}

        //public virtual void ExeDrawFunc(CbGetObjFunc cbDrawFunc, CmnObjFilter filter)
        //{
        //    foreach (var x in GetObjGroupList(filter) ?? Enumerable.Empty<CmnObjGroup>())
        //    {
        //        x.ExeDrawFunc(this, cbDrawFunc, filter?.GetSubFilter(x.Type));
        //    }
        //    //GetObjGroupList(filter).ForEach(x => x?.ExeDrawFunc(this, cbDrawFunc, filter?.GetSubFilter(x.Type)));

        //    //cbDrawFunc(Type, SubType, getGeometry());
        //}


        public virtual void AddObj(UInt32 objType, CmnObj obj)
        {
            GetObjGroup(objType)?.AddObj(obj);
        }

        //削除予定
        public static bool CheckObjTypeMatch(UInt32 objType, UInt32 objTypeBits)
        {
            if ((objTypeBits & objType) != 0)
                return true;
            else
                return false;
        }

        public bool IsContentsLoaded(UInt32 objType, ushort subType) =>
            GetObjGroup(objType)?.IsContentsLoaded(subType) ?? false;


    }



    /* オブジェクト参照クラス *************************************************/


    public class CmnObjHandle
    {
        public CmnTile tile;
        public CmnObj obj;
        public DirectionCode direction = DirectionCode.None;

        /* コンストラクタ・データ生成 ==============================================*/

        //public CmnObjHandle() { }
        public CmnObjHandle(CmnTile tile, CmnObj obj, DirectionCode direction = DirectionCode.None)
        {
            this.tile = tile;
            this.obj = obj;
            this.direction = direction;
        }

        public CmnObjHandle SetDirection(DirectionCode direction)
        {
            this.direction = direction;
            return this;
        }


        /* 追加メソッド =====================================================*/
        public virtual bool IsEqualTo(CmnObjHandle objHdl)
        {
            if (objHdl != null && (this.TileId == objHdl.TileId) && (this.ObjId == objHdl.ObjId) && (this.Index == objHdl.Index))
                return true;
            else
                return false;
        }

        public virtual uint TileId => tile.TileId;

        public virtual LatLon[] DirGeometry => obj.GetGeometry(direction);
        

        /* 属性取得にタイル情報が必要な場合はオーバーライド *****************************/

        /* プロパティ =====================================================*/
        public virtual UInt64 ObjId => obj.Id;
        public virtual UInt32 Type => obj.Type;
        public virtual UInt16 SubType => obj.SubType;
        public virtual LatLon[] Geometry => obj.Geometry;
        public virtual LatLon Location => obj.Location;
        public virtual UInt16 Index => obj.Index;
        public virtual double Length => obj.Length;
        public virtual int Cost => obj.Cost;
        public virtual DirectionCode Oneway => obj.Oneway;
        public virtual bool IsOneway => obj.IsOneway;

        /* メソッド =====================================================*/
        public virtual List<CmnObjRef> GetObjAllRefList() => obj.GetObjAllRefList(tile);
        public virtual List<CmnObjRef> GetObjAllRefList(DirectionCode direction = DirectionCode.None) => obj.GetObjAllRefList(tile, direction);
        public virtual List<CmnObjHdlRef> GetObjRefHdlList(int refType, DirectionCode direction = DirectionCode.None) => obj.GetObjRefHdlList(refType, tile, direction);
        public virtual double GetDistance(LatLon latlon) => obj.GetDistance(latlon);
        public virtual LatLon GetCenterLatLon() => obj.GetCenterLatLon();
        public virtual CmnObjHandle ToCmnObjHandle(CmnTile tile) => obj.ToCmnObjHandle(tile);
        public virtual List<AttrItemInfo> GetAttributeListItem() => obj.GetAttributeListItem(tile);
        public virtual int ExeCallbackFunc(CbGetObjFunc cbGetObjFuncForDraw) => obj.ExeCallbackFunc(tile, cbGetObjFuncForDraw);
        public virtual LatLon[] GetGeometry(DirectionCode direction) => obj.GetGeometry(direction);

    }

    //地理検索用
    public class CmnObjHdlDistance
    {
        public CmnObjHandle objHdl;
        public double distance;

        public CmnObjHdlDistance(CmnObjHandle objHdl, double distance)
        {
            this.objHdl = objHdl;
            this.distance = distance;
        }
    }

    public class CmnObjDistance
    {
        public CmnObj obj;
        public double distance;

        public CmnObjDistance(CmnObj obj, double distance)
        {
            this.obj = obj;
            this.distance = distance;
        }

        public CmnObjHdlDistance ToCmnObjHdlDistance(CmnTile tile)
        {
            return new CmnObjHdlDistance(obj.ToCmnObjHandle(tile), distance);
        }
    }


    public class CmnObjHdlRef //参照属性拡張
    {
        public CmnObjHandle objHdl;
        public int objRefType;
        public CmnObjRef nextRef; //NULLになるまで、データ参照を再帰的に続ける必要がある
        public bool noData = false; //検索結果がない場合、最後のRef情報を返却

        public CmnObjHdlRef(CmnObjHandle objHdl, CmnObjRef nextRef, bool noData = false)
        {
            this.objHdl = objHdl;
            this.nextRef = nextRef;
            this.noData = noData;
        }

        public CmnObjHdlRef(CmnObjHandle objHdl, int refType, UInt32 objType)
        {
            this.objHdl = objHdl;
            this.objRefType = refType;
            this.nextRef = new CmnObjRef(refType, new CmnSearchKey(objType));
        }

        public CmnObjHdlRef(CmnObjHandle objHdl, int refType)
        {
            this.objHdl = objHdl;
            this.objRefType = refType;
            this.nextRef = new CmnObjRef(refType, null);
        }

        //public CmnObjHdlRef(CmnDirObjHandle dirObjHdl, int refType)// : base(objHdl?.tile, objHdl?.obj)
        //{
        //    this.objHdl = (CmnObjHandle)dirObjHdl;
        //    this.isDirObjHandle = true;
        //    this.objRefType = refType;
        //    this.nextRef = new CmnObjRef(refType, null);
        //}

        //public CmnObjHdlRef(CmnTile tile, CmnObj obj, int refType, CmnObjRef objRef) : base(tile, obj)
        //{
        //    this.nextRef.refType = refType;
        //    this.nextRef = objRef;
        //}

        //public CmnObjHdlRef(CmnTile tile, CmnObj obj, int refType, UInt32 objType) : base(tile, obj)
        //{
        //    this.nextRef.refType = refType;
        //    this.nextRef = new CmnObjRef(refType, objType);
        //}

        //public CmnObjHdlRef(CmnObjHandle objHdl, int refType) : base(objHdl?.tile, objHdl?.obj)
        //{
        //    this.nextRef.refType = refType;
        //}
        //public static CmnObjHdlRef GenCmnObjHdlRef(CmnObjHandle objHdl, int refType)
        //{
        //    if (objHdl == null)
        //        return null;

        //    return new CmnObjHdlRef(objHdl, refType);

        //}
    }


    /* オブジェクト検索用クラス **********************************************/

    public class CmnObjRef
    {
        //参照種別。最終目的。リンクでも前後や対向車線などを区別する
        public int refType;
        public CmnSearchKey key; //直近の検索キー
        public bool final = true; //参照先が最終

        public CmnObjRef(int refType, CmnSearchKey key, bool final = true)
        {
            this.refType = refType;
            this.final = final;
            this.key = key;
        }

        public CmnObjRef(int refType, UInt32 objType, bool final = true)
        {
            this.refType = refType;
            this.final = final;
            this.key = new CmnSearchKey(objType);
        }
    }

    public class CmnSearchKey
    {
        public UInt32 objType;
        public CmnTile tile;
        public uint tileId = 0xffffffff;
        public TileXY tileOFfset; //未対応
        public CmnObj obj;
        public UInt64 objId = 0xffffffffffffffff;
        public UInt16 objIndex = 0xffff;
        public DirectionCode objDirection = DirectionCode.None;
        public UInt16 subType = 0xffff;
        public Func<CmnObj, bool> matchFunc;

        public CmnSearchKey(UInt32 objType)
        {
            this.objType = objType;
        }
        public CmnSearchKey AddObjHandle(CmnTile tile, CmnObj obj)
        {
            this.tile = tile;
            this.obj = obj;
            return this;
        }
    }

    public struct TileObjId
    {
        public uint tileId;
        public UInt64 id;

        public TileObjId(uint tileId, UInt64 id)
        {
            this.tileId = tileId;
            this.id = id;
        }

        override public string ToString()
        {
            return $"{tileId}-{id}";
        }
    }

    public struct TileObjIndex
    {
        public uint tileId;
        public short index;

        public TileObjIndex(uint tileId, short index)
        {
            this.tileId = tileId;
            this.index = index;
        }
    }



    /* 描画用 ****************************************************************/

    public delegate int CbGetObjFunc(CmnObjHandle objHdl);

    public delegate void CbDrawFunc(Object g, Object viewParam, CmnObj cmnObj);


    public interface IViewObj
    {

        List<AttrItemInfo> GetAttributeListItem(CmnTile tile, bool detail);

        
    }

    //ビューワ属性表示用
    public class AttrTag
    {
        public int refType;
        public CmnSearchKey searchKey;
        public LatLon latlon;

        public AttrTag(int refType, CmnSearchKey key, LatLon latlon)
        {
            this.refType = refType;
            this.searchKey = key;
            this.latlon = latlon;
        }
    }

    /* コード ****************************************************************/

    public enum ERectPos
    {
        SouthWest,
        SouthEast,
        NorthWest,
        NorthEast,
        Center
    }



    public enum DirectionCode : byte
    {
        //0と1の値が重要
        Negative = 0, //逆方向。終点⇒始点
        Positive = 1, //順方向。始点⇒終点
        None = 0xff   //方向なし
    }

    public static class DirectionCodeExtend
    {
        public static DirectionCode Reverse(this DirectionCode param)
        {
            if (param == DirectionCode.Positive)
                return DirectionCode.Negative;
            else if (param == DirectionCode.Negative)
                return DirectionCode.Positive;
            else
                return param;
        }
    }

    //サンプル。使うかどうかは自由

    public enum ECmnMapContentType
    {
        Link = 0x0001,
        Node = 0x0002,
        LinkGeometry = 0x0004,
        LinkAttribute = 0x0008,
        RoadNetwork = 0x0010,
        All = 0xffff
    }

    public enum ECmnMapRefType
    {
        Selected,
        RelatedLink,
        NextLink,
        BackLink,
        RelatedNode,
        StartNode,
        EndNode,
        RelatedLine,
        LaneBoundary,
        RoadBoundary,
        NextLane,
        BackLane,
        RelatedObj,
        RelatedTile,
        LatLon
    }

}
