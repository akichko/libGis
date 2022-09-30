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
using System.Collections.Concurrent;

namespace Akichko.libGis
{
    public class TileMng
    {
        //いずれ移行
        //private ConcurrentDictionary<uint, CmnTile> tileDic;
        private Dictionary<uint, CmnTile> tileDic;

        public TileMng()
        {
            tileDic = new Dictionary<uint, CmnTile>();
        }

        public bool ContainsTile(uint tileId) => tileDic.ContainsKey(tileId);

        public CmnTile GetTile(uint tileId)
        {
            if (tileDic.ContainsKey(tileId))
                return tileDic[tileId];
            else
                return null;
        }

        public void AddTile(CmnTile tile) => tileDic.Add(tile.TileId, tile);

        public bool RemoveTile(uint tileId) => tileDic.Remove(tileId);

        public IEnumerable<CmnTile> GetTileList() => tileDic.Select(x => x.Value);
    }

    //いずれ切替？
    //public class TileMng2
    //{
    //    //いずれ移行
    //    //private ConcurrentDictionary<uint, CmnTile> tileDic;
    //    private Dictionary<uint, CmnTile> tileDic;

    //    public TileMng2()
    //    {
    //        tileDic = new Dictionary<uint, CmnTile>();
    //    }
    //    public bool ContainsTile(CmnTileCode tileCode)
    //    {
    //        return tileDic.ContainsKey(tileCode.TileId);
    //    }

    //    public CmnTile GetTile(CmnTileCode tileCode)
    //    {

    //        if (tileDic.ContainsKey(tileCode.TileId))
    //            return tileDic[tileCode.TileId];
    //        else
    //            return null;
    //    }

    //    public void AddTile(CmnTile tile)
    //    {
    //        tileDic.Add(tile.TileId, tile);
    //    }

    //    public bool RemoveTile(CmnTileCode tileCode)
    //    {
    //        return tileDic.Remove(tileCode.TileId);
    //    }
    //    public List<CmnTile> GetTileList()
    //    {
    //        return tileDic.Select(x => x.Value).ToList();
    //    }

    //}

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


    public abstract class CmnMapMgr : CmnMapAccess
    {
        public ICmnTileCodeApi tileApi;
        protected TileMng tileMng;
        protected CmnMapAccess mapAccess;

        protected bool isConnected = false;

        SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

        //抽象メソッド
        //現状なし。MAL側

        public CmnMapMgr(ICmnTileCodeApi tileCodeApi, CmnMapAccess mapAccess = null)
        {
            this.tileApi = tileCodeApi;
            this.mapAccess = mapAccess;
            tileMng = new TileMng();
        }

        /* 地図データ接続 ************************************************************/

        public int Connect(string connectStr)
        {
            int ret = mapAccess.ConnectMap(connectStr);
            if (ret == 0)
            {
                Console.WriteLine("Connected");
                isConnected = true;
            }
            else
                Console.WriteLine("Connect Error!");

            return ret;
        }

        public int Disconnect()
        {
            isConnected = false;
            int ret = mapAccess.DisconnectMap();

            Console.WriteLine("disconnected");
            return ret;

        }

        public override bool IsConnected => mapAccess.IsConnected && isConnected;


        /* データ操作メソッド ******************************************************/

        public abstract CmnTile CreateTile(uint tileId);

        //削除予定？
        //public virtual int LoadTile(uint tileId, UInt32 reqType, UInt16 reqMaxSubType = 0xFFFF)
        //{
        //    if (!IsConnected) return -1;

        //    bool isNew = false;

        //    CmnTile tmpTile = tileMng.GetTile(tileId);
        //    //if (tileDic.ContainsKey(tileId))
        //    if (tmpTile != null)
        //    {
        //        //tmpTile = tileDic[tileId];

        //        //更新必要有無チェック

        //        //未読み込み（NULL）のObgGroupがあるか
        //        int numObjGrToBeRead = mapAccess.GetMapContentTypeList()
        //            .Where(x => (reqType & x) == x)
        //            .Select(x => tmpTile.GetObjGroup(x))
        //            //.Where(x => x == null)
        //            .Count(x => x == null || x.loadedSubType < reqMaxSubType);


        //        if (numObjGrToBeRead == 0)
        //            return 0; //更新不要

        //    }
        //    //タイルがなければ作成
        //    else
        //    {
        //        tmpTile = CreateTile(tileId);
        //        isNew = true;
        //    }


        //    //必要となった場合


