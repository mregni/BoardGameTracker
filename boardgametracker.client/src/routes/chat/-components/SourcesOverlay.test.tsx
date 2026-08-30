import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { RagCitation } from "@/models";
import { render, screen } from "@/test/test-utils";
import { SourcesOverlay } from "./SourcesOverlay";

vi.mock("@/services/manualService", () => ({
	getManualPageImageCall: vi.fn().mockRejectedValue(new Error("no renderer")),
}));

const buildCitations = (count: number): RagCitation[] =>
	Array.from({ length: count }, (_, index) => ({
		manualId: 1,
		title: "Rulebook",
		page: index + 1,
		snippet: "",
		score: 0.5,
		imageUrl: null,
	}));

describe("SourcesOverlay", () => {
	it("should render as a dialog", () => {
		render(<SourcesOverlay citations={buildCitations(3)} index={0} onIndexChange={vi.fn()} onClose={vi.fn()} />);

		expect(screen.getByRole("dialog")).toBeInTheDocument();
	});

	it("should render one page dot per citation", () => {
		const { container } = render(
			<SourcesOverlay citations={buildCitations(3)} index={0} onIndexChange={vi.fn()} onClose={vi.fn()} />,
		);

		// dots are the small pills after the previous button
		expect(container.querySelectorAll("span.rounded-full").length).toBeGreaterThanOrEqual(3);
	});

	it("should call onClose from the close button", async () => {
		const user = userEvent.setup();
		const onClose = vi.fn();
		render(<SourcesOverlay citations={buildCitations(3)} index={0} onIndexChange={vi.fn()} onClose={onClose} />);

		await user.click(screen.getByRole("button", { name: "close" }));

		expect(onClose).toHaveBeenCalledTimes(1);
	});

	it("should advance to the next page", async () => {
		const user = userEvent.setup();
		const onIndexChange = vi.fn();
		render(<SourcesOverlay citations={buildCitations(3)} index={0} onIndexChange={onIndexChange} onClose={vi.fn()} />);

		await user.click(screen.getByRole("button", { name: "next" }));

		expect(onIndexChange).toHaveBeenCalledWith(1);
	});

	it("should disable previous on the first page", () => {
		render(<SourcesOverlay citations={buildCitations(3)} index={0} onIndexChange={vi.fn()} onClose={vi.fn()} />);

		expect(screen.getByRole("button", { name: "previous" })).toBeDisabled();
	});

	it("should disable next on the last page", () => {
		render(<SourcesOverlay citations={buildCitations(3)} index={2} onIndexChange={vi.fn()} onClose={vi.fn()} />);

		expect(screen.getByRole("button", { name: "next" })).toBeDisabled();
	});
});
