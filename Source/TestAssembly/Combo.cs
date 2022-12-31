namespace TestAssembly
{
    public class Combo<TClass> where TClass : notnull
    {
        public string GetValue<TMethod>(bool returnNull, TClass inputValue, ref TMethod refValue, bool outputNull) where TMethod : class
        {
            string returnValue = returnNull ? null! : inputValue.ToString() + refValue.ToString();

            if (outputNull)
                refValue = default!;

            return returnValue;
        }
    }
}