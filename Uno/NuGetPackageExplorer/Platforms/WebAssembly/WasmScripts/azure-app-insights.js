define([`${config.uno_app_base}/ai.3.3.6.min.js`], () => {

    // disable telemetry if the instrumentation key is not set so we don't get bad requests
    const key = config.environmentVariables['NPE_AI_INSTRUMENTATIONKEY'] || null;
    var snippet = {
        config: {
            connectionString: key || 'InstrumentationKey=00000000-0000-0000-0000-000000000000',
            disableTelemetry: !key
        }
    };
    
    var init = new Microsoft.ApplicationInsights.ApplicationInsights(snippet);
    appInsights = init.loadAppInsights();
});