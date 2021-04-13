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



        public int LoadTile(uint tileId, bool multiContents = true, UInt16 reqType = 0xFFFF, UInt16 reqMaxSubType = 0xFFFF)
        {
            if (!IsConnected) return -1;

            CmnTile tmpTile;
            bool isNew = false;

            if (tileDic.ContainsKey(tileId))
            {
                tmpTile = tileDic[tileId];  
            }
            else
            {
                tmpTile = mal.CreateTile(tileId);
                isNew = true;
            }

            List<UInt16> mapObjTypeList;
            if (multiContents)
                mapObjTypeList = mal.GetMapContentTypeList();
            else
            {
                mapObjTypeList = new List<UInt16>();
                mapObjTypeList.Add(reqType);
            }

            foreach (UInt16 mapObjType in mapObjTypeList)
            {
                if ((reqType & mapObjType) == mapObjType)
                {
                    //既存データチェック
                    CmnObjGroup currentMapContents = tmpTile.GetObjGroup(mapObjType);
                    if (currentMapContents != null && currentMapContents.loadedSubType >= reqMaxSubType)
                    {
                        continue;
                    }
                    tmpTile.UpdateObjGroup(mapObjType, mal.LoadObjGroup(tileId, mapObjType, reqMaxSubType));

                }
            }

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


        //オブジェクト検索メソッド ***********************************************

        public CmnObjHandle SearchObj(uint tileId, UInt16 objType, UInt64 objId)
        {
            CmnTile tmpTile = SearchTile(tileId);
            if (tmpTile == null)
                return null;
            CmnObj tmpObj = tmpTile.GetObj(objType, objId);
            if (tmpObj == null)
                return null;

            return new CmnObjHandle(tmpTile, tmpObj);

        }

        public CmnObjHandle SearchObj(uint tileId, UInt16 objType, UInt16 objIndex)
        {
            CmnTile tmpTile = SearchTile(tileId);
            if (tmpTile == null)
                return null;
            CmnObj tmpObj = tmpTile.GetObj(objType, objIndex);
            if (tmpObj == null)
                return null;

            return new CmnObjHandle(tmpTile, tmpObj);

        }

        public CmnObjHandle SearchObj(LatLon latlon, bool multiContents = true, UInt16 objType = 0xFFFF, UInt16 maxSubType = 0xFFFF)
        {
            uint tileId = tileApi.CalcTileId(latlon);

            List<CmnTile> tileList = SearchTiles(tileId, 1, 1);

            CmnObjHdlDistance nearestObj = tileList
                .Select(x => x?.GetNearestObj(latlon, objType, maxSubType))
                .OrderBy(x => x?.distance)
                .FirstOrDefault();

            if (nearestObj == null)
                return null;

            return (CmnObjHandle)nearestObj;
            
        }

        public List<uint> GetMapTileIdList()
        {
            if (!IsConnected) return null;
            return mal.GetMapTileIdList();
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

       // CmnTile LoadTile(uint tileId, UInt16 type = 0xFFFF, UInt16 subType = 0xFFFF);
        CmnObjGroup LoadObjGroup(uint tileId, UInt16 type, UInt16 subType = 0xFFFF);
    }







}
