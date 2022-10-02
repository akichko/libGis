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
using System.IO;

namespace Akichko.libGis
{

    public class MapPosition
    {
        public PolyLinePos linePos;
        public CmnObjHandle objHdl;
        public double reliability;

        public MapPosition(PolyLinePos linePos, CmnObjHandle objHdl, double reliability = 0)
        {
            this.linePos = linePos;
            this.objHdl = objHdl;
            this.reliability = reliability;
        }
    }

    public class Location
    {
        public List<MapPosition> mapPositions;
        public LatLon latlon;
        public double speedKpH; //[km/h]
        public double direction;

        public ulong linkId => mapPositions?.FirstOrDefault()?.objHdl.ObjId ?? 0;

        public Location(List<MapPosition> mapPositions, LatLon latlon, double speedKpH, double direction)
        {
            this.mapPositions = mapPositions;
            this.latlon = latlon;
            this.speedKpH = speedKpH;
            this.direction = direction;
        }
    }

    public class CmnLocator : IObservable<Location>
    {   
        protected CmnMapMgr mapMgr;
        LocateMapType locateMapType;
        public double speedKpH = 30; //[km/h]
        public double direction = 10; //[km/h]
        LocatorMode mode;

        //CmnObjHandle[] routeLinkArray;
        public List<CmnObjHandle> routeLinks;

        private List<IObserver<Location>> observers = new List<IObserver<Location>>();

        private Location _currentLoc;
        public Location CurrentLoc
        {
            get { return _currentLoc; }
            set
            {
                if (_currentLoc?.latlon != value?.latlon)
                {
                    _currentLoc = value;
                    Publish();
                }
                _currentLoc = value;
            }
        }

        public CmnLocator(CmnMapMgr mapMgr, LocateMapType locateMapType)
        {
            this.mapMgr = mapMgr;
            this.locateMapType = locateMapType;
        }

        public int Connect()
        {

            return 0;
        }

        public int UnloadTile(LatLon latlon)
        {
            return -1;
        }

        /* 位置計算 **********************************************************************/

        public List<MapPosition> CalcMapPosition(LatLon latlon)
        {
            //Calc tile needed

            uint currentTileId = mapMgr.tileApi.CalcTileId(latlon);
            IEnumerable<uint> tileIdList = mapMgr.tileApi.CalcTileIdAround(latlon, 1000, mapMgr.tileApi.DefaultLevel);

            //LoadTile

            tileIdList.ForEach(x => mapMgr.LoadTile(x, null));

            //Get around link

            IEnumerable<CmnObjHdlDistance> objHdlDsts = mapMgr.SearchObjsAround(latlon, locateMapType.roadNwObjFilter, 20)
                .OrderBy(x => x.distance);

            objHdlDsts.ForEach(x => { if (x.distance == 0) x.distance = 0.001; });
            double distanceSum = objHdlDsts.Sum(x => 1 / (x.distance * x.distance));

            //Calc link pos
            var ret = objHdlDsts
                .Select(x => new MapPosition(LatLon.CalcNearestPoint(latlon,
                                            x.objHdl.Geometry),
                                            x.objHdl,
                                            (1 / (x.distance * x.distance)) / distanceSum))
                .ToList();

            return ret;
        }

        public List<MapPosition> CalcMapPositions(LatLon[] latlon)
        {
            throw new NotImplementedException();
        }

        public List<MapPosition> CalcMapPositions(LatLon latlon)
        {
            throw new NotImplementedException();
        }

        public List<MapPosition> CalcMapPositions(LatLon latlon, double moveTime)
        {
            throw new NotImplementedException();
        }

