import { safeRedirectPath } from "./redirectUtils";

const normalize = (configured: string | null | undefined) => (configured ?? "").trim().replace(/\/+$/, "");

export const isPublicUrlConfigured = (configured: string | null | undefined): boolean => normalize(configured) !== "";

export const effectivePublicUrl = (configured: string | null | undefined, origin: string): string =>
	isPublicUrlConfigured(configured) ? normalize(configured) : origin;

export const publicHostLoginUrl = (
	configured: string | null | undefined,
	currentOrigin: string,
	redirect: string | null | undefined,
): string | null => {
	const base = normalize(configured);
	if (!base) {
		return null;
	}

	let parsed: URL;
	try {
		parsed = new URL(base);
	} catch {
		return null;
	}

	if (parsed.protocol !== "https:" || parsed.origin === currentOrigin) {
		return null;
	}

	const search = new URLSearchParams({ redirect: safeRedirectPath(redirect), sso: "true" });
	return `${base}/login?${search.toString()}`;
};
