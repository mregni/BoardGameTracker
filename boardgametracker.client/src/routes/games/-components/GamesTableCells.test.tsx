import { describe, expect, it, vi } from "vitest";
import { renderWithProviders } from "@/test/test-utils";
import { SelectAllHeader as ImportSelectAllHeader } from "../import/-components/ImportListCells";
import { PlayersCell } from "./GamesTableCells";

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

describe("table cells outside their table", () => {
	it("refuse to render without the table context", () => {
		vi.spyOn(console, "error").mockImplementation(() => undefined);
		const row = { row: { original: {} } } as unknown as Parameters<typeof PlayersCell>[0];

		expect(() => renderWithProviders(<PlayersCell {...row} />)).toThrow("GamesTableContext is missing");
		expect(() => renderWithProviders(<ImportSelectAllHeader />)).toThrow("ImportListContext is missing");
		vi.restoreAllMocks();
	});
});
