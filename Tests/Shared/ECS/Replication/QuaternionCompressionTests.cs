using System;
using System.Numerics;
using LiteNetLib.Utils;
using NSubstitute;
using Shared.Replication;
using Xunit;

namespace SharedUnitTests.ECS.Replication
{
    public class QuaternionCompressionTests
    {
        private static Vector3 Rotate(Vector3 v, Quaternion q)
        {
            return Vector3.Transform(v, q);
        }

        private static void AssertRotationEquivalent(Quaternion expected, Quaternion actual, float tol = 0.002f)
        {
            // Compare rotation effect on basis vectors to be robust to q ~ -q
            var basis = new[]
            {
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 0, 1)
            };

            foreach (var v in basis)
            {
                var e = Rotate(v, expected);
                var a = Rotate(v, actual);
                Assert.InRange(a.X, e.X - tol, e.X + tol);
                Assert.InRange(a.Y, e.Y - tol, e.Y + tol);
                Assert.InRange(a.Z, e.Z - tol, e.Z + tol);
            }
        }

        [Theory]
        [InlineData(0f, 0f, 0f)]
        [InlineData(MathF.PI / 2, 0f, 0f)]
        [InlineData(0f, MathF.PI / 2, 0f)]
        [InlineData(0f, 0f, MathF.PI / 2)]
        [InlineData(MathF.PI, 0f, 0f)]
        [InlineData(MathF.PI / 4, 0f, 0f)]
        public void RoundTrip_KnownAngles_RotationPreserved(float yaw, float pitch, float roll)
        {
            var q = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
            var adapterQ = RoundTrip(q);
            AssertRotationEquivalent(q, adapterQ);
        }

        [Fact]
        public void RoundTrip_LargestComponentNegative_RotationPreserved()
        {
            // Construct a quaternion with a negative largest component
            var q = new Quaternion(0.1f, -0.2f, 0.3f, -0.9f);
            q = Quaternion.Normalize(q);
            var adapterQ = RoundTrip(q);
            AssertRotationEquivalent(q, adapterQ);
        }

        [Fact]
        public void RoundTrip_RandomQuaternions_RotationPreserved()
        {
            var rng = new Random(12345);
            for (int i = 0; i < 100; i++)
            {
                var q = new Quaternion(
                    (float)(rng.NextDouble() * 2 - 1),
                    (float)(rng.NextDouble() * 2 - 1),
                    (float)(rng.NextDouble() * 2 - 1),
                    (float)(rng.NextDouble() * 2 - 1));
                q = Quaternion.Normalize(q);

                var adapterQ = RoundTrip(q);
                AssertRotationEquivalent(q, adapterQ);
            }
        }

        private static Quaternion RoundTrip(Quaternion q)
        {
            var writer = new NetDataWriter();
            var adapterWriter = new NetDataWriterAdapter(writer, Substitute.For<IComponentSerializer>());
            adapterWriter.PutQuaternionCompressed(q);

            var reader = new NetDataReader(writer);
            var adapterReader = new NetDataReaderAdapter(reader, Substitute.For<IComponentSerializer>());
            return adapterReader.GetQuaternionCompressed();
        }
    }
}