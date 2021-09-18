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
using System.Threading;
using System.Threading.Tasks;

namespace Akichko.libGis
{
    public class TileMng{
        private Dictionary<uint, CmnTile> tileDic;

        public TileMng()
        {
            tileDic = new Dictionary<uint, CmnTile>();
        }
        public bool ContainsTile(uint tileId)
        {
            return tileDic.ContainsKey(tileId);
        }

        public CmnTile GetTile(uint tileId)
        {

            if (tileDic.ContainsKey(tileId))
                return tileDic[tileId];
            else
                return null;            
        }

        public void AddTile(CmnTile tile)
        {
            tileDic.Add(tile.TileId, tile);
        }

        public bool RemoveTile(uint tileId)
        {
            return tileDic.Remove(tileId);
        }
        public List<CmnTile> GetTileList()
        {
            return tileDic.Select(x => x.Value).ToList();
        }

    }

    //いずれ切替
    public class TileMng2
    {
        private Dictionary<uint, CmnTile> tileDic;

        public TileMng2()
        {
            tileDic = new Dictionary<uint, CmnTile>();
        }
        public bool ContainsTile(CmnTileCode tileCode)
        {
            return tileDic.ContainsKey(tileCode.TileId);
        }

        public CmnTile GetTile(CmnTileCode tileCode)
        {

            if (tileDic.ContainsKey(tileCode.TileId))
                return tileDic[tileCode.TileId];
            else
                return null;
        }

        public void AddTile(CmnTile tile)
        {
            tileDic.Add(tile.TileId, tile);
        }

        public bool RemoveTile(CmnTileCode tileCode)
        {
            return tileDic.Remove(tileCode.TileId);
        }
        public List<CmnTile> GetTileList()
        {
            return tileDic.Select(x => x.Value).ToList();
        }

    }

    //public class ReqType : ReqSubType
    //{
    //    public uint type;

    //    public ReqType(uint type, ushort maxSubType = ushort.MaxValue, ushort minSubType = 0) : base(maxSubType, minSubType)
    //    {
    //        this.type = type;
    //    }

    //    public ReqType[] ToArray()
    //    {
    //        ReqType[] ret = new ReqType[1];
    //        ret[0] = this;
    //        return ret;
    //    }
    //}

    //public class ReqSubType
    //{
    //    public ushort maxSubType = ushort.MaxValue;
    //    public ushort minSubType = 0;

    //    public ReqSubType() { }

    //    public ReqSubType(ushort maxSubType = ushort.MaxValue, ushort minSubType = 0)
    //    {
    //        this.maxSubType = maxSubType;
    //        this.minSubType = minSubType;
    //    }
    //}



    public abstract class CmnMapMgr
    {
        public ICmnTileCodeApi tileApi;
        protected TileMng tileMng;        
        protected ICmnMapAccess mal;

        Semaphore semaphore = new Semaphore(0, 1, "tileMng");

        //抽象メソッド
        //現状なし。MAL側

        public CmnMapMgr(ICmnTileCodeApi tileCodeApi)
        {
            this.tileApi = tileCodeApi;
            tileMng = new TileMng();
        }

        /* 地図データ接続 ************************************************************/
        
        public int Connect(string connectStr)
        {
            int ret = mal.ConnectMap(connectStr);
            if(ret == 0)
                Console.WriteLine("Connected");
            else
                Console.WriteLine("Connect Error!");

            return ret;
        }

        public int Disconnect()
        {
            int ret = mal.DisconnectMap();

            Console.WriteLine("disconnected");
            return ret;

        }

        public bool IsConnected => mal.IsConnected;


        /* データ操作メソッド ******************************************************/

        public abstract CmnTile CreateTile(uint tileId);

