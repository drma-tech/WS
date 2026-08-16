namespace WS.Shared.Core
{
    public abstract class RequestBase
    {
        public int? Skip { get; set; }
        public int? Take { get; set; }
    }
}