namespace NFun.Tests.FluentApi
{
    class ModelWithInt{ public int id { get; set; }}

    class ComplexModel
    {
        public ModelWithInt a { get; set; }
        public ModelWithInt b { get; set; }
    }
        class UserInputModel
        {
         
            public UserInputModel( string name = "vasa", int age = 22, double size = 13.5, float iq = 50, params int[] ids)
            {
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