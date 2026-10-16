import type { BrowserOptions, ErrorEvent, EventHint } from "@sentry/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const sentryMocks = vi.hoisted(() => ({
	init: vi.fn(),
	addEventProcessor: vi.fn(),
	browserTracingIntegration: vi.fn(() => ({ name: "BrowserTracing" })),
	replayIntegration: vi.fn((options: unknown) => ({ name: "Replay", options })),
}));

vi.mock("@sentry/react", () => sentryMocks);

type TransactionEvent = Parameters<NonNullable<BrowserOptions["beforeSendTransaction"]>>[0];

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

	it("should scrub invite link ids from transactions", async () => {
		const options = await loadOptions();
		const event: TransactionEvent = {
			type: "transaction",
			transaction: "/rsvp",
			request: { url: "https://bgt.example.com/rsvp?linkId=abc" },
		};

		const result = (await options.beforeSendTransaction?.(event, {} as EventHint)) as TransactionEvent;

		expect(result.transaction).toBe("/rsvp");
		expect(result.request?.url).toBe("https://bgt.example.com/rsvp?linkId=[Filtered]");
	});

	it("should scrub navigation breadcrumbs", async () => {
		const options = await loadOptions();

		const result = options.beforeBreadcrumb?.({
			category: "navigation",
			data: { from: "/login", to: "/auth-callback?code=abc&state=def" },
		});

		expect(result?.data).toEqual({ from: "/login", to: "/auth-callback?code=[Filtered]&state=[Filtered]" });
	});

	it("should register the scrubber as event processor and replay hook", async () => {
		await loadOptions();

		const processor = sentryMocks.addEventProcessor.mock.calls[0][0] as (event: ErrorEvent) => ErrorEvent;
		expect(processor({ type: undefined, transaction: "/auth-callback?code=abc" }).transaction).toBe(
			"/auth-callback?code=[Filtered]",
		);

		const replayOptions = sentryMocks.replayIntegration.mock.calls[0][0] as {
			beforeAddRecordingEvent: (event: unknown) => unknown;
		};
		expect(replayOptions.beforeAddRecordingEvent({ type: 4, data: { href: "/rsvp?linkId=abc" } })).toEqual({
			type: 4,
			data: { href: "/rsvp?linkId=[Filtered]" },
		});
	});
});