        //削除予定？
        public virtual int LoadTile(uint tileId, UInt32 reqType, UInt16 reqMaxSubType = 0xFFFF)
        {
            if (!IsConnected) return -1;

            bool isNew = false;

            CmnTile tmpTile = tileMng.GetTile(tileId);
            //if (tileDic.ContainsKey(tileId))
            if (tmpTile != null)
            {
                //tmpTile = tileDic[tileId];

                //更新必要有無チェック

                //未読み込み（NULL）のObgGroupがあるか
                int numObjGrToBeRead = mal.GetMapContentTypeList()
                    .Where(x => (reqType & x) == x)
                    .Select(x => tmpTile.GetObjGroup(x))
                    //.Where(x => x == null)
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
                tileMng.AddTile(tmpTile);
            //tileDic.Add(tileId, tmpTile);

            return 0;

        }

        public virtual int LoadTile(uint tileId, CmnObjFilter filter = null)
        {
            if (!IsConnected)
                return -1;

            //タイル読み込み
            CmnTile tmpTile = tileMng.GetTile(tileId);
            if (tmpTile == null)
            {
                //タイルがなければ作成
                tmpTile = CreateTile(tileId);
                tileMng.AddTile(tmpTile);
            }

            //ObjGroup読み込み
            List<CmnObjGroup> tmpObjGrList = mal.GetMapContentTypeList()
                .Where(type => !tmpTile.IsContentsLoaded(type, filter?.SubTypeRangeMax(type) ?? ushort.MaxValue))
                .Select(type => mal.LoadObjGroup(tileId, type, filter?.SubTypeRangeMax(type) ?? ushort.MaxValue))
                .ToList<CmnObjGroup>();

            //インデックス付与（仮）
            tmpObjGrList.ForEach(x => x.SetIndex());

            //タイル更新
            tmpTile.UpdateObjGroupList(tmpObjGrList);

            return 0;
        }

        public virtual int LoadTile(uint tileId, List<uint> reqTypeList, UInt16 reqMaxSubType = 0xFFFF)
        {
            if (!IsConnected)
                return -1;

            //タイル読み込み
            CmnTile tmpTile = tileMng.GetTile(tileId);
            if (tmpTile == null)
            {
                //タイルがなければ作成
                tmpTile = CreateTile(tileId);
                tileMng.AddTile(tmpTile);
            }

            //ObjGroup読み込み
            List<CmnObjGroup> tmpObjGrList = reqTypeList
                .Where(type => !tmpTile.IsContentsLoaded(type, reqMaxSubType))
                .Select(type => mal.LoadObjGroup(tileId, type, reqMaxSubType))
                .ToList<CmnObjGroup>();

            //インデックス付与（仮）
            tmpObjGrList.ForEach(x => x.SetIndex());

            //タイル更新
            tmpTile.UpdateObjGroupList(tmpObjGrList);

            return 0;
        }

        public bool UnloadTile(uint tileId) => tileMng.RemoveTile(tileId);
       

        public int AddObj(uint tileId, UInt32 objType, CmnObj obj)
        {
            SearchTile(tileId)?.AddObj(objType, obj);
            return 0;
        }


        /* タイル検索メソッド ******************************************************/

        public CmnTile SearchTile(uint tileId) => tileMng.GetTile(tileId);
        

        public CmnTile SearchTile(LatLon latlon)
        {
            uint tileId = tileApi.CalcTileId(latlon);
            return SearchTile(tileId);
        }

        public IEnumerable<CmnTile> SearchTiles(uint tileId, int rangeX, int rangeY)
        {
            int tileX = tileApi.CalcTileX(tileId);
            int tileY = tileApi.CalcTileY(tileId);
            List<CmnTile> retTileList = new List<CmnTile>();

            

            for (int x = tileX - rangeX; x <= tileX + rangeX; x++)
            {
                for (int y = tileY - rangeY; y <= tileY + rangeY; y++)
                {
                    uint tmpTileId = tileApi.CalcTileId(x, y);

                    if (tileMng.ContainsTile(tmpTileId))
                        retTileList.Add(tileMng.GetTile(tmpTileId));
                    else
                        continue;
                }
            }

            return retTileList;

        }


        public IEnumerable<CmnTile> SearchTiles(LatLon latlon, int searchRange = 1)
        {
            IEnumerable<CmnTile> searchTileList;

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
            return searchTileList;
        }

        public IEnumerable<CmnTile> GetLoadedTileList() => tileMng.GetTileList();
      

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
        }

        public CmnObjHandle SearchObj(uint tileId, UInt32 objType, UInt16 objIndex)
        {
            return SearchTile(tileId)?.GetObjHandle(objType, objIndex);
        }

        //削除予定
        //public CmnObjHandle SearchObj(LatLon latlon, int searchRange = 1, bool multiContents = true, UInt32 objType = 0xFFFFFFFF, UInt16 maxSubType = 0xFFFF)
        //{
        //    List<CmnTile> searchTileList = SearchTiles(latlon, searchRange);

        //    CmnObjHdlDistance nearestObj = searchTileList
        //        .Select(x => x?.GetNearestObj(latlon, objType, maxSubType))
        //        .Where(x => x != null)
        //        .OrderBy(x => x.distance)
        //        .FirstOrDefault();

        //    if (nearestObj == null)
        //        return null;

        //    return nearestObj.objHdl;

        //}

        //public CmnObjHandle SearchObj(LatLon latlon, ReqType[] reqTypeArray, int searchRange = 1)
        //{
        //    List<CmnTile> searchTileList = SearchTiles(latlon, searchRange);

        //    CmnObjHdlDistance nearestObj = searchTileList
        //        .Select(x => x.GetNearestObj(latlon, reqTypeArray))
        //        .Where(x => x != null)
        //        .OrderBy(x => x.distance)
        //        .FirstOrDefault();

        //    if (nearestObj == null)
        //        return null;

        //    return nearestObj.objHdl;

        //}

