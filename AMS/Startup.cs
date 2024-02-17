using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(AMS.Startup))]
namespace AMS
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
