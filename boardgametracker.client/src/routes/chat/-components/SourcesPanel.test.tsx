import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RagCitation } from "@/models";
import { downloadManualCall } from "@/services/manualService";
import { render, screen } from "@/test/test-utils";
import { SourcesPanel } from "./SourcesPanel";

vi.mock("@/services/manualService", () => ({
	downloadManualCall: vi.fn(),
	getManualPageImageCall: vi.fn().mockRejectedValue(new Error("no renderer")),
}));

const downloadMock = vi.mocked(downloadManualCall);

const buildCitations = (): RagCitation[] => [
	{ manualId: 5, title: "rulebook.pdf", page: 7, snippet: "", score: 0.6, imageUrl: null },
	{ manualId: 5, title: "rulebook.pdf", page: 8, snippet: "", score: 0.5, imageUrl: null },
];

describe("SourcesPanel", () => {
	beforeEach(() => {
		downloadMock.mockReset();
		URL.createObjectURL = vi.fn(() => "blob:mock-url");
		URL.revokeObjectURL = vi.fn();
	});

	it("should not render the filename as a label", () => {
		render(<SourcesPanel citations={buildCitations()} focusedIndex={0} onFocus={vi.fn()} onExpand={vi.fn()} />);

		expect(screen.queryByText("rulebook.pdf")).not.toBeInTheDocument();
	});

	it("should download the manual when the download button is clicked", async () => {
		const user = userEvent.setup();
		render(<SourcesPanel citations={buildCitations()} focusedIndex={0} onFocus={vi.fn()} onExpand={vi.fn()} />);

		await user.click(screen.getByRole("button", { name: "download-manual" }));

		expect(downloadMock).toHaveBeenCalledWith(5, "rulebook.pdf");
	});

	it("should call onExpand when the preview is clicked", async () => {
		const user = userEvent.setup();
		const onExpand = vi.fn();
		render(<SourcesPanel citations={buildCitations()} focusedIndex={0} onFocus={vi.fn()} onExpand={onExpand} />);

		await user.click(screen.getByRole("button", { name: "click-to-enlarge" }));

		expect(onExpand).toHaveBeenCalledTimes(1);
	});

	it("should focus another page from the thumbnail strip", async () => {
		const user = userEvent.setup();
		const onFocus = vi.fn();
		render(<SourcesPanel citations={buildCitations()} focusedIndex={0} onFocus={onFocus} onExpand={vi.fn()} />);

		const thumbnails = screen.getAllByRole("button").filter((button) => button.hasAttribute("aria-current"));
		await user.click(thumbnails[1]);

		expect(onFocus).toHaveBeenCalledWith(1);
	});
});