        public List<MapPosition> CalcMapPositionOnRoute(double moveTimeMs)
        {
            if (routeLinks == null)
                return null;

            //現在リンク
            var link = routeLinks.Where(x => x.ObjId == CurrentLoc?.linkId).FirstOrDefault();

            //該当リンクなし⇒経路先頭
            if (link == null)
            {
                MapPosition ret = new MapPosition(
                    new PolyLinePos(routeLinks[0].DirGeometry[0], 0, 0),
                    routeLinks[0],
                    1.0);

                return new List<MapPosition> { ret };
            }

            int index = routeLinks.IndexOf(link);

            //現在リンク始点からの移動距離計算
            double moveLengthM = CurrentLoc.speedKpH * 1000 / 3600.0 * moveTimeMs / 1000.0
                + CurrentLoc.mapPositions[0].linePos.shapeOffset;

            //経路に沿って移動
            while (moveLengthM > 0)
            {
                if(moveLengthM > routeLinks[index].Length)
                {
                    moveLengthM -= routeLinks[index].Length;

                    //経路終点
                    if(index == routeLinks.Count - 1)
                    {
                        int shapeNum = routeLinks[index].DirGeometry.Length;
                        MapPosition ret = new MapPosition(
                            new PolyLinePos(routeLinks[index].DirGeometry[shapeNum-1], shapeNum, routeLinks[index].Length),
                            routeLinks[index],
                            1.0);

                        return new List<MapPosition> { ret };
                    }
                    index++;

                }
                else //リンク内位置計算
                {
                    PolyLinePos linePos = LatLon.CalcOffsetLinkePosAlongPolyline(routeLinks[index].DirGeometry, moveLengthM);
                    MapPosition ret = new MapPosition(linePos, routeLinks[index], 1.0);

                    return new List<MapPosition> { ret };
                }
            }

            throw new NotImplementedException();
        }

        public void UpdateCurrentLoc(double moveTime)
        {
            List<MapPosition> mapPosList = CalcMapPositionOnRoute(moveTime);
            CurrentLoc = new Location(mapPosList, mapPosList[0].linePos.latLon, speedKpH, direction);

        }

        /* observer機能 **********************************************************************/
        public IDisposable Subscribe(IObserver<Location> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);

            //購読解除用のクラスをIDisposableとして返す
            return new Unsubscriber(observers, observer);
        }

        public void Publish()
        {
            foreach (var observer in observers)
            {
                observer.OnNext(CurrentLoc);
            }
        } 
        
        //購読解除用内部クラス
        private class Unsubscriber : IDisposable
        {
            //発行先リスト
            private List<IObserver<Location>> observers;

            //DisposeされたときにRemoveするIObserver<int>
            private IObserver<Location> observer;

            public Unsubscriber(List<IObserver<Location>> observers, IObserver<Location> observer)
            {
                this.observers = observers;
                this.observer = observer;
            }

            public void Dispose()
            {
                //Disposeされたら発行先リストから対象の発行先を削除する
                this.observers.Remove(observer);
            }
        }

    }

    public class LocatorExe
    {
        CmnLocator locator;
        Timer timer;
        int count;
        public int calcInterval = 1000; //[ms]

        public LocatorExe(CmnLocator locator, int calcInterval)
        {
            this.locator = locator;
            this.calcInterval = calcInterval;
            
            timer = new Timer(new TimerCallback(ThreadingTimerCallback));
        }

        public int LoopStart()
        {
            // タイマーをすぐに1秒間隔で開始
            timer.Change(0, calcInterval);

            return 0;
        }

        public int LoopStop()
        {
            // タイマーを停止
            timer.Change(Timeout.Infinite, Timeout.Infinite);

            return 0;
        }

        //コールバック
        public void ThreadingTimerCallback(object args)
        {
            count++;
            Console.WriteLine("{count}");

            //locator.CalcMapPositionOnRoute(1000);
            locator.UpdateCurrentLoc(calcInterval);

        }
    }

    public class LocateMapType
    {
        public List<UInt32> roadNwObjTypeList; //位置推定に必要な地図コンテンツ。リンクだけとは限らない

        public CmnObjFilter roadNwObjFilter;
        public UInt32 roadGeometryObjType; //結果表示用
        public UInt32 linkObjType; //リンク（コスト・方向あり）

        public Int32 nextLinkRefType; //次リンクの参照タイプ
        public Int32 backLinkRefType; //前リンクの参照タイプ

    }

    public enum LocatorMode
    {
        AlongRoute,
        Random
    }
}
