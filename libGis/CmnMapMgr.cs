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

        /* 地図データ接続 ************************************************************/
        
        public int Connect(string connectStr)
        {
            int ret = mal.ConnectMapData(connectStr);
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
            get { return mal.IsConnected; }

        }


        /* データ操作メソッド ******************************************************/

        public abstract CmnTile CreateTile(uint tileId);

        public int LoadTile(uint tileId, UInt32 reqType = 0xFFFFFFFF, UInt16 reqMaxSubType = 0xFFFF)
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
                tmpTile = CreateTile(tileId);
                isNew = true;
            }


            //必要となった場合


            //データ読み込み
            List<CmnObjGroup> tmpObjGrList = mal.LoadObjGroupList(tileId, reqType, reqMaxSubType);

            //インデックス付与（仮）
            tmpObjGrList.ForEach(x => x.SetIndex());

            //タイル更新
            tmpTile.UpdateObjGroupList(tmpObjGrList);


            if (isNew)
                tileDic.Add(tileId, tmpTile);

            return 0;

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

        //    foreach (UInt32 objType in objTypeList)
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

        public bool UnloadTile(uint tileId)
        {
            return tileDic.Remove(tileId);
        }

        public int AddObj(uint tileId, UInt32 objType, CmnObj obj)
        {
            SearchTile(tileId)?.AddObj(objType, obj);
            return 0;
        }


        /* タイル検索メソッド ******************************************************/

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


        public virtual uint CalcTileId(LatLon latlon)
        {
            return tileApi.CalcTileId(latlon);
        }

        /* オブジェクト検索メソッド ************************************************/

        public CmnObjHandle SearchObj(uint tileId, UInt32 objType, UInt64 objId)
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

        public CmnObjHandle SearchObj(uint tileId, UInt32 objType, UInt16 objIndex)
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

        public CmnObjHandle SearchObj(LatLon latlon, int searchRange = 1, bool multiContents = true, UInt32 objType = 0xFFFFFFFF, UInt16 maxSubType = 0xFFFF)
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

            return nearestObj.objHdl;

        }

        public CmnObjHandle SearchObj(CmnSearchKey cmnSearchKey)
        {
            if (cmnSearchKey == null)
                return null;

            //Tile <- offset未対応
            CmnTile tile;
            if (cmnSearchKey.tile != null)
                tile = cmnSearchKey.tile;
            else
                tile = SearchTile(cmnSearchKey.tileId);

            if (tile == null)
                return null;

            //Obj
            if (cmnSearchKey.obj != null)
                return cmnSearchKey.obj.ToCmnObjHandle(tile);
            else if (cmnSearchKey.objIndex != 0xffff)
                return tile.GetObjHandle(cmnSearchKey.objType, cmnSearchKey.objIndex);
            else
                return tile.GetObjHandle(cmnSearchKey.objType, cmnSearchKey.objId);
        }


        /* 関連オブジェクト検索 *************************************************************/

        //関連オブジェクト取得（参照種別指定）
        public virtual List<CmnObjHdlRef> SearchRefObject(CmnObjHandle objHdl, int refType)
        {
            List<CmnObjHdlRef> retList = new List<CmnObjHdlRef>();

            List<CmnObjHdlRef> tmpRefHdlList = objHdl.obj.GetObjRefHdlList(refType, objHdl.tile, objHdl.direction); //Objの参照先一覧

            foreach (var tmpRefHdl in tmpRefHdlList ?? new List<CmnObjHdlRef>())
            {
                CmnObjHdlRef objHdlRef = new CmnObjHdlRef(null, tmpRefHdl.nextRef);

                retList.AddRange(SearchObjHandleRef(objHdlRef.nextRef));
            }

            return retList;
        }


        //関連オブジェクト取得（全て）。必要に応じてオーバーライド
        public virtual List<CmnObjHdlRef> SearchRefObject(CmnObjHandle objHdl, byte direction = 0xff)
        {
            List<CmnObjHdlRef> retList = new List<CmnObjHdlRef>();

            List<CmnObjRef> tmpObjRefList = objHdl.obj.GetObjAllRefList(objHdl.tile, direction); //Objの参照先一覧

            foreach (var tmpObjRef in tmpObjRefList ?? new List<CmnObjRef>())
            {
                CmnObjHdlRef objHdlRef = new CmnObjHdlRef(null, tmpObjRef);

                retList.AddRange(SearchObjHandleRef(objHdlRef.nextRef));
            }

            return retList;

        }


        //再帰検索する内部関数
        private List<CmnObjHdlRef> SearchObjHandleRef(CmnObjRef objRef)
        {
            List<CmnObjHdlRef> retList = new List<CmnObjHdlRef>();

            if (objRef == null || objRef.key == null)
                return retList; //異常

            //CmnObjHandle objHdl = SearchObj(objRef); //ハンドル
            CmnObjHandle objHdl = SearchObj(objRef.key); //キー⇒ハンドル
            //検索失敗
            if (objHdl == null)
            {
                CmnObjHdlRef ret = new CmnObjHdlRef(null, objRef, true);
                retList.Add(ret);
                return retList;
            }

            //検索成功

            if (objRef.final == true)
            {
                if (objRef.key.objDirection != 0xff)
                    objHdl = new CmnObjHandle(objHdl.tile, objHdl.obj, objRef.key.objDirection);
                retList.Add(new CmnObjHdlRef(objHdl, objRef.refType));
                return retList;
            }


            List<CmnObjHdlRef> tmpObjHdlRefList = objHdl.obj.GetObjRefHdlList(objRef.refType, objHdl.tile, objRef.key.objDirection); //Objの参照先一覧（種別指定）

            foreach (var tmpObjHdlRef in tmpObjHdlRefList ?? new List<CmnObjHdlRef>())
            {
                //ハンドルありor検索失敗
                if (tmpObjHdlRef.objHdl != null || tmpObjHdlRef.noData)
                {
                    retList.Add(tmpObjHdlRef);
                }
                //検索情報あり
                else if (tmpObjHdlRef.nextRef != null)
                {
                    //追加検索
                    retList.AddRange(SearchObjHandleRef(tmpObjHdlRef.nextRef));
                }
            }

            return retList;
        }


        /* 地図コンテンツ仕様情報取得（基本的にorverride前提） ***********************************************/

        //public virtual uint GetMapObjType(ECmnMapContentType cmnRefType)
        //{
        //    switch (cmnRefType)
        //    {
        //        case ECmnMapContentType.Link:
        //            return (int)ECmnMapContentType.Link;
        //        case ECmnMapContentType.Node:
        //            return (int)ECmnMapContentType.Node;
        //        default:
        //            return 0;
        //    }
        //}

        //public virtual int GetMapRefType(ECmnMapRefType cmnRefType)
        //{
        //    switch (cmnRefType)
        //    {
        //        case ECmnMapRefType.NextLink:
        //            return (int)ECmnMapRefType.NextLink;
        //        case ECmnMapRefType.BackLink:
        //            return (int)ECmnMapRefType.BackLink;
        //        case ECmnMapRefType.NextLane:
        //            return (int)ECmnMapRefType.NextLane;
        //        case ECmnMapRefType.BackLane:
        //            return (int)ECmnMapRefType.BackLane;
        //        default:
        //            return 0;
        //    }
        //}

        public virtual RoutingMapType RoutingMapType => null;

    }

    public interface ICmnMapAccess
    {
        bool IsConnected { get; }

        int ConnectMapData(string connectStr);
        
        int DisconnectMapData();

        List<uint> GetMapTileIdList();

        List<UInt32> GetMapContentTypeList();

        //CmnTile CreateTile(uint tileId);
                
        List<CmnObjGroup> LoadObjGroupList(uint tileId, UInt32 type = 0xFFFFFFFF, UInt16 subType = 0xFFFF);

        CmnObjGroup LoadObjGroup(uint tileId, UInt32 type, UInt16 subType = 0xFFFF);
    }


    public interface ICmnMapMgr { }


    public interface ICmnRoutePlanner { }



}
