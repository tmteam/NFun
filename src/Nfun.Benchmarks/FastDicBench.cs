using System;
using System.Collections;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using NFun.Tic;

namespace Nfun.Benchmarks
{
    public class FastDicBench
    {
        private string[] Array;
        private object[] ObjArray;
        private Dictionary<string, object> Dic;
        private SmallStringDictionary<object> Sdic;

        [GlobalSetup]
        public void Setup()
        {
            Sdic = new SmallStringDictionary<object>();
            
            ObjArray = new[]
            {
                new object(), 
                new object(), 
                new object(), 
                new object(), 
                new object(),
            };
            Array = new[] {
                "vasa",
                "x",
                "arg1",
             //   "someVariable",
            };
            Dic = new Dictionary<string, object>();

            for (int i = 0; i < Array.Length; i++)
            {
                Sdic.Add(Array[i], ObjArray[i]);
                Dic.Add(Array[i],  ObjArray[i]);
            }
        }

        [Benchmark(Description = "Dic.Get")]
        public void GetDic()
        {
            foreach (var key in Array)
            {
                var res = Dic[key];
            }
        }
        [Benchmark(Description = "SDic.Get")]
        public void GetSDic()
        {
            foreach (var key in Array)
            {
                var res = Sdic[key];
            }
        }
        [Benchmark(Description = "Dic.TryGet")]
        public bool TryGetDic()
        {
            foreach (var key in Array)
            {
                if(!Dic.TryGetValue(key, out var result))
                    return false;
            }

            return true;
        }
        
        [Benchmark(Description = "SDic.TryGet")]
        public bool TryGeStDic()
        {
            foreach (var key in Array)
            {
                if(!Sdic.TryGetValue(key, out var result))
                    return false;
            }

            return true;
        }
        
        [Benchmark(Description = "Arr.Get")]
        public bool GetFromArray()
        {
            foreach (var key in Array)
            { 
                var res = GetFromArray(key);
                if (res == null)
                    return false;
            }
            return true;
        }
        [Benchmark(Description = "Dic.Contains")]
        public bool ContainsDic()
        {
            foreach (var key in Array)
            {
                if (!Dic.ContainsKey(key))
                    return false;
            }

            return true;
        }
        [Benchmark(Description = "Sdic.Contains")]
        public bool ContainsSDic()
        {
            foreach (var key in Array)
            {
                if (!Sdic.ContainsKey(key))
                    return false;
            }

            return true;
        }
        [Benchmark(Description = "Arr.Contains")]
        public bool ContainsArr()
        {
            foreach (var key in Array)
            {
                if (!Contains(key))
                    return false;
            }

            return true;
        }
        [Benchmark(Description = "Dic.Contains false")]
        public bool MissContainsDic() => Dic.ContainsKey("KavaBanga");
        [Benchmark(Description = "Sdic.Contains false")]
        public bool MissContainsSdic() => Sdic.ContainsKey("KavaBanga");

        /*
        [Benchmark(Description = "Dic.Contains case")]
        public bool CaseContainsDic() => Dic.ContainsKey("ArG1");
        */

        [Benchmark(Description = "Arr.Contains false")]
        public bool MissContainsArr() => Contains("KavaBanaga");
        
        /*
        [Benchmark(Description = "Arr.Contains case")]
        public bool CaseContainsArr() => Contains("ArG1");
        */

        private bool Contains(string key)
        {
            if (Dic == null)
                return false;
            foreach (var t in Array)
            {
                if (string.Compare(t,key, StringComparison.OrdinalIgnoreCase)==0)
                    return true;
            }
            return false;
        }
        private object GetFromArray(string key)
        {
            if (Dic == null)
                return null;
            for (int i = 0; i < Array.Length; i++)
            {
                if (Array[i] == key)
                    return ObjArray[i];
            }

            return null;
        }
    }


}