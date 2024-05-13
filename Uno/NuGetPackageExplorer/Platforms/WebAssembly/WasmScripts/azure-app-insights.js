define([`${config.uno_app_base}/ai.2.6.3.min.js`], m => {
    var snippet = {
        config: {
            instrumentationKey: config.environmentVariables['NPE_AI_INSTRUMENTATIONKEY'] || "CHANGE_ME",
        }
    };
    var init = new m.ApplicationInsights(snippet);;
    appInsights = init.loadAppInsights();
});