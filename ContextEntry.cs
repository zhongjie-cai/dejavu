namespace Dejavu
{
    public class ContextEntry
    {
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public string[] InputParameters { get; set; }
        public string ReturnValue { get; set; }
        public string ErrorType { get; set; }
    }
}
