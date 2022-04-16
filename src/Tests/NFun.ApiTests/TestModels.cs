// ReSharper disable All

using System;
using System.Globalization;

namespace NFun.ApiTests {

class TheContext:ICloneable {
    public string SField = "some val";
    
    public TheContext(int intRVal = 42, UserInputModel model = null) {
        IntRVal = intRVal;
        IModel = model;
    }
    public long LongRWVal { get; set; }
    public int IntRVal { get; }
    
    public ContractOutputModel OModel { get; set; }
    public UserInputModel IModel { get; }
    
    public string MyToString(int i, double d) => (i + d).ToString(CultureInfo.InvariantCulture);
    public object Clone() => new TheContext(IntRVal, IModel?.Clone() as UserInputModel) {
        LongRWVal = this.LongRWVal,
        OModel = this.OModel?.Clone() as ContractOutputModel
    };
}

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

class ContractOutputModel:ICloneable {
    public int Id { get; set; } = 123;
    public string[] Items { get; set; } = { "default" };
    public double Price { get; set; } = 12.3;
    public Decimal Taxes { get; set; } = Decimal.One;
    public object Clone() {
        return new ContractOutputModel {
            Id = Id,
            Items = (string[])Items.Clone(),
            Price = Price,
            Taxes = Taxes
        };
    }
}


class ModelWithInt {
    public int id { get; set; }
}

class ComplexModel {
    public ModelWithInt a { get; set; }
    public ModelWithInt b { get; set; }
}

class UserInputModel:ICloneable {
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
    public object Clone() {
        return new UserInputModel(Name, Age, Size, Balance, Iq, (int[])Ids.Clone());
    }
}

}