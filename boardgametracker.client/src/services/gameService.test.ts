import { describe, expect, it, vi } from "vitest";

const axiosMock = vi.hoisted(() => ({ post: vi.fn() }));

vi.mock("../utils/axiosInstance", () => ({ axiosInstance: axiosMock }));

import { uploadCsvImportCall } from "./gameService";
import { getCsvImportGames, getCsvImportRejected } from "./queries/games";

describe("gameService CSV import", () => {
	it("posts the file as multipart form data with a long timeout", async () => {
		axiosMock.post.mockResolvedValue({ data: { games: [], rejected: [] } });
		const file = new File(["objectid\n13"], "collection.csv", { type: "text/csv" });

		await expect(uploadCsvImportCall(file)).resolves.toEqual({ games: [], rejected: [] });

		const [url, body, config] = axiosMock.post.mock.calls[0];
		expect(url).toBe("game/import/csv");
		expect((body as FormData).get("file")).toBe(file);
		expect(config).toEqual({ headers: { "Content-Type": "multipart/form-data" }, timeout: 120000 });
	});

	it.each([
		["games", getCsvImportGames],
		["rejected rows", getCsvImportRejected],
	])("refuses to fetch the CSV %s, which only come from an upload", (_, options) => {
		const queryFn = options().queryFn as () => unknown;

		expect(queryFn).toThrow();
	});
});
