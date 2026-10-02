const SEEDED_PUBLIC_URL = "http://localhost:5444";

export const effectivePublicUrl = (configured: string | null | undefined, origin: string): string => {
	const url = (configured ?? "").trim().replace(/\/+$/, "");
	return url === "" || url.toLowerCase() === SEEDED_PUBLIC_URL ? origin : url;
};
