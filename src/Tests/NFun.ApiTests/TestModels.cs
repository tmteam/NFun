// ReSharper disable All

using System;

namespace NFun.ApiTests {

public class ModelWithCharArray {
    public char[] Chars { get; set; }
}

public class ModelWithCharArray2 {
    public char[] Letters { get; set; }
}

public class ModelWithoutEmptyConstructor {
    public ModelWithoutEmptyConstructor(string name) { Name = name; }

    public string Name { get; }
}

class ContractOutputModel {
    public int Id { get; set; } = 123;
    public string[] Items { get; set; } = { "default" };
    public double Price { get; set; } = 12.3;
    public Decimal Taxes { get; set; } = Decimal.One;
}


class ModelWithInt {
    public int id { get; set; }
}

class ComplexModel {
    public ModelWithInt a { get; set; }
    public ModelWithInt b { get; set; }
}

class UserInputModel {
    public UserInputModel(string name = "vasa", int age = 22, double size = 13.5, Decimal balance = Decimal.One, float iq = 50,  params int[] ids) {
        Ids = ids;
        Name = name;
        Age = age;
        Size = size;
        Iq = iq;
        Balance = balance;
    }

    public int[] Ids { get; }
    public string Name { get; }
    public int Age { get; }
    public double Size { get; }
    public float Iq { get; }
    public Decimal Balance { get; }
}

}