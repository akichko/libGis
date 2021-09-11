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

namespace libGis
{
    public abstract class Filter<KeyType>
    {
        public abstract bool CheckPass(KeyType target);
    }


    public class RangeFilter<KeyType> : Filter<KeyType> where KeyType : IComparable
    {
        bool boolOutOfRange = false; //レンジ外
        KeyType min;
        KeyType max;

        public RangeFilter(KeyType min, KeyType max, bool boolOutOfRange = false)
        {
            this.min = min;
            this.max = max;
        }

        public override bool CheckPass(KeyType target)
        {
            if (target.CompareTo(min) >= 0 && target.CompareTo(max) <= 0)
                return !boolOutOfRange;
            else
                return boolOutOfRange;
        }
    }




    public class DicFilter<KeyType> : Filter<KeyType>
    {
        bool defaultBool = false; //辞書に存在しない対象

        Dictionary<KeyType, bool> dic;

        public DicFilter(bool defaultBool)
        {
            this.defaultBool = defaultBool;
            dic = new Dictionary<KeyType, bool>();
        }

        public override bool CheckPass(KeyType target)
        {
            if (!dic.ContainsKey(target))
                return defaultBool;

            return dic[target];
        }

    }


    public class ListFilter<KeyType> : Filter<KeyType>
    {
        bool boolNotInList = false; //リスト外
        List<KeyType> list;

        public ListFilter(bool boolOutOfList = false)
        {
            this.boolNotInList = boolOutOfList;
        }

        public override bool CheckPass(KeyType target)
        {
            int index = list.IndexOf(target);
            if (index > 0)
                return !boolNotInList;
            else
                return boolNotInList;
        }

        public ListFilter<KeyType> AddList(KeyType key)
        {
            list.Add(key);
            return this;
        }

    }


    public class HierarchicalFilter<KeyType, KeySubType> : Filter<KeyType>
    {
        protected bool boolNotInList = false; //辞書に存在しない対象
        protected Dictionary<KeyType, Filter<KeySubType>> dic;

        public HierarchicalFilter(bool boolNotInList = false)
        {
            this.boolNotInList = boolNotInList;
            dic = new Dictionary<KeyType, Filter<KeySubType>>();
        }


        public override bool CheckPass(KeyType target)
        {
            if (!dic.ContainsKey(target))
                return boolNotInList;

            return true;
        }

        public bool CheckPass(KeyType targetType, KeySubType targetSubType)
        {
            if (!dic.ContainsKey(targetType))
                return boolNotInList;

            return dic[targetType]?.CheckPass(targetSubType) ?? true;
        }

        public Filter<KeySubType> GetSubFilter(KeyType targetType)
        {
            if (!dic.ContainsKey(targetType))
                //???
                return null;

            return dic[targetType];
        }

        public HierarchicalFilter<KeyType, KeySubType> AddRule(KeyType type, Filter<KeySubType> subFilter)
        {
            dic.Add(type, subFilter);
            return this;
        }

        public HierarchicalFilter<KeyType, KeySubType> AddRule(List<KeyType> type)
        {
            type.ForEach(x => dic.Add(x, null));
            return this;
        }

    }


    public class CmnObjFilter : HierarchicalFilter<uint, ushort>
    {
        public CmnObjFilter(bool defaultBool = false) : base(defaultBool) { }

    }
}