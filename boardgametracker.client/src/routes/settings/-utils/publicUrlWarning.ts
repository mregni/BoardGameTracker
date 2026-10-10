export type PublicUrlWarning = "insecure" | "host-mismatch" | null;

export const getPublicUrlWarning = (
	publicUrl: string | null | undefined,
	current: { protocol: string; host: string },
): PublicUrlWarning => {
	if (!publicUrl) return null;

	let parsed: URL;
	try {
		parsed = new URL(publicUrl);
	} catch {
		return null;
	}

	if (parsed.protocol === "http:" && current.protocol === "https:") return "insecure";
	if (parsed.host.toLowerCase() !== current.host.toLowerCase()) return "host-mismatch";
	return null;
};
