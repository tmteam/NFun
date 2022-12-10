// ReSharper disable All

using System;
using System.Globalization;
using System.Linq;

namespace NFun.TestTools;

public class ContextModel1 : ICloneable {
    public string SField = "some val";

    public ContextModel1(int intRVal = 42, UserInputModel imodel = null) {
        IntRVal = intRVal;
        IModel = imodel;
    }

    public long LongRWVal { get; set; }
    public double RealRWVal { get; set; }

    public int IntRVal { get; }

    public ContractOutputModel OModel { get; set; }
    public UserInputModel IModel { get; }

    public string MyToString(int i, double d) => (i + d).ToString(CultureInfo.InvariantCulture);

    public object Clone() => new ContextModel1(IntRVal, IModel?.Clone() as UserInputModel) {
        LongRWVal = this.LongRWVal, RealRWVal = this.RealRWVal, OModel = this.OModel?.Clone() as ContractOutputModel
    };
}

public class ModelWithCharArray {
    public char[] Chars { get; set; }
}

public class ModelWithCharArray2 {
    public char[] Letters { get; set; }
}

public class ModelWithoutEmptyConstructor {
    public ModelWithoutEmptyConstructor(string name) => Name = name;
    public string Name { get; }
}

public class ContractOutputModel : ICloneable {
    public int Id { get; set; } = 123;
    public string[] Items { get; set; } = { "default" };
    public double Price { get; set; } = 12.3;
    public Decimal Taxes { get; set; } = Decimal.One;

    public object Clone() => new ContractOutputModel {
        Id = Id, Items = (string[])Items.Clone(), Price = Price, Taxes = Taxes
    };
}

public class ContextModel2 : ICloneable {
    public ContextModel2(int id, int[] inputs, UserInputModel[] users) {
        Id = id;
        Inputs = inputs;
        Users = users;
    }

    //Inputs:
    public UserInputModel[] Users { get; }
    public int Id { get; }

    public int[] Inputs { get; private set; }

    //Outputs:
    public double Price { get; set; } = 12.3;
    public string[] Results { get; set; } = { "default" };
    public Decimal Taxes { get; set; } = Decimal.One;
    public ContractOutputModel[] Contracts { get; set; }

    public object Clone() => new ContextModel2(Id, Inputs, Users) {
        Results = (string[])Results?.Clone(),
        Price = Price,
        Taxes = Taxes,
        Contracts = Contracts?.Select(a => (ContractOutputModel)a.Clone()).ToArray()
    };
}

public class ModelWithInt {
    public int id { get; set; }
}

public class ComplexModel {
    public ModelWithInt a { get; set; }
    public ModelWithInt b { get; set; }
}

public class UserInputModel : ICloneable {
    public UserInputModel(string name = "vasa", int age = 22, double size = 13.5, Decimal balance = Decimal.One,
        float iq = 50, params int[] ids) {
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
    public object Clone() => new UserInputModel(Name, Age, Size, Balance, Iq, (int[])Ids.Clone());
}
