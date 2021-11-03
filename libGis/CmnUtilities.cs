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
    public abstract class Filter<TKey>
    {
        public abstract bool CheckPass(TKey target);
    }


    public class RangeFilter<TKey> : Filter<TKey> where TKey : IComparable
    {
        bool boolOutOfRange = false; //レンジ外
        TKey min;
        TKey max;
        public TKey SubTypeRangeMax => max;

        public RangeFilter(TKey min, TKey max, bool boolOutOfRange = false)
        {
            this.min = min;
            this.max = max;
        }

        public override bool CheckPass(TKey target)
        {
            if (target.CompareTo(min) >= 0 && target.CompareTo(max) <= 0)
                return !boolOutOfRange;
            else
                return boolOutOfRange;
        }

    }


    //public override RangeFilter<ushort> GetFilter(uint number)
    //{
    //    RangeFilter<ushort> subFilter;
    //    if (number == 0)
    //        subFilter = null;
    //    else
    //        subFilter = new RangeFilter<ushort>(0, (ushort)number);

    //    return subFilter;
    //}


    public class DicFilter<TKey> : Filter<TKey>
    {
        bool defaultBool = false; //辞書に存在しない対象

        Dictionary<TKey, bool> dic;

        public DicFilter(bool defaultBool)
        {
            this.defaultBool = defaultBool;
            dic = new Dictionary<TKey, bool>();
        }

        public override bool CheckPass(TKey target)
        {
            if (!dic.ContainsKey(target))
                return defaultBool;

            return dic[target];
        }

    }


    public class ListFilter<TKey> : Filter<TKey>
    {
        bool boolNotInList = false; //リスト外
        List<TKey> list;

        public ListFilter(bool boolOutOfList = false)
        {
            this.boolNotInList = boolOutOfList;
        }

        public override bool CheckPass(TKey target)
        {
            int index = list.IndexOf(target);
            if (index > 0)
                return !boolNotInList;
            else
                return boolNotInList;
        }

        public ListFilter<TKey> AddList(TKey key)
        {
            list.Add(key);
            return this;
        }

    }


    public class HierarchicalFilter<TKey, TSubKey> : Filter<TKey>
    {
        protected bool boolNotInList = false; //辞書に存在しない対象
        protected Dictionary<TKey, Filter<TSubKey>> dic;

        public HierarchicalFilter(bool boolNotInList = false)
        {
            this.boolNotInList = boolNotInList;
            dic = new Dictionary<TKey, Filter<TSubKey>>();
        }


        public override bool CheckPass(TKey target)
        {
            if (!dic.ContainsKey(target))
                return boolNotInList;

            return true;
        }

        public bool CheckPass(TKey targetType, TSubKey targetSubType)
        {
            if (!dic.ContainsKey(targetType))
                return boolNotInList;

            return dic[targetType]?.CheckPass(targetSubType) ?? true;
        }

        public Filter<TSubKey> GetSubFilter(TKey targetType)
        {
            if (!dic.ContainsKey(targetType))
                //???
                return null;

            return dic[targetType];
        }

        public HierarchicalFilter<TKey, TSubKey> AddRule(TKey type, Filter<TSubKey> subFilter)
        {
            dic.Add(type, subFilter);
            return this;
        }

        public HierarchicalFilter<TKey, TSubKey> AddRule(List<TKey> type)
        {
            type.ForEach(x => dic.Add(x, null));
            return this;
        }
        public HierarchicalFilter<TKey, TSubKey> DelRule(TKey type)
        {
            dic.Remove(type);
            return this;
        }

        public HierarchicalFilter<TKey, TSubKey> DelRule(List<TKey> type)
        {
            type.ForEach(x => dic.Remove(x));
            return this;
        }


    }






    public class CmnObjFilter : HierarchicalFilter<uint, ushort>
    {
        public CmnObjFilter(bool defaultBool = false) : base(defaultBool) { }

        public CmnObjFilter(IEnumerable<uint> typeList, ushort maxSubType = 0xFFFF) : base(false) 
        {
            foreach(var type in typeList)
            {
                if (maxSubType == 0xffff)
                    dic[type] = null;
                else
                    dic[type] = new RangeFilter<ushort>(0, maxSubType);
            }

        }

        public CmnObjFilter AddRule(uint type, RangeFilter<ushort> subFilter)
        {
            dic[type] = subFilter;
            return this;
        }

        public List<uint> GetTypeList()
        {
            return dic.Select(x => x.Key).ToList();
        }

        public ushort SubTypeRangeMax(uint type)
        {
            if (dic.ContainsKey(type))
                  return ((RangeFilter<ushort>)dic[type])?.SubTypeRangeMax ?? ushort.MaxValue;
            else
                return ushort.MaxValue;
        }

    }


    //拡張メソッド
    public static class Extensions
    {
        public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> function)
        {
            foreach(TSource x in source)
            {
                function(x);
            }
        }

    }

    public class PackData64
    {
        public UInt64 rawData;

        public PackData64() { }
        public PackData64(byte[] serialData)
        {
            rawData = BitConverter.ToUInt64(serialData, 0);
        }


        public UInt64 GetUInt(int bitStart, int bitLength)
        {
            return GetUInt(rawData, bitStart, bitLength);
        }

        public Int64 GetInt(int bitStart, int bitLength)
        {
            return GetInt(rawData, bitStart, bitLength);
        }

        public void SetUInt(int bitStart, int bitLength, UInt64 setData)
        {
            rawData = SetUInt(rawData, bitStart, bitLength, setData);
        }

        public void SetInt(int bitStart, int bitLength, Int64 setData)
        {
            rawData = SetInt(rawData, bitStart, bitLength, setData);
        }



        public static UInt64 CalcBitMask(int bitStart, int bitLength)
        {
            UInt64 mainBit = 0;
            for (int i = 0; i < bitLength; i++)
            {
                mainBit = (mainBit << 1) | 1;
            }
            mainBit <<= bitStart;

            return mainBit;
        }


        public static UInt64 GetUInt(UInt64 packData, int bitStart, int bitLength)
        {
            UInt64 bitMask = CalcBitMask(bitStart, bitLength);

            UInt64 uintData = (packData & bitMask) >> bitStart;

            return uintData;
        }

        public static Int64 GetInt(UInt64 packData, int bitStart, int bitLength)
        {
            UInt64 numberData = GetUInt(packData, bitStart, bitLength - 1);
            byte signBit = (byte)GetUInt(packData, bitStart + bitLength - 1, 1);

            //UInt64 uintData = GetUInt(packData, bitStart, bitLength);

            //byte signBit = (byte)(uintData >> (bitLength - 1));

            if (signBit == 1)
                return -(Int64)numberData;
            else
                return (Int64)numberData;
        }

        public static UInt64 SetUInt(UInt64 packData, int bitStart, int bitLength, UInt64 setData)
        {
            if (setData >= Math.Pow(2, bitLength))
            {
                throw new OverflowException();
            }

            UInt64 bitMask = CalcBitMask(bitStart, bitLength);

            UInt64 uintData = (setData << bitStart) & bitMask;

            packData &= ~bitMask;
            packData |= uintData;

            return packData;
        }

        public static UInt64 SetInt(UInt64 packData, int bitStart, int bitLength, Int64 setData)
        {
            if (Math.Abs(setData) >= Math.Pow(2, bitLength - 1))
            {
                throw new OverflowException();
            }

            UInt64 bitMask = CalcBitMask(bitStart, bitLength);

            UInt64 signBit = 0;
            UInt64 uintData;
            if (setData < 0)
            {
                signBit = (UInt64)1 << bitStart << (bitLength - 1);
                uintData = (UInt64)(-setData);
            }
            else
            {
                uintData = (UInt64)setData;
            }


            uintData = ((uintData << bitStart) & bitMask) | signBit;

            packData &= ~bitMask;
            packData |= uintData;

            return packData;
        }


    }


    public class PackData32
    {
        public UInt32 rawData;

        public PackData32() { }
        public PackData32(byte[] serialData)
        {
            rawData = BitConverter.ToUInt32(serialData, 0);
        }


        public UInt32 GetUInt(int bitStart, int bitLength)
        {
            return GetUInt(rawData, bitStart, bitLength);
        }

        public Int32 GetInt(int bitStart, int bitLength)
        {
            return GetInt(rawData, bitStart, bitLength);
        }

        public void SetUInt(int bitStart, int bitLength, UInt32 setData)
        {
            rawData = SetUInt(rawData, bitStart, bitLength, setData);
        }

        public void SetInt(int bitStart, int bitLength, Int32 setData)
        {
            rawData = SetInt(rawData, bitStart, bitLength, setData);
        }



        public static UInt32 CalcBitMask(int bitStart, int bitLength)
        {
            UInt32 mainBit = 0;
            for (int i = 0; i < bitLength; i++)
            {
                mainBit = (mainBit << 1) | 1;
            }
            mainBit <<= bitStart;

            return mainBit;
        }


        public static UInt32 GetUInt(UInt32 packData, int bitStart, int bitLength)
        {
            UInt32 bitMask = CalcBitMask(bitStart, bitLength);

            UInt32 uintData = (packData & bitMask) >> bitStart;

            return uintData;
        }

        public static Int32 GetInt(UInt32 packData, int bitStart, int bitLength)
        {
            UInt32 numberData = GetUInt(packData, bitStart, bitLength - 1);
            byte signBit = (byte)GetUInt(packData, bitStart + bitLength - 1, 1);

            if (signBit == 1)
                return -(Int32)numberData;
            else
                return (Int32)numberData;
        }

        public static UInt32 SetUInt(UInt32 packData, int bitStart, int bitLength, UInt32 setData)
        {
            if (setData >= Math.Pow(2, bitLength))
            {
                throw new OverflowException();
            }

            UInt32 bitMask = CalcBitMask(bitStart, bitLength);

            UInt32 uintData = (setData << bitStart) & bitMask;

            packData &= ~bitMask;
            packData |= uintData;

            return packData;
        }

        public static UInt32 SetInt(UInt32 packData, int bitStart, int bitLength, Int32 setData)
        {
            if (Math.Abs(setData) >= Math.Pow(2, bitLength - 1))
            {
                throw new OverflowException();
            }

            UInt32 bitMask = CalcBitMask(bitStart, bitLength);

            UInt32 signBit = 0;
            UInt32 uintData;
            if (setData < 0)
            {
                signBit = (UInt32)1 << bitStart << (bitLength - 1);
                uintData = (UInt32)(-setData);
            }
            else
            {
                uintData = (UInt32)setData;
            }


            uintData = ((uintData << bitStart) & bitMask) | signBit;

            packData &= ~bitMask;
            packData |= uintData;

            return packData;
        }


    }

}