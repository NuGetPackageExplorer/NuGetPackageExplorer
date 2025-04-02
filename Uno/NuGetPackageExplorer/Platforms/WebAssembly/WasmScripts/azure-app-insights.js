define([`${config.uno_app_base}/ai.3.gbl.min.js`], m => {
    var snippet = {
        config: {
            connectionString: config.environmentVariables['NPE_AI_INSTRUMENTATIONKEY'] || "CHANGE_ME",
        }
    };
    var init = new m.ApplicationInsights(snippet);;
    appInsights = init.loadAppInsights();
});