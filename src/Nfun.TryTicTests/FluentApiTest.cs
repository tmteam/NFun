using NFun.FluentApi;
using NUnit.Framework;

namespace Nfun.ModuleTests
{
    class UserInputModel
    {
     
        public UserInputModel(string name = "vasa", int age = 22, double size = 13.5, float iq = 50)
        {
            Name = name;
            Age = age;
            Size = size;
            Iq = iq;
        }

        public string Name { get; }
        public int Age { get; }
        public double Size { get; }
        public float Iq { get; }

    }
    public class FluentApiTest
    {
        [Test]
        public void Smoke()
        {
            var input = new UserInputModel("vasa", 13);
            var result =  Fun.Calc<UserInputModel, int>("if (name=='vasa') age else 42",input);
            Assert.AreEqual(13, result);
        }
    }
}