namespace Loudenvier.Utils.Tests
{
    public class IsOneOfExtensionsTests
    {

        enum Test { Value1, Value2, Value3, Value4 }

        [Fact]
        public void TestIsOneOfWithEnum_True() => Assert.True(Test.Value1.IsOneOf(Test.Value3, Test.Value1));
        [Fact]
        public void TestIsOneOfWithEnum_False() => Assert.False(Test.Value1.IsOneOf(Test.Value3, Test.Value2));

        [Fact]
        public void TestIsOneOfWithInt_True() => Assert.True(1.IsOneOf(10, 20, 40, 1));
        [Fact]
        public void TestIsOneOfWithInt_False() => Assert.False(1.IsOneOf(10, 20, 40, 12));

        record TestRec(string Name, int Age);
        [Fact]
        public void TestIsOneOfWithRecord_True() {
            var rec1 = new TestRec("Felipe", 48);
            var rec2 = new TestRec("Julio", 46);
            var rec3 = new TestRec("Marcelle", 53);
            Assert.True(new TestRec("Felipe", 48).IsOneOf(rec3, rec2, rec1));
        }
        [Fact]
        public void TestIsOneOfWithRecord_False() {
            var rec1 = new TestRec("Felipe", 48);
            var rec2 = new TestRec("Julio", 46);
            var rec3 = new TestRec("Marcelle", 53);
            Assert.False(new TestRec("Felipe", 50).IsOneOf(rec3, rec2, rec1));
        }

        class TestClass {
            public TestClass(string name, int age) {
                Name = name;
                Age = age;
            }
            public string Name { get; }
            public int Age { get; }
        }
        [Fact]
        public void TestIsOneOfWithClassUsesReference_True() {
            var item1 = new TestClass("Felipe", 48);
            var item2 = new TestClass("Julio", 46);
            var item3 = new TestClass("Marcelle", 53);
            Assert.True(item1.IsOneOf(item3, item2, item1));
        }
        [Fact]
        public void TestIsOneOfWithClassUsesReference_False() {
            var item1 = new TestClass("Felipe", 48);
            var item2 = new TestClass("Julio", 46);
            var item3 = new TestClass("Marcelle", 53);
            Assert.False(new TestClass("Felipe", 48).IsOneOf(item3, item2, item1));
        }

        class TestClassWithEquals : TestClass, IEquatable<TestClassWithEquals?>
        {
            public TestClassWithEquals(string name, int age) : base(name, age) { }
            public override bool Equals(object? obj) => Equals(obj as TestClassWithEquals);
            public bool Equals(TestClassWithEquals? other) {
                return other is not null &&
                       Name == other.Name &&
                       Age == other.Age;
            }
            public static bool operator ==(TestClassWithEquals? left, TestClassWithEquals? right) 
                => EqualityComparer<TestClassWithEquals>.Default.Equals(left, right);

            public static bool operator !=(TestClassWithEquals? left, TestClassWithEquals? right)
                => !(left == right);

            public override int GetHashCode() => throw new NotImplementedException();
        }
        [Fact]
        public void TestIsOneOfWithClassUsesEqualsIfOverriden_True() {
            var item1 = new TestClassWithEquals("Felipe", 48);
            var item2 = new TestClassWithEquals("Julio", 46);
            var item3 = new TestClassWithEquals("Marcelle", 53);
            Assert.True(new TestClassWithEquals("Felipe", 48).IsOneOf(item3, item2, item1));
        }
        [Fact]
        public void TestIsOneOfWithClassUsesEqualsIfOverriden_False() {
            var item1 = new TestClassWithEquals("Felipe", 48);
            var item2 = new TestClassWithEquals("Julio", 46);
            var item3 = new TestClassWithEquals("Marcelle", 53);
            Assert.False(new TestClassWithEquals("Felipe", 47).IsOneOf(item3, item2, item1));
        }
    }

    public class BitConverterBigEndiandTests {
        static readonly byte[] value_in_big_endian = { 0x12, 0x34, 0x56, 0x78, 0x21, 0x43, 0x65, 0x87 };

        [Fact]
        public void ToUInt16WorksOnLittleEndian() {
            if (!BitConverter.IsLittleEndian) return;
            ushort expected = 0x1234;
            var result = BitConverterBigEndian.ToUInt16(value_in_big_endian, 0);
            Assert.Equal(expected, result);
        }
        [Fact]
        public void ToInt16WorksOnLittleEndian() {
            if (!BitConverter.IsLittleEndian) return;
            short expected = 0x1234;
            var result = BitConverterBigEndian.ToInt16(value_in_big_endian, 0);
            Assert.Equal(expected, result);
        }
        [Fact]
        public void ToUInt32WorksOnLittleEndian() {
            if (!BitConverter.IsLittleEndian) return;
            uint expected = 0x12345678;
            var result = BitConverterBigEndian.ToUInt32(value_in_big_endian, 0);
            Assert.Equal(expected, result);
        }
        [Fact]
        public void ToInt32WorksOnLittleEndian() {
            if (!BitConverter.IsLittleEndian) return;
            int expected = 0x12345678;
            var result = BitConverterBigEndian.ToInt32(value_in_big_endian, 0);
            Assert.Equal(expected, result);
        }
        [Fact]
        public void ToUInt64WorksOnLittleEndian() {
            if (!BitConverter.IsLittleEndian) return;
            ulong expected = 0x1234567821436587;
            var result = BitConverterBigEndian.ToUInt64(value_in_big_endian, 0);
            Assert.Equal(expected, result);
        }
        [Fact]
        public void ToInt64WorksOnLittleEndian() {
            if (!BitConverter.IsLittleEndian) return;
            long expected = 0x1234567821436587;
            var result = BitConverterBigEndian.ToInt64(value_in_big_endian, 0);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetBytesUInt16WorksOnLittleEndian() {
            if (!BitConverter.IsLittleEndian) return;
            ushort value = 0x1234;
            var result = BitConverterBigEndian.GetBytes(value);
            Assert.Equal(value_in_big_endian[0..2], result);
        }
        [Fact]
        public void GetBytesInt16WorksOnLittleEndian() {
            if (!BitConverter.IsLittleEndian) return;
            short value = 0x1234;
            var result = BitConverterBigEndian.GetBytes(value);
            Assert.Equal(value_in_big_endian[0..2], result);
        }
        [Fact]
        public void GetBytesUInt32WorksOnLittleEndian() {
            if (!BitConverter.IsLittleEndian) return;
            uint value = 0x12345678;
            var result = BitConverterBigEndian.GetBytes(value);
            Assert.Equal(value_in_big_endian[0..4], result);
        }
        [Fact]
        public void GetBytesInt32WorksOnLittleEndian() {
            if (!BitConverter.IsLittleEndian) return;
            int value = 0x12345678;
            var result = BitConverterBigEndian.GetBytes(value);
            Assert.Equal(value_in_big_endian[0..4], result);
        }
        [Fact]
        public void GetBytesUInt64WorksOnLittleEndian() {
            if (!BitConverter.IsLittleEndian) return;
            ulong value = 0x1234567821436587;
            var result = BitConverterBigEndian.GetBytes(value);
            Assert.Equal(value_in_big_endian[0..8], result);
        }
        [Fact]
        public void GetBytesInt64WorksOnLittleEndian() {
            if (!BitConverter.IsLittleEndian) return;
            long value = 0x1234567821436587;
            var result = BitConverterBigEndian.GetBytes(value);
            Assert.Equal(value_in_big_endian[0..8], result);
        }

    }
}