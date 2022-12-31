using RuntimeNullables;

#pragma warning disable CA1801 // Review unused parameters

namespace TestAssembly
{
    public static class NestedContainer
    {
        [NullChecks(false)]
        public static class Nested
        {
            public static void Unchecked(string value) { }

            [NullChecks(true)]
            public static void Checked(string value) { }

            public static class NestedDeeper
            {
                public static void Unchecked(string value) { }

                [NullChecks(true)]
                public static void Checked(string value) { }
            }
        }
    }
}