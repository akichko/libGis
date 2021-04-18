using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libGis
{
    public abstract class CmnMapMgr
    {
        //public CmnTile tileDataApi;
        public CmnTileCode tileApi;
        protected Dictionary<uint, CmnTile> tileDic;
        protected ICmnMapAccess mal;

        //抽象メソッド
        //現状なし。MAL側

        public CmnMapMgr(CmnTileCode tileCode)
        {
           // tileDataApi = tile;
            tileApi = tileCode;
            tileDic = new Dictionary<uint, CmnTile>();
        }

        public int Connect(string mapPath)
        {
            int ret = mal.ConnectMapData(mapPath);
            Console.WriteLine("Connected");

            return ret;
        }

        public int Disconnect()
        {
            int ret = mal.DisconnectMapData();

            Console.WriteLine("disconnected");
            return ret;

        }

        public bool IsConnected
        {
            get  { return mal.IsConnected;   }

        }



        //public int LoadTile(uint tileId, bool multiContents = true, UInt16 reqType = 0xFFFF, UInt16 reqMaxSubType = 0xFFFF)
        //{
        //    if (!IsConnected) return -1;

        //    CmnTile tmpTile;
        //    bool isNew = false;

        //    if (tileDic.ContainsKey(tileId))
        //    {
        //        tmpTile = tileDic[tileId];  
        //    }
        //    else
        //    {
        //        tmpTile = mal.CreateTile(tileId);
        //        isNew = true;
        //    }

        //    List<UInt16> objTypeList;
        //    if (multiContents)
        //        objTypeList = mal.GetMapContentTypeList();
        //    else
        //    {
        //        objTypeList = new List<UInt16>();
        //        objTypeList.Add(reqType);
        //    }

        //    foreach (UInt16 objType in objTypeList)
        //    {
        //        if ((reqType & objType) == objType)
        //        {
        //            //既存データチェック
        //            CmnObjGroup currentMapContents = tmpTile.GetObjGroup(objType);
        //            if (currentMapContents != null && currentMapContents.loadedSubType >= reqMaxSubType)
        //            {
        //                continue;
        //            }

        //            tmpTile.UpdateObjGroup(objType, mal.LoadObjGroup(tileId, objType, reqMaxSubType));
        //        }
        //    }

        //    if (isNew)
        //        tileDic.Add(tileId, tmpTile);

        //    return 0;

        //}

        public int LoadTile(uint tileId, UInt16 reqType = 0xFFFF, UInt16 reqMaxSubType = 0xFFFF)
        {
            if (!IsConnected) return -1;

            CmnTile tmpTile;
            bool isNew = false;

            if (tileDic.ContainsKey(tileId))
            {
                tmpTile = tileDic[tileId];

                //更新必要有無チェック

                //未読み込み（NULL）のObgGroupがあるか
                int numObjGrToBeRead = mal.GetMapContentTypeList()
                    .Where(x => (reqType & x) == x)
                    .Select(x => tmpTile.GetObjGroup(x))
                    .Count(x => x == null || x.loadedSubType < reqMaxSubType);


                if (numObjGrToBeRead == 0)
                    return 0; //更新不要

            }
            //タイルがなければ作成
            else
            {
                tmpTile = mal.CreateTile(tileId);
                isNew = true;
            }


            //必要となった場合


            //データ読み込み
            List<CmnObjGroup> tmpObjGrList = mal.LoadObjGroupList(tileId, reqType, reqMaxSubType);

            //タイル更新
            tmpTile.UpdateObjGroupList(tmpObjGrList);


            if (isNew)
                tileDic.Add(tileId, tmpTile);

            return 0;

        }



        public bool UnloadTile(uint tileId)
        {
            return tileDic.Remove(tileId);
        }



        //タイル検索メソッド *****************************************************

        public CmnTile SearchTile(uint tileId)
        {
            if (tileDic.ContainsKey(tileId))
                return tileDic[tileId];
            else
                return null;
        }

        public CmnTile SearchTile(LatLon latlon)
        {
            uint tileId = tileApi.CalcTileId(latlon);
            return SearchTile(tileId);
        }

        public List<CmnTile> SearchTiles(uint tileId, int rangeX, int rangeY)
        {
            int tileX = tileApi.CalcTileX(tileId);
            int tileY = tileApi.CalcTileY(tileId);
            List<CmnTile> retTileList = new List<CmnTile>();

            for (int x = tileX - rangeX; x <= tileX + rangeX; x++)
            {
                for (int y = tileY - rangeY; y <= tileY + rangeY; y++)
                {
                    uint tmpTileId = tileApi.CalcTileId(x, y);

                    if (tileDic.ContainsKey(tmpTileId))
                        retTileList.Add(tileDic[tmpTileId]);
                    else
                        continue;
                }
            }

            return retTileList;

        }

        public List<CmnTile> GetLoadedTileList()
        {
            return tileDic.Select(x => x.Value).ToList();
        }

        public List<uint> GetMapTileIdList()
        {
            if (!IsConnected)
                return null;
            return mal.GetMapTileIdList();
        }

        //オブジェクト検索メソッド ***********************************************

        public CmnObjHandle SearchObj(uint tileId, UInt16 objType, UInt64 objId)
        {
            return SearchTile(tileId)?.GetObjHandle(objType, objId);

            //CmnTile tmpTile = SearchTile(tileId);
            //if (tmpTile == null)
            //    return null;
            //CmnObj tmpObj = tmpTile.GetObj(objType, objId);
            //if (tmpObj == null)
            //    return null;

            //return new CmnObjHandle(tmpTile, tmpObj);

        }

        public CmnObjHandle SearchObj(uint tileId, UInt16 objType, UInt16 objIndex)
        {
            return SearchTile(tileId)?.GetObjHandle(objType, objIndex);

            //CmnTile tmpTile = SearchTile(tileId);
            //if (tmpTile == null)
            //    return null;
            //CmnObj tmpObj = tmpTile.GetObj(objType, objIndex);
            //if (tmpObj == null)
            //    return null;

            //return new CmnObjHandle(tmpTile, tmpObj);

        }

        public CmnObjHandle SearchObj(LatLon latlon, int searchRange = 1, bool multiContents = true, UInt16 objType = 0xFFFF, UInt16 maxSubType = 0xFFFF)
        {
            List<CmnTile> searchTileList;
            
            //seachRange = Max -> 全タイルから検索
            if (searchRange == int.MaxValue)
            {
                searchTileList = GetLoadedTileList();
            }
            else
            {
                uint tileId = tileApi.CalcTileId(latlon);

                searchTileList = SearchTiles(tileId, searchRange, searchRange);

            }

            CmnObjHdlDistance nearestObj = searchTileList
                .Select(x => x?.GetNearestObj(latlon, objType, maxSubType))
                .Where(x => x != null)
                .OrderBy(x => x.distance)
                .FirstOrDefault();

            if (nearestObj == null)
                return null;

            return (CmnObjHandle)nearestObj;
            
        }

        public CmnObjHandle SearchObj(CmnObjRef objRef)
        {
            if (objRef == null)
                return null;

            //Tile <- offset未対応
            CmnTile tile;
            if (objRef.tile != null)
                tile = objRef.tile;
            else
                tile = SearchTile(objRef.tileId);

            if (tile == null)
                return null;

            //Obj
            if (objRef.obj != null)
                return new CmnObjHandle(tile, objRef.obj);
            else if (objRef.objIndex != 0xffff)
                return tile.GetObjHandle(objRef.objType, objRef.objIndex);
            else
                return tile.GetObjHandle(objRef.objType, objRef.objId);

        }


        //関連オブジェクト取得 ***********************************************

        public virtual List<CmnObjHdlRef> SearchObjHandle(CmnObjRef objRef)
        {
            List<CmnObjHdlRef> retList = new List<CmnObjHdlRef>();

            if (objRef == null)
                return retList;

            CmnObjHandle objHdl = SearchObj(objRef); //ハンドル
            if (objHdl == null)
                return retList;

            if(objRef.final == true)
            {
                retList.Add(new CmnObjHdlRef(objHdl, objRef.refType));
                return retList;
            }


            List<CmnObjHdlRef> tmpObjRefList = objHdl.obj.GetObjRefHdlList(objRef.refType, objHdl.tile); //Objの参照先一覧（種別指定）

            foreach (var tmpObjRef in tmpObjRefList)
            {
                if (tmpObjRef.obj != null)
                {
                    retList.Add(tmpObjRef);
                }
                else if (tmpObjRef.nextRef != null)
                {
                    retList.AddRange(SearchObjHandle(tmpObjRef.nextRef));
                }
            }

            return retList;
        }

        //必要に応じてオーバーライド
        public virtual List<CmnObjHdlRef> SearchRefObject(CmnObjHandle objHdl)
        {
            List<CmnObjHdlRef> retList = new List<CmnObjHdlRef>();


            List<CmnObjRef> tmpObjRefList = objHdl.obj.GetObjAllRefList(objHdl.tile); //Objの参照先一覧
            

            foreach (var tmpObjRef in tmpObjRefList)
            {
                CmnObjHdlRef objHdlRef = new CmnObjHdlRef(null, null, tmpObjRef.refType, tmpObjRef);

                retList.AddRange(SearchObjHandle(objHdlRef.nextRef));
                
            }

            return retList;


            //return cmnObjHdl.obj.GetObjAllRefList(cmnObjHdl.tile)
            //    .Select(x => CmnObjHdlRef.GenCmnObjHdlRef(SearchObjHandle(x), x.refType))
            //    .Where(x=>x!=null)
            //    .ToList();
        }



    }



    public interface ICmnMapAccess
    {
        bool IsConnected { get; }

        int ConnectMapData(string mapPath);

        int DisconnectMapData();

        List<uint> GetMapTileIdList();

        List<UInt16> GetMapContentTypeList();

        CmnTile CreateTile(uint tileId);

        //  List<CmnObjGroup> LoadObjGroupList(uint tileId, UInt16 type = 0xFFFF, UInt16 subType = 0xFFFF);

        List<CmnObjGroup> LoadObjGroupList(uint tileId, UInt16 type = 0xFFFF, UInt16 subType = 0xFFFF);

        CmnObjGroup LoadObjGroup(uint tileId, UInt16 type, UInt16 subType = 0xFFFF);
    }







}
