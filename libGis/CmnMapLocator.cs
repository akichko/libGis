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


    public class CmnLocator
    {   
        protected CmnMapMgr mapMgr;
        LocateMapType locateMapType;
        public double speedKpH = 30; //[km/h]
        public double direction = 10; //[km/h]
        LocatorMode mode;


        //CmnObjHandle[] routeLinkArray;
        public List<CmnObjHandle> routeLinks;

        public LatLon[] routeGeometry;
        public double routeLength;
        public double distanceFromOrg = 0;
        public LatLon latlonOnRoute;

        public Location currentLoc;
        public bool isFinishedRouteRun = false;

        public CmnLocator() { }

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

        /* 経路入力 */
        public void SetRoute( LatLon[] routeGeometry, List<CmnObjHandle> route = null)
        {
            routeLinks = route;
            this.routeGeometry = routeGeometry;
            routeLength = LatLon.CalcLength(routeGeometry);
            distanceFromOrg = 0;
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

            IEnumerable<CmnObjHdlDistance> objHdlDsts = mapMgr.SearchObjsAround(latlon, 20, locateMapType.roadNwObjFilter, null)
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


        public List<MapPosition> CalcMapPositionOnRoute(double moveTimeMs)
        {
            if (routeLinks == null && routeGeometry == null)
                return null;
#if false
            //現在リンク
            //var link = routeLinks.Where(x => x.ObjId == CurrentLoc?.linkId).FirstOrDefault();

            ////該当リンクなし⇒経路先頭
            //if (link == null)
            //{
            //    MapPosition ret = new MapPosition(
            //        new PolyLinePos(routeLinks[0].DirGeometry[0], 0, 0),
            //        routeLinks[0],
            //        1.0);

            //    return new List<MapPosition> { ret };
            //}


            //現在リンク始点からの移動距離計算
            //double moveLengthM = CurrentLoc.speedKpH * 1000 / 3600.0 * moveTimeMs / 1000.0
            //    + CurrentLoc.mapPositions[0].linePos.shapeOffset;
#endif

            double moveLengthM = currentLoc?.speedKpH * 1000 / 3600.0 * moveTimeMs / 1000.0 ?? 0;
            PolyLinePos routeLinePos;
            distanceFromOrg += moveLengthM;
            if (distanceFromOrg > routeLength)
            {
                latlonOnRoute = routeGeometry[routeGeometry.Length - 1];
                routeLinePos = new PolyLinePos(latlonOnRoute, 0, routeLength);
            }
            else
            {
                routeLinePos = LatLon.CalcOffsetLinkePosAlongPolyline(routeGeometry, distanceFromOrg);
            }
#if false
            //経路に沿って移動
            //int index = routeLinks.IndexOf(link);
            //while (moveLengthM > 0)
            //{
            //    if (moveLengthM > routeLinks[index].Length)
            //    {
            //        moveLengthM -= routeLinks[index].Length;

            //        //経路終点
            //        if (index == routeLinks.Count - 1)
            //        {
            //            break;
            //            //int shapeNum = routeLinks[index].DirGeometry.Length;
            //            //MapPosition ret = new MapPosition(
            //            //    new PolyLinePos(routeLinks[index].DirGeometry[shapeNum - 1], shapeNum, routeLinks[index].Length),
            //            //    routeLinks[index],
            //            //    1.0);

            //            //return new List<MapPosition> { ret };
            //        }
            //        index++;

            //    }
            //    else //リンク内位置計算
            //    {
            //        break;
            //        //PolyLinePos linePos = LatLon.CalcOffsetLinkePosAlongPolyline(routeLinks[index].DirGeometry, moveLengthM);
            //        //MapPosition ret = new MapPosition(linePos, routeLinks[index], 1.0);

            //        //return new List<MapPosition> { ret };
            //    }
            //}
#endif
            MapPosition ret = new MapPosition(routeLinePos, routeLinks?[0], 1.0);
            return new List<MapPosition> { ret };

        }

        public Location UpdateCurrentLoc(double moveTime)
        {
            List<MapPosition> mapPosList = CalcMapPositionOnRoute(moveTime);
            currentLoc = new Location(mapPosList, mapPosList?[0].linePos.latLon, speedKpH, direction);

            if (routeGeometry != null && (routeGeometry[routeGeometry.Length - 1] == mapPosList?[0].linePos.latLon))
            {
                isFinishedRouteRun = true;
            }
            else
            {
                isFinishedRouteRun = false;
            }

            return currentLoc;
        }

    }


    public abstract class CmnLocatorObserver : IObserver<Location>
    {
        private IDisposable cancellation;
        public bool IsSubscribing { get; private set; } = false;

        public virtual void Subscribe(IObservable<Location> provider)
        {
            cancellation = provider.Subscribe(this);
            IsSubscribing = true;
        }

        public virtual void Unsubscribe()
        {
            if (!IsSubscribing)
                return;
            cancellation.Dispose();
            IsSubscribing = false;
        }

        public abstract void OnCompleted();

        public abstract void OnError(Exception error);

        public abstract void OnNext(Location value);
    }

    public class LocatorTask : IObservable<Location>
    {
        CmnLocator locator;
        public int calcInterval; //[ms]

        Location location;

        public LatLon Latlon => location?.latlon;
        private LatLon[] routeGeometry;

        private Timer timer;
        private bool isPlaying = false;
        private List<IObserver<Location>> observers = new List<IObserver<Location>>();
        private SemaphoreSlim semaphore;

        public LocatorTask(CmnLocator locator, int calcInterval)
        {
            this.locator = locator;
            this.calcInterval = calcInterval;

            semaphore = new SemaphoreSlim(1);
            timer = new Timer(new TimerCallback(CyclicTimerCallback));
        }

        public void SetRoute(LatLon[] routeGeometry)
        {
            this.routeGeometry = routeGeometry;
            locator.SetRoute(routeGeometry);
        }

        public void SetSpeedKpH(double speed)
        {
            locator.speedKpH = speed;
        }

        /* タイマー **********************************************************************/
        public int LoopStart()
        {
            // タイマーをすぐに1秒間隔で開始
            timer.Change(0, calcInterval);
            isPlaying = true;
            return 0;
        }

        public int LoopPause()
        {
            if (isPlaying)
            {
                // タイマーを停止
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                isPlaying = false;
            }
            else
            {
                // タイマー開始
                timer.Change(0, calcInterval);
                isPlaying = true;
            }

            return 0;
        }

        public int LoopStop()
        {
            // タイマーを停止
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            isPlaying = false;

            return 0;
        }

        public void CyclicTimerCallback(object args)
        {
            semaphore.Wait();
            try
            {
                Location newLoc = locator.UpdateCurrentLoc(calcInterval);

                if (routeGeometry != null && locator.isFinishedRouteRun)
                {
                    Complete();
                }

                if (newLoc?.latlon != location?.latlon)
                {
                    Publish(newLoc);
                }

                location = newLoc;
            }
            catch(Exception e)
            {
                throw new NotImplementedException();
                //LoopStop();
            }
            semaphore.Release();
        }

        /* observer機能 **********************************************************************/
        public IDisposable Subscribe(IObserver<Location> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);

            //購読解除用のクラスをIDisposableとして返す
            return new Unsubscriber(observers, observer);
        }

        public void Publish(Location location)
        {
            foreach (var observer in new List<IObserver<Location>>(observers))
            {
                observer.OnNext(location);
            }
        }

        public void Complete()
        {
            foreach (var observer in new List<IObserver<Location>>(observers))
            {
                observer.OnCompleted();
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
