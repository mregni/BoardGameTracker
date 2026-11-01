import { describe, expect, it } from "vitest";
import { toCalendarFile, toCalendarHref } from "./calendarFile";

const event = {
	uid: "abc@boardgametracker",
	title: "Night, with; friends",
	start: "2026-11-04T18:30:00Z",
	location: "Thuis",
	description: "Bring snacks.\nStart light.",
	url: "https://bgt.example/rsvp?linkId=abc",
};

describe("toCalendarFile", () => {
	it("writes a three-hour event in UTC with escaped text", () => {
		const lines = toCalendarFile(event, new Date("2026-11-01T10:00:00Z")).split("\r\n");

		expect(lines).toContain("DTSTART:20261104T183000Z");
		expect(lines).toContain("DTEND:20261104T213000Z");
		expect(lines).toContain("DTSTAMP:20261101T100000Z");
		expect(lines).toContain("SUMMARY:Night\\, with\\; friends");
		expect(lines).toContain("DESCRIPTION:Bring snacks.\\nStart light.");
		expect(lines[0]).toBe("BEGIN:VCALENDAR");
		expect(lines.at(-1)).toBe("END:VCALENDAR");
	});

	it("encodes the file as a calendar data link", () => {
		const href = toCalendarHref(event);

		expect(href.startsWith("data:text/calendar;charset=utf-8,")).toBe(true);
		expect(decodeURIComponent(href.split(",").slice(1).join(","))).toContain("LOCATION:Thuis");
	});
});
