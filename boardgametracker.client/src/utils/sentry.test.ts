import type { BrowserOptions, ErrorEvent, EventHint } from "@sentry/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const sentryMocks = vi.hoisted(() => ({
	init: vi.fn(),
	addEventProcessor: vi.fn(),
	reactErrorHandler: vi.fn(() => vi.fn()),
}));

vi.mock("@sentry/react", () => sentryMocks);

const loadOptions = async () => {
	vi.resetModules();
	const { initSentry } = await import("./sentry");
	initSentry();
	return sentryMocks.init.mock.calls[0][0] as BrowserOptions;
};

describe("initSentry", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("should initialise sentry only once", async () => {
		vi.resetModules();
		const { initSentry } = await import("./sentry");

		initSentry();
		initSentry();

		expect(sentryMocks.init).toHaveBeenCalledTimes(1);
		expect(sentryMocks.addEventProcessor).toHaveBeenCalledTimes(1);
	});

	it("should scrub oidc codes from error events", async () => {
		const options = await loadOptions();
		const event: ErrorEvent = {
			type: undefined,
			request: { url: "https://bgt.example.com/auth-callback?code=abc&state=def&lang=en" },
		};

		const result = (await options.beforeSend?.(event, {} as EventHint)) as ErrorEvent;

		expect(result.request?.url).toBe("https://bgt.example.com/auth-callback?code=[Filtered]&state=[Filtered]&lang=en");
	});

	it("should not enable tracing or session replay", async () => {
		const options = await loadOptions();

		expect(options.integrations).toBeUndefined();
		expect(options.tracesSampleRate).toBeUndefined();
		expect(options.replaysSessionSampleRate).toBeUndefined();
		expect(options.replaysOnErrorSampleRate).toBeUndefined();
	});

	it("should scrub navigation breadcrumbs", async () => {
		const options = await loadOptions();

		const result = options.beforeBreadcrumb?.({
			category: "navigation",
			data: { from: "/login", to: "/auth-callback?code=abc&state=def" },
		});

		expect(result?.data).toEqual({ from: "/login", to: "/auth-callback?code=[Filtered]&state=[Filtered]" });
	});

	it("should report errors caught by react error boundaries", async () => {
		vi.resetModules();
		const reporter = vi.fn();
		sentryMocks.reactErrorHandler.mockReturnValueOnce(reporter);
		const { reportReactError } = await import("./reactErrorReporter");
		const { initSentry } = await import("./sentry");
		const error = new Error("render failed");
		const errorInfo = { componentStack: "at Dashboard" };
		vi.spyOn(console, "error").mockImplementation(() => {});

		initSentry();
		reportReactError(error, errorInfo);

		expect(reporter).toHaveBeenCalledWith(error, errorInfo);
	});

	it("should register the scrubber as event processor", async () => {
		await loadOptions();

		const processor = sentryMocks.addEventProcessor.mock.calls[0][0] as (event: ErrorEvent) => ErrorEvent;
		expect(processor({ type: undefined, transaction: "/auth-callback?code=abc" }).transaction).toBe(
			"/auth-callback?code=[Filtered]",
		);
	});
});
