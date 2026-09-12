import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { RagCitation } from "@/models";
import { render, screen } from "@/test/test-utils";
import { CitationList } from "./CitationList";

const buildCitation = (overrides: Partial<RagCitation> = {}): RagCitation => ({
	manualId: 1,
	title: "Base rules",
	page: 4,
	snippet: "Draw two cards",
	score: 0.87,
	imageUrl: null,
	...overrides,
});

describe("CitationList", () => {
	it("should render the sources header", () => {
		render(<CitationList citations={[buildCitation()]} onSelect={vi.fn()} />);

		expect(screen.getByText("sources")).toBeInTheDocument();
	});

	it("should render one chip per citation", () => {
		render(
			<CitationList
				citations={[buildCitation(), buildCitation({ manualId: 2, page: 9 })]}
				onSelect={vi.fn()}
			/>,
		);

		expect(screen.getAllByRole("button")).toHaveLength(2);
		expect(screen.getAllByText("page")).toHaveLength(2);
	});

	it("should flag only the first citation as the top match", () => {
		render(
			<CitationList
				citations={[buildCitation(), buildCitation({ manualId: 2, page: 9 })]}
				onSelect={vi.fn()}
			/>,
		);

		expect(screen.getAllByText("top-match")).toHaveLength(1);
	});

	it("should show unknown-page when the page is null", () => {
		render(<CitationList citations={[buildCitation({ page: null })]} onSelect={vi.fn()} />);

		expect(screen.getByText("unknown-page")).toBeInTheDocument();
	});

	it("should call onSelect with the citation index when a chip is clicked", async () => {
		const user = userEvent.setup();
		const onSelect = vi.fn();
		render(
			<CitationList
				citations={[buildCitation(), buildCitation({ manualId: 2, page: 9 })]}
				onSelect={onSelect}
			/>,
		);

		await user.click(screen.getAllByRole("button")[1]);

		expect(onSelect).toHaveBeenCalledWith(1);
	});

	it("should not render any image (images live in the sources panel)", () => {
		render(<CitationList citations={[buildCitation({ imageUrl: "/manuals/1/pages/4" })]} onSelect={vi.fn()} />);

		expect(screen.queryByRole("img")).not.toBeInTheDocument();
	});
});
