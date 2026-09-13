import { beforeEach, describe, expect, it, vi } from "vitest";
import { getManualPageImageCall } from "@/services/manualService";
import { render, screen } from "@/test/test-utils";
import { PageImage } from "./PageImage";

vi.mock("@/services/manualService", () => ({
	getManualPageImageCall: vi.fn(),
}));

const getManualPageImageCallMock = vi.mocked(getManualPageImageCall);

describe("PageImage", () => {
	beforeEach(() => {
		getManualPageImageCallMock.mockReset();
		URL.createObjectURL = vi.fn(() => "blob:mock-url");
		URL.revokeObjectURL = vi.fn();
	});

	it("should render the image once the page loads", async () => {
		getManualPageImageCallMock.mockResolvedValue(new Blob(["image-data"]));
		render(<PageImage url="/manuals/1/page/7-a" alt="page seven" fallback={<span>placeholder</span>} />);

		expect(await screen.findByRole("img", { name: "page seven" })).toBeInTheDocument();
		expect(getManualPageImageCallMock).toHaveBeenCalledWith("/manuals/1/page/7-a");
	});

	it("should show the fallback when there is no url", () => {
		render(<PageImage url={null} alt="x" fallback={<span>unavailable</span>} />);

		expect(screen.getByText("unavailable")).toBeInTheDocument();
		expect(screen.queryByRole("img")).not.toBeInTheDocument();
		expect(getManualPageImageCallMock).not.toHaveBeenCalled();
	});

	it("should show the fallback when the fetch fails", async () => {
		getManualPageImageCallMock.mockRejectedValue(new Error("renderer unavailable"));
		render(<PageImage url="/manuals/1/page/7-b" alt="x" fallback={<span>unavailable</span>} />);

		expect(await screen.findByText("unavailable")).toBeInTheDocument();
		expect(screen.queryByRole("img")).not.toBeInTheDocument();
	});
});
