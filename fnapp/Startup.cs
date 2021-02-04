using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.Text;

[assembly: FunctionsStartup(typeof(fnapp.Startup))]

namespace fnapp
{
	public class Startup : FunctionsStartup
	{
		public Startup()
		{
		}

		public override void Configure(IFunctionsHostBuilder builder)
		{
			// This is configuration from environment variables, settings.json etc.
			var configuration = builder.GetContext().Configuration;

			builder.Services.AddAuthentication(sharedOptions =>
			{
				sharedOptions.DefaultScheme = "Bearer";
				sharedOptions.DefaultChallengeScheme = "Bearer";
			})
					.AddMicrosoftIdentityWebApi(configuration)
							.EnableTokenAcquisitionToCallDownstreamApi()
							.AddMicrosoftGraph()
							.AddInMemoryTokenCaches();
		}

	}
}
