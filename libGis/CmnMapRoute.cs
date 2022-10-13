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

namespace Akichko.libGis
{

    public abstract class RouteGenerator
    {
        protected CmnMapMgr mapMgr;

        //汎用種別
        protected RoutingMapType routingMapType;


    }


    public class AutoRouteGenerator : RouteGenerator
    {

        public AutoRouteGenerator(CmnMapMgr mapMgr, RoutingMapType routingMapType)
        {
            this.mapMgr = mapMgr;
            this.routingMapType = routingMapType;
        }


        public virtual CmnObjHandle SelectNextRouteLink(CmnObjHandle currentDLinkHdl)
        {
            List<CmnObjHdlRef> objHdlRefList;

            //接続リンク取得
            while (true)
            {
                //接続リンク。向きは自動判別
                objHdlRefList = mapMgr.SearchRefObject(currentDLinkHdl, routingMapType.nextLinkRefType);

                //未読み込みタイル
                List<uint> noDataTileIdList = objHdlRefList
                    .Select(x => (x.objHdl?.tile?.TileId ?? x.nextRef?.key?.tileId) ?? uint.MaxValue)
                    .Where(x => x != uint.MaxValue && !mapMgr.IsTileLoaded(x))
                    .ToList();

                //不足タイルがあれば読み込み
                if (noDataTileIdList.Count > 0)
                    noDataTileIdList.ForEach(x => mapMgr.LoadTile(x, routingMapType.roadNwObjFilter.ToObjReqType().ToList()));
                else
                    break;
            }

            IEnumerable<CmnObjHandle> connectLinkList = objHdlRefList
                .Select(x => x.objHdl)
                .Where(x => x != null);

            //接続リンクとコスト参照
            foreach (CmnObjHandle nextLinkRef in connectLinkList ?? new List<CmnObjHandle>())
            {
                //方向なしの場合に方向決定
                if (nextLinkRef.direction == DirectionCode.None)
                {
                    nextLinkRef.direction = nextLinkRef.obj.GetDirection(currentDLinkHdl.obj, true);
                }

                //Uターンリンク
                if (nextLinkRef.IsEqualTo(currentDLinkHdl))
                    continue;

                //一方通行逆走
                if (nextLinkRef.IsOneway && nextLinkRef.Oneway != nextLinkRef.direction)
                    continue;

                //行き止まり

                return nextLinkRef;
            }

            return null;
        }

        /****** 自律経路計算メイン ******************************************************************************/
       
        public virtual RouteResult CalcAutoRoute(CmnObjHandle objHdl, double routeLengthM = 5000.0)
        { 
            List<CmnObjHandle> routeLinkList = new List<CmnObjHandle>() { objHdl };
            double routeLength = 0;

            CmnObjHandle currentLink = objHdl;

            while (routeLength < routeLengthM)
            {
                currentLink = SelectNextRouteLink(currentLink);
                if (currentLink == null)
                    break;

                routeLinkList.Add(currentLink);
                routeLength += currentLink.obj.Length;

                if (routeLength >= routeLengthM)
                    break;
            }

            //計算
            ResultCode retCode = ResultCode.Success;

            //結果出力
            List<CostRecord> result = null;

            RouteResult ret = new RouteResult(retCode, null, routeLinkList);
            return ret;
        }

    }


    public class RoutingMapType
    {
        //public UInt32 roadNwObjType; //探索に必要な地図コンテンツ。リンクだけとは限らない
        public List<UInt32> roadNwObjTypeList; //探索に必要な地図コンテンツ。リンクだけとは限らない

        //public ReqType[] roadNwObjReqType;
        public CmnObjFilter roadNwObjFilter;
        public UInt32 roadGeometryObjType; //結果表示用
        public CmnObjFilter roadGeometryFilter;
        public UInt32 linkObjType; //リンク（コスト・方向あり）

        public Int32 nextLinkRefType; //次リンクの参照タイプ
        public Int32 backLinkRefType; //前リンクの参照タイプ
    }

    public class RouteResult
    {
        public ResultCode resultCode;
        public List<CostRecord> route;
        public List<CmnObjHandle> links;

        public RouteResult(ResultCode resultCode, List<CostRecord> route)
        {
            this.resultCode = resultCode;
            this.route = route;
        }

        public RouteResult(ResultCode resultCode, List<CostRecord> route, List<CmnObjHandle> links)
        {
            this.resultCode = resultCode;
            this.route = route;
            this.links = links;
        }
    }

    public enum RouteType
    {
        OdRoute = 0,
        Straight,
        Random
    }

    public enum ResultCode
    {
        Success = 0,
        Continue,
        NotFound,
        CalcError,
        ODError
    }

    /****** 経路計算マネージャ ******************************************************************************/


    public abstract class CmnRouteMgr
    {

        protected CmnMapMgr mapMgr;
        //protected DykstraSetting setting;

        //public LatLon orgLatLon;
        //public LatLon dstLatLon;

        public CmnObjHandle orgHdl;
        public CmnObjHandle dstHdl;

        //経路計算用メモリ
        public Dykstra dykstra;

        public RouteGenerator routeGenerator;

        //結果格納
        public List<CmnObjHandle> routeHdlList;

        public abstract RoutingMapType RoutingMapType { get; }

        public CmnRouteMgr() { }

        public CmnRouteMgr(CmnMapMgr mapMgr, DykstraSetting setting)
        {
            //this.setting = setting;
            dykstra = new Dykstra(mapMgr, setting, RoutingMapType);
            this.mapMgr = mapMgr;
        }

        public CmnRouteMgr(CmnMapMgr mapMgr, RouteGenerator routeGenerator)
        {
            this.mapMgr = mapMgr;
            this.routeGenerator = routeGenerator;
        }



        //public void SetMapMgr(CmnMapMgr mapMgr)
        //{
        //    dykstra = new Dykstra(mapMgr, this.setting, RoutingMapType);
        //    this.mapMgr = mapMgr;
        //}


        //データ準備

        public virtual int Prepare(LatLon orgLatLon, LatLon dstLatLon, bool allCache)
        {
            //探索レベルを決める？
            //CalcSearchLevel();

            if (orgLatLon == null || dstLatLon == null)
                return -1;

            //始点終点ハンドル取得

            //RoutingMapType routingMapType = mapMgr.RoutingMapType;

            uint startTileId = mapMgr.tileApi.CalcTileId(orgLatLon);
            IEnumerable<uint> tileIdListS = mapMgr.tileApi.CalcTileIdAround(orgLatLon, 1000, mapMgr.tileApi.DefaultLevel);
            tileIdListS.ForEach(x => mapMgr.LoadTile(x, null));
            orgHdl = mapMgr.SearchObj(orgLatLon, RoutingMapType.roadNwObjFilter, 1, -1);
            if (orgHdl == null)
                return -1;
            PolyLinePos orgLinkPos = LatLon.CalcNearestPoint(orgLatLon, orgHdl.Geometry);
            
            uint destTileId = mapMgr.tileApi.CalcTileId(dstLatLon);
            IEnumerable<uint> tileIdListD = mapMgr.tileApi.CalcTileIdAround(dstLatLon, 1000, mapMgr.tileApi.DefaultLevel);
            tileIdListD.ForEach(x => mapMgr.LoadTile(x, null));        
            dstHdl = mapMgr.SearchObj(dstLatLon, RoutingMapType.roadNwObjFilter, 1, -1);
            if (dstHdl == null)
                return -1;
            PolyLinePos dstLinkPos = LatLon.CalcNearestPoint(dstLatLon, dstHdl.Geometry);

            //始終点タイル登録

            tileIdListS.ForEach(x => ReadTile(x));
            tileIdListD.ForEach(x => ReadTile(x));

            Console.WriteLine($"[{Environment.TickCount / 1000.0:F3}] Memory = {(Environment.WorkingSet / 1024.0 / 1024.0):F1} MB");

            //利用タイル決定
            //List<uint> searchTileId = CalcRouteTileId2();
            List<uint> searchTileId = mapMgr.tileApi.CalcTileEllipse(orgHdl.tile.TileId, dstHdl.tile.TileId, 1.2);

            Console.WriteLine($"[{Environment.TickCount / 1000.0:F3}] calc tile num = {searchTileId.Count}");

            //タイル読み込み・コストテーブル登録
            ReadTiles(searchTileId, allCache);

            //Console.WriteLine($"[{Environment.TickCount / 1000.0:F3}] dicTileCostInfo.Count = {dykstra.dicTileCostInfo.Count}");


            //始終点コスト設定
            dykstra.AddTileInfo(orgHdl.tile.TileId);
            dykstra.AddTileInfo(dstHdl.tile.TileId);

            dykstra.SetStartCost(orgHdl, orgLinkPos.shapeOffset, orgHdl.obj.Oneway);
            dykstra.SetDestination(dstHdl, dstLinkPos.shapeOffset, dstHdl.obj.Oneway);


            return 0;
        }



