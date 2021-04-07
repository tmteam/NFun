using System;
using System.Text.Json;

namespace NFun.ModuleTests
{
    public static class TestHelper
    {
        public static bool AreSame(object a, object b)
        {
            if (a == null || b == null)
                return false;
            if (a.GetType() != b.GetType())
                return false;
            var ajson = JsonSerializer.Serialize(a);
            var bjson = JsonSerializer.Serialize(b);
            Console.WriteLine($"Comparing object. \r\norigin: \r\n{ajson}\r\nexpected: \r\n{bjson}");
            return ajson.Equals(bjson);
        }
    }
}