using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libGis
{
    public class CostRecord
    {
        public int totalCost; //経路始点～当該リンクまでのコスト
        public int remainCost; //目的地側から計算した残りコスト

        public byte status = 0; //0:未開始　1:計算中　2:探索終了
        public byte statusD = 0; //0:未開始　1:計算中　2:探索終了  終点側
        public bool isGoal = false; //以降は目的地側から計算済み

        public TileCostInfo tileCostInfo; //親参照
        public short linkIndex;
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

        public CmnObjHandle LinkHdl
        {
            //get { return tileCostInfo.tile.GetMapLink(linkIndex); }
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

            for (short i = 0; i < numLink; i++)
            {
                costInfo[i] = new CostRecord[2];
                costInfo[i][0] = new CostRecord();
                costInfo[i][1] = new CostRecord();
                costInfo[i][0].tileCostInfo = this;
                costInfo[i][1].tileCostInfo = this;
                costInfo[i][0].linkIndex = i;
                costInfo[i][1].linkIndex = i;
                costInfo[i][0].linkDirection = 0;
                costInfo[i][1].linkDirection = 1;
            }
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
        public int numElement { get; private set; } = 0;
        CostRecord[] unprocessed;

        public CostInfoManage(int maxNum)
        {
            unprocessed = new CostRecord[maxNum];
        }

        public void Add(CostRecord costRecord)
        {
            unprocessed[numElement] = costRecord;
            numElement++;
        }

        public void Delete(int index)
        {
            unprocessed[index] = unprocessed[numElement - 1];
            numElement--;
        }

        public int GetMinCostIndex()
        {
            int tmpMinCost = int.MaxValue;
            int tmpMinIndex = -1;
            for(int i=0; i<numElement; i++)
            {
                if (unprocessed[i].totalCost < tmpMinCost)
                {
                    tmpMinCost = unprocessed[i].totalCost;
                    tmpMinIndex = i;
                }
            }
            return tmpMinIndex;
        }

        public CostRecord GetCostRecord(int index)
        {
            return unprocessed[index];
        }

    }


    public class Dykstra
    {
        public Dictionary<uint, TileCostInfo> dicTileCostInfo;
        CostInfoManage unprocessed;
        List<CostRecord> goalInfo;

        public CmnObjHandle startHdl;
        public CmnObjHandle destHdl;

        CmnMapMgr mapMgr;

        //汎用種別
        UInt32 linkObjType = (UInt32)ECmnMapContentType.Link;
        UInt32 nextLinkRefType = (UInt32)ECmnMapRefType.NextLink;
        UInt32 backLinkRefType = (UInt32)ECmnMapRefType.BackLink;

        //性能測定用
        public int[] logTickCountList;
        public int[] logUnprocessedCount;
        public int logMaxQueue = 0;
        public int logCalcCount = 0;

        public Dykstra(CmnMapMgr mapMgr)
        {
            //costTab = new RouteCost();
            this.mapMgr = mapMgr;
            dicTileCostInfo = new Dictionary<uint, TileCostInfo>();
            goalInfo = new List<CostRecord>();

            unprocessed = new CostInfoManage(10000);
            logUnprocessedCount = new int[1000000];
            logTickCountList = new int[1000000];

        }


        /****** データアクセス ******************************************************************************/

     


        /****** 設定 ******************************************************************************/

        //public int SetStartCost(CmnObjHandle linkHdr, int offset, sbyte direction)
        //{
        //    //TileObjId start = new TileObjId(mapPos.tileId, mapPos.linkId);
        //    //MapLink sMapLink = mapMgr.SearchMapLink(start);

        //    if (direction == 1 || direction >= 2)
        //    {
        //        CostRecord costRecS = GetLinkCostInfo(null, linkHdr.tile.tileId, linkHdr.mapLink.index, 1);

        //        costRecS.linkDirection = 1;
        //        costRecS.totalCost = offset;
        //        costRecS.status = 1;
        //        unprocessed.Add(costRecS);
        //    }

        //    if (direction == 0 || direction >= 2)
        //    {
        //        CostRecord costRecE = GetLinkCostInfo(null, linkHdr.tile.tileId, linkHdr.mapLink.index, 0);

        //        costRecE.linkDirection = 0;
        //        costRecE.totalCost = offset;
        //        costRecE.status = 1;
        //        unprocessed.Add(costRecE);
        //    }

        //    return 0;
        //}

        //public int SetDestination(CmnObjHandle linkHdr, int offset, sbyte direction)
        //{
        //    //offsetが暫定
        //    if (direction == 1 || direction >= 2)
        //    {
        //        CostRecord costRecS = GetLinkCostInfo(null, linkHdr.tile.tileId, linkHdr.mapLink.index, 1);

        //        costRecS.isGoal = true;
        //        costRecS.remainCost = offset;
        //        goalInfo.Add(costRecS);
        //    }

        //    if (direction == 0 || direction >= 2)
        //    {
        //        CostRecord costRecE = GetLinkCostInfo(null, linkHdr.tile.tileId, linkHdr.mapLink.index, 0);

        //        costRecE.isGoal = true;
        //        costRecE.remainCost = offset;
        //        goalInfo.Add(costRecE);
        //    }

        //    return 0;
        //}

        //public int SetTileInfo(uint tileId, byte maxUsableRoadType)
        //{
        //    if (dicTileCostInfo.ContainsKey(tileId))
        //        return 0;

        //    TileCostInfo tmpTileCostInfo = new TileCostInfo(tileId);

        //    tmpTileCostInfo.CalcTileDistance(startLinkHdl.tile.tileId, destLinkHdl.tile.tileId);
        //    tmpTileCostInfo.maxUsableRoadType = maxUsableRoadType;
        //    dicTileCostInfo.Add(tileId, tmpTileCostInfo);

        //    return 0;
        //}

        public int AddTileInfo(uint tileId)
        {
            if (!dicTileCostInfo.ContainsKey(tileId))
                return 0;

            TileCostInfo tmpTileCostInfo = dicTileCostInfo[tileId];
            if (tmpTileCostInfo.tile != null)
                return 0;

            mapMgr.LoadTile(tileId, linkObjType, tmpTileCostInfo.maxUsableRoadType);
            CmnTile tmpTile = mapMgr.SearchTile(tileId);

            tmpTileCostInfo.SetTileCostInfo(tmpTile, linkObjType);
            Console.Write($"\r {dicTileCostInfo.Count()} tiles read");


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
            //計算対象選定　処理未完了＆コスト最小を探す
            int minIndex = unprocessed.GetMinCostIndex();

            if (minIndex < 0) //探索失敗
            {
                Console.WriteLine($"[{Environment.TickCount / 1000.0:F3}] All Calculation Finished! Destination Not Found");
                return -1;
            }

            CostRecord currentCostInfo = unprocessed.GetCostRecord(minIndex);

            if (currentCostInfo == null) //異常
            {
                Console.WriteLine("Fatal Error");
                return -1;
            }
            if (currentCostInfo.isGoal) //探索成功
            {
                Console.WriteLine($"[{Environment.TickCount / 1000.0:F3}] Goal Found !! (CalcCount = {logCalcCount}, totalCost = {currentCostInfo.totalCost})");
                currentCostInfo.status = 2;
                return -1;
            }
            if (currentCostInfo.status == 2) //処理済みデータ
            {
                unprocessed.Delete(minIndex);
                return 0;
            }

            CmnDirObjHandle currentDLinkHdl = currentCostInfo.DLinkHdl;

            List<CmnDirObjHandle> connectLinkList = null;
            List<CmnObjHdlRef> objHdlRefList;
            //接続リンク取得
            while (true)
            {
                //接続リンク。向きは自動判別
                objHdlRefList = mapMgr.SearchRefObject(currentDLinkHdl, (int)(ECmnMapRefType.NextLink));

                //不足タイルがあれば読み込み
                //初期設定の読み込み可能範囲内タイル
                List<uint> noDataTileIdList = objHdlRefList
                    .Where(x => x.noData)
                    .Select(x => x.nextRef.key.tileId)
                    .Where(x => x != 0xffffffff)
                    .ToList();

                if(noDataTileIdList.Count > 0)
                    noDataTileIdList.ForEach(x => AddTileInfo(x));
                else
                    break;
            }


            //接続リンクとコスト参照
            foreach (CmnDirObjHandle nextLinkRef in connectLinkList)
            {
                //探索除外：　Uターンリンク、タイルに応じた使用可能道路種別でない、スタート付近で道路種別が下がる移動

                if (nextLinkRef.obj == currentDLinkHdl.obj
                    || nextLinkRef.obj.SubType > currentCostInfo.tileCostInfo.maxUsableRoadType)
                    continue;
                //.Where(x => x.mapLink != currentLinkHdl.mapLink && x.mapLink.roadType <= currentCostInfo.tileCostInfo.maxUsableRoadType) )


                if (nextLinkRef.obj.SubType >= 6
                    && currentDLinkHdl.obj.SubType < nextLinkRef.obj.SubType
                    && currentCostInfo.tileCostInfo.DistFromDestTile > 8000)
                    continue;


                CostRecord nextCostInfo = GetLinkCostInfo(currentCostInfo, nextLinkRef);

                int nextTotalCost;

                //ゴールフラグの場合は、残コストを足す。足すけど保存NG？ゴール側statusを見るべき？
                if (nextCostInfo.isGoal)
                {
                    nextTotalCost = currentCostInfo.totalCost + nextCostInfo.remainCost;
                }
                else
                {
                    //コストは暫定でリンク長
                   nextTotalCost = currentCostInfo.totalCost + nextLinkRef.obj.Length;
                }
                //コストを足した値を、接続リンクの累積コストを見て、より小さければ上書き
                if (nextCostInfo.status == 0 || nextTotalCost < nextCostInfo.totalCost)
                {
                    nextCostInfo.totalCost = nextTotalCost;
                    nextCostInfo.status = 1;
                    //nextCostInfo.tileCostInfo.status = 1;
                    nextCostInfo.back = currentCostInfo;
                    unprocessed.Add(nextCostInfo);
                }

            }

            //リンクの探索ステータス更新
            currentCostInfo.status = 2;
            unprocessed.Delete(minIndex);

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
                logUnprocessedCount[logCalcCount] = unprocessed.numElement;
                if (unprocessed.numElement > logMaxQueue)
                    logMaxQueue = unprocessed.numElement;
                logCalcCount++;

                if (ret != 0) break;
            }

            return 0;
        }


        /****** 計算結果出力 ******************************************************************************/

        //public int PrintResult()
        //{
        //    //List<int> routeIdList = new 
        //    CostRecord Bestgoal = goalInfo.Where(x => x.status == 2).OrderBy(x => x.totalCost).FirstOrDefault();
        //    if (Bestgoal == null)
        //    {
        //        Console.WriteLine("No Result!");
        //        return -1;
        //    }
        //    Console.WriteLine($"Goal: {Bestgoal.tileCostInfo.tile.link[Bestgoal.linkIndex]}");
        //    CostRecord tmp = Bestgoal;
        //    while (tmp.back != null)
        //    {
        //        Console.WriteLine($" {tmp.tileCostInfo.tileId}\t{tmp.LinkId}\t{tmp.linkIndex}\t{tmp.linkDirection}\t{tmp.MapLink.roadType}\t{tmp.totalCost}");
        //        //Console.WriteLine($" {tmp.linkId}\t{tmp.totalCost}");
        //        tmp = tmp.back;
        //    }


        //    return 0;
        //}

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

}
