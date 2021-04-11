namespace NFun.Tests.FluentApi
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