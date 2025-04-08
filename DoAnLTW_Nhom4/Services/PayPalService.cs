using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using System.Text.Json;

public class PayPalService
{
    private readonly IConfiguration _configuration;

    public PayPalService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private PayPalEnvironment GetEnvironment()
    {
        var clientId = _configuration["PayPal:ClientId"];
        var secret = _configuration["PayPal:Secret"];
        var mode = _configuration["PayPal:Mode"];

        return mode == "live"
            ? new LiveEnvironment(clientId, secret)
            : new SandboxEnvironment(clientId, secret);
    }

    private PayPalHttpClient GetClient() => new PayPalHttpClient(GetEnvironment());

    public async Task<string> CreateOrder(decimal? amount)
    {
        var request = new OrdersCreateRequest();
        request.Prefer("return=representation");
        request.RequestBody(new OrderRequest
        {
            CheckoutPaymentIntent = "CAPTURE",
            PurchaseUnits = new List<PurchaseUnitRequest>
            {
                new PurchaseUnitRequest
                {
                    AmountWithBreakdown = new AmountWithBreakdown
                    {
                        CurrencyCode = "USD",
                        Value = amount?.ToString("F2")
                    }
                }
            },
            ApplicationContext = new ApplicationContext
            {
                ReturnUrl = _configuration["PayPal:ReturnUrl"],
                CancelUrl = _configuration["PayPal:CancelUrl"]
            }
        });

        var response = await GetClient().Execute(request);
        var statusCode = response.StatusCode;
        var result = response.Result<Order>();

        var approvalLink = result.Links.FirstOrDefault(x => x.Rel == "approve")?.Href;
        return approvalLink;
    }

    public async Task<Order> CaptureOrder(string orderId)
    {
        var request = new OrdersCaptureRequest(orderId);
        request.RequestBody(new OrderActionRequest());
        var response = await GetClient().Execute(request);
        return response.Result<Order>();
    }
}
