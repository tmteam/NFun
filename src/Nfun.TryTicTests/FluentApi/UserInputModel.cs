namespace NFun.ModuleTests.FluentApi
{
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