// Google Analytics
window.initGoogleAnalytics = function (code, version) {
    SetLocalStorage("app-version", version);
    const PLATFORM = GetLocalStorage("platform");

    if (!window.location.host.includes("localhost")) {
        window.dataLayer = window.dataLayer || [];
        function gtag() { dataLayer.push(arguments); }
        gtag("js", new Date());

        const config = {
            'app_version': version,
            'platform': PLATFORM
        };

        gtag("config", code, config);
    }
}

// Microsoft Clarity
window.initClarity = function (code) {
    if (!window.location.host.includes("localhost")) {
        (function (c, l, a, r, i, t, y) {
            c[a] = c[a] || function () { (c[a].q = c[a].q || []).push(arguments) };
            t = l.createElement(r); t.async = 1; t.src = "https://www.clarity.ms/tag/" + i;
            y = l.getElementsByTagName(r)[0]; y.parentNode.insertBefore(t, y);
        })(window, document, "clarity", "script", code);

        // Check if Clarity is loaded and call the consent function
        const clarityCheckInterval = setInterval(function () {
            if (window.clarity) {
                window.clarity("consent");
                clearInterval(clarityCheckInterval);
            }
        }, 5000);
    }
}

// userback
window.initUserBack = function () {
    window.Userback = window.Userback || {};
    Userback.access_token = "A-A2J4M5NKCbDp1QyQe7ogemmmq";
    (function (d) {
        var s = d.createElement('script'); s.async = true; s.src = 'https://static.userback.io/widget/v1.js?vs=202511'; (d.head || d.body).appendChild(s);
    })(document);
    const browserLang = navigator.language || navigator.userLanguage;
    Userback.widget_settings = {
        language: GetLocalStorage("language") ?? browserLang.slice(0, 2),
        logo: window.location.origin + "/icon/icon-71.png"
    };
    Userback.custom_data = {
        platform: GetLocalStorage("platform"),
        app_version: GetLocalStorage("app-version")
    };
    //Userback.on_load = () => {
    //    getUserInfo()
    //        .then(user => {
    //            if (user) {
    //                Userback.identify(user.userId, {
    //                    name: user.name,
    //                    email: user.email
    //                });
    //            }
    //        })
    //        .catch(error => {
    //            showError(error.message);
    //        });
    //};
    //Userback.on_survey_submit = (obj) => {
    //    if (obj.key == "mjj9Ta") {
    //        let rating = obj.data[0].question_answer;
    //        SetLocalStorage("survey-rating", rating);
    //    }
    //};
}

// adsense
window.createAd = function (adClient, adSlot, adFormat, containerId) {
    try {
        const container = document.getElementById(containerId);
        if (!container) return;

        container.innerHTML = ""; // remove old ad

        const isMobile = window.innerWidth <= 600 || window.innerHeight <= 600;

        const ins = document.createElement('ins');
        ins.className = 'adsbygoogle ' + (isMobile ? 'custom-ad-mobile' : 'custom-ad');
        ins.setAttribute('data-ad-client', adClient);
        ins.setAttribute('data-ad-slot', adSlot);
        if (!isMobile) ins.setAttribute('data-ad-format', adFormat); //on mobile, adsense doesnt respect horizontal format
        //ins.setAttribute('data-full-width-responsive', true); //this forces it to take up half the screen
        container.appendChild(ins);

        (adsbygoogle = window.adsbygoogle || []).push({});
    } catch (error) {
        sendLog(error);
        showError(error.message);
    }
};