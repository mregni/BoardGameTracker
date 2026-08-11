import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RagCitation } from "@/models";
import { getManualPageImageCall } from "@/services/manualService";
import { render, screen } from "@/test/test-utils";
import { CitationList } from "./CitationList";

vi.mock("@/services/manualService", () => ({
	getManualPageImageCall: vi.fn(),
}));

const getManualPageImageCallMock = vi.mocked(getManualPageImageCall);

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
	beforeEach(() => {
		getManualPageImageCallMock.mockReset();
		URL.createObjectURL = vi.fn(() => "blob:mock-url");
		URL.revokeObjectURL = vi.fn();
	});

	it("should render the sources header", () => {
		render(<CitationList citations={[buildCitation()]} />);

		expect(screen.getByText("sources")).toBeInTheDocument();
	});

	it("should render the title, page and rounded score", () => {
		render(<CitationList citations={[buildCitation()]} />);

		expect(screen.getByText(/Base rules · page/)).toBeInTheDocument();
		expect(screen.getByText("87%")).toBeInTheDocument();
	});

	it("should fall back to untitled-manual when the title is empty", () => {
		render(<CitationList citations={[buildCitation({ title: "" })]} />);

		expect(screen.getByText(/untitled-manual · page/)).toBeInTheDocument();
	});

	it("should show unknown-page when the page is null", () => {
		render(<CitationList citations={[buildCitation({ page: null })]} />);

		expect(screen.getByText(/Base rules · unknown-page/)).toBeInTheDocument();
	});

	it("should clamp negative scores to zero", () => {
		render(<CitationList citations={[buildCitation({ score: -0.5 })]} />);

		expect(screen.getByText("0%")).toBeInTheDocument();
	});

	it("should render one entry per citation", () => {
		render(
			<CitationList
				citations={[buildCitation(), buildCitation({ manualId: 2, title: "Expansion rules", page: 9, score: 0.5 })]}
			/>,
		);

		expect(screen.getByText(/Base rules · page/)).toBeInTheDocument();
		expect(screen.getByText(/Expansion rules · page/)).toBeInTheDocument();
		expect(screen.getByText("50%")).toBeInTheDocument();
	});

	it("should render the page image when the citation has an image url", async () => {
		getManualPageImageCallMock.mockResolvedValue(new Blob(["image-data"]));
		render(<CitationList citations={[buildCitation({ imageUrl: "/manuals/1/pages/4" })]} />);

		expect(await screen.findByRole("img", { name: "Base rules · page" })).toBeInTheDocument();
		expect(getManualPageImageCallMock).toHaveBeenCalledWith("/manuals/1/pages/4");
	});

	it("should use the untitled fallback without a page suffix in the image alt", async () => {
		getManualPageImageCallMock.mockResolvedValue(new Blob(["image-data"]));
		render(<CitationList citations={[buildCitation({ title: "", page: null, imageUrl: "/manuals/1/pages/4" })]} />);

		expect(await screen.findByRole("img", { name: "untitled-manual" })).toBeInTheDocument();
	});

	it("should not render an image when the citation has no image url", () => {
		render(<CitationList citations={[buildCitation({ imageUrl: null })]} />);

		expect(screen.queryByRole("img")).not.toBeInTheDocument();
		expect(getManualPageImageCallMock).not.toHaveBeenCalled();
	});
});
