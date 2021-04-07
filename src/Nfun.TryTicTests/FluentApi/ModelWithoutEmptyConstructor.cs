namespace NFun.ModuleTests.FluentApi
{
    public class ModelWithoutEmptyConstructor
    {
        public ModelWithoutEmptyConstructor( string name)
        {
            Name = name;
         
        }
        public string Name { get; }
    }
}