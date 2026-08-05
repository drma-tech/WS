using Microsoft.JSInterop;

namespace WS.WEB.Core.Helper.Javascript
{
    public class ServicesJs(IJSRuntime js) : JsModuleBase(js, "./js/services.js")
    {
        public Task InitGoogleAnalytics(string version, CancellationToken cancellationToken) => InvokeVoid("services.initGoogleAnalytics", cancellationToken, version);

        public Task InitUserBack(string version, CancellationToken cancellationToken) => InvokeVoid("services.initUserBack", cancellationToken, version);

        public Task InitAdSense(string adClient, string adSlot, string containerId, CancellationToken cancellationToken) => InvokeVoid("services.initAdSense", cancellationToken, adClient, adSlot, containerId);

        public Task InitYandex(string id, CancellationToken cancellationToken) => InvokeVoid("services.initYandex", cancellationToken, id);
    }
}