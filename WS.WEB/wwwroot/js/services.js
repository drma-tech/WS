"use strict";

import { storage, notification } from "./utils.js";

export const services = {
    initGoogleAnalytics(version) {
        if (window.appConfig?.isLocalhost === true) return;
        if (window.appConfig?.isDev === true) return;

        const PLATFORM = storage.getLocalStorage("platform");

        window.dataLayer = window.dataLayer || [];
        function gtag() {
            window.dataLayer.push(arguments);
        }
        gtag("js", new Date());

        const config = {
            app_version: version,
            platform: PLATFORM,
        };

        gtag("config", window.appConfig?.servicesConfig?.AnalyticsCode, config);
    },
    initMicrosoftClarity(code) {
        if (window.appConfig?.isLocalhost === true) return;
        if (window.appConfig?.isDev === true) return;

        (function (c, l, a, r, i, t, y) {
            c[a] = c[a] || function () { (c[a].q = c[a].q || []).push(arguments) };
            t = l.createElement(r); t.async = 1; t.src = "https://www.clarity.ms/tag/" + i;
            y = l.getElementsByTagName(r)[0]; y.parentNode.insertBefore(t, y);
        })(window, document, "clarity", "script", code);

        //todo: whem implement tracking consent, modify this to wait for user consent
        let attempts = 0;
        const maxAttempts = 12;
        const clarityCheckInterval = setInterval(function () {
            if (window.clarity) {
                clearInterval(clarityCheckInterval);
                window.clarity("consentv2", {
                    ad_Storage: "granted",
                    analytics_Storage: "granted"
                });
                return;
            }

            attempts++;
            if (attempts >= maxAttempts) clearInterval(clarityCheckInterval);
        }, 5000);
    },
    initUserJot() {
        if (window.appConfig?.isBot === true) return;
        if (window.appConfig?.blazorSupported !== true) return;

        const browserLang = navigator.language || navigator.userLanguage;

        window.uj ||= new Proxy(
            {},
            {
                get: (_, property) =>
                    property == "then"
                        ? 0
                        : (...args) => {
                            (window.$ujq ||= []).push([property, ...args]);
                        }
            }
        );
        window.uj.init(window.appConfig?.servicesConfig?.UserJotToken, {
            widget: {
                position: 'left', // 'left' or 'right'
                theme: 'auto', // 'auto', 'light', or 'dark'
            },
            locale: storage.getLocalStorage("app-language") ?? browserLang.slice(0, 2),
        });

        const script = document.createElement("script");
        script.type = "module";
        script.async = true;
        script.src = "https://cdn.userjot.com/sdk/v3/uj.js";
        (document.head || document.body).appendChild(script);
    },
    initAdSense(adClient, adSlot, containerId) {
        if (window.appConfig?.isBot === true) return;
        if (window.appConfig?.isLocalhost === true) return;
        if (window.appConfig?.isDev === true) return;
        if (window.appConfig?.blazorSupported !== true) return;

        try {
            const container = document.getElementById(containerId);
            if (!container) return;

            container.innerHTML = ""; // remove old ad

            const ins = document.createElement("ins");
            ins.className = "adsbygoogle custom-ad";
            ins.setAttribute("data-ad-client", adClient);
            ins.setAttribute("data-ad-slot", adSlot);
            ins.setAttribute("data-ad-format", "auto");
            ins.setAttribute('data-full-width-responsive', false);
            container.appendChild(ins);

            (window.adsbygoogle = window.adsbygoogle || []).push({});
        } catch (error) {
            Sentry.captureException(error);
            notification.showError(error.message);
        }
    },
    initYandex(id) {
        if (window.appConfig?.isBot === true) return;
        if (window.appConfig?.isLocalhost === true) return;
        if (window.appConfig?.isDev === true) return;
        if (window.appConfig?.blazorSupported !== true) return;

        window.yaContextCb = window.yaContextCb || [];
        window.yaContextCb.push(() => {
            Ya.Context.AdvManager.render({
                "blockId": id,
                "renderTo": "yandex_rtb_" + id
            })
        });
    }
};

services.initMicrosoftClarity(window.appConfig?.servicesConfig?.ClarityKey);
