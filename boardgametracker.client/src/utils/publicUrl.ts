const normalize = (configured: string | null | undefined) => (configured ?? "").trim().replace(/\/+$/, "");

export const isPublicUrlConfigured = (configured: string | null | undefined): boolean => normalize(configured) !== "";

export const effectivePublicUrl = (configured: string | null | undefined, origin: string): string =>
	isPublicUrlConfigured(configured) ? normalize(configured) : origin;
