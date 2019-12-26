using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(VPGSampleCS.Startup))]
namespace VPGSampleCS
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {

        }
    }
}