        //    //データ読み込み
        //    List<CmnObjGroup> tmpObjGrList = mapAccess.LoadObjGroupList(tileId, reqType, reqMaxSubType);

        //    //インデックス付与（仮）
        //    tmpObjGrList.ForEach(x => x.SetIndex());

        //    //タイル更新
        //    tmpTile.UpdateObjGroupList(tmpObjGrList);


        //    if (isNew)
        //        tileMng.AddTile(tmpTile);
        //    //tileDic.Add(tileId, tmpTile);

        //    return 0;

        //}

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
            List<CmnObjGroup> tmpObjGrList = (filter?.GetTypeList() ?? mapAccess.GetMapContentTypeList())
                .Where(type => !tmpTile.IsContentsLoaded(type, filter?.SubTypeRangeMax(type) ?? ushort.MaxValue))
                .Select(type => mapAccess.LoadObjGroup(tileId, type, filter?.SubTypeRangeMax(type) ?? ushort.MaxValue))
                //.Where(x=>x!=null)
                //.SelectMany(x=>x)
                .ToList();

            //インデックス付与（仮）
            tmpObjGrList.ForEach(x => x.SetIndex());

            //タイル更新
            tmpTile.UpdateObjGroupList(tmpObjGrList);

            return 0;
        }


        public virtual async Task<int> LoadTileAsync(uint tileId, CmnObjFilter filter = null)
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

#if true //一括読み込み
            //ObjGroup読み込み
            List<ObjReqType> reqTypes = (filter?.ToObjReqType() ?? mapAccess.GetMapContentTypeList().Select(x => new ObjReqType(x)))
                .Where(reqType => !tmpTile.IsContentsLoaded(reqType.type, reqType.maxSubType))
                .ToList();

            var objGroups = await mapAccess.LoadObjGroupAsync(tileId, reqTypes).ConfigureAwait(false);
            List<CmnObjGroup> tmpObjGrList = objGroups.ToList();

#else //SubType毎読み込み
            //ObjGroup読み込み
            var task = (filter?.GetTypeList() ?? mapAccess.GetMapContentTypeList())
                .Where(type => !tmpTile.IsContentsLoaded(type, filter?.SubTypeRangeMax(type) ?? ushort.MaxValue))
                .Select(type => mapAccess.LoadObjGroupAsync(tileId, type, filter?.SubTypeRangeMax(type) ?? ushort.MaxValue));
            var tmp2 = await Task.WhenAll(task).ConfigureAwait(false);
            List<CmnObjGroup> tmpObjGrList = tmp2.Where(x=>x!=null).SelectMany(x=>x).ToList();
#endif

            //インデックス付与（仮）
            //tmpObjGrList.ForEach(x => x.SetIndex());

            //タイル更新
            tmpTile.UpdateObjGroupList(tmpObjGrList);

