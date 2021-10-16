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

        public List<CmnObjGroup> LoadObjGroup(uint tileId, uint type, ushort subType = ushort.MaxValue)
        {
            throw new NotImplementedException();
        }

        public List<CmnObjGroup> LoadObjGroup2(uint tileId, uint type, ushort subType = ushort.MaxValue)
        {
            throw new NotImplementedException();
        }

        public Task<List<CmnObjGroup>> LoadObjGroupAsync(uint tileId, uint type, ushort subType = ushort.MaxValue)
        {
            throw new NotImplementedException();
        }
    }
}
