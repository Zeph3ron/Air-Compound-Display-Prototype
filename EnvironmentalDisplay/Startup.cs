using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(EnvironmentalDisplay.Startup))]
namespace EnvironmentalDisplay
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
