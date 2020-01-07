function getAppSetting(key) {
    if (typeof appSettings === 'undefined') {
        alert("AppSettings not available");
        return null;
    }
    else {
        return appSettings[key];
    }
}