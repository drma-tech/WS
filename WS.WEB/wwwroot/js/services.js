"use strict";

import { isBot, isLocalhost, isDev, servicesConfig } from "./main.js";
import { storage, notification } from "./utils.js";

export const services = {
    initGoogleAnalytics(version) {
        if (isBot) return;

        if (isLocalhost) return;

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

        gtag("config", servicesConfig.AnalyticsCode, config);
    },
    initMicrosoftClarity(code) {
        if (isBot) return;
        if (isLocalhost) return;

        (function (c, l, a, r, i, t, y) {
            c[a] = c[a] || function () { (c[a].q = c[a].q || []).push(arguments) };
            t = l.createElement(r); t.async = 1; t.src = "https://www.clarity.ms/tag/" + i;
            y = l.getElementsByTagName(r)[0]; y.parentNode.insertBefore(t, y);
        })(window, document, "clarity", "script", code);

        //todo: whem implement tracking consent, modify this to wait for user consent
        const clarityCheckInterval = setInterval(function () {
            if (window.clarity) {
                window.clarity("consent");
                clearInterval(clarityCheckInterval);
            }
        }, 5000);
    },
    initUserBack(version) {
        if (isBot) return;

        const browserLang = navigator.language || navigator.userLanguage;

        window.Userback = window.Userback || {};

        window.Userback.access_token = servicesConfig.UserBackToken;

        window.Userback.widget_settings = {
            language: storage.getLocalStorage("language") ?? browserLang.slice(0, 2),
            logo: location.origin + "/icon/icon-71.png",
        };
        window.Userback.custom_data = {
            platform: storage.getLocalStorage("platform"),
            app_version: version,
        };
        window.Userback.on_survey_submit = (obj) => {
            if (obj.key === servicesConfig.UserBackSurveyKey) {
                let rating = obj.data[0].question_answer;
                storage.setLocalStorage("survey-rating", rating);
            }
        };
    },
    initAdSense(adClient, adSlot, adFormat, containerId) {
        if (isBot) return;
        if (isLocalhost) return;
        if (isDev) return;

        try {
            const container = document.getElementById(containerId);
            if (!container) return;

            container.innerHTML = ""; // remove old ad

            const isMobile = window.innerWidth <= 600 || window.innerHeight <= 600;

            const ins = document.createElement("ins");
            ins.className = "adsbygoogle " + (isMobile ? "custom-ad-mobile" : "custom-ad");
            ins.setAttribute("data-ad-client", adClient);
            ins.setAttribute("data-ad-slot", adSlot);
            if (!isMobile) ins.setAttribute("data-ad-format", adFormat); //on mobile, adsense doesnt respect horizontal format
            //ins.setAttribute('data-full-width-responsive', true); //this forces it to take up half the screen
            container.appendChild(ins);

            (window.adsbygoogle = window.adsbygoogle || []).push({});
        } catch (error) {
            notification.sendLog(error);
            notification.showError(error.message);
        }
    },
};

services.initMicrosoftClarity(servicesConfig.ClarityKey);