            return 0;
        }


        public virtual int LoadTile(uint tileId, IEnumerable<uint> reqTypeList, UInt16 reqMaxSubType = 0xFFFF)
        {
            return LoadTile(tileId, new CmnObjFilter(reqTypeList, reqMaxSubType));
        }

        //削除予定
        //public virtual int LoadTileOld(uint tileId, List<uint> reqTypeList, UInt16 reqMaxSubType = 0xFFFF)
        //{
        //    if (!IsConnected)
        //        return -1;

        //    //タイル読み込み
        //    CmnTile tmpTile = tileMng.GetTile(tileId);
        //    if (tmpTile == null)
        //    {
        //        //タイルがなければ作成
        //        tmpTile = CreateTile(tileId);
        //        tileMng.AddTile(tmpTile);
        //    }

        //    //ObjGroup読み込み
        //    List<CmnObjGroup> tmpObjGrList = reqTypeList
        //        .Where(type => !tmpTile.IsContentsLoaded(type, reqMaxSubType))
        //        .SelectMany(type => mapAccess.LoadObjGroup(tileId, type, reqMaxSubType))
        //        .ToList<CmnObjGroup>();

        //    //インデックス付与（仮）
        //    tmpObjGrList.ForEach(x => x.SetIndex());

        //    //タイル更新
        //    tmpTile.UpdateObjGroupList(tmpObjGrList);

        //    return 0;
        //}

        public bool UnloadTile(uint tileId) => tileMng.RemoveTile(tileId);


        public int AddObj(uint tileId, UInt32 objType, CmnObj obj)
        {
            SearchTile(tileId)?.AddObj(objType, obj);
            return 0;
        }

        public int DelObj(uint tileId, UInt32 objType, ulong objId, long endTimeStamp = 0)
        {
            SearchTile(tileId)?.DelObj(objType, objId, endTimeStamp);
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
            IEnumerable<CmnTile> ret;

            //seachRange = Max -> 全タイルから検索
            if (searchRange == int.MaxValue)
            {
                ret = GetLoadedTileList();
            }
            else
            {
                uint tileId = tileApi.CalcTileId(latlon);

                ret = SearchTiles(tileId, searchRange, searchRange);

            }
            return ret;
        }

        public IEnumerable<CmnTile> GetLoadedTileList() => tileMng.GetTileList();


        public override List<uint> GetMapTileIdList()
        {
            if (!IsConnected)
                return null;
            return mapAccess.GetMapTileIdList();
        }


        public virtual uint CalcTileId(LatLon latlon)
        {
            return tileApi.CalcTileId(latlon);
        }


        /* オブジェクト検索メソッド ************************************************/

        public CmnObjHandle SearchObj(uint tileId, UInt32 objType, UInt64 objId, long timeStamp = -1)
        {
            return SearchTile(tileId)?.GetObjHandle(objType, objId, timeStamp);
        }

        public CmnObjHandle SearchObj(uint tileId, UInt32 objType, UInt16 objIndex, long timeStamp = -1)
        {
            return SearchTile(tileId)?.GetObjHandle(objType, objIndex, timeStamp);
        }


        public CmnObjHandle SearchObj(LatLon latlon, CmnObjFilter filter, int searchRange, long timeStamp = -1)
        {
            var ret = SearchTiles(latlon, searchRange)
                .Select(x => x.GetNearestObj(latlon, filter, timeStamp))
                .Where(x => x != null)
                .OrderBy(x => x.distance)
                .FirstOrDefault()
                ?.objHdl;
            return ret;
        }

        //曖昧検索
        public CmnObjHandle SearchObj(CmnSearchKey cmnSearchKey, long timeStamp = -1)
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
            else if (cmnSearchKey.objIndex != ushort.MaxValue)
                return tile.GetObjHandle(cmnSearchKey.objType, cmnSearchKey.objIndex)?.SetDirection(cmnSearchKey.objDirection);
            //ID検索
            else if (cmnSearchKey.objId != ulong.MaxValue)
                return tile.GetObjHandle(cmnSearchKey.objType, cmnSearchKey.objId, timeStamp)?.SetDirection(cmnSearchKey.objDirection);
            //カスタム検索
            else if (cmnSearchKey.selector != null)
                return tile.GetObjHandles(cmnSearchKey.objType, cmnSearchKey.selector).FirstOrDefault()?.SetDirection(cmnSearchKey.objDirection);
            else
                return null;
        }


        public IEnumerable<CmnObjHandle> SearchObjs(uint tileId, UInt32 objType, Func<CmnObj, bool> selector)
        {
            return SearchTile(tileId)?.GetObjHandles(objType, selector);
        }

        //ランダム
        public CmnObjHandle SearchRandomObj(uint tileId, UInt32 objType)
        {
            return SearchTile(tileId)?.GetRandomObj(objType);
        }

        public uint GetRandomTileId()
        {
            List<uint> allTileList = GetMapTileIdList();
            Random rnd = new Random();
            uint ret = allTileList[rnd.Next(0, allTileList.Count - 1)];
            return ret;
        }


        /* 関連オブジェクト検索 *************************************************************/

        //関連オブジェクト取得（参照種別指定）
        public virtual List<CmnObjHdlRef> SearchRefObject(CmnObjHandle objHdl, int refType)
        {
            var ret = objHdl.GetObjRefHdlList(refType, objHdl.direction)
                .Select(x => new CmnObjHdlRef(null, x.nextRef))
                .SelectMany(x => SearchObjHandleRef(x.nextRef))
                .ToList();

            return ret;

            //List<CmnObjHdlRef> retList = new List<CmnObjHdlRef>();

            //List<CmnObjHdlRef> tmpRefHdlList = objHdl.GetObjRefHdlList(refType, objHdl.direction); //Objの参照先一覧

            //foreach (var tmpRefHdl in tmpRefHdlList ?? new List<CmnObjHdlRef>())
            //{
            //    CmnObjHdlRef objHdlRef = new CmnObjHdlRef(null, tmpRefHdl.nextRef);

            //    retList.AddRange(SearchObjHandleRef(objHdlRef.nextRef));
            //}

            //return retList;
        }


        //関連オブジェクト取得（全て）。必要に応じてオーバーライド
        public virtual List<CmnObjHdlRef> SearchRefObject(CmnObjHandle objHdl, DirectionCode direction = DirectionCode.None)
        {
            var ret = objHdl?.GetObjAllRefList(direction)
                .Select(x => new CmnObjHdlRef(null, x))
                .SelectMany(x => SearchObjHandleRef(x.nextRef))
                .ToList();

            return ret;

            //if (objHdl == null)
            //    return null;

            //List<CmnObjHdlRef> retList = new List<CmnObjHdlRef>();

            //List<CmnObjRef> tmpObjRefList = objHdl.GetObjAllRefList(); //Objの参照先一覧

            //foreach (var tmpObjRef in tmpObjRefList ?? new List<CmnObjRef>())
            //{
            //    CmnObjHdlRef objHdlRef = new CmnObjHdlRef(null, tmpObjRef);

            //    retList.AddRange(SearchObjHandleRef(objHdlRef.nextRef));
            //}

            //return retList;
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


        public virtual string[] GetMapContentTypeNames() => null;
        //{
        //    var ret = ((MapContentType[])Enum.GetValues(typeof(MapContentType)))
        //        .Select(x => Enum.GetName(typeof(MapContentType), x)).ToArray();
        //    return ret;
        //}

        public virtual uint GetMapContentTypeValue(string objTypeName) => 0;
            //(uint)Enum.Parse(typeof(MapContentType), objTypeName);


        /* 経路計算 *************************************************************/

        public virtual CmnRouteMgr CreateRouteMgr(DykstraSetting setting = null)
        {
            return new CmnRouteMgr(this, setting);
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

        public TimeStampRange GetTimeStampRange()
        {
            return mapAccess.GetTimeStampRange();
        }


        /* MapMgr多段接続 *************************************************************/

        public override int ConnectMap(string connectStr)
        {
            if (mapAccess.IsConnected)
            {
                Console.WriteLine("Connected");
                isConnected = true;
                return 0;
            }
            else
            {
                Console.WriteLine("Connect Error!");
                return -1;
            }
        }

        public override int DisconnectMap()
        {
            isConnected = false;
            return 0;
        }

        public override List<uint> GetMapContentTypeList() => mapAccess.GetMapContentTypeList();

        public override CmnObjGroup LoadObjGroup(uint tileId, uint type, ushort subType = ushort.MaxValue)
        {

            var objGrp = SearchTile(tileId)?.GetObjGroup(type);
            if (objGrp != null)
            {
                if (subType == ushort.MaxValue && objGrp.loadedSubType == ushort.MaxValue)
                    return objGrp;
                
                if (subType <= objGrp.loadedSubType)
                {
                    //フィルタ未対応
                    return objGrp;
                }
            }
            return mapAccess.LoadObjGroup(tileId, type, subType);
        }

        public override IEnumerable<CmnObjGroup> LoadObjGroup(uint tileId, List<ObjReqType> reqTypes)
        {
            throw new NotImplementedException();
        }

    }


    public interface ICmnMapAccess
    {
        bool IsConnected { get; }
        int ConnectMap(string connectStr);
        int DisconnectMap();
        List<UInt32> GetMapContentTypeList();
        List<uint> GetMapTileIdList();

        CmnObjGroup LoadObjGroup(uint tileId, UInt32 type, UInt16 subType = 0xFFFF);

        IEnumerable<CmnObjGroup> LoadObjGroup(uint tileId, List<ObjReqType> reqTypes);

        Task<CmnObjGroup> LoadObjGroupAsync(uint tileId, UInt32 type, UInt16 subType = 0xFFFF);

        Task<IEnumerable<CmnObjGroup>> LoadObjGroupAsync(uint tileId, List<ObjReqType> reqType);

        TimeStampRange GetTimeStampRange();

    }

    public class TimeStampRange
    {
        public long minTime;
        public long maxTime;

        public TimeStampRange(long minTime, long maxTime)
        {
            this.minTime = minTime;
            this.maxTime = maxTime;
        }
    }

    public interface ICmnMapMgr { }


    public interface ICmnRoutePlanner { }


    public class ObjReqType
    {
        public uint type;
        public ushort maxSubType = ushort.MaxValue;

        public ObjReqType(uint objType, ushort maxSubType = ushort.MaxValue)
        {
            this.type = objType;
            this.maxSubType = maxSubType;
        }

    }

}
