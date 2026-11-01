import { addHours } from "date-fns";

interface CalendarEvent {
	uid: string;
	title: string;
	start: Date | string;
	location: string;
	description: string;
	url: string;
}

const EVENT_HOURS = 3;

const toIcsDate = (date: Date) =>
	date
		.toISOString()
		.replace(/[-:]/g, "")
		.replace(/\.\d{3}/, "");

const escapeText = (text: string) =>
	text.replace(/\\/g, "\\\\").replace(/;/g, "\\;").replace(/,/g, "\\,").replace(/\r?\n/g, "\\n");

export const toCalendarFile = (event: CalendarEvent, now = new Date()): string => {
	const start = new Date(event.start);
	return [
		"BEGIN:VCALENDAR",
		"VERSION:2.0",
		"PRODID:-//BoardGameTracker//Game night//EN",
		"BEGIN:VEVENT",
		`UID:${event.uid}`,
		`DTSTAMP:${toIcsDate(now)}`,
		`DTSTART:${toIcsDate(start)}`,
		`DTEND:${toIcsDate(addHours(start, EVENT_HOURS))}`,
		`SUMMARY:${escapeText(event.title)}`,
		`LOCATION:${escapeText(event.location)}`,
		`DESCRIPTION:${escapeText(event.description)}`,
		`URL:${event.url}`,
		"END:VEVENT",
		"END:VCALENDAR",
	].join("\r\n");
};

export const toCalendarHref = (event: CalendarEvent) =>
	`data:text/calendar;charset=utf-8,${encodeURIComponent(toCalendarFile(event))}`;
