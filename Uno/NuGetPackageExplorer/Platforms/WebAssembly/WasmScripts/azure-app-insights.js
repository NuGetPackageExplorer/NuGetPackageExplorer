let appInsights = null;

define([`${config.uno_app_base}/ai.3.3.6.min.js`], () => {
    const key = (config.environmentVariables['NPE_AI_INSTRUMENTATIONKEY'] || '').trim();
    if (!key) {
        return;
    }

    const snippet = {
        config: {
            disableTelemetry: false
        }
    };

    if (key.includes('=')) {
        snippet.config.connectionString = key;
    } else {
        snippet.config.instrumentationKey = key;
    }

    try {
        const init = new Microsoft.ApplicationInsights.ApplicationInsights(snippet);
        appInsights = init.loadAppInsights();
    } catch (error) {
        console.warn('Application Insights disabled:', error);
    }
});
