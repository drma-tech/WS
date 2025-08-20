﻿"use strict";

function share(url) {
    if (!("share" in navigator) || window.isSecureContext === false) {
        showError("Web Share API not supported.");
        return;
    }

    navigator
        .share({ url: url })
        .then(() => console.log("Successful share"))
        .catch(error => showError(error.message));
}

function jsSaveAsFile(filename, contentType, content) {
    // Create the URL
    const file = new File([content], filename, { type: contentType });
    const exportUrl = URL.createObjectURL(file);

    // Create the <a> element and click on it
    const a = document.createElement("a");
    document.body.appendChild(a);
    a.href = exportUrl;
    a.download = filename;
    a.target = "_self";
    a.click();

    // We don't need to keep the object URL, let's release the memory
    // On older versions of Safari, it seems you need to comment this line...
    URL.revokeObjectURL(exportUrl);
}

function GetLocalStorage(key) {
    return window.localStorage.getItem(key);
}

function SetLocalStorage(key, value) {
    if (typeof key !== "string" || typeof value !== "string") {
        showError("Key/value must be strings");
        return null;
    }
    return window.localStorage.setItem(key, value);
}

function TryDetectPlatform() {
    if (GetLocalStorage("platform")) return; //if populate before, cancel, cause detection (windows) only works for first call

    const isWindows = document.referrer == "app-info://platform/microsoft-store";
    const isAndroid = /(android)/i.test(navigator.userAgent);
    const isIOS = /iphone|ipad|ipod/i.test(navigator.userAgent);
    //const isMac = /macintosh|mac os x/i.test(navigator.userAgent);
    const isHuawei = /huawei|honor/i.test(navigator.userAgent);

    if (isWindows)
        SetLocalStorage("platform", "windows");
    else if (isAndroid)
        SetLocalStorage("platform", "play");
    else if (isIOS)
        SetLocalStorage("platform", "ios");
    else if (isHuawei)
        SetLocalStorage("platform", "huawei");
    else
        SetLocalStorage("platform", "webapp");
}

function showError(message) {
    if (window.DotNet) {
        try {
            DotNet.invokeMethodAsync("WS.WEB", "ShowError", message);
        }
        catch {
            showToast(message);
        }
    }
    else {
        showToast(message);
    }
}

function showToast(message) {
    const container = document.getElementById("error-container");
    if (!container) return;

    container.textContent = message;
    container.style.display = "block";

    setTimeout(() => {
        container.style.display = "none";
    }, 5000);
}

(function () {
    const theme = GetLocalStorage("theme") || "light";
    document.documentElement.setAttribute("data-bs-theme", theme);
})();

async function detectBrowserFeatures() {
    const [simd, bulkMemory, bigInt] = await Promise.all([
        wasmFeatureDetect.simd(),
        wasmFeatureDetect.bulkMemory(),
        wasmFeatureDetect.bigInt()
    ]);

    return simd && bulkMemory && bigInt;
}

function showBrowserWarning() {
    document.body.innerHTML = `
        <div style="display: flex; align-items: center; justify-content: center; min-height: 100vh; background: #f0f2f5; font-family: 'Segoe UI', Roboto, sans-serif; padding: 1rem;">
            <div style="background: #fff; padding: 0.6rem; border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.1); max-width: 460px; text-align: center; color: #333;">
                <div style="font-size: 2rem; margin-bottom: 0.5rem;">⚠️</div>
                <h2 style="font-size: 1.5rem; margin-bottom: 0.5rem;">Your browser needs an update</h2>
                <p style="font-size: 1rem; line-height: 1.6; margin-bottom: 0.5rem; text-align: justify;">
                    This app uses modern browser features. Your current browser version isn’t compatible. Even when installed from a store, this app runs inside your device’s built-in browser.
                </p>
                <ul style="list-style: none; padding: 0; margin: 0; font-size: 0.95rem; color: #555; text-align: left; padding-top: 0.5rem;">
                    <li style="margin: 0.5rem 0; text-align: center;">
                        <img src="https://cdn.jsdelivr.net/npm/simple-icons@v11/icons/googleplay.svg" alt="Play Store" width="20" style="margin-right: 4px;" />
                        <strong>Android</strong>: uses <strong>Chrome</strong>
                    </li>
                    <li style="margin: 0.5rem 0; text-align: center;">
                        <img src="https://cdn.jsdelivr.net/npm/simple-icons@v11/icons/apple.svg" alt="App Store" width="20" style="margin-right: 4px;" />
                        <strong>iOS/macOS</strong>: uses <strong>Safari</strong>
                    </li>
                    <li style="margin: 0.5rem 0; text-align: center;">
                        <img src="https://cdn.jsdelivr.net/npm/simple-icons@v11/icons/microsoftstore.svg" alt="Microsoft Store" width="20" style="margin-right: 4px;" />
                        <strong>Windows</strong>: uses <strong>Edge</strong>
                    </li>
                </ul>
            </div>
        </div>
    `;
}