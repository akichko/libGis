using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libGis
{
    public class CostRecord
    {
        public int totalCostS; //経路始点～当該リンクまでのコスト
        public int totalCostD; //目的地側から計算した残りコスト

        public byte statusS = 0; //0:未開始　1:計算中　2:探索終了
        public byte statusD = 0; //0:未開始　1:計算中　2:探索終了  終点側
        public bool isGoal = false; //以降は目的地側から計算済み

        public TileCostInfo tileCostInfo; //親参照
        public ushort linkIndex;
        public byte linkDirection;

        public CostRecord back;
        public CostRecord next;

        public UInt64 LinkId
        {
            get { return tileCostInfo.linkArray[linkIndex].Id; }
        }

        public CmnObj MapLink
        {
            get { return tileCostInfo.linkArray[linkIndex]; }
        }

        public uint TileId => tileCostInfo.tileId;

        public int Cost => (int)MapLink.Length;

        public CmnObjHandle LinkHdl
        {
            get { return new CmnObjHandle(tileCostInfo.tile, tileCostInfo.linkArray[linkIndex]); }
        }

        public CmnDirObjHandle DLinkHdl
        {
            get { return new CmnDirObjHandle(tileCostInfo.tile, tileCostInfo.linkArray[linkIndex], linkDirection); }
        }
    }

    public class TileCostInfo
    {
        public uint tileId;
        public CmnTile tile;
        public CmnObj[] linkArray;
        public CostRecord[][] costInfo; //始点方向リンク、終点方向リンク
        public byte status = 0; //0:未開始　1:計算中　2:探索終了

        public bool isLoaded = false;

        public byte maxUsableRoadType = 255; //分岐先に移動可能な道路種別
        public byte readStatus = 0; //読み込んだ最大道路種別
        public float DistFromStartTile;
        public float DistFromDestTile;

        public TileCostInfo(uint tileId)
        {
            this.tileId = tileId;
        }

        public int SetTileCostInfo(CmnTile tile, UInt32 linkObjType)
        {
            this.tileId = tile.tileId;
            this.tile = tile;
            linkArray = tile.GetObjArray(linkObjType);

            //データ上限求める

            int numLink = linkArray.Length;
            costInfo = new CostRecord[numLink][];

            for (ushort i = 0; i < numLink; i++)
            {
                costInfo[i] = new CostRecord[2];
                costInfo[i][0] = new CostRecord();
                costInfo[i][1] = new CostRecord();
                costInfo[i][0].tileCostInfo = this;
                costInfo[i][1].tileCostInfo = this;

                //使用時に設定すれば高速化かも
                costInfo[i][0].linkIndex = i;
                costInfo[i][1].linkIndex = i;
                costInfo[i][0].linkDirection = 0;
                costInfo[i][1].linkDirection = 1;
            }

            isLoaded = true;
            return 0;
        }

        public int CalcTileDistance(uint startTileId, uint destTileId)
        {
            DistFromStartTile = (float)GisTileCode.SCalcTileDistance(tileId, startTileId);
            DistFromDestTile = (float)GisTileCode.SCalcTileDistance(tileId, destTileId);

            return 0;
        }


    }





    public class CostInfoManage
    {
        public int numElementS { get; private set; } = 0;
        public int numElementD { get; private set; } = 0;
        CostRecord[] unprocessedS;
        CostRecord[] unprocessedD;

        public int minCostS = 0;
        public int minCostD = 0;

        public CostInfoManage(int maxNum)
        {
            unprocessedS = new CostRecord[maxNum];
            unprocessedD = new CostRecord[maxNum];
        }

        public void Add(CostRecord costRecord, bool isStartSide)
        {
            if (isStartSide)
            {
                unprocessedS[numElementS] = costRecord;
                numElementS++;
            }
            else
            {
                unprocessedD[numElementD] = costRecord;
                numElementD++;
            }
        }

        public void Delete(int index, bool isStartSide)
        {
            if (isStartSide)
            {
                unprocessedS[index] = unprocessedS[numElementS - 1];
                numElementS--;
            }
            else
            {
                unprocessedD[index] = unprocessedD[numElementD - 1];
                numElementD--;
            }
        }

        public int GetMinCostIndex(bool isStartSide)
        {
            int tmpMinCost = int.MaxValue;
            int tmpMinIndex = -1;
            if (isStartSide)
            {
                for (int i = 0; i < numElementS; i++)
                {
                    if (unprocessedS[i].totalCostS < tmpMinCost)
                    {
                        tmpMinCost = unprocessedS[i].totalCostS;
                        tmpMinIndex = i;
                    }
                }
                minCostS = tmpMinCost;
            }
            else
            {
                for (int i = 0; i < numElementD; i++)
                {
                    if (unprocessedD[i].totalCostD < tmpMinCost)
                    {
                        tmpMinCost = unprocessedD[i].totalCostD;
                        tmpMinIndex = i;
                    }
                }
                minCostD = tmpMinCost;
            }
            return tmpMinIndex;
        }

        public CostRecord GetCostRecord(int index, bool isStartSide)
        {
            if (isStartSide)
                return unprocessedS[index];
            else
                return unprocessedD[index];
        }

    }


    public class Dykstra
    {
        public Dictionary<uint, TileCostInfo> dicTileCostInfo;
        CostInfoManage unprocessed;
        List<CostRecord> goalInfo;

        CostRecord finalRecord; //双方向ダイクストラの終了ポイント
        public List<CostRecord> routeResult;

        CmnMapMgr mapMgr;

        //汎用種別
        public RoutingMapType routingMapType;

        //性能測定用
        public int[] logTickCountList;
        public int[] logUnprocessedCount;
        public int logMaxQueue = 0;
        public int logCalcCount = 0;
        public int numTileLoad = 0;

        public Dykstra(CmnMapMgr mapMgr)
        {
            this.mapMgr = mapMgr;
            dicTileCostInfo = new Dictionary<uint, TileCostInfo>();
            goalInfo = new List<CostRecord>();

            unprocessed = new CostInfoManage(10000);
            logUnprocessedCount = new int[1000000];
            logTickCountList = new int[1000000];

            routingMapType = mapMgr.RoutingMapType;

            //roadNwObjType = mapMgr.GetMapObjType(ECmnMapContentType.Link) | mapMgr.GetMapObjType(ECmnMapContentType.Node);
            //roadGeometryObjType = mapMgr.GetMapObjType(ECmnMapContentType.LinkGeometry);
            //linkObjType = mapMgr.GetMapObjType(ECmnMapContentType.Link);
            //nextLinkRefType = mapMgr.GetMapRefType(ECmnMapRefType.NextLink);
            //backLinkRefType = mapMgr.GetMapRefType(ECmnMapRefType.BackLink);



        }


        /****** 設定 ******************************************************************************/

        public int SetStartCost(CmnObjHandle linkHdl, int offset, byte direction = 0xff)
        {
            //TileObjId start = new TileObjId(mapPos.tileId, mapPos.linkId);
            //MapLink sMapLink = mapMgr.SearchMapLink(start);

            //offsetが暫定
            CostRecord costRec;
            //順方向
            if (direction == 1 || direction == 0xff)
            {
                costRec = GetLinkCostInfo(null, linkHdl.tile.tileId, linkHdl.obj.Index, 1);

                costRec.linkIndex = linkHdl.obj.Index;
                costRec.linkDirection = 1;
                costRec.totalCostS = (int)linkHdl.obj.Length - offset;
                costRec.totalCostD = int.MaxValue;
                costRec.statusS = 1;
                unprocessed.Add(costRec, true);
            }

            //逆方向
            if (direction == 0 || direction == 0xff)
            {
                costRec = GetLinkCostInfo(null, linkHdl.tile.tileId, linkHdl.obj.Index, 0);

                costRec.linkIndex = linkHdl.obj.Index;
                costRec.linkDirection = 0;
                costRec.totalCostS = offset;
                costRec.totalCostD = int.MaxValue;
                costRec.statusS = 1;
                unprocessed.Add(costRec, true);
            }

            return 0;
        }

        public int SetDestination(CmnObjHandle linkHdl, int offset, byte direction = 0xff)
        {
            //offsetが暫定
            CostRecord costRec;
            //順方向
            if (direction == 1 || direction == 0xff)
            {
                costRec = GetLinkCostInfo(null, linkHdl.tile.tileId, linkHdl.obj.Index, 1);

                costRec.linkIndex = linkHdl.obj.Index;
                costRec.linkDirection = 1;
                costRec.totalCostS = int.MaxValue;
                costRec.totalCostD = offset;
                costRec.statusD = 1;
                unprocessed.Add(costRec, false);

                costRec.isGoal = true;
                goalInfo.Add(costRec);
            }
            //逆方向
            if (direction == 0 || direction == 0xff)
            {
                costRec = GetLinkCostInfo(null, linkHdl.tile.tileId, linkHdl.obj.Index, 0);

                costRec.linkIndex = linkHdl.obj.Index;
                costRec.linkDirection = 0;
                costRec.totalCostS = int.MaxValue;
                costRec.totalCostD = (int)linkHdl.obj.Length - offset;
                costRec.statusD = 1;
                unprocessed.Add(costRec, false);

                costRec.isGoal = true;
                goalInfo.Add(costRec);
            }

            return 0;
        }

        //利用可能タイルを事前登録
        public int SetTileInfo(uint tileId, byte maxUsableRoadType, uint tileIdS, uint tileIdE)
        {
            if (dicTileCostInfo.ContainsKey(tileId))
                return 0;

            TileCostInfo tileCostInfo = new TileCostInfo(tileId);

            tileCostInfo.CalcTileDistance(tileIdS, tileIdE);
            tileCostInfo.maxUsableRoadType = maxUsableRoadType;
            dicTileCostInfo.Add(tileId, tileCostInfo);

            return 0;
        }

        public int AddTileInfo(uint tileId)
        {
            if (!dicTileCostInfo.ContainsKey(tileId))
                return 0;

            TileCostInfo tmpTileCostInfo = dicTileCostInfo[tileId];
            if (tmpTileCostInfo.tile != null)
                return 0;

            mapMgr.LoadTile(tileId, routingMapType.roadNwObjType, tmpTileCostInfo.maxUsableRoadType);
            CmnTile tmpTile = mapMgr.SearchTile(tileId);

            tmpTileCostInfo.SetTileCostInfo(tmpTile, routingMapType.linkObjType);

            numTileLoad++;

            //Console.WriteLine($"Loaded: tileId = {tileId}, num = {numTileLoad}, {dicTileCostInfo.Count()} tiles read");

            return 0;
        }

        /****** 経路計算用 ******************************************************************************/

        public TileCostInfo GetTileCostInfo(uint tileId)
        {
            if (dicTileCostInfo.ContainsKey(tileId))
                return null;

            return dicTileCostInfo[tileId];
        }

        public CostRecord GetLinkCostInfo(CostRecord currentInfo, uint tileId, int linkIndex, int linkDirection)
        {
            if (currentInfo != null && currentInfo.tileCostInfo.tileId == tileId)
            {
                return currentInfo.tileCostInfo.costInfo[linkIndex][linkDirection];
            }

            if (!dicTileCostInfo.ContainsKey(tileId))
                return null;

            return dicTileCostInfo[tileId].costInfo[linkIndex][linkDirection];

        }

        public CostRecord GetLinkCostInfo(CostRecord currentInfo, CmnDirObjHandle linkRef)
        {
            return GetLinkCostInfo(currentInfo, linkRef.tile.tileId, linkRef.obj.Index, linkRef.direction);
        }




        /****** 経路計算メイン ******************************************************************************/

        public int CalcRouteStep()
        {
            //処理側決定
            bool isStartSide;
            if (unprocessed.minCostS < unprocessed.minCostD)
                isStartSide = true;
            else
                isStartSide = false;


            //計算対象選定　処理未完了＆コスト最小を探す
            int minIndex = unprocessed.GetMinCostIndex(isStartSide);

            if (minIndex < 0) //探索失敗
            {
                Console.WriteLine($"[{Environment.TickCount / 1000.0:F3}] All Calculation Finished! Destination Not Found");
                return -1;
            }

            CostRecord currentCostInfo = unprocessed.GetCostRecord(minIndex, isStartSide);

            if (currentCostInfo == null) //異常
            {
                Console.WriteLine("Fatal Error");
                return -1;
            }

            //if (currentCostInfo.isGoal) //探索成功
            if ((!isStartSide && currentCostInfo.next?.statusS == 2) || (isStartSide && currentCostInfo.back?.statusD == 2)) //探索成功
            {
                Console.WriteLine($"[{Environment.TickCount / 1000.0:F3}] Goal Found !! (CalcCount = {logCalcCount}, totalCost = {currentCostInfo.totalCostS})");
                //currentCostInfo.statusS = 2;
                //currentCostInfo.statusD = 2;
                finalRecord = currentCostInfo;
                return -1;
            }

            //処理済みデータ
            if ((isStartSide && currentCostInfo.statusS == 2) || (!isStartSide && currentCostInfo.statusD == 2))
            {
                unprocessed.Delete(minIndex, isStartSide);
                return 0;
            }

            //出発地側か目的地側か

            CmnDirObjHandle currentDLinkHdl = currentCostInfo.DLinkHdl;

            List<CmnDirObjHandle> connectLinkList = null;
            List<CmnObjHdlRef> objHdlRefList;
            List<uint> noDataTileIdList;

            //接続リンク取得
            while (true)
            {
                if (isStartSide)
                {
                    //接続リンク。向きは自動判別
                    objHdlRefList = mapMgr.SearchRefObject(currentDLinkHdl, routingMapType.nextLinkRefType);
                }
                else
                {
                    objHdlRefList = mapMgr.SearchRefObject(currentDLinkHdl, routingMapType.backLinkRefType);
                }

                //初期設定の読み込み可能範囲内タイル
                noDataTileIdList = objHdlRefList
                    //.Where(x => x.noData)
                    .Select(x => (x.objHdl?.tile?.tileId ?? x.nextRef?.key?.tileId) ?? 0xffffffff)
                    .Where(x => x != 0xffffffff && dicTileCostInfo.ContainsKey(x) && !dicTileCostInfo[x].isLoaded)
                    .ToList();

                //不足タイルがあれば読み込み
                if (noDataTileIdList.Count > 0)
                    noDataTileIdList.ForEach(x => AddTileInfo(x));
                else
                    break;
            }

            connectLinkList = objHdlRefList
                .Select(x => (CmnDirObjHandle)x.objHdl)
                .Where(x => x != null)
                .ToList();


            //接続リンクとコスト参照
            foreach (CmnDirObjHandle nextLinkRef in connectLinkList ?? new List<CmnDirObjHandle>())
            {
                //探索除外：　Uターンリンク、タイルに応じた使用可能道路種別でない、スタート付近で道路種別が下がる移動、一方通行逆走

                if (nextLinkRef.obj == currentDLinkHdl.obj
                    || nextLinkRef.obj.SubType > currentCostInfo.tileCostInfo.maxUsableRoadType
                    || (nextLinkRef.obj.IsOneway && nextLinkRef.obj.Oneway != nextLinkRef.direction))
                    continue;
                //.Where(x => x.mapLink != currentLinkHdl.mapLink && x.mapLink.roadType <= currentCostInfo.tileCostInfo.maxUsableRoadType) )


                if (nextLinkRef.obj.SubType >= 6
                    && currentDLinkHdl.obj.SubType < nextLinkRef.obj.SubType
                    && currentCostInfo.tileCostInfo.DistFromDestTile > 8000)
                    continue;


                CostRecord nextCostInfo = GetLinkCostInfo(currentCostInfo, nextLinkRef);

                //MapMgr共有などで、使用可能タイル以外のハンドル（経路のタイル管理にない）が取得できてしまうケースがある
                if (nextCostInfo == null)
                    continue;

                int nextTotalCost;

                //ゴールフラグの場合は、残コストを足す。足すけど保存NG？ゴール側statusを見るべき？
                //双方向ダイクストラでは不要

                if (isStartSide)
                {
                    //コストは暫定でリンク長、ではなく50
                    nextTotalCost = currentCostInfo.totalCostS + nextLinkRef.obj.Cost;

                    //コストを足した値を、接続リンクの累積コストを見て、より小さければ上書き
                    if (nextCostInfo.statusS == 0 || nextTotalCost < nextCostInfo.totalCostS)
                    {
                        nextCostInfo.totalCostS = nextTotalCost;
                        nextCostInfo.statusS = 1;
                        //nextCostInfo.tileCostInfo.status = 1;
                        nextCostInfo.back = currentCostInfo;
                        unprocessed.Add(nextCostInfo, isStartSide);
                    }
                    //リンクの探索ステータス更新
                    currentCostInfo.statusS = 2;

                }
                else //目的地側
                {
                    //コストは暫定でリンク長、ではなく50
                    nextTotalCost = currentCostInfo.totalCostD + nextLinkRef.obj.Cost;

                    //コストを足した値を、接続リンクの累積コストを見て、より小さければ上書き
                    if (nextCostInfo.statusD == 0 || nextTotalCost < nextCostInfo.totalCostD)
                    {
                        nextCostInfo.totalCostD = nextTotalCost;
                        nextCostInfo.statusD = 1;
                        //nextCostInfo.tileCostInfo.status = 1;
                        nextCostInfo.next = currentCostInfo;
                        unprocessed.Add(nextCostInfo, isStartSide);
                    }
                    //リンクの探索ステータス更新
                    currentCostInfo.statusD = 2;

                }

            }

            unprocessed.Delete(minIndex, isStartSide);

            return 0;
        }


        public int CalcRoute()
        {
            int ret;

            int pastTickCount = Environment.TickCount;
            int nowTickCount;
            //計算
            while (true)
            {
                ret = CalcRouteStep();

                nowTickCount = Environment.TickCount;
                logTickCountList[logCalcCount] = nowTickCount - pastTickCount;
                pastTickCount = nowTickCount;

                logUnprocessedCount[logCalcCount] = unprocessed.numElementS + unprocessed.numElementD;

                if (unprocessed.numElementS + unprocessed.numElementD > logMaxQueue)
                    logMaxQueue = unprocessed.numElementS + unprocessed.numElementD;

                logCalcCount++;

                if (ret != 0) break;
            }

            return 0;
        }


        /****** 計算結果出力 ******************************************************************************/

        public List<CostRecord> MakeRouteInfo()
        {
            if (finalRecord == null)
                return null;

            CostRecord tmpCostRecord = finalRecord;

            List<CostRecord> routeList = new List<CostRecord>();

            while (tmpCostRecord != null)
            {
                routeList.Add(tmpCostRecord);
                tmpCostRecord = tmpCostRecord.back;
            }
            routeList.Reverse();

            tmpCostRecord = finalRecord;
            if (tmpCostRecord.next != null) //重複登録排除
            {
                tmpCostRecord = tmpCostRecord.next;
            }

            while (tmpCostRecord != null)
            {
                routeList.Add(tmpCostRecord);
                tmpCostRecord = tmpCostRecord.next;
            }

            routeResult = routeList;
            return routeList;
        }

        public LatLon[] GetRouteGeometry()
        {
            routeResult.ForEach(x =>
            {
                mapMgr.LoadTile(x.tileCostInfo.tileId, routingMapType.roadGeometryObjType, x.tileCostInfo.maxUsableRoadType);
            });
            return routeResult.Select(x => x.DLinkHdl).Select(x => x.obj.GetGeometry(x.direction)).SelectMany(x => x).ToArray();

        }

        public void PrintResult()
        {

            Console.WriteLine($" TileId\tLinkId\tIndex\tDirection\tSubType\tCost\ttotalCostS\ttotalCostD");
            routeResult.ForEach(x =>
            {
                Console.WriteLine($" {x.TileId}\t{x.LinkId}\t{x.linkIndex}\t{x.linkDirection}\t{x.DLinkHdl.obj.SubType}\t{x.Cost}\t{x.totalCostS}\t{x.totalCostD}");
            });
        }

        //public int WriteResult()
        //{
        //    return 0;
        //}

        //public List<CmnObjHdlDir> GetResult()
        //{
        //    //List<List<LinkRef>> resultInfo = new List<List<LinkRef>>();
        //    CostRecord goal = goalInfo.Where(x => x.status == 2).OrderBy(x => x.totalCost).FirstOrDefault();
        //    if (goal == null)
        //    {
        //        Console.WriteLine("no goal");
        //        return null;
        //    }
        //    List<DLinkHandle> routeIdList = new List<DLinkHandle>();
        //    CostRecord tmp = goal;
        //    while (tmp.back != null)
        //    {
        //        DLinkHandle tmpLinkRef = new DLinkHandle();
        //        tmpLinkRef.tile = tmp.tileCostInfo.tile;
        //        tmpLinkRef.mapLink = tmp.tileCostInfo.tile.link[tmp.linkIndex];
        //        tmpLinkRef.direction = tmp.linkDirection;
        //        tmp = tmp.back;
        //        routeIdList.Add(tmpLinkRef);
        //    }
        //    routeIdList.Reverse();
        //    return routeIdList;
        //    //return resultInfo;
        //}


        //public int PrintCalcCount()
        //{
        //    Console.WriteLine($"calcCount = {logCalcCount}");
        //    return 0;
        //}

    }

    public class RoutingMapType
    {
        public UInt32 roadNwObjType; //探索に必要な地図コンテンツ。リンクだけとは限らない
        public UInt32 roadGeometryObjType; //結果表示用
        public UInt32 linkObjType; //リンク（コスト・方向あり）

        public Int32 nextLinkRefType; //次リンクの参照タイプ
        public Int32 backLinkRefType; //前リンクの参照タイプ
    }


/****** 経路計算マネージャ ******************************************************************************/


public class CmnRouteMgr
    {

        CmnMapMgr mapMgr;

        public LatLon orgLatLon;
        public LatLon dstLatLon;

        public CmnObjHandle orgHdl;
        public CmnObjHandle dstHdl;

        //経路計算用メモリ
        public Dykstra dykstra;

        public CmnRouteMgr(CmnMapMgr mapMgr)
        {
            //startPos = new MapPos();
            //destPos = new MapPos();
            dykstra = new Dykstra(mapMgr);
            this.mapMgr = mapMgr;
        }


        //public int SetOrgin(CmnDirObjHandle handle)
        //{
        //    this.orgHdl = handle;
        //    return 0;
        //}


        //public int SetDestination(CmnDirObjHandle handle)
        //{
        //    this.dstHdl = handle;
        //    return 0;
        //}


        //データ準備

        public int Prepare(bool allCache)
        {
            //探索レベルを決める？
            //CalcSearchLevel();

            if (orgLatLon == null || dstLatLon == null)
                return -1;

            //始点終点ハンドル取得

            RoutingMapType routingMapType = mapMgr.RoutingMapType;

            uint startTileId = mapMgr.tileApi.CalcTileId(orgLatLon);
            List<uint> tileIdListS = mapMgr.tileApi.CalcTileIdAround(orgLatLon, 1000, mapMgr.tileApi.DefaultLevel);
            tileIdListS.ForEach(x => mapMgr.LoadTile(x));
            orgHdl = mapMgr.SearchObj(orgLatLon, 1, false, routingMapType.roadNwObjType);

            uint destTileId = mapMgr.tileApi.CalcTileId(dstLatLon);
            List<uint> tileIdListD = mapMgr.tileApi.CalcTileIdAround(dstLatLon, 1000, mapMgr.tileApi.DefaultLevel);
            tileIdListD.ForEach(x => mapMgr.LoadTile(x));        
            dstHdl = mapMgr.SearchObj(dstLatLon, 1, false, routingMapType.roadNwObjType);

            if (orgHdl == null || dstHdl == null)
                return -1;

            //始終点タイル登録

            tileIdListS.ForEach(x => ReadTile(x));
            tileIdListD.ForEach(x => ReadTile(x));

            Console.WriteLine($"[{Environment.TickCount / 1000.0:F3}] Memory = {(Environment.WorkingSet / 1024.0 / 1024.0):F1} MB");

            //利用タイル決定
            List<uint> searchTileId = CalcRouteTileId2();

            Console.WriteLine($"[{Environment.TickCount / 1000.0:F3}] calc tile num = {searchTileId.Count}");

            //タイル読み込み・コストテーブル登録
            ReadTiles(searchTileId, allCache);

            Console.WriteLine($"[{Environment.TickCount / 1000.0:F3}] read tile num = {dykstra.dicTileCostInfo.Count}");


            //始終点コスト設定
            dykstra.AddTileInfo(orgHdl.tile.tileId);
            dykstra.AddTileInfo(dstHdl.tile.tileId);

            dykstra.SetStartCost(orgHdl, 10, orgHdl.obj.Oneway);
            dykstra.SetDestination(dstHdl, 10, dstHdl.obj.Oneway);


            return 0;
        }



        //ダイクストラ計算
        public int CalcRoute()
        {
            //計算
            dykstra.CalcRoute();

            //結果出力
            List<CostRecord> result = dykstra.MakeRouteInfo();

            return 0;
        }


        private List<uint> CalcRouteTileId2()
        {
            //最小エリア対応

            double ratio = 1.2;
            return GisTileCode.CalcTileEllipse(orgHdl.tile.tileId, dstHdl.tile.tileId, ratio);

        }


        private int ReadTile(uint tileId)
        {
            byte maxRoadType = CalcMaxUsableRoadType(tileId, orgHdl.tile.tileId, dstHdl.tile.tileId);

            dykstra.SetTileInfo(tileId, maxRoadType, orgHdl.tile.tileId, dstHdl.tile.tileId);
            dykstra.AddTileInfo(tileId);

            return 0;
        }


        private int ReadTiles(List<uint> tileIdList, bool allCache)
        {
            Console.WriteLine("tile reading");
            //int count = 0;
            foreach (uint tileId in tileIdList)
            {
                byte maxRoadType = CalcMaxUsableRoadType(tileId, orgHdl.tile.tileId, dstHdl.tile.tileId);

                dykstra.SetTileInfo(tileId, maxRoadType, orgHdl.tile.tileId, dstHdl.tile.tileId);

                if (allCache)
                {
                    dykstra.AddTileInfo(tileId);
                }

            }
            Console.WriteLine("tile read finished");

            return 0;
        }


        private byte CalcMaxUsableRoadType(uint targetTileId, uint startTileId, uint destTileId)
        {
            float DistFromStartTile = (float)GisTileCode.SCalcTileDistance(targetTileId, startTileId);
            float DistFromDestTile = (float)GisTileCode.SCalcTileDistance(targetTileId, destTileId);

            double minDist = Math.Min(DistFromStartTile, DistFromDestTile);

            //double minDist = MapTool.CalcTileDistance(tileId, startTileId);
            //double distFromDest = MapTool.CalcTileDistance(tileId, destTileId);
            //minDist = Math.Min(minDist, distFromDest);

            if (minDist < 2000)
            {
                return 7;
            }
            else if (minDist < 8000)
            {
                return 6;
            }
            else if (minDist < 15000)
            {
                return 4;
            }
            else if (minDist < 50000)
            {
                return 3;
            }
            else if (minDist < 100000)
            {
                return 2;
            }
            else //  >100km
            {
                return 1;
            }

        }


        /* 結果出力 *****************/

        public LatLon[] GetResult()
        {
            return dykstra.GetRouteGeometry();
        }

        //public void WriteCacheTileXY()
        //{
        //    Console.WriteLine("Writing Cache Tile List ... ");

        //    using (var sw = new StreamWriter(@"D:\share\osm\cacheTile.txt"))
        //    {

        //        foreach (var x in dykstra.dicTileCostInfo.Where(x => x.Value.costInfo != null))
        //        {
        //            LatLon tmp = MapTool.CalcTileLatLon(x.Value.tileId);
        //            //TileXY tmp = new TileXY(x.Value.tileId);
        //            //Console.WriteLine($" [{x.Value.tileId}], {tmp.lon}, {tmp.lat}");
        //            sw.WriteLine($"{x.Value.tileId}, {tmp.lon}, {tmp.lat}");
        //        }
        //    }
        //}

        //public void WriteCalclatedLinks()
        //{
        //    Console.WriteLine("Writing calculated Link List ... ");

        //    int tileCount = 0;
        //    using (var sw = new StreamWriter(@"D:\share\osm\calculatedLink.txt"))
        //    {
        //        foreach (var tileCost in dykstra.dicTileCostInfo.Values.Where(x => x.costInfo != null))
        //        {

        //            var linkList = tileCost.costInfo
        //                .SelectMany(x => x)
        //                .Where(x => x.status != 0)
        //                .Select(x => x.tileCostInfo.road.link[x.linkIndex]);

        //            if (linkList.Count() > 0)
        //            {
        //                tileCount++;
        //                //Console.WriteLine($"tile = {tileCost.tileId} count={tileCount}");
        //                mapMgr.LoadTile(tileCost.tileId, MapContentType.All, 0xff);
        //            }

        //            foreach (var link in linkList)
        //            {
        //                link.WriteGeometry(0, sw);
        //                sw.WriteLine("");

        //            }

        //        }
        //        Console.WriteLine($"calculated tile num = {tileCount}");

        //    }
        //}

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



    }

}
