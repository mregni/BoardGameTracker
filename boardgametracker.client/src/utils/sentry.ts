import * as Sentry from "@sentry/react";

import { setReactErrorReporter } from "./reactErrorReporter";
import { scrubBreadcrumb, scrubEvent } from "./sentryScrub";

let initialized = false;

export const initSentry = () => {
	if (initialized) return;
	initialized = true;

	Sentry.init({
		dsn: import.meta.env.VITE_SENTRY_DSN,
		sendDefaultPii: false,
		ignoreErrors: ["Transition was skipped", "Transition was aborted because of invalid state"],
		beforeSend: scrubEvent,
		beforeBreadcrumb: scrubBreadcrumb,
	});

	Sentry.addEventProcessor(scrubEvent);
	setReactErrorReporter(Sentry.reactErrorHandler());
};
