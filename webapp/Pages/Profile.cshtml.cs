using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace webapp.Pages
{
	[AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
	public class ProfileModel : PageModel
	{
		private readonly ILogger<ProfileModel> _logger;
		private readonly GraphServiceClient _graphServiceClient;

		public User GraphUser { get; set; }
		public string Photo { get; set; }

		[BindProperty(SupportsGet = true)]
		public string SearchString { get; set; }

		public ProfileModel(ILogger<ProfileModel> logger,
											GraphServiceClient graphServiceClient)
		{
			_logger = logger;
			_graphServiceClient = graphServiceClient;
		}


		public async Task OnGetAsync()
		{

			if (string.IsNullOrEmpty(SearchString))
			{
				SearchString = User.Identity.Name;
			}

			var r = _graphServiceClient.Users[SearchString].Request();

			GraphUser = await _graphServiceClient.Users[SearchString].Request().GetAsync();

				try
				{
					// Get user photo
					using (var photoStream = await _graphServiceClient.Users[SearchString].Photo.Content.Request().GetAsync())
					{
						byte[] photoByte = ((MemoryStream)photoStream).ToArray();
						Photo = Convert.ToBase64String(photoByte);
					}
				}
				catch (System.Exception ex)
				{
					_logger.LogError(ex, "Error retrieving photo");
				}
		}
	}
}
