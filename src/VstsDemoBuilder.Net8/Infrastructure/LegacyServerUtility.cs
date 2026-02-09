namespace VstsDemoBuilder.Infrastructure
{
    public sealed class LegacyServerUtility
    {
        public string MapPath(string virtualPath)
        {
            return System.Web.Hosting.HostingEnvironment.MapPath(virtualPath);
        }
    }
}
