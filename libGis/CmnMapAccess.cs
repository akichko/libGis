using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akichko.libGis
{
    public abstract class CmnMapAccess : ICmnMapAccess
    {
        public bool IsConnected => throw new NotImplementedException();

        public int ConnectMap(string connectStr)
        {
            throw new NotImplementedException();
        }

        public int DisconnectMap()
        {
            throw new NotImplementedException();
        }

        public List<uint> GetMapContentTypeList()
        {
            throw new NotImplementedException();
        }

        public List<uint> GetMapTileIdList()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CmnObjGroup> LoadObjGroup(uint tileId, uint type, ushort subType = ushort.MaxValue)
        {
            throw new NotImplementedException();
        }


        public async Task<IEnumerable<CmnObjGroup>> LoadObjGroupAsync(uint tileId, UInt32 type, UInt16 subType = 0xFFFF)
        {
            Task<IEnumerable<CmnObjGroup>> taskRet = Task.Run(() => LoadObjGroup(tileId, type, subType));
            IEnumerable<CmnObjGroup> ret = await taskRet.ConfigureAwait(false);
            return ret;
        }

    }
}