        //ダイクストラ計算
        public virtual RouteResult CalcRoute(LatLon orgLatLon, LatLon dstLatLon)
        {
            int ret;

            //Prepare
            ret = Prepare(orgLatLon, dstLatLon, false);
            if (ret != 0)
            {
                return new RouteResult(ResultCode.ODError, null);
            }

            //計算
            ResultCode result = dykstra.CalcRoute();

            //結果出力
            List<CostRecord> costRecordList = dykstra.MakeRouteInfo();

            return new RouteResult(result, costRecordList);

            //計算
            //RouteResult routeCalcResult = CalcOdRoute();

            //return routeCalcResult;
        }

        //ダイクストラ計算
        //public virtual RouteResult CalcOdRoute()
        //{
        //    //計算
        //    ResultCode ret = dykstra.CalcRoute();

        //    //結果出力
        //    List<CostRecord> result = dykstra.MakeRouteInfo();

        //    return new RouteResult(ret, result);

        //}

        //自律経路計算

        public virtual RouteResult CalcAutoRoute(LatLon orgLatLon, double routeLengthM = 5000.0)
        {
            uint startTileId = mapMgr.tileApi.CalcTileId(orgLatLon);
            IEnumerable<uint> tileIdListS = mapMgr.tileApi.CalcTileIdAround(orgLatLon, 1000, mapMgr.tileApi.DefaultLevel);
            tileIdListS.ForEach(x => mapMgr.LoadTile(x, null));
            orgHdl = mapMgr.SearchObj(orgLatLon, RoutingMapType.roadNwObjFilter, 1, -1);
            if (orgHdl == null)
                return null;

            RouteResult ret = CalcAutoRoute(orgHdl, routeLengthM);
            return ret;
        }

        public virtual RouteResult CalcAutoRoute(CmnObjHandle objHdl, double routeLengthM = 5000.0)
        {
            AutoRouteGenerator routeGenerator = new AutoRouteGenerator(mapMgr, RoutingMapType);

            //計算
            //ResultCode ret = ResultCode.CalcError;
            RouteResult ret = routeGenerator.CalcAutoRoute(objHdl, routeLengthM);

            //結果出力
            //List<CostRecord> result = null;

            //return new RouteResult(ret, result);

            return ret;

        }

        //private List<uint> CalcRouteTileId2()
        //{
        //    //最小エリア対応

        //    double ratio = 1.2;
        //    return GisTileCode.CalcTileEllipse(orgHdl.tile.tileId, dstHdl.tile.tileId, ratio);

        //}


        private int ReadTile(uint tileId)
        {
            byte maxRoadType = CalcMaxUsableRoadType(tileId);

            dykstra.SetTileInfo(tileId, maxRoadType, orgHdl.tile.TileId, dstHdl.tile.TileId);
            dykstra.AddTileInfo(tileId);

            return 0;
        }


        private int ReadTiles(List<uint> tileIdList, bool allCache)
        {
            //Console.WriteLine("tile reading");
            //int count = 0;
            foreach (uint tileId in tileIdList)
            {
                byte maxRoadType = CalcMaxUsableRoadType(tileId);

                dykstra.SetTileInfo(tileId, maxRoadType, orgHdl.tile.TileId, dstHdl.tile.TileId);

                if (allCache)
                {
                    dykstra.AddTileInfo(tileId);
                }

            }
            Console.WriteLine("tile read finished");

            return 0;
        }


