namespace NFun.ApiTests
{
    public class HardcoreUpdateTest
    {
        public void Smoke()
        {
            var runtime = Funny.Hardcore.Build("y= 2*x");
                // runtime.GetAllVariableSources()
        }
    }
}