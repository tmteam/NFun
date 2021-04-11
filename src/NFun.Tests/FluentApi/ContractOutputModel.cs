namespace NFun.Tests.FluentApi
{
    class ContractOutputModel
    {
        public int Id { get; set; } = 123;
        public string[] Items { get; set; } = {"default"};
        public double Price { get; set; } = 12.3;
    }
}