(function () {
    const loginPath = "/api/auth/login";
    const originalFetch = window.fetch;

    function tryAuthorizeFromResponse(url, method, response) {
        if (!url.includes(loginPath) || method !== "POST" || !response.ok) {
            return;
        }

        response.clone().json().then((payload) => {
            const token = payload?.token;
            if (!token || !window.ui?.authActions) {
                return;
            }

            window.ui.authActions.authorize({
                Bearer: {
                    name: "Bearer",
                    schema: {
                        type: "http",
                        in: "header",
                        name: "Authorization",
                        scheme: "bearer",
                        bearerFormat: "JWT"
                    },
                    value: token
                }
            });
        });
    }

    window.fetch = async (...args) => {
        const response = await originalFetch(...args);
        const request = args[0];
        const requestOptions = args[1];

        const url = typeof request === "string" ? request : request.url;
        const method = (requestOptions?.method || request?.method || "GET").toUpperCase();

        tryAuthorizeFromResponse(url, method, response);
        return response;
    };
})();
