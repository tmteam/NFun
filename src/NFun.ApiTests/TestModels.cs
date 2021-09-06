// ReSharper disable All

namespace NFun.ApiTests {

public class ModelWithCharArray {
    public char[] Chars { get; set; }
}

public class ModelWithCharArray2 {
    public char[] Letters { get; set; }
}

public class ModelWithoutEmptyConstructor {
    public ModelWithoutEmptyConstructor(string name) {
        Name = name;
    }

    public string Name { get; }
}

class ContractOutputModel {
    public int Id { get; set; } = 123;
    public string[] Items { get; set; } = { "default" };
    public double Price { get; set; } = 12.3;
}

class ModelWithInt {
    public int id { get; set; }
}

class ComplexModel {
    public ModelWithInt a { get; set; }
    public ModelWithInt b { get; set; }
}

class UserInputModel {
    public UserInputModel(string name = "vasa", int age = 22, double size = 13.5, float iq = 50, params int[] ids) {
        Ids = ids;
        Name = name;
        Age = age;
        Size = size;
        Iq = iq;
    }

    public int[] Ids { get; }
    public string Name { get; }
    public int Age { get; }
    public double Size { get; }
    public float Iq { get; }
}

}