import type { ErrorEvent, Event } from "@sentry/react";
import { describe, expect, it } from "vitest";

import { FILTERED, scrubBreadcrumb, scrubEvent, scrubRecordingEvent, scrubUrl, scrubValue } from "./sentryScrub";

const LINK_ID = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";

describe("sentryScrub", () => {
	describe("scrubUrl", () => {
		it.each([
			[
				"https://bgt.example.com/api/auth/oidc/callback?code=abc&state=xyz",
				"https://bgt.example.com/api/auth/oidc/callback?code=[Filtered]&state=[Filtered]",
			],
			["/auth-callback?code=abc&state=def&page=2", "/auth-callback?code=[Filtered]&state=[Filtered]&page=2"],
			[`/rsvp?linkId=${LINK_ID}`, "/rsvp?linkId=[Filtered]"],
			["?CODE=abc", "?CODE=[Filtered]"],
			["code=abc&session_state=s1", "code=[Filtered]&session_state=[Filtered]"],
			[
				"/cb?token=t&id_token=i&access_token=a&refresh_token=r",
				"/cb?token=[Filtered]&id_token=[Filtered]&access_token=[Filtered]&refresh_token=[Filtered]",
			],
			[
				"/x?password=p&client_secret=s&api_key=k&apiKey=k2",
				"/x?password=[Filtered]&client_secret=[Filtered]&api_key=[Filtered]&apiKey=[Filtered]",
			],
			["/x#access_token=a&state=s", "/x#access_token=[Filtered]&state=[Filtered]"],
			[`/api/gamenight/link/${LINK_ID}/rsvp`, "/api/gamenight/link/[Filtered]/rsvp"],
			[`/api/manual/gamenight/${LINK_ID}/manual/4/download`, "/api/manual/gamenight/[Filtered]/manual/4/download"],
		])("should filter %s", (input, expected) => {
			expect(scrubUrl(input)).toBe(expected);
		});

		it.each(["/api/games?page=2&sort=name&zipcode=1000&codes=1", "/api/gamenight/4", "/auth-callback", ""])(
			"should leave %s untouched",
			(input) => {
				expect(scrubUrl(input)).toBe(input);
			},
		);

		it("should give the same result when called repeatedly", () => {
			const input = "/auth-callback?code=abc&state=def";
			expect(scrubUrl(input)).toBe(scrubUrl(input));
			expect(scrubUrl(scrubUrl(input))).toBe(scrubUrl(input));
		});
	});

	describe("scrubValue", () => {
		it("should filter values of sensitive keys and nested strings", () => {
			expect(
				scrubValue({
					code: "abc",
					linkId: LINK_ID,
					password: 42,
					page: 2,
					missing: null,
					nested: { url: "/rsvp?linkId=abc", list: ["/auth-callback?state=s", 1] },
				}),
			).toEqual({
				code: FILTERED,
				linkId: FILTERED,
				password: FILTERED,
				page: 2,
				missing: null,
				nested: { url: `/rsvp?linkId=${FILTERED}`, list: [`/auth-callback?state=${FILTERED}`, 1] },
			});
		});

		it("should keep non-string primitives", () => {
			expect(scrubValue(5)).toBe(5);
			expect(scrubValue(true)).toBe(true);
			expect(scrubValue(undefined)).toBeUndefined();
			expect(scrubValue(null)).toBeNull();
		});
	});

	describe("scrubEvent", () => {
		it("should scrub request, transaction, tags, breadcrumbs and spans of an error event", () => {
			const event: ErrorEvent = {
				type: undefined,
				message: "failed on /auth-callback?code=abc",
				transaction: "/auth-callback?code=abc",
				request: {
					url: "https://bgt.example.com/auth-callback?code=abc&state=def&lang=en",
					query_string: { code: "abc", lang: "en" },
					headers: { Referer: "https://bgt.example.com/rsvp?linkId=abc", Accept: "application/json" },
				},
				tags: { url: "/auth-callback?code=abc", route: "games" },
				breadcrumbs: [{ category: "navigation", data: { from: "/login", to: "/auth-callback?code=abc" } }],
				exception: { values: [{ type: "Error", value: "boom" }] },
			};

			const result = scrubEvent(event);

			expect(result).toBe(event);
			expect(result.message).toBe("failed on /auth-callback?code=[Filtered]");
			expect(result.transaction).toBe("/auth-callback?code=[Filtered]");
			expect(result.request).toEqual({
				url: "https://bgt.example.com/auth-callback?code=[Filtered]&state=[Filtered]&lang=en",
				query_string: { code: FILTERED, lang: "en" },
				headers: { Referer: "https://bgt.example.com/rsvp?linkId=[Filtered]", Accept: "application/json" },
			});
			expect(result.tags).toEqual({ url: "/auth-callback?code=[Filtered]", route: "games" });
			expect(result.breadcrumbs?.[0].data).toEqual({ from: "/login", to: "/auth-callback?code=[Filtered]" });
			expect(result.exception).toEqual({ values: [{ type: "Error", value: "boom" }] });
		});

		it("should scrub spans and trace context of a transaction", () => {
			const event: Event = {
				type: "transaction",
				transaction: "/rsvp",
				contexts: { trace: { trace_id: "t", span_id: "s", data: { url: "/rsvp?linkId=abc" } } },
				spans: [
					{
						span_id: "a",
						trace_id: "t",
						start_timestamp: 1,
						data: { url: `/api/gamenight/link/${LINK_ID}`, "http.query": "?code=abc" },
						description: `GET /api/gamenight/link/${LINK_ID}`,
					},
				],
			};

			const result = scrubEvent(event);

			expect(result.transaction).toBe("/rsvp");
			expect(result.contexts?.trace?.data).toEqual({ url: "/rsvp?linkId=[Filtered]" });
			expect(result.spans?.[0].data).toEqual({
				url: "/api/gamenight/link/[Filtered]",
				"http.query": "?code=[Filtered]",
			});
			expect(result.spans?.[0].description).toBe("GET /api/gamenight/link/[Filtered]");
		});

		it("should scrub urls of replay events", () => {
			const event = {
				type: "replay_event",
				urls: ["https://bgt.example.com/auth-callback?code=abc"],
			} as unknown as ErrorEvent;

			const result = scrubEvent(event) as unknown as { urls: string[] };

			expect(result.urls).toEqual(["https://bgt.example.com/auth-callback?code=[Filtered]"]);
		});
	});

	describe("scrubBreadcrumb", () => {
		it("should scrub fetch and navigation breadcrumbs", () => {
			expect(
				scrubBreadcrumb({
					category: "fetch",
					message: "GET /rsvp?linkId=abc",
					data: { url: `/api/manual/gamenight/${LINK_ID}`, status_code: 200, method: "GET" },
					timestamp: 10,
				}),
			).toEqual({
				category: "fetch",
				message: "GET /rsvp?linkId=[Filtered]",
				data: { url: "/api/manual/gamenight/[Filtered]", status_code: 200, method: "GET" },
				timestamp: 10,
			});
		});

		it("should leave harmless breadcrumbs unchanged", () => {
			const breadcrumb = { category: "navigation", data: { from: "/games", to: "/games/4?tab=sessions" } };
			expect(scrubBreadcrumb(breadcrumb)).toEqual(breadcrumb);
		});
	});

	describe("scrubRecordingEvent", () => {
		it("should scrub meta events", () => {
			expect(
				scrubRecordingEvent({
					type: 4,
					timestamp: 1,
					data: { href: "https://bgt.example.com/auth-callback?code=abc" },
				}),
			).toEqual({ type: 4, timestamp: 1, data: { href: "https://bgt.example.com/auth-callback?code=[Filtered]" } });
		});

		it("should scrub custom events", () => {
			expect(
				scrubRecordingEvent({
					type: 5,
					timestamp: 1,
					data: { tag: "performanceSpan", payload: { description: "https://bgt.example.com/rsvp?linkId=abc" } },
				}),
			).toEqual({
				type: 5,
				timestamp: 1,
				data: { tag: "performanceSpan", payload: { description: "https://bgt.example.com/rsvp?linkId=[Filtered]" } },
			});
		});

		it("should return other events as they are", () => {
			const snapshot = { type: 2, timestamp: 1, data: { text: "?code=abc" } };
			expect(scrubRecordingEvent(snapshot)).toBe(snapshot);
			expect(scrubRecordingEvent(null)).toBeNull();
		});
	});
});
