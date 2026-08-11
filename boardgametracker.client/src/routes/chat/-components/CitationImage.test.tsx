import { beforeEach, describe, expect, it, vi } from "vitest";
import { getManualPageImageCall } from "@/services/manualService";
import { render, screen, waitFor } from "@/test/test-utils";
import { CitationImage } from "./CitationImage";

vi.mock("@/services/manualService", () => ({
	getManualPageImageCall: vi.fn(),
}));

const getManualPageImageCallMock = vi.mocked(getManualPageImageCall);

describe("CitationImage", () => {
	beforeEach(() => {
		getManualPageImageCallMock.mockReset();
		URL.createObjectURL = vi.fn(() => "blob:mock-url");
		URL.revokeObjectURL = vi.fn();
	});

	it("should render nothing while the image is loading", () => {
		getManualPageImageCallMock.mockReturnValue(new Promise(() => {}));
		const { container } = render(<CitationImage url="/manuals/1/pages/2" alt="Manual page 2" />);

		expect(container).toBeEmptyDOMElement();
	});

	it("should render the fetched image with alt text", async () => {
		getManualPageImageCallMock.mockResolvedValue(new Blob(["image-data"]));
		render(<CitationImage url="/manuals/1/pages/2" alt="Manual page 2" />);

		const image = await screen.findByRole("img", { name: "Manual page 2" });

		expect(image).toHaveAttribute("src", "blob:mock-url");
		expect(getManualPageImageCallMock).toHaveBeenCalledWith("/manuals/1/pages/2");
	});

	it("should render nothing when fetching the image fails", async () => {
		getManualPageImageCallMock.mockRejectedValue(new Error("renderer unavailable"));
		const { container } = render(<CitationImage url="/manuals/1/pages/2" alt="Manual page 2" />);

		await waitFor(() => {
			expect(getManualPageImageCallMock).toHaveBeenCalled();
		});

		expect(container).toBeEmptyDOMElement();
	});

	it("should revoke the object url on unmount", async () => {
		getManualPageImageCallMock.mockResolvedValue(new Blob(["image-data"]));
		const { unmount } = render(<CitationImage url="/manuals/1/pages/2" alt="Manual page 2" />);
		await screen.findByRole("img");

		unmount();

		expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:mock-url");
	});

	it("should fetch again when the url changes", async () => {
		getManualPageImageCallMock.mockResolvedValue(new Blob(["image-data"]));
		const { rerender } = render(<CitationImage url="/manuals/1/pages/2" alt="Manual page 2" />);
		await screen.findByRole("img");

		rerender(<CitationImage url="/manuals/1/pages/3" alt="Manual page 3" />);

		await waitFor(() => {
			expect(getManualPageImageCallMock).toHaveBeenCalledWith("/manuals/1/pages/3");
		});
		expect(getManualPageImageCallMock).toHaveBeenCalledTimes(2);
	});
});
