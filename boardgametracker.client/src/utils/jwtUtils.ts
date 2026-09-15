const base64UrlDecode = (value: string): string => {
	const base64 = value.replace(/-/g, "+").replace(/_/g, "/");
	const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
	const binary = atob(padded);
	const bytes = Uint8Array.from(binary, (char) => char.charCodeAt(0));
	return new TextDecoder().decode(bytes);
};

export const decodeJwtPayload = (token: string): Record<string, unknown> => {
	const parts = token.split(".");
	if (parts.length < 2) {
		throw new Error("Not a JWT");
	}

	const payload = JSON.parse(base64UrlDecode(parts[1]));
	if (payload === null || typeof payload !== "object") {
		throw new Error("Invalid JWT payload");
	}

	return payload as Record<string, unknown>;
};
