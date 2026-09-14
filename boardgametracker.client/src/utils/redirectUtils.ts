export const safeRedirectPath = (redirect: string | undefined | null, fallback = "/"): string => {
	if (!redirect) {
		return fallback;
	}

	if (!redirect.startsWith("/") || redirect.startsWith("//") || redirect.startsWith("/\\")) {
		return fallback;
	}

	return redirect;
};
