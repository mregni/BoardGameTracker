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

const isLoopbackHost = (hostname: string): boolean => {
	const name = hostname.toLowerCase().replace(/\.$/, "").replace(/^\[(.*)\]$/, "$1");
	return (
		name === "localhost" || name.endsWith(".localhost") || name === "::1" || /^127(\.\d{1,3}){3}$/.test(name)
	);
};

export const breaksSingleSignOn = (url: string): boolean => {
	let parsed: URL;
	try {
		parsed = new URL(url);
	} catch {
		return false;
	}

	return parsed.protocol === "http:" && !isLoopbackHost(parsed.hostname);
};