        //地図仕様に応じてオーバーライド
        protected virtual byte CalcMaxUsableRoadType(uint targetTileId)
        {
            return 0xff;

            //if (targetTileId == orgHdl.TileId || targetTileId == dstHdl.TileId)
            //    return 9;

            //float DistFromStartTile = (float)mapMgr.tileApi.CalcTileDistance(targetTileId, orgHdl.TileId);
            //float DistFromDestTile = (float)mapMgr.tileApi.CalcTileDistance(targetTileId, dstHdl.TileId);

            //double minDist = Math.Min(DistFromStartTile, DistFromDestTile);

            ////double minDist = MapTool.CalcTileDistance(tileId, startTileId);
            ////double distFromDest = MapTool.CalcTileDistance(tileId, destTileId);
            ////minDist = Math.Min(minDist, distFromDest);

            //if (minDist < 5000)
            //{
            //    return 8;
            //}
            //else if (minDist < 8000)
            //{
            //    return 6;
            //}
            //else if (minDist < 12000)
            //{
            //    return 5;
            //}
            //else if (minDist < 15000)
            //{
            //    return 4;
            //}
            //else if (minDist < 50000)
            //{
            //    return 3;
            //}
            //else if (minDist < 100000)
            //{
            //    return 2;
            //}
            //else //  >100km
            //{
            //    return 1;
            //}

        }


        /* 結果出力 *****************/

        public virtual List<CmnObjHandle> GetRouteHdlList()
        {
            routeHdlList = dykstra.routeResult.Select(x=>x.DLinkHdl).ToList();
            return routeHdlList;
        }



        public LatLon[] GetRouteGeometry(List<CmnObjHandle> routeLinks)
        {
            foreach (var x in routeLinks.Select(x=>x.TileId).Distinct())
            {
                mapMgr.LoadTile(x, new List<ObjReqType> { new ObjReqType(RoutingMapType.roadGeometryObjType) });
                //mapMgr.LoadTile(x, new List<uint> { RoutingMapType.roadGeometryObjType });
            }

            List<LatLon> retList = new List<LatLon>();
            retList.Add(routeLinks[0].DirGeometry[0]);
            retList.AddRange(routeLinks.Select(x => x.DirGeometry.Skip(1)).SelectMany(x => x));
            return retList.ToArray();

        }

        public virtual LatLon[] GetResult()
        {
            //dykstra.routeResult.ForEach(x =>
            //{
            //    mapMgr.LoadTile(x.tileCostInfo.tileId, dykstra.routingMapType.roadGeometryObjType, x.tileCostInfo.maxUsableRoadType);
            //});

            //List<LatLon> retList = new List<LatLon>();
            //retList.Add(dykstra.routeResult[0].DLinkHdl.DirGeometry[0]);
            //retList.AddRange(dykstra.routeResult.Select(x => x.DLinkHdl.DirGeometry.Skip(1)).SelectMany(x => x));
            //return retList.ToArray();


           return dykstra.GetRouteGeometry();
        }


        public void PrintResult()
        {
            dykstra.PrintResult();
        }

        //public int PrintCalcCount()
        //{
        //    return dykstra.PrintCalcCount();

        //}

        //public int WriteResult()
        //{
        //    return dykstra.WriteResult();
        //}

        //public List<CmnDirObjHandle> GetResult()
        //{
        //    return dykstra.GetResult();
        //}


        //Lambda用
        public LatLon[] CalcRouteGeometry(LatLon orgLatLon, LatLon dstLatLon)
        {

            RouteResult routeCalcResult = CalcRoute(orgLatLon, dstLatLon);

            //Prepare(orgLatLon, dstLatLon, false);

            //CalcOdRoute();

            //道路NWメモリ解放？
            dykstra.dicTileCostInfo = null;

            CmnMapView mapView = new CmnMapView();
            //List<CmnObjHandle> routeHdlList = GetRouteHdlList();
            LatLon[] routeGeometry = GetResult();


            Console.WriteLine($"maxQueue = {dykstra.logMaxQueue} (average = {dykstra.logUnprocessedCount.Take(dykstra.logCalcCount).Average():F2})");


            return routeGeometry;
        }

        public List<CmnObjHandle> CalcRouteLinkList(LatLon orgLatLon, LatLon dstLatLon)
        {
            RouteResult routeCalcResult = CalcRoute(orgLatLon, dstLatLon);

            //CmnRouteMgr routeMgr = CreateRouteMgr();

            //this.orgLatLon = orgLatLon;
            //this.dstLatLon = dstLatLon;

            //Prepare(orgLatLon, dstLatLon, false);

            //CalcOdRoute();

            //道路NWメモリ解放？
            dykstra.dicTileCostInfo = null;

            CmnMapView mapView = new CmnMapView();
            return GetRouteHdlList();

        }
    }

}
