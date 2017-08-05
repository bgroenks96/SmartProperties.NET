
namespace SmartPropertiesTest
{
	using NUnit.Framework;
	using System;
    using System.ComponentModel;

    using SmartProperties;
    using System.Runtime.CompilerServices;

    [TestFixture]
    public class PropertyModelTest
    {
        [Test]
        public void TestPropertyChangedPropagatesToDependents()
        {
            var obj = new TestType();
            obj.A = "test";
            Assert.AreEqual(1, obj.CountA);
            Assert.AreEqual(1, obj.CountB);
            Assert.AreEqual(1, obj.CountC);
            Assert.AreEqual(1, obj.CountD);
            Assert.AreEqual(1, obj.CountE);
        }

        [Test]
        public void TestPropertyChangedDoesNotPropagateToParents()
        {
            var obj = new TestType();
            obj.C = "test";
            Assert.AreEqual(0, obj.CountA);
            Assert.AreEqual(0, obj.CountB);
            Assert.AreEqual(1, obj.CountC);
            Assert.AreEqual(1, obj.CountD);
            Assert.AreEqual(1, obj.CountE);
        }

        [Test]
        public void TestDependencyCycleCallsOnlyOnce_1()
        {
            var obj = new TestType();
            obj.D = "test";
            Assert.AreEqual(0, obj.CountA);
            Assert.AreEqual(0, obj.CountB);
            Assert.AreEqual(0, obj.CountC);
            Assert.AreEqual(1, obj.CountD);
            Assert.AreEqual(1, obj.CountE);
        }

        [Test]
        public void TestDependencyCycleCallsOnlyOnce_2()
        {
            var obj = new TestType();
            obj.E = "test";
            Assert.AreEqual(0, obj.CountA);
            Assert.AreEqual(0, obj.CountB);
            Assert.AreEqual(0, obj.CountC);
            Assert.AreEqual(1, obj.CountD);
            Assert.AreEqual(1, obj.CountE);
        }

        [Test]
		public void TestPropertyModelInitFailsForSelfDependency()
		{
            Assert.Throws<ArgumentException>(() => new IllegalType(), "expected exception for illegal self reference");
		}

        private class TestType : NotifyPropertyChangedBase
        {
            private string a, b, c, d, e;

            internal string A
            {
                get
                {
                    return this.a;
                }

                set
                {
                    this.a = value;
                    this.OnPropertyChanged();
                }
            }

            [DependsOn(nameof(A))]
            internal string B
            {
                get
                {
                    return this.b;
                }

                set
                {
                    this.b = value;
                    this.OnPropertyChanged();
                }
            }

            [DependsOn(nameof(B))]
            internal string C
            {
                get
                {
                    return this.c;
                }

                set
                {
                    this.c = value;
                    this.OnPropertyChanged();
                }
            }

            // The inclusion of both B and C is redundant here, since any dependent
            // of C will be called as a result of a change to B. We include it here,
            // however, for testing purposes, to ensure that PropertyModel doesn't
            // dispatch the property change event multiple times to D.
			[DependsOn(nameof(E), nameof(B), nameof(C))]
			internal string D
			{
				get
				{
					return this.d;
				}

				set
				{
					this.d = value;
					this.OnPropertyChanged();
				}
			}

			[DependsOn(nameof(D))]
			internal string E
			{
				get
				{
					return this.e;
				}

				set
				{
					this.e = value;
					this.OnPropertyChanged();
				}
			}

            internal int CountA { get; private set; }

            internal int CountB { get; private set; }

            internal int CountC { get; private set; }

            internal int CountD { get; private set; }

            internal int CountE { get; private set; }

            protected override void OnPropertyChanged([CallerMemberName]string property = null)
            {
                base.OnPropertyChanged(property);
                switch (property)
                {
                    case nameof(A):
                        this.CountA++;
                        break;
                    case nameof(B):
                        this.CountB++;
                        break;
                    case nameof(C):
                        this.CountC++;
                        break;
					case nameof(D):
						this.CountD++;
						break;
					case nameof(E):
						this.CountE++;
						break;
                }
            }
        }

        private class IllegalType : NotifyPropertyChangedBase
        {
            [DependsOn(nameof(A))]
            public string A { get; }
        }
    }
}
