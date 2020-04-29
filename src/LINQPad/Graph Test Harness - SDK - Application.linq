<Query Kind="Program">
  <NuGetReference>Microsoft.Graph</NuGetReference>
  <NuGetReference>Microsoft.Identity.Client</NuGetReference>
  <Namespace>Microsoft.Graph</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
</Query>

private static GraphServiceClient _graphClient;

void Main()
{
	GraphServiceClient graphClient = GetAuthenticatedGraphClient(AuthenticationMode.ClientSecret);
	
	graphClient.Users.Request().GetAsync().Result[0].DisplayName.Dump("Graph result");
}

private GraphServiceClient GetAuthenticatedGraphClient(AuthenticationMode authenticationMode)
{
	var authenticationProvider = CreateAuthorizationProvider(authenticationMode);
	_graphClient = new GraphServiceClient(authenticationProvider);
	return _graphClient;
}

private static IAuthenticationProvider CreateAuthorizationProvider(AuthenticationMode authenticationMode)
{
	var clientId = Util.GetPassword("clientId");
	var certificateThumbprint = Util.GetPassword("certificateThumbprint");
	var redirectUri = Util.GetPassword("redirectUri");
	var tenantId = Util.GetPassword("tenantId");
	var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";

	//this specific scope means that application will default to what is defined in the application registration rather than using dynamic scopes
	List<string> scopes = new List<string>();
	scopes.Add("https://graph.microsoft.com/.default");

	ConfidentialClientApplicationOptions options = new ConfidentialClientApplicationOptions()
	{
		ClientId = clientId,
		TenantId = tenantId,
		RedirectUri = redirectUri,
	};

	var builder = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(options);

	switch (authenticationMode)
	{
		case AuthenticationMode.ClientSecret:
			var clientSecret = Util.GetPassword("clientSecret");

			builder.WithClientSecret(clientSecret);
			break;
		case AuthenticationMode.Certificate:
			// defaulting to CurrentUser certificate store under My (Personal), change these if stored elsewhere
			X509Certificate2 cert = GetCertificate(certificateThumbprint, StoreName.My, StoreLocation.CurrentUser);

			builder.WithCertificate(cert);
			break;
	}

	var cca = builder.Build();
	
	return new MsalAuthenticationProvider(cca, scopes);
}

private static X509Certificate2 GetCertificate(string thumbprint, StoreName storeName, StoreLocation storeLocation)
{
	X509Store store = new X509Store(storeName, storeLocation);
	try
	{
		store.Open(OpenFlags.ReadOnly);

		var col = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
		if (col == null || col.Count == 0)
		{
			return null;
		}
		return col[0];
	}
	finally
	{
		store.Close();
	}
}

// Define other methods and classes here
public class MsalAuthenticationProvider : IAuthenticationProvider
{
	private IConfidentialClientApplication _clientApplication;
	private List<string> _scopes;

	public MsalAuthenticationProvider(IConfidentialClientApplication clientApplication, List<string> scopes)
	{
		_clientApplication = clientApplication;
		_scopes = scopes;
	}

	/// <summary>
	/// Update HttpRequestMessage with credentials
	/// </summary>
	public async Task AuthenticateRequestAsync(HttpRequestMessage request)
	{
		var token = await GetTokenAsync();
		request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
	}

	/// <summary>
	/// Acquire Token 
	/// </summary>
	public async Task<string> GetTokenAsync()
	{
		AuthenticationResult authResult = null;
		authResult = await _clientApplication.AcquireTokenForClient(_scopes)
							.ExecuteAsync();
		return authResult.AccessToken;
	}
}

public class AuthHandler : DelegatingHandler
{
	private IAuthenticationProvider _authenticationProvider;

	public AuthHandler(IAuthenticationProvider authenticationProvider, HttpMessageHandler innerHandler)
	{
		InnerHandler = innerHandler;
		_authenticationProvider = authenticationProvider;
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		await _authenticationProvider.AuthenticateRequestAsync(request);
		return await base.SendAsync(request, cancellationToken);
	}
}

public enum AuthenticationMode
{
	ClientSecret=1,
	Certificate=2
}