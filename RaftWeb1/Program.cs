using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RaftWeb1;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<GatewayService>();
builder.Services.AddSingleton<Productservice>();
builder.Services.AddSingleton<OrderService>();
builder.Services.AddSingleton<OrderProcessingService>();


var baseAddress = builder.HostEnvironment.IsDevelopment() || builder.HostEnvironment.BaseAddress.Contains("localhost")
    ? "http://localhost:2010"
    : "http://100.112.33.3:2010";

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });

await builder.Build().RunAsync();
