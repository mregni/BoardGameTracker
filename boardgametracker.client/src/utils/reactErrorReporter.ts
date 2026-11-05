import type { ErrorInfo } from "react";

type ReactErrorReporter = (error: unknown, errorInfo: ErrorInfo) => void;

const pending: [unknown, ErrorInfo][] = [];
let reporter: ReactErrorReporter | undefined;

export const setReactErrorReporter = (next: ReactErrorReporter) => {
	reporter = next;
	for (const [error, errorInfo] of pending.splice(0)) {
		next(error, errorInfo);
	}
};

export const reportReactError: ReactErrorReporter = (error, errorInfo) => {
	// biome-ignore lint/suspicious/noConsole: keeps React's default logging of caught errors
	console.error(error);
	if (reporter) {
		reporter(error, errorInfo);
	} else {
		pending.push([error, errorInfo]);
	}
};
