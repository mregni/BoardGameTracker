import * as Sentry from "@sentry/react";

let initialized = false;

export const initSentry = () => {
	if (initialized) return;
	initialized = true;

	Sentry.init({
		dsn: import.meta.env.VITE_SENTRY_DSN,
		integrations: [Sentry.browserTracingIntegration(), Sentry.replayIntegration()],
		tracesSampleRate: 0.2,
		tracePropagationTargets: [/^\/api/],
		replaysSessionSampleRate: 0.1,
		replaysOnErrorSampleRate: 1,
		sendDefaultPii: false,
	});
};
