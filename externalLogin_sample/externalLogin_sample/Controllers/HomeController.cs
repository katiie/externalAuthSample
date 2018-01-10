using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace Sample_app.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient client;
        public string Google_clientId;
        public string Facebook_clientId;
        public string Google_clientSecret;
        public string Facebook_clientSecret;
        public HomeController()
        {
            client = new HttpClient();

            //the values are gotten from the web config for security purpose
            // replace with your app id and secret respectively
            Facebook_clientId = WebConfigurationManager.AppSettings["Facebook_clientId"];
            Facebook_clientSecret = WebConfigurationManager.AppSettings["Facebook_clientSecret"];

           

            //replace with google app id and secret key
            Google_clientSecret = WebConfigurationManager.AppSettings["Google_clientSecret"];
            Google_clientId = WebConfigurationManager.AppSettings["Google_clientId"];

        }
       

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        private Uri RedirectUri
        {
            get
            {
                var uriBuilder = new UriBuilder(Request.Url);
                uriBuilder.Query = null;
                uriBuilder.Fragment = null;
                uriBuilder.Path = Url.Action("CustomOauthCallback");

                return uriBuilder.Uri;
            }

        }

        #region google Oauth
        private Uri googleOauthUrl
        {

            get
            {
                var scope = "email";
                var uriBuilder = new UriBuilder();
                uriBuilder.Scheme = "https";
                uriBuilder.Host = "accounts.google.com";
                uriBuilder.Path = "o/oauth2/auth";
                uriBuilder.Query = "redirect_uri=" + RedirectUri.AbsoluteUri + "&response_type=code" + "&client_id=" + Google_clientId + "&scope=" + scope;
                return uriBuilder.Uri;
            }
        }
        #endregion
        #region Facebook Oauth
        private Uri facebookOauthUrl
        {
            get
            {
                var scope = "email";
                var uriBuilder = new UriBuilder();
                uriBuilder.Scheme = "https";
                uriBuilder.Host = "facebook.com";
                uriBuilder.Path = "dialog/oauth";
                uriBuilder.Query = "redirect_uri=" + RedirectUri.AbsoluteUri + "&response_type=code" + "&client_id=" + Facebook_clientId + "&scope=" + scope;
                return uriBuilder.Uri;
            }
        }

        #endregion

        [AllowAnonymous]
        public ActionResult CustomLogin(string provider)
        {
            //save type either Facebook or Google
            Session["provider"] = provider;
            if (provider.Contains("Facebook"))
            {
                //redirect to facebook for Login
                return Redirect(facebookOauthUrl.AbsoluteUri);
            }
            else if (provider.Contains("Google"))
            {
                //redirect to Google for Login
                return Redirect(googleOauthUrl.AbsoluteUri);
            }
            else
            {
                return HttpNotFound();
            }
        }
        [AllowAnonymous]
        public async Task<ActionResult> CustomOauthCallback(string code)
        {
            string Type, BaseAddress = string.Empty;
            string UserPermissionaddress = string.Empty;
            string ClientId = string.Empty;
            string ClientSecret = string.Empty;
            Dictionary<string, string> urlContent = new Dictionary<string, string>();
            Dictionary<string, string> accessTokenContent = new Dictionary<string, string>();
            Type = Session["provider"] != null ? Session["provider"].ToString() : string.Empty;
            
            if (Type.ToString().Contains("Google"))
            {
                ClientSecret = Google_clientSecret;
                ClientId = Google_clientId;
                BaseAddress = "https://accounts.google.com/o/oauth2/token";
                UserPermissionaddress = "https://www.googleapis.com/oauth2/v2/userinfo?access_token=";
            }
            else if (Type.ToString().Contains("Facebook"))
            {
                ClientId = Facebook_clientId;
                ClientSecret = Facebook_clientSecret;
                BaseAddress = "https://graph.facebook.com/v2.11/oauth/access_token";
                UserPermissionaddress = "https://graph.facebook.com/me?fields=id,first_name,last_name,email,picture";
            }
            //build url address
            urlContent = new Dictionary<string, string>
                {
                    {"code", code },
                    { "redirect_uri",RedirectUri.AbsoluteUri},
                    { "client_id",ClientId},
                    { "client_secret",ClientSecret},
                    { "grant_type", "authorization_code" }
                };
            //get the access token
            var EncodedFormData = new FormUrlEncodedContent(urlContent);
            var response = await client.PostAsync(BaseAddress, EncodedFormData);
            string responseString = await response.Content.ReadAsStringAsync();
            AccessTokenClass result = JsonConvert.DeserializeObject<AccessTokenClass>(responseString);
            accessTokenContent = new Dictionary<string, string> { { "access_token", result.access_token } };
            var EncodedAccessTokenData = new FormUrlEncodedContent(accessTokenContent);

            //create a post to handle permission request. Here we creating one to get the email address
            var response_ = Type.Contains("Facebook") ? await client.PostAsync(UserPermissionaddress, EncodedAccessTokenData) :
                await client.GetAsync(UserPermissionaddress + result.access_token);
            //string responseString_ = await response_.Content.ReadAsStringAsync();;
            string responseString_ = await response_.Content.ReadAsStringAsync();
            dynamic authresult = JsonConvert.DeserializeObject<dynamic>(responseString_.ToString());
            ViewBag.FullName = Type.Contains("Facebook") ? authresult.first_name + " ," + authresult.last_name : authresult.name;
            ViewBag.Email = authresult.email; 
            return View();
        }
    }
    #region reponsedetails
    public class AccessTokenClass
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }

        public string Scope { get; set; }
        public string refresh_token { get; set; }

        public string xoauth_yahoo_guid { get; set; }
    }
    #endregion
}