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

    public abstract class CostInfo2
    {
        public int totalCost; //経路始点～当該リンクまでのコスト
        public byte status = 0; //0:未開始　1:計算中　2:探索終了
        public CostRecord back;

        public abstract DirectionCode Direction { get; }
    }

    public class CostInfoPositive : CostInfo2
    {
        public override DirectionCode Direction => DirectionCode.Positive;
    }
    public class CostInfoNegative : CostInfo2
    {
        public override DirectionCode Direction => DirectionCode.Negative;
    }


    public class LinkCostInfo2
    {
        ushort linkIndex;
        CostInfoPositive costInfo;
    }


    public class CostInfoHandle
    {
        uint tileId;
        uint linkId;
        DirectionCode direction;
    }



    public class CostRecord
    {
        public TileCostInfo tileCostInfo; //親参照
        public ushort linkIndex; //リンク参照用
        public DirectionCode linkDirection;

        public int totalCostS; //経路始点～当該リンクまでのコスト
        public int totalCostD; //目的地側から計算した残りコスト

        public byte statusS = 0; //0:未開始　1:計算中　2:探索終了
        public byte statusD = 0; //0:未開始　1:計算中　2:探索終了  終点側
        //public bool isGoal = false; //以降は目的地側から計算済み

        public CostRecord back;
        public CostRecord next;

        public int TotalCost(bool isStartSide) => isStartSide ? totalCostS : totalCostD;

        public void SetTotalCost(bool isStartSide, int value)
        {
            if (isStartSide)
                totalCostS = value;
            else
                totalCostD = value;
        }
        
        public int Status(bool isStartSide) => isStartSide ? statusS : statusD;

        public void SetStatus(bool isStartSide, byte value)
        {
            if (isStartSide)
                statusS = value;
            else
                statusD = value;
        }

        public CostRecord BackCostRecord(bool isStartSide) => isStartSide ? back : next;

        public void SetBackCostRecord(bool isStartSide, CostRecord value)
        {
            if (isStartSide)
                back = value;
            else
                next = value;
        }

        public UInt64 LinkId => tileCostInfo.linkArray[linkIndex].Id;

        public CmnObj MapLink => tileCostInfo.linkArray[linkIndex];

        public uint TileId => tileCostInfo.tileId;

        public int Cost => (int)MapLink.Cost;

        public CmnObjHandle LinkHdl => tileCostInfo.linkArray[linkIndex].ToCmnObjHandle(tileCostInfo.tile);

        public CmnObjHandle DLinkHdl => tileCostInfo.linkArray[linkIndex].ToCmnObjHandle(tileCostInfo.tile, linkDirection);
    }


    public class TileCostInfo
    {
        public uint tileId;
        public CmnTile tile;
        public CmnObj[] linkArray;
        public CostRecord[][] costInfo; //始点方向リンク、終点方向リンク

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
            this.tileId = tile.TileId;
            this.tile = tile;
            linkArray = tile.GetObjArray(linkObjType) ?? Array.Empty<CmnObj>();

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
                costInfo[i][0].linkDirection = DirectionCode.Negative;
                costInfo[i][1].linkDirection = DirectionCode.Positive;
            }

            isLoaded = true;
            return 0;
        }

        //public int CalcTileDistance(uint startTileId, uint destTileId)
        //{
        //    DistFromStartTile = (float)tile.tileInfo.CalcTileDistance(tileId, startTileId);
        //    DistFromDestTile = (float)tile.tileInfo.CalcTileDistance(tileId, destTileId);

        //    //DistFromStartTile = (float)GisTileCode.S_CalcTileDistance(tileId, startTileId);
        //    //DistFromDestTile = (float)GisTileCode.S_CalcTileDistance(tileId, destTileId);

        //    return 0;
        //}


    }


    public class CostRecordManage
    {
        public int numElement { get; private set; } = 0;
        CostRecord[] unprocessed;

        public int minCost = 0;

        public CostRecordManage(int maxNum)
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

        public int GetMinCostIndex(bool isStartSide)
        {
            int tmpMinCost = int.MaxValue;
            int tmpMinIndex = -1;

            for (int i = 0; i < numElement; i++)
            {
                //まだバグ残っているかも
                //if (unprocessed[i].Status(isStartSide) == 2)
                //{
                //    Delete(i);
                //    if (i == numElement)
                //        break;
                //}
#if false
                if (unprocessed[i].TotalCost(isStartSide) < tmpMinCost)
                {
                    tmpMinCost = unprocessed[i].TotalCost(isStartSide);
                    tmpMinIndex = i;
                }
#else
                if (isStartSide)
                {
                    if (unprocessed[i].totalCostS < tmpMinCost)
                    {
                        tmpMinCost = unprocessed[i].totalCostS;
                        tmpMinIndex = i;
                    }
                }
                else
                {
                    if (unprocessed[i].totalCostD < tmpMinCost)
                    {
                        tmpMinCost = unprocessed[i].totalCostD;
                        tmpMinIndex = i;
                    }
                }
#endif
            }
            minCost = tmpMinCost;
            return tmpMinIndex;
        }

        public CostRecord GetCostRecord(int index)
        {
            return unprocessed[index];
        }

        public CostRecord GetMinCostRecord(bool isStartSide)
        {
            if (isStartSide)
            {
                return unprocessed.OrderBy(x => x.totalCostS).FirstOrDefault();
            }
            else
            {
                return unprocessed.OrderBy(x => x.totalCostD).FirstOrDefault();
            }
        }

    }

    public class CostRecordManageOD
    {
        CostRecordManage costMgrO;
        CostRecordManage costMgrD;

        public CostRecordManageOD(int maxNum)
        {
            costMgrO = new CostRecordManage(maxNum);
            costMgrD = new CostRecordManage(maxNum);
        }

        private CostRecordManage GetCostMgr(bool isStartSide)
        {
            if (isStartSide)
                return costMgrO;
            else
                return costMgrD;
        }

        public void Add(CostRecord costRecord, bool isStartSide)
        {
            GetCostMgr(isStartSide).Add(costRecord);
        }

        public void Delete(int index, bool isStartSide)
        {
            GetCostMgr(isStartSide).Delete(index);
        }

        public int GetMinCostIndex(bool isStartSide)
        {
            return GetCostMgr(isStartSide).GetMinCostIndex(isStartSide);
        }

        public CostRecord GetCostRecord(int index, bool isStartSide)
        {
            return GetCostMgr(isStartSide).GetCostRecord(index);
        }

        public CostRecord GetMinCostRecord(bool isStartSide)
        {
            int index = GetCostMgr(isStartSide).GetMinCostIndex(isStartSide);
            if (index < 0)
                return null;

            CostRecord ret = GetCostMgr(isStartSide).GetCostRecord(index);
            Delete(index, isStartSide);

            return ret;

        }


        public bool IsNextStartSide()
        {
            if (costMgrO.minCost < costMgrD.minCost)
                return true;
            else
                return false;
        }

        public int GetTotalElement()
        {
            return costMgrO.numElement + costMgrD.numElement;
        }
    }


    public class CostInfoManage //削除予定
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
                    //どこかにバグありそう
                    //if (unprocessedS[i].statusS == 2)
                    //{
                    //    Delete(i, isStartSide);
                    //    if (i == numElementS - 1)
                    //        break;
                    //}

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
                    //どこかにバグありそう
                    //if (unprocessedD[i].statusD == 2)
                    //{
                    //    Delete(i, isStartSide);
                    //    if (i == numElementD - 1)
                    //        break;
                    //}
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

        public bool IsNextStartSide()
        {
            if (minCostS < minCostD)
                return true;
            else
                return false;
        }

        public int GetTotalElement()
        {
            return numElementS + numElementD;
        }
    }


    public class Dykstra
    {
        public Dictionary<uint, TileCostInfo> dicTileCostInfo;
        CostRecordManageOD unprocessed;
        List<CostRecord> goalInfo;

        CostRecord finalRecord; //双方向ダイクストラの終了ポイント
        public List<CostRecord> routeResult;

        CmnMapMgr mapMgr;

        //汎用種別
        public RoutingMapType routingMapType;

        //探索初期の制限
        public byte rankDownRestrictSubType = 6;
        public double rankDownRestrictDistance = 8000;

        //性能測定用
       // public int[] logTickCountList;
        public int[] logUnprocessedCount;
        public int logMaxQueue = 0;
        public int logCalcCount = 0;
        public int numTileLoad = 0;

        public Dykstra(CmnMapMgr mapMgr)
        {
            this.mapMgr = mapMgr;
            dicTileCostInfo = new Dictionary<uint, TileCostInfo>();
            goalInfo = new List<CostRecord>();

            unprocessed = new CostRecordManageOD(10000);
            logUnprocessedCount = new int[1000000];
            //logTickCountList = new int[1000000];

            routingMapType = mapMgr.RoutingMapType;

            //roadNwObjType = mapMgr.GetMapObjType(ECmnMapContentType.Link) | mapMgr.GetMapObjType(ECmnMapContentType.Node);
            //roadGeometryObjType = mapMgr.GetMapObjType(ECmnMapContentType.LinkGeometry);
            //linkObjType = mapMgr.GetMapObjType(ECmnMapContentType.Link);
            //nextLinkRefType = mapMgr.GetMapRefType(ECmnMapRefType.NextLink);
            //backLinkRefType = mapMgr.GetMapRefType(ECmnMapRefType.BackLink);



        }


        /****** 設定 ******************************************************************************/
        public int SetStartCost(CmnObjHandle linkHdl, double offset, DirectionCode direction = DirectionCode.None)
        {
            //TileObjId start = new TileObjId(mapPos.tileId, mapPos.linkId);
            //MapLink sMapLink = mapMgr.SearchMapLink(start);

            //offsetが暫定
            CostRecord costRec;
            //順方向
            if (direction == DirectionCode.Positive || direction == DirectionCode.None)
            {
                costRec = GetLinkCostInfo(null, linkHdl.tile.TileId, linkHdl.obj.Index, DirectionCode.Positive);

                costRec.linkIndex = linkHdl.obj.Index;
                costRec.linkDirection = DirectionCode.Positive;
                costRec.totalCostS = (int)(linkHdl.obj.Cost * ( - offset / linkHdl.Length));
                //costRec.totalCostD = int.MaxValue;
                costRec.statusS = 1;
                unprocessed.Add(costRec, true);
            }

            //逆方向
            if (direction == DirectionCode.Negative || direction == DirectionCode.None)
            {
                costRec = GetLinkCostInfo(null, linkHdl.tile.TileId, linkHdl.obj.Index, DirectionCode.Negative);

                costRec.linkIndex = linkHdl.obj.Index;
                costRec.linkDirection = DirectionCode.Negative;
                costRec.totalCostS = (int)(linkHdl.obj.Cost * (offset / linkHdl.Length - 1.0));
                //costRec.totalCostD = int.MaxValue;
                costRec.statusS = 1;
                unprocessed.Add(costRec, true);
            }

            return 0;
        }

        public int SetDestination(CmnObjHandle linkHdl, double offset, DirectionCode direction = DirectionCode.None)
        {
            //offsetが暫定
            CostRecord costRec;
            //順方向
            if (direction == DirectionCode.Positive || direction == DirectionCode.None)
            {
                costRec = GetLinkCostInfo(null, linkHdl.tile.TileId, linkHdl.obj.Index, DirectionCode.Positive);

                costRec.linkIndex = linkHdl.obj.Index;
                costRec.linkDirection = DirectionCode.Positive;
                //costRec.totalCostS = int.MaxValue;
                //costRec.totalCostD = offset;
                costRec.totalCostD = (int)(linkHdl.obj.Cost * (offset / linkHdl.Length - 1.0));
                costRec.statusD = 1;
                unprocessed.Add(costRec, false);

                //costRec.isGoal = true;
                goalInfo.Add(costRec);
            }
            //逆方向
            if (direction == DirectionCode.Negative || direction == DirectionCode.None)
            {
                costRec = GetLinkCostInfo(null, linkHdl.tile.TileId, linkHdl.obj.Index, DirectionCode.Negative);

                costRec.linkIndex = linkHdl.obj.Index;
                costRec.linkDirection = DirectionCode.Negative;
                //costRec.totalCostS = int.MaxValue;
                costRec.totalCostD = (int)(linkHdl.obj.Cost * ( -offset / linkHdl.Length));
                //costRec.totalCostD = (int)linkHdl.obj.Length - offset;
                costRec.statusD = 1;
                unprocessed.Add(costRec, false);

                //costRec.isGoal = true;
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

            tileCostInfo.DistFromStartTile = (float)mapMgr.tileApi.CalcTileDistance(tileId, tileIdS);
            tileCostInfo.DistFromDestTile = (float)mapMgr.tileApi.CalcTileDistance(tileId, tileIdE);

            //tileCostInfo.CalcTileDistance(tileIdS, tileIdE);
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

            mapMgr.LoadTile(tileId, routingMapType.roadNwObjTypeList, tmpTileCostInfo.maxUsableRoadType);
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

        public CostRecord GetLinkCostInfo(CostRecord currentInfo, uint tileId, int linkIndex, DirectionCode linkDirection)
        {
            if (currentInfo != null && currentInfo.tileCostInfo.tileId == tileId)
            {
                return currentInfo.tileCostInfo.costInfo[linkIndex][(int)linkDirection];
            }

            if (!dicTileCostInfo.ContainsKey(tileId))
                return null;

            return dicTileCostInfo[tileId].costInfo[linkIndex][(int)linkDirection];

        }

        public CostRecord GetLinkCostInfo(CostRecord currentInfo, CmnObjHandle linkRef)
        {
            return GetLinkCostInfo(currentInfo, linkRef.tile.TileId, linkRef.obj.Index, linkRef.direction);
        }




        /****** 経路計算メイン ******************************************************************************/

        public ResultCode CalcRouteStep()
        {
            //処理側決定
            bool isStartSide = unprocessed.IsNextStartSide();
            CostRecord currentCostInfo = unprocessed.GetMinCostRecord(isStartSide);

            //異常
            if (currentCostInfo == null)
            {
                Console.WriteLine($"[{Environment.TickCount / 1000.0:F3}] All Calculation Finished! Destination Not Found");
                return ResultCode.NotFound;
            }

            //探索成功
            if ((!isStartSide && currentCostInfo.next?.statusS == 2) || (isStartSide && currentCostInfo.back?.statusD == 2))
            {
                Console.WriteLine($"[{Environment.TickCount / 1000.0:F3}] Goal Found !! (CalcCount = {logCalcCount}, totalCost = {currentCostInfo.totalCostS})");

                if (isStartSide)
                    finalRecord = currentCostInfo.back;
                else
                    finalRecord = currentCostInfo.next;
                return ResultCode.Success;
            }

            //処理済みデータ　⇒キュー取り出し時に削除
            if (currentCostInfo.Status(isStartSide) == 2)
            {
                //unprocessed.Delete(minIndex, isStartSide);
                return ResultCode.Continue;
            }


            CmnObjHandle currentDLinkHdl = currentCostInfo.DLinkHdl;

            List<CmnObjHdlRef> objHdlRefList;
            //List<uint> noDataTileIdList;

            //接続リンク取得
            while (true)
            {
                //if (isStartSide)
                //{
                //    //接続リンク。向きは自動判別
                //    objHdlRefList = mapMgr.SearchRefObject(currentDLinkHdl, routingMapType.nextLinkRefType);
                //}
                //else
                //{
                //    objHdlRefList = mapMgr.SearchRefObject(currentDLinkHdl, routingMapType.backLinkRefType);
                //}

                //接続リンク。向きは自動判別
                objHdlRefList = mapMgr.SearchRefObject(currentDLinkHdl, isStartSide ? routingMapType.nextLinkRefType : routingMapType.backLinkRefType);

                //初期設定の読み込み可能範囲内タイル
                List<uint> noDataTileIdList = objHdlRefList
                    .Select(x => (x.objHdl?.tile?.TileId ?? x.nextRef?.key?.tileId) ?? 0xffffffff)
                    .Where(x => x != 0xffffffff && dicTileCostInfo.ContainsKey(x) && !dicTileCostInfo[x].isLoaded)
                    .ToList();

                //不足タイルがあれば読み込み
                if (noDataTileIdList.Count > 0)
                    noDataTileIdList.ForEach(x => AddTileInfo(x));
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
                if(nextLinkRef.direction == DirectionCode.None)
                {
                    nextLinkRef.direction = nextLinkRef.obj.GetDirection(currentCostInfo.MapLink, isStartSide);
                }

                //探索除外
                if (IsCalcSkip(currentCostInfo, nextLinkRef))
                    continue;


                CostRecord nextCostInfo = GetLinkCostInfo(currentCostInfo, nextLinkRef);

                //MapMgr共有などで、使用可能タイル以外のハンドル（経路のタイル管理にない）が取得できてしまうケースがある
                if (nextCostInfo == null)
                    continue;

                int nextTotalCost;

                //ゴールフラグの場合は、残コストを足す。足すけど保存NG？ゴール側statusを見るべき？
                //双方向ダイクストラでは不要


#if true //速度が課題
                //並走レーンをいずれ考慮する場合は、自コストは除外＋車線変更コスト
                nextTotalCost = currentCostInfo.TotalCost(isStartSide) + currentDLinkHdl.obj.Cost;

                //コストを足した値を、接続リンクの累積コストを見て、より小さければ上書き
                if (nextCostInfo.Status(isStartSide) == 0 || nextTotalCost < nextCostInfo.TotalCost(isStartSide))
                {
                    nextCostInfo.SetTotalCost(isStartSide, nextTotalCost);
                    nextCostInfo.SetStatus(isStartSide, 1);
                    nextCostInfo.SetBackCostRecord(isStartSide, currentCostInfo);
                    unprocessed.Add(nextCostInfo, isStartSide);
                }
                //リンクの探索ステータス更新
                currentCostInfo.SetStatus(isStartSide, 2);

#else
                if (isStartSide)
                {
                    //並走レーンをいずれ考慮する場合は、自コストは除外＋車線変更コスト
                    nextTotalCost = currentCostInfo.totalCostS + currentDLinkHdl.obj.Cost;

                    //コストを足した値を、接続リンクの累積コストを見て、より小さければ上書き
                    if (nextCostInfo.statusS == 0 || nextTotalCost < nextCostInfo.totalCostS)
                    {
                        nextCostInfo.totalCostS = nextTotalCost;
                        nextCostInfo.statusS = 1;
                        nextCostInfo.back = currentCostInfo;
                        unprocessed.Add(nextCostInfo, isStartSide);
                    }
                    //リンクの探索ステータス更新
                    currentCostInfo.statusS = 2;

                }
                else //目的地側
                {
                    nextTotalCost = currentCostInfo.totalCostD + currentDLinkHdl.obj.Cost;

                    //コストを足した値を、接続リンクの累積コストを見て、より小さければ上書き
                    if (nextCostInfo.statusD == 0 || nextTotalCost < nextCostInfo.totalCostD)
                    {
                        nextCostInfo.totalCostD = nextTotalCost;
                        nextCostInfo.statusD = 1;
                        nextCostInfo.next = currentCostInfo;
                        unprocessed.Add(nextCostInfo, isStartSide);
                    }
                    //リンクの探索ステータス更新
                    currentCostInfo.statusD = 2;

                }
#endif
            }

            //キュー取り出し時に削除
            //unprocessed.Delete(minIndex, isStartSide);

            return ResultCode.Continue;
        }

        public virtual bool IsCalcSkip(CostRecord currentCostInfo, CmnObjHandle nextLinkRef)
        {
            CmnObjHandle currentDLinkHdl = currentCostInfo.DLinkHdl;

            //Uターンリンク
            if (nextLinkRef.IsEqualTo(currentDLinkHdl))
                return true;

            //タイルに応じた使用可能道路種別でない
            if (nextLinkRef.SubType > currentCostInfo.tileCostInfo.maxUsableRoadType)
                return true;

            //一方通行逆走
            if (nextLinkRef.IsOneway && nextLinkRef.Oneway != nextLinkRef.direction)
                return true;

            //スタート付近で道路種別が下がる移動
            if (nextLinkRef.SubType >= rankDownRestrictSubType
                && currentDLinkHdl.SubType < nextLinkRef.SubType
                && currentCostInfo.tileCostInfo.DistFromDestTile > rankDownRestrictDistance)
                return true;

            return false;
        }

        public ResultCode CalcRoute()
        {
            ResultCode ret;

            int pastTickCount = Environment.TickCount;
            int nowTickCount;
            //計算
            while (true)
            {
                ret = CalcRouteStep();

                nowTickCount = Environment.TickCount;
                //logTickCountList[logCalcCount] = nowTickCount - pastTickCount;
                pastTickCount = nowTickCount;

                logUnprocessedCount[logCalcCount] = unprocessed.GetTotalElement();

                if (unprocessed.GetTotalElement() > logMaxQueue)
                    logMaxQueue = unprocessed.GetTotalElement();

                logCalcCount++;

                if (ret != ResultCode.Continue)
                    break;
            }
            return ret;
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

            tmpCostRecord = finalRecord.next;
            //if (tmpCostRecord.next != null) //重複登録排除
            //{
            //    tmpCostRecord = tmpCostRecord.next;
            //}

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
            foreach(var x in routeResult.Distinct())
            {
                mapMgr.LoadTile(x.tileCostInfo.tileId, new List<uint> { routingMapType.roadGeometryObjType }, x.tileCostInfo.maxUsableRoadType);
            }

            //routeResult.ForEach(x =>
            //{
            //    mapMgr.LoadTile(x.tileCostInfo.tileId, routingMapType.roadGeometryObjType, x.tileCostInfo.maxUsableRoadType);
            //});

            List<LatLon> retList = new List<LatLon>();
            retList.Add(routeResult[0].DLinkHdl.DirGeometry[0]);
            retList.AddRange(routeResult.Select(x => x.DLinkHdl.DirGeometry.Skip(1)).SelectMany(x => x));
            return retList.ToArray();

        }

        public void PrintResult()
        {

            Console.WriteLine($" TileId\tLinkId\tIndex\tDirection\tSubType\tCost\ttotalCostS\ttotalCostD");
            routeResult.ForEach(x =>
            {
                Console.WriteLine($" {x.TileId}\t{x.LinkId}\t{x.linkIndex}\t{x.linkDirection}\t{x.DLinkHdl.obj.SubType}\t{x.Cost}\t{x.totalCostS}\t{x.totalCostD}");
            });
        }


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


    }

    public class RoutingMapType
    {
        public UInt32 roadNwObjType; //探索に必要な地図コンテンツ。リンクだけとは限らない
        public List<UInt32> roadNwObjTypeList; //探索に必要な地図コンテンツ。リンクだけとは限らない

        //public ReqType[] roadNwObjReqType;
        public CmnObjFilter roadNwObjFilter;
        public UInt32 roadGeometryObjType; //結果表示用
        public UInt32 linkObjType; //リンク（コスト・方向あり）

        public Int32 nextLinkRefType; //次リンクの参照タイプ
        public Int32 backLinkRefType; //前リンクの参照タイプ
    }

    public class RouteResult
    {
        public ResultCode resultCode;
        public List<CostRecord> route;

        public RouteResult(ResultCode resultCode, List<CostRecord> routeResult)
        {
            this.resultCode = resultCode;
            this.route = routeResult;
        }
    }

    public enum ResultCode
    {
        Success = 0,
        Continue,
        NotFound,
        CalcError
    }

    /****** 経路計算マネージャ ******************************************************************************/


    public class CmnRouteMgr
    {

        protected CmnMapMgr mapMgr;

        public LatLon orgLatLon;
        public LatLon dstLatLon;

        public CmnObjHandle orgHdl;
        public CmnObjHandle dstHdl;

        //経路計算用メモリ
        public Dykstra dykstra;

        //結果格納
        public List<CmnObjHandle> routeHdlList;

        public CmnRouteMgr() { }

        public CmnRouteMgr(CmnMapMgr mapMgr)
        {
            dykstra = new Dykstra(mapMgr);
            this.mapMgr = mapMgr;
        }

        public void SetMapMgr(CmnMapMgr mapMgr)
        {
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

        public virtual int Prepare(bool allCache)
        {
            //探索レベルを決める？
            //CalcSearchLevel();

            if (orgLatLon == null || dstLatLon == null)
                return -1;

            //始点終点ハンドル取得

            RoutingMapType routingMapType = mapMgr.RoutingMapType;

            uint startTileId = mapMgr.tileApi.CalcTileId(orgLatLon);
            IEnumerable<uint> tileIdListS = mapMgr.tileApi.CalcTileIdAround(orgLatLon, 1000, mapMgr.tileApi.DefaultLevel);
            tileIdListS.ForEach(x => mapMgr.LoadTile(x, null));
            //orgHdl = mapMgr.SearchObj(orgLatLon, 1, false, routingMapType.roadNwObjType);
            //orgHdl = mapMgr.SearchObj(orgLatLon, routingMapType.roadNwObjReqType,1);
            orgHdl = mapMgr.SearchObj(orgLatLon, routingMapType.roadNwObjFilter, 1, -1);
            PolyLinePos orgLinkPos = LatLon.CalcNearestPoint(orgLatLon, orgHdl.Geometry);
            
            uint destTileId = mapMgr.tileApi.CalcTileId(dstLatLon);
            IEnumerable<uint> tileIdListD = mapMgr.tileApi.CalcTileIdAround(dstLatLon, 1000, mapMgr.tileApi.DefaultLevel);
            tileIdListD.ForEach(x => mapMgr.LoadTile(x, null));        
            //dstHdl = mapMgr.SearchObj(dstLatLon, 1, false, routingMapType.roadNwObjType);
            //dstHdl = mapMgr.SearchObj(dstLatLon, routingMapType.roadNwObjReqType, 1);
            dstHdl = mapMgr.SearchObj(dstLatLon, routingMapType.roadNwObjFilter, 1, -1);
            PolyLinePos dstLinkPos = LatLon.CalcNearestPoint(dstLatLon, dstHdl.Geometry);

            if (orgHdl == null || dstHdl == null)
                return -1;

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
        public virtual RouteResult CalcRoute()
        {
            //計算
            ResultCode ret = dykstra.CalcRoute();

            //結果出力
            List<CostRecord> result = dykstra.MakeRouteInfo();

            return new RouteResult(ret, result);
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