        public CmnObjHandle SearchObj(LatLon latlon, CmnObjFilter filter, int searchRange, int timeStamp)
        {
            return SearchTiles(latlon, searchRange)
                .Select(x => x.GetNearestObj(latlon, filter, timeStamp))
                .Where(x => x != null)
                .OrderBy(x => x.distance)
                .FirstOrDefault()
                ?.objHdl;

            //IEnumerable<CmnTile> searchTileList = SearchTiles(latlon, searchRange);

            //CmnObjHdlDistance nearestObj = searchTileList
            //    .Select(x => x.GetNearestObj(latlon, filter))
            //    .Where(x => x != null)
            //    .OrderBy(x => x.distance)
            //    .FirstOrDefault();

            //if (nearestObj == null)
            //    return null;

            //return nearestObj.objHdl;
        }

        //曖昧検索
        public CmnObjHandle SearchObj(CmnSearchKey cmnSearchKey)
        {
            if (cmnSearchKey == null)
                return null;

            //Tile <- offset未対応
            //そのうちTile情報なしで全タイル検索を実装したい
            CmnTile tile;
            if (cmnSearchKey.tile != null)
                tile = cmnSearchKey.tile;
            else
                tile = SearchTile(cmnSearchKey.tileId);

            if (tile == null)
                return null;

            //キーにObjあり
            if (cmnSearchKey.obj != null)
                return cmnSearchKey.obj.ToCmnObjHandle(tile);
            //Index検索
            else if (cmnSearchKey.objIndex != 0xffff)
                return tile.GetObjHandle(cmnSearchKey.objType, cmnSearchKey.objIndex)?.SetDirection(cmnSearchKey.objDirection);
            //ID検索
            else
                return tile.GetObjHandle(cmnSearchKey.objType, cmnSearchKey.objId)?.SetDirection(cmnSearchKey.objDirection);


        }


        /* 関連オブジェクト検索 *************************************************************/

        //関連オブジェクト取得（参照種別指定）
        public virtual List<CmnObjHdlRef> SearchRefObject(CmnObjHandle objHdl, int refType)
        {
            List<CmnObjHdlRef> retList = new List<CmnObjHdlRef>();

            List<CmnObjHdlRef> tmpRefHdlList = objHdl.GetObjRefHdlList(refType, objHdl.direction); //Objの参照先一覧

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

            List<CmnObjRef> tmpObjRefList = objHdl.GetObjAllRefList(); //Objの参照先一覧

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
                //if (objRef.key.objDirection != 0xff)
                //    objHdl = new CmnObjHandle(objHdl.tile, objHdl.obj, objRef.key.objDirection);
                retList.Add(new CmnObjHdlRef(objHdl, objRef.refType));
                return retList;
            }


            List<CmnObjHdlRef> tmpObjHdlRefList = objHdl.GetObjRefHdlList(objRef.refType, objRef.key.objDirection); //Objの参照先一覧（種別指定）

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


        /* 経路計算 *************************************************************/

        public virtual CmnRouteMgr CreateRouteMgr()
        {
            return new CmnRouteMgr(this);
        }

        public LatLon[] CalcRouteGeometry(LatLon orgLatLon, LatLon dstLatLon)
        {
            CmnRouteMgr routeMgr = CreateRouteMgr();

            routeMgr.orgLatLon = orgLatLon;
            routeMgr.dstLatLon = dstLatLon;

            routeMgr.Prepare(false);

            routeMgr.CalcRoute();

            //道路NWメモリ解放？
            routeMgr.dykstra.dicTileCostInfo = null;

            CmnMapView mapView = new CmnMapView();
            //List<CmnObjHandle> routeHdlList = routeMgr.GetRouteHdlList();
            LatLon[] routeGeometry = routeMgr.GetResult();


            Console.WriteLine($"maxQueue = {routeMgr.dykstra.logMaxQueue} (average = {routeMgr.dykstra.logUnprocessedCount.Take(routeMgr.dykstra.logCalcCount).Average():F2})");


            return routeGeometry;
        }

        public List<CmnObjHandle> CalcRoute(LatLon orgLatLon, LatLon dstLatLon)
        {
            CmnRouteMgr routeMgr = CreateRouteMgr();

            routeMgr.orgLatLon = orgLatLon;
            routeMgr.dstLatLon = dstLatLon;

            routeMgr.Prepare(false);

            routeMgr.CalcRoute();

            //道路NWメモリ解放？
            routeMgr.dykstra.dicTileCostInfo = null;

            CmnMapView mapView = new CmnMapView();
            return routeMgr.GetRouteHdlList();

        }


    }

    public interface ICmnMapAccess
    {
        bool IsConnected { get; }

        int ConnectMap(string connectStr);
        
        int DisconnectMap();

        List<uint> GetMapTileIdList();

        List<UInt32> GetMapContentTypeList();

        //CmnTile CreateTile(uint tileId);
                
        List<CmnObjGroup> LoadObjGroupList(uint tileId, UInt32 type = 0xFFFFFFFF, UInt16 subType = 0xFFFF);

        
        CmnObjGroup LoadObjGroup(uint tileId, UInt32 type, UInt16 subType = 0xFFFF);
    }


    public interface ICmnMapMgr { }


    public interface ICmnRoutePlanner { }



}
