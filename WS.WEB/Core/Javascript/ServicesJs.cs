using Microsoft.JSInterop;

namespace WS.WEB.Core.Javascript
{
    public class ServicesJs(IJSRuntime js) : JsModuleBase(js, "./js/services.js")
    {
        public Task InitGoogleAnalytics(string version, CancellationToken cancellationToken) => InvokeVoid("services.initGoogleAnalytics", cancellationToken, version);

        public Task InitUserJot(CancellationToken cancellationToken) => InvokeVoid("services.initUserJot", cancellationToken);

        public Task InitAdSense(string adClient, string adSlot, string containerId, CancellationToken cancellationToken) => InvokeVoid("services.initAdSense", cancellationToken, adClient, adSlot, containerId);

        public Task InitYandex(string id, CancellationToken cancellationToken) => InvokeVoid("services.initYandex", cancellationToken, id);
    }
}
