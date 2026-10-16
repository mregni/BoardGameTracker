import type { Breadcrumb, Event } from "@sentry/react";

export const FILTERED = "[Filtered]";

const SENSITIVE_PARAM =
	/(^|[?&#;])(code|state|token|id_token|access_token|refresh_token|session_state|linkid|[^=&#?;\s/]*(?:password|secret|key)[^=&#?;\s/]*)=([^&#\s"']*)/gi;

const SENSITIVE_PATH =
	/(\/gamenight\/link\/|\/manual\/gamenight\/)[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;

const SENSITIVE_KEY =
	/^(code|state|token|id_token|access_token|refresh_token|session_state|linkid)$|password|secret|key/i;

const EVENT_FIELDS = [
	"request",
	"transaction",
	"tags",
	"extra",
	"contexts",
	"breadcrumbs",
	"spans",
	"message",
	"urls",
] as const;

const RRWEB_META = 4;
const RRWEB_CUSTOM = 5;

export const scrubUrl = (value: string): string =>
	value
		.replace(SENSITIVE_PARAM, (_, prefix: string, name: string) => `${prefix}${name}=${FILTERED}`)
		.replace(SENSITIVE_PATH, (_, path: string) => `${path}${FILTERED}`);

export const scrubValue = (value: unknown): unknown => {
	if (typeof value === "string") return scrubUrl(value);
	if (Array.isArray(value)) return value.map(scrubValue);
	if (value && typeof value === "object") {
		return Object.fromEntries(
			Object.entries(value).map(([key, entry]) => [
				key,
				SENSITIVE_KEY.test(key) && entry != null && typeof entry !== "object" ? FILTERED : scrubValue(entry),
			]),
		);
	}
	return value;
};

export const scrubEvent = <T extends Event>(event: T): T => {
	const target = event as Record<string, unknown>;
	for (const field of EVENT_FIELDS) {
		if (target[field] !== undefined) {
			target[field] = scrubValue(target[field]);
		}
	}
	return event;
};

export const scrubBreadcrumb = (breadcrumb: Breadcrumb): Breadcrumb => scrubValue(breadcrumb) as Breadcrumb;

export const scrubRecordingEvent = <T>(event: T): T => {
	const type = (event as { type?: unknown } | null)?.type;
	return type === RRWEB_META || type === RRWEB_CUSTOM ? (scrubValue(event) as T) : event;
};
