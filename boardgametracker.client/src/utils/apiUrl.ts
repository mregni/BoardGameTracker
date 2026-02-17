let baseUrl = "/api/";
if (import.meta.env.DEV) {
	baseUrl = "http://localhost:6554/api/";
}

export const apiUrl = baseUrl;
