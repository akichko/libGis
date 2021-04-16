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

        public uint tileId { get; protected set; }
        public byte Lv { get { return CalcTileLv(tileId); } }
        public int X { get { return CalcTileX(tileId); } }
        public int Y { get { return CalcTileY(tileId); } }

        //抽象メソッド

        abstract public byte DefaultLevel { get; }

        abstract public uint CalcTileId(int x, int y, byte level);

        abstract public int CalcTileX(uint tileId);

        abstract public int CalcTileY(uint tileId);

        abstract public byte CalcTileLv(uint tileId);

        abstract public uint CalcTileId(LatLon latlon, byte level);

        abstract public LatLon CalcLatLon(uint tileId, ERectPos tilePos = ERectPos.Center);


        //通常メソッド

        public uint CalcTileId(int x, int y)
        {
            return CalcTileId(x, y, DefaultLevel);
        }

        public uint CalcTileId(LatLon latlon)
        {
            return CalcTileId(latlon, DefaultLevel);
        }


        public List<uint> CalcTileIdAround(uint tileId, int tileRangeX, int tileRangeY)
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


        public uint CalcOffsetTileId(Int16 offsetX, Int16 offsetY)
        {
            return CalcTileId(X + offsetX, Y + offsetY, Lv);
        }
    }

    public struct CmnTileOffset
    {
        public Int16 offsetX;
        public Int16 offsetY;
    }

    public struct TileXYL
    {
        public int x;
        public int y;
        public byte lv;
    }

    /* Tileデータ *************************************************************/
    public abstract class CmnObj : IViewObj
    {
        //抽象プロパティ
        public abstract UInt64 Id { get; }
        public abstract UInt16 Type { get; }
        public abstract UInt16 SubType { get; }
        public abstract LatLon[] Geometry { get; }


        //抽象メソッド

        public abstract double GetDistance(LatLon latlon);


        //描画用
        public abstract List<string[]> GetAttributeListItem();


        public int DrawData(CbGetObjFunc cbGetObjFuncForDraw)
        {
            //Graphic, ViewParam が課題
            return cbGetObjFuncForDraw(this);
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


        public CmnObj GetObj(UInt64 objId)
        {
            if (!isIdSearchable)
                return null;

            return objArray
                ?.Where(x => x.Id == objId)
                .FirstOrDefault();
        }

        public CmnObj GetObj(UInt16 objIndex)
        {
            if (objArray == null || objIndex >= objArray.Length)
                return null;
            else
                return objArray[objIndex];
        }

        public CmnObjHdlDistance GetNearestObj(LatLon latlon, UInt16 maxSubType = 0xFFFF)
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

        public void DrawData(CbGetObjFunc cbDrawFunc)
        {
            if (isDrawable)
            {
                if (isDrawReverse)
                    objArray.Reverse().Select(x => x.DrawData(cbDrawFunc));
                else
                    Array.ForEach(objArray, x => x.DrawData(cbDrawFunc));


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


    public abstract class CmnTile
    {
        public CmnTileCode tileInfo;
        protected Dictionary<UInt16, CmnObjGroup> objDic;

        public uint tileId { get { return tileInfo.tileId; } }
        public int X { get { return tileInfo.X; } }
        public int Y { get { return tileInfo.Y; } }

        // 抽象メソッド
        abstract public CmnTile CreateTile(uint tileId);

        public abstract int UpdateObjGroup(CmnObjGroup objGroup);

        public abstract int UpdateObjGroupList(List<CmnObjGroup> objGroup);

        //通常メソッド

        public CmnObjGroup GetObjGroup(UInt16 objType)
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

        public CmnObj[] GetObjArray(UInt16 objType)
        {
            return GetObjGroup(objType)?.objArray;

            //if (!objDic.ContainsKey(objType))
            //    return null;

            //return objDic[objType].objArray;
        }

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

        public CmnObjHdlDistance GetNearestObj(LatLon latlon, UInt16 objType = 0xFFFF, UInt16 maxSubType = 0xFFFF)
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

        public void DrawData(CbGetObjFunc cbDrawFunc, UInt16 objType = 0xFFFF)
        {
            GetObjGroupList(objType).ForEach(x => x?.DrawData(cbDrawFunc));
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
        public CmnObj mapObj;

        public CmnObjHandle(CmnTile tile, CmnObj obj)
        {
            this.tile = tile;
            this.mapObj = obj;
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

        public CmnObjHdlDistance(CmnTile tile, CmnObj mapObj, double distance) : base(tile, mapObj)
        {
            this.distance = distance;
        }

        public new CmnObjHdlDistance SetTile(CmnTile tile)
        {
            this.tile = tile;
            return this;
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

        List<string[]> GetAttributeListItem();

        
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
