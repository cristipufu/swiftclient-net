namespace SwiftClient.Test
{
    public class SwiftFixtureDemo<TStartup> : SwiftFixtureWebHost<TStartup>
        where TStartup : class
    {
        public SwiftFixtureDemo() : base("http://localhost:5000")
        {
        }
    }
}
