import * as Sentry from "@sentry/react";

import { scrubBreadcrumb, scrubEvent, scrubRecordingEvent } from "./sentryScrub";

let initialized = false;

export const initSentry = () => {
	if (initialized) return;
	initialized = true;

	Sentry.init({
		dsn: import.meta.env.VITE_SENTRY_DSN,
		integrations: [
			Sentry.browserTracingIntegration(),
			Sentry.replayIntegration({ beforeAddRecordingEvent: scrubRecordingEvent }),
		],
		tracesSampleRate: 0.2,
		tracePropagationTargets: [/^\/api/],
		replaysSessionSampleRate: 0.1,
		replaysOnErrorSampleRate: 1,
		sendDefaultPii: false,
		beforeSend: scrubEvent,
		beforeSendTransaction: scrubEvent,
		beforeBreadcrumb: scrubBreadcrumb,
	});

	Sentry.addEventProcessor(scrubEvent);
};
