using Shared.Math;
using Xunit;

namespace SharedUnitTests.Math
{
    public class LerpingTests
    {
        [Theory]
        [InlineData(0f, 10f, 0f, 0f)]
        [InlineData(0f, 10f, 1f, 10f)]
        [InlineData(0f, 10f, 0.5f, 5f)]
        [InlineData(-5f, 5f, 0.5f, 0f)]
        [InlineData(10f, 20f, 0.25f, 12.5f)]
        public void Lerp_Float_ReturnsExpected(float a, float b, float t, float expected)
        {
            var result = Lerping.Lerp(a, b, t);
            Assert.Equal(expected, result, 5);
        }

        [Theory]
        [InlineData(0.0, 10.0, 0.0, 0.0)]
        [InlineData(0.0, 10.0, 1.0, 10.0)]
        [InlineData(0.0, 10.0, 0.5, 5.0)]
        [InlineData(-5.0, 5.0, 0.5, 0.0)]
        [InlineData(10.0, 20.0, 0.25, 12.5)]
        public void Lerp_Double_ReturnsExpected(double a, double b, double t, double expected)
        {
            var result = Lerping.Lerp(a, b, t);
            Assert.Equal(expected, result, 10);
        }

        [Theory]
        [InlineData(0u, 10u, 0f, 0u)]
        [InlineData(0u, 10u, 1f, 10u)]
        [InlineData(0u, 10u, 0.5f, 5u)]
        [InlineData(10u, 20u, 0.25f, 12u)]
        [InlineData(10u, 20u, 0.75f, 17u)]
        [InlineData(100u, 200u, 0.1f, 110u)]
        public void Lerp_UInt_ReturnsExpected(uint a, uint b, float t, uint expected)
        {
            var result = Lerping.Lerp(a, b, t);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Lerp_Float_Extrapolation()
        {
            Assert.Equal(15f, Lerping.Lerp(10f, 20f, 0.5f));
            Assert.Equal(25f, Lerping.Lerp(10f, 20f, 1.5f));
            Assert.Equal(5f, Lerping.Lerp(10f, 20f, -0.5f));
        }

        [Fact]
        public void Lerp_Double_Extrapolation()
        {
            Assert.Equal(15.0, Lerping.Lerp(10.0, 20.0, 0.5), 10);
            Assert.Equal(25.0, Lerping.Lerp(10.0, 20.0, 1.5), 10);
            Assert.Equal(5.0, Lerping.Lerp(10.0, 20.0, -0.5), 10);
        }
    }
}