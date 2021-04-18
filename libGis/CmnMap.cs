using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace libGis
{
    //
    /* Tileコード *************************************************************/
    public abstract class CmnTileCode
    {
        //プロパティ
        public virtual uint tileId { get; protected set; }
        public virtual byte Lv { get { return CalcTileLv(tileId); } }
        public virtual int X { get { return CalcTileX(tileId); } }
        public virtual int Y { get { return CalcTileY(tileId); } }

        //抽象メソッド

        public abstract byte DefaultLevel { get; }

        public abstract uint CalcTileId(int x, int y, byte level);

        public abstract int CalcTileX(uint tileId);

        public abstract int CalcTileY(uint tileId);

        public abstract byte CalcTileLv(uint tileId);

        public abstract uint CalcTileId(LatLon latlon, byte level);

        public abstract LatLon CalcLatLon(uint tileId, ERectPos tilePos = ERectPos.Center);


        //通常メソッド

        public virtual uint CalcTileId(int x, int y)
        {
            return CalcTileId(x, y, DefaultLevel);
        }

        public virtual uint CalcTileId(LatLon latlon)
        {
            return CalcTileId(latlon, DefaultLevel);
        }

        public virtual uint CalcTileId(TileXYL xyl)
        {
            return CalcTileId(xyl.x, xyl.y, xyl.lv);
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


        public virtual uint CalcOffsetTileId(Int16 offsetX, Int16 offsetY)
        {
            return CalcTileId(X + offsetX, Y + offsetY, Lv);
        }

        public virtual TileXY CalcTileOffsetTo(uint tileId)
        {
            int offsetX = CalcTileX(tileId) - this.X;
            int offsetY = CalcTileY(tileId) - this.Y;

            return new TileXY(offsetX, offsetY);
        }

        public virtual TileXY CalcTileAbsOffset(uint tileIdA, uint tileIdB)
        {
            int offsetX = Math.Abs(CalcTileX(tileIdA) - CalcTileX(tileIdB));
            int offsetY = Math.Abs(CalcTileY(tileIdA) - CalcTileY(tileIdB));

            return new TileXY(offsetX, offsetY);
        }

        public virtual LatLon[] GetGeometry()
        {
            LatLon[] ret = new LatLon[5];
            ret[0] = CalcLatLon(tileId, ERectPos.SouthWest);
            ret[1] = CalcLatLon(tileId, ERectPos.SouthEast);
            ret[2] = CalcLatLon(tileId, ERectPos.NorthEast);
            ret[3] = CalcLatLon(tileId, ERectPos.NorthWest);
            ret[4] = CalcLatLon(tileId, ERectPos.SouthWest);

            return ret;
        }
    }

    public struct CmnTileOffset
    {
        public Int16 offsetX;
        public Int16 offsetY;
    }


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


    /* Tileデータ *************************************************************/
    public abstract class CmnObj : IViewObj
    {
        //抽象プロパティ
        public abstract UInt64 Id { get; }
        public abstract UInt16 Type { get; }

        public virtual UInt16 SubType { get { return 0xffff; } }
        public virtual LatLon[] Geometry { get { return null; } }

        //抽象メソッド
        public virtual List<CmnObjRef> GetObjAllRefList() { return null; }

        public virtual List<CmnObjRef> GetObjAllRefList(CmnTile tile) { return null; }

        public virtual List<CmnObjHdlRef> GetObjRefHdlList(int refType, CmnTile tile) { return null; }


        //仮想メソッド

        public virtual double GetDistance(LatLon latlon)
        {
            return latlon.GetDistanceToPolyline(Geometry);

        }

        public virtual LatLon GetCenterLatLon()
        {
            if (Geometry == null)
                return null;

            double lon = (Geometry.Select(x => x.lon).Min() + Geometry.Select(x => x.lon).Max()) / 2.0;
            double lat = (Geometry.Select(x => x.lat).Min() + Geometry.Select(x => x.lat).Max()) / 2.0;

            return new LatLon(lat, lon);
        }

        public virtual CmnObjHandle ToCmnObjHandle(CmnTile tile)
        {
            return new CmnObjHandle(tile, this);
        }


        //描画用

        public virtual List<AttrItemInfo> GetAttributeListItem() { return null; }


        public virtual int DrawData(CmnTile tile, CbGetObjFunc cbGetObjFuncForDraw)
        {
            //Graphic, ViewParam が課題
            return cbGetObjFuncForDraw(this);
        }

    }

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


    public abstract class CmnObjGroup
    {
        public abstract UInt16 Type { get; }

        public bool isDrawable = true;
        public bool isDrawReverse = false;
        public bool isGeoSearchable = true;
        public bool isIdSearchable = true;

        public CmnObj[] objArray;
        public UInt16 loadedSubType = 0;


        public virtual CmnObj GetObj(UInt64 objId)
        {
            if (!isIdSearchable)
                return null;

            return objArray
                ?.Where(x => x.Id == objId)
                .FirstOrDefault();
        }

        public virtual CmnObj GetObj(UInt16 objIndex)
        {
            if (objArray == null || objIndex >= objArray.Length)
                return null;
            else
                return objArray[objIndex];
        }

        public virtual CmnObjHdlDistance GetNearestObj(LatLon latlon, UInt16 maxSubType = 0xFFFF)
        {
            if (!isGeoSearchable)
                return null;

            CmnObjHdlDistance nearestObjDistance = objArray
                ?.Where(x => x.SubType <= maxSubType)
                .Select(x => new CmnObjHdlDistance(null, x, x.GetDistance(latlon)))
                .OrderBy(x => x.distance)
                .FirstOrDefault();

            if (nearestObjDistance == null || nearestObjDistance.distance == double.MaxValue)
                return null;

            return nearestObjDistance;

        }

        public virtual void DrawData(CmnTile tile, CbGetObjFunc cbDrawFunc)
        {
            if (isDrawable)
            {
                if (isDrawReverse)
                    objArray.Reverse().ToList().ForEach(x => x.DrawData(tile, cbDrawFunc));
                    //Array.ForEach(objArray.Reverse().ToArray(), x => x.DrawData(cbDrawFunc));
                    //objArray.Reverse().Select(x => x.DrawData(cbDrawFunc));
                else
                    Array.ForEach(objArray, x => x.DrawData(tile, cbDrawFunc));


                //if (isDrawReverse)
                //{
                //    for (int i = objArray.Length - 1; i >= 0; i--)
                //    {
                //        objArray[i].DrawData(cbDrawFunc);
                //    }
                //}
                //else
                //{
                //    for (int i = 0; i < objArray.Length; i++)
                //    {
                //        objArray[i].DrawData(cbDrawFunc);
                //    }
                //}
            }
        }
    }


    public abstract class CmnTile : CmnObj
    {
        public CmnTileCode tileInfo;
        protected Dictionary<UInt16, CmnObjGroup> objDic;

        override public UInt64 Id { get { return (UInt64)tileInfo.tileId; } }

        public uint tileId { get { return tileInfo.tileId; } }
        public int X { get { return tileInfo.X; } }
        public int Y { get { return tileInfo.Y; } }

        //override public UInt64 Id { get { return tileInfo.tileId; } }


        // 抽象メソッド
        public abstract CmnTile CreateTile(uint tileId);

        //必要に応じてオーバーライド
        public virtual int UpdateObjGroup(CmnObjGroup objGroup) /* abstract ?? */
        {
            UInt16 objType = objGroup.Type;
            //上書き
            objDic[objType] = objGroup;

            return 0;
        }


        //必要に応じてオーバーライド
        public virtual int UpdateObjGroupList(List<CmnObjGroup> objGroupList)
        {
            objGroupList.ForEach(x => this.UpdateObjGroup(x));

            return 0;
        }


        //通常メソッド

         public virtual CmnObjGroup GetObjGroup(UInt16 objType)
        {
            if (objDic.ContainsKey(objType))
                return objDic[objType];
            else
                return null;
        }

        private List<CmnObjGroup> GetObjGroupList(UInt16 objTypeBits = 0xFFFF)
        {

            return objDic
                .Where(x => CheckObjTypeMatch(x.Key, objTypeBits))
                .Select(x => x.Value)
                .ToList();

            //List<CmnObjGroup> objGroupList = new List<CmnObjGroup>();
            //foreach (var dicRec in objDic)
            //{
            //    if(CheckObjTypeMatch(dicRec.Key, objTypeBits))

            //    if ((objType & dicRec.Key) == dicRec.Key)
            //    {
            //        objGroupList.Add(dicRec.Value);
            //    }
            //}
            //return objGroupList;
        }

        private List<UInt16> GetObjTypeList(UInt16 objTypeBits = 0xFFFF)
        {
            return objDic
                .Where(x => CheckObjTypeMatch(x.Key, objTypeBits))
                .Select(x => x.Key)
                .ToList();

            //List<UInt16> objTypeList = new List<UInt16>();

            //foreach (var dicRec in objDic)
            //{
            //    if ((objTypeBits & dicRec.Key) == dicRec.Key)
            //    {
            //        objTypeList.Add(dicRec.Key);
            //    }
            //}
            //return objTypeList;
        }

        public virtual CmnObj[] GetObjArray(UInt16 objType)
        {
            return GetObjGroup(objType)?.objArray;

            //if (!objDic.ContainsKey(objType))
            //    return null;

            //return objDic[objType].objArray;
        }

        //非推奨。GetObjHandle推奨
        public CmnObj GetObj(UInt16 objType, UInt64 objId)
        {
            return GetObjGroup(objType)?.GetObj(objId);

            //return GetObjArray(objType)
            //    ?.Where(x => x.Id == objId)
            //    .FirstOrDefault();

            //CmnObj[] ObjArray = GetObjs(objType);
            //if (ObjArray == null)
            //    return null;

            //for (int i = 0; i < ObjArray.Length; i++)
            //{
            //    if (ObjArray[i].Id == objId)
            //        return ObjArray[i];
            //}
            //return null;
        }

        //非推奨。GetObjHandle推奨
        public CmnObj GetObj(UInt16 objType, UInt16 objIndex)
        {
            return GetObjGroup(objType)?.GetObj(objIndex);

            //CmnObj[] ObjArray = GetObjArray(objType);
            //if (ObjArray == null)
            //    return null;

            //if (objIndex >= ObjArray.Length)
            //    return null;
            //else
            //    return ObjArray[objIndex];
        }

        public virtual CmnObjHandle GetObjHandle(UInt16 objType, UInt64 objId)
        {
            return GetObjGroup(objType)?.GetObj(objId)?.ToCmnObjHandle(this);
        }

        public virtual CmnObjHandle GetObjHandle(UInt16 objType, UInt16 objIndex)
        {
            return GetObjGroup(objType)?.GetObj(objIndex)?.ToCmnObjHandle(this);
        }

        public virtual CmnObjHdlDistance GetNearestObj(LatLon latlon, UInt16 objType = 0xFFFF, UInt16 maxSubType = 0xFFFF)
        {

            var ret = GetObjGroupList(objType)
                ?.Select(x => x?.GetNearestObj(latlon, maxSubType)?.SetTile(this))
                .Where(x=>x!=null)
                .OrderBy(x => x.distance)
                .FirstOrDefault();

            return ret;


            //CmnObjHdlDistance nearestObjDistance = GetObjArray(objType)
            //    ?.Where(x=>x.SubType <= maxSubType)
            //    .Select(x => new CmnObjHdlDistance(this, x, x.GetDistance(latlon)))
            //    .OrderBy(x => x.distance)
            //    .FirstOrDefault();

            //if (nearestObjDistance.distance == double.MaxValue)
            //    return null;

            //return nearestObjDistance;

        }

        //public CmnObjDistance GetNearestObj2(LatLon latlon, UInt16 objType = 0xFFFF, UInt16 maxSubType = 0xFFFF)
        //{
        //    CmnObjDistance nearestObjDistance = new CmnObjDistance(null, double.MaxValue);

        //    foreach (var (type, objArray) in objDic)
        //    {
        //        if (type != objType)
        //            continue;

        //        CmnObjDistance tmpObjDistance = objArray
        //            .Select(x => new CmnObjDistance(x, x.GetDistance(latlon)))
        //            .OrderBy(x => x.distance)
        //            .FirstOrDefault();

        //        if (tmpObjDistance.distance < nearestObjDistance.distance)
        //            nearestObjDistance = tmpObjDistance;
        //    }

        //    return nearestObjDistance;

        //}



        //描画用

        public virtual void DrawData(CbGetObjFunc cbDrawFunc, UInt16 objType = 0xFFFF)
        {
            GetObjGroupList(objType).ForEach(x => x?.DrawData(this, cbDrawFunc));
            //cbDrawFunc(Type, SubType, getGeometry());
        }

        public static bool CheckObjTypeMatch(UInt16 objType, UInt16 objTypeBits)
        {
            if ((objTypeBits & objType) == objType)
                return true;
            else
                return false;
        }


    }



    /* オブジェクト参照クラス *************************************************/

    public class CmnObjHandle
    {
        public CmnTile tile;
        public CmnObj obj;

        public CmnObjHandle(CmnTile tile, CmnObj obj)
        {
            this.tile = tile;
            this.obj = obj;
        }
        public CmnObjHandle SetTile(CmnTile tile)
        {
            this.tile = tile;
            return this;
        }
    }

    public class CmnObjHdlDistance : CmnObjHandle
    {
        public double distance;

        public CmnObjHdlDistance(CmnTile tile, CmnObj obj, double distance) : base(tile, obj)
        {
            this.distance = distance;
        }

        public new CmnObjHdlDistance SetTile(CmnTile tile)
        {
            this.tile = tile;
            return this;
        }
    }

    public class CmnObjHdlRef : CmnObjHandle
    {
        public int refType;
        public CmnObjRef nextRef; //NULLになるまで、データ参照を再帰的に続ける必要がある

        public CmnObjHdlRef(CmnTile tile, CmnObj obj, int refType) : base(tile, obj)
        {
            this.refType = refType;
        }

        public CmnObjHdlRef(CmnTile tile, CmnObj obj, int refType, CmnObjRef objRef) : base(tile, obj)
        {
            this.refType = refType;
            this.nextRef = objRef;
        }

        public CmnObjHdlRef(CmnTile tile, CmnObj obj, int refType, UInt16 objType) : base(tile, obj)
        {
            this.refType = refType;
            this.nextRef = new CmnObjRef(refType, objType);
        }

        public CmnObjHdlRef(CmnObjHandle objHdl, int refType) : base(objHdl?.tile, objHdl?.obj)
        {
            this.refType = refType;
        }
        public static CmnObjHdlRef GenCmnObjHdlRef(CmnObjHandle objHdl, int refType)
        {
            if (objHdl == null)
                return null;

            return new CmnObjHdlRef(objHdl, refType);

        }
    }


    public class CmnObjRef
    {
        //参照種別。最終目的。リンクでも前後や対向車線などを区別する
        public int refType;

        //直近の参照（何度かデータを経由する場合）
        public UInt16 objType;
        public CmnTile tile;
        public uint tileId = 0xffffffff;
        public TileXY tileOFfset;
        public CmnObj obj;
        public UInt64 objId = 0xffffffffffffffff;
        public UInt16 objIndex = 0xffff;
        public bool final = true;

        public CmnObjRef(int refType, ushort objType)
        {
            this.refType = refType;
            this.objType = objType;
        }
    }
    //public class CmnDLinkHandle
    //{
    //    public CmnTile tile;
    //    public CmnObj mapLink;
    //    public byte direction;

    //    public CmnDLinkHandle() { }

    //    public CmnDLinkHandle(CmnTile tile, CmnObj mapLink, byte direction)
    //    {
    //        this.tile = tile;
    //        this.mapLink = mapLink;
    //        this.direction = direction;
    //    }

    //    public CmnDLinkHandle(CmnTile tile, CmnObj mapLink, byte direction, uint tileId, short linkIndex)
    //    {
    //        this.tile = tile;
    //        this.mapLink = mapLink;
    //        this.direction = direction;
    //    }

    //    //public CmnDLinkHandle(LinkHandle linkHdl, byte direction)
    //    //{
    //    //    this.tile = linkHdl.tile;
    //    //    this.mapLink = linkHdl.mapLink;
    //    //    this.direction = direction;
    //    //}

    //}


    public struct TileObjId
    {
        public uint tileId;
        public Int64 id;

        public TileObjId(uint tileId, Int64 id)
        {
            this.tileId = tileId;
            this.id = id;

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

    public abstract class CmnViewParam
    {

    }


    public delegate int CbGetObjFunc(CmnObj cmnObj);

    //public delegate CmnObj CbGetObjFunc();


    public delegate void CbDrawFunc(Object g, Object viewParam, CmnObj cmnObj);


    public interface IViewObj
    {
        //public int DrawData(CbGetObjFunc cbDrawFund);
        
        //public CmnObj DrawData(CbGetObjFunc cbDrawFund);

        List<AttrItemInfo> GetAttributeListItem();

        
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

}
