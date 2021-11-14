﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akichko.libGis
{
    public abstract class CmnMapAccess : ICmnMapAccess
    {
        public abstract bool IsConnected { get; }

        public abstract int ConnectMap(string connectStr);

        public abstract int DisconnectMap();

        public abstract List<uint> GetMapContentTypeList();

        public abstract List<uint> GetMapTileIdList();

        public virtual TimeStampRange GetTimeStampRange() => null;

        public abstract IEnumerable<CmnObjGroup> LoadObjGroup(uint tileId, uint type, ushort subType = ushort.MaxValue);

        public abstract List<CmnObjGroup> LoadObjGroup(uint tileId, IEnumerable<ObjReqType> reqTypes);

        public virtual async Task<IEnumerable<CmnObjGroup>> LoadObjGroupAsync(uint tileId, UInt32 type, UInt16 subType = 0xFFFF)
        {
            Task<IEnumerable<CmnObjGroup>> taskRet = Task.Run(() => LoadObjGroup(tileId, type, subType));
            IEnumerable<CmnObjGroup> ret = await taskRet.ConfigureAwait(false);
            return ret;
        }

        public virtual async Task<IEnumerable<CmnObjGroup>> LoadObjGroupAsync(uint tileId, IEnumerable<ObjReqType> reqTypes)
        {
            var tasks = reqTypes.Select(reqType => Task.Run(() => LoadObjGroup(tileId, reqType.type, reqType.maxSubType)));
            var tmp = await Task.WhenAll(tasks).ConfigureAwait(false);
            List<CmnObjGroup> ret = tmp.Where(x => x != null).SelectMany(x => x).ToList();

            return ret;
        }
    }
}
