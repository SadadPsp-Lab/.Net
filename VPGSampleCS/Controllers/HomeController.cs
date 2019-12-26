using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using VPGSampleCS.Models;

namespace VPGSampleCS.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            PaymentRequest model = new PaymentRequest();
            if (Request.Cookies.AllKeys.Contains("Data") && Request.Cookies["Data"] != null)
            {
                var cookie = Request.Cookies["Data"].Value;
                model = JsonConvert.DeserializeObject<PaymentRequest>(cookie);
            }
            if (model.MultiplexingData == null) model.MultiplexingData = new MultiplexingData();
            return View(model);
        }

        [HttpPost]
        public ActionResult Index(PaymentRequest request)
        {
            if (!ModelState.IsValid)
                return View(request);
            try
            {
                request.OrderId = new Random().Next(1000, int.MaxValue).ToString();
                var dataBytes = Encoding.UTF8.GetBytes(string.Format("{0};{1};{2}", request.TerminalId, request.OrderId, request.Amount));

                var symmetric = SymmetricAlgorithm.Create("TripleDes");
                symmetric.Mode = CipherMode.ECB;
                symmetric.Padding = PaddingMode.PKCS7;

                var encryptor = symmetric.CreateEncryptor(Convert.FromBase64String(request.MerchantKey), new byte[8]);

                request.SignData = Convert.ToBase64String(encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length));

                if (HttpContext.Request.Url != null)
                    request.ReturnUrl = string.Format("{0}://{1}{2}Home/Verify", Request.Url.Scheme, Request.Url.Authority, Url.Content("~"));

                var ipgUri = string.Format("{0}/api/v0/Request/PaymentRequest", request.PurchasePage);


                HttpCookie merchantTerminalKeyCookie = new HttpCookie("Data", JsonConvert.SerializeObject(request));
                Response.Cookies.Add(merchantTerminalKeyCookie);

                var data = new
                {
                    request.TerminalId,
                    request.MerchantId,
                    request.Amount,
                    request.SignData,
                    request.ReturnUrl,
                    LocalDateTime = DateTime.Now,
                    request.OrderId,
                    //MultiplexingData = request.MultiplexingData
                };

                var res = CallApi<PayResultData>(ipgUri, data);
                res.Wait();

                if (res != null && res.Result != null)
                {
                    if (res.Result.ResCode == "0")
                    {
                        Response.Redirect(string.Format("{0}/Purchase/Index?token={1}", request.PurchasePage, res.Result.Token));
                    }
                    ViewBag.Message = res.Result.Description;
                    return View(); ;
                }

            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.ToString();
            }
            return View();
        }

        [HttpPost]
        public ActionResult Verify(PurchaseResult result)
        {
            return View(result);
        }

        [HttpPost]
        public ActionResult VerifyRequest(PurchaseResult result)
        {
            try
            {
                var cookie = Request.Cookies["Data"].Value;
                var model = JsonConvert.DeserializeObject<PaymentRequest>(cookie);

                var dataBytes = Encoding.UTF8.GetBytes(result.Token);

                var symmetric = SymmetricAlgorithm.Create("TripleDes");
                symmetric.Mode = CipherMode.ECB;
                symmetric.Padding = PaddingMode.PKCS7;

                var encryptor = symmetric.CreateEncryptor(Convert.FromBase64String(model.MerchantKey), new byte[8]);

                var signedData = Convert.ToBase64String(encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length));

                var data = new
                {
                    token = result.Token,
                    SignData = signedData
                };

                var ipgUri = string.Format("{0}/api/v0/Advice/Verify", model.PurchasePage);

                var res = CallApi<VerifyResultData>(ipgUri, data);
                if (res != null && res.Result != null)
                {
                    if (res.Result.ResCode == "0")
                    {
                        result.VerifyResultData = res.Result;
                        res.Result.Succeed = true;
                        ViewBag.Success = res.Result.Description;
                        return View("Verify", result);
                    }
                    ViewBag.Message = res.Result.Description;
                    return View("Verify");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.ToString();
            }

            return View("Verify", result);
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public static async Task<T> CallApi<T>(string apiUrl, object value)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
            using (var client = new HttpClient())
            {

                client.BaseAddress = new Uri(apiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                var w = client.PostAsJsonAsync(apiUrl, value);
                w.Wait();
                HttpResponseMessage response = w.Result;
                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsAsync<T>();
                    result.Wait();
                    return result.Result;
                }
                return default(T);
            }
        }
    }
}