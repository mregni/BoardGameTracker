import { beforeEach, describe, expect, it, vi } from "vitest";
import type { GameManual } from "@/models";
import { fireEvent, renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { ManualsDialog } from "./ManualsDialog";

const mockUseGameManuals = vi.fn();

vi.mock("../-hooks/useGameManuals", () => ({
	useGameManuals: (gameId: number, pollWhileIndexing: boolean) => mockUseGameManuals(gameId, pollWhileIndexing),
}));

vi.mock("@/assets/icons/download.svg?react", () => ({
	default: () => <svg data-testid="download-icon" />,
}));

vi.mock("@/assets/icons/trash.svg?react", () => ({
	default: () => <svg data-testid="trash-icon" />,
}));

vi.mock("@/assets/icons/x.svg?react", () => ({
	default: () => <svg data-testid="close-icon" />,
}));

const uploadManuals = vi.fn();
const deleteManual = vi.fn();
const downloadManual = vi.fn();
const reindexManual = vi.fn();

const hookResult = (overrides: Record<string, unknown> = {}) => ({
	manuals: [],
	uploadManuals,
	deleteManual,
	downloadManual,
	reindexManual,
	isReindexing: false,
	...overrides,
});

const createManual = (overrides: Partial<GameManual> = {}): GameManual => ({
	id: 5,
	gameId: 7,
	title: "Rulebook",
	fileSizeBytes: 512,
	uploadDate: new Date(2026, 0, 15),
	contentType: "application/pdf",
	indexStatus: "indexed",
	indexedChunkCount: 10,
	indexError: null,
	indexedDate: null,
	...overrides,
});

describe("ManualsDialog", () => {
	const defaultProps = {
		gameId: 7,
		open: true,
		close: vi.fn(),
		canWrite: true,
		ragEnabled: true,
		dateFormat: "yyyy-MM-dd",
		uiLanguage: "en-US",
	};

	beforeEach(() => {
		vi.clearAllMocks();
		mockUseGameManuals.mockReturnValue(hookResult());
	});

	describe("Dialog visibility", () => {
		it("should render content when open", () => {
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			expect(screen.getByText("manuals.description")).toBeInTheDocument();
		});

		it("should not render content when closed", () => {
			renderWithTheme(<ManualsDialog {...defaultProps} open={false} />);

			expect(screen.queryByText("manuals.description")).not.toBeInTheDocument();
		});

		it("should call close when the close button is clicked", async () => {
			const user = userEvent.setup();
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			await user.click(screen.getByTestId("close-icon").closest("button") as HTMLButtonElement);

			expect(defaultProps.close).toHaveBeenCalledTimes(1);
		});
	});

	describe("Polling", () => {
		it("should poll while indexing when rag is enabled and the dialog is open", () => {
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			expect(mockUseGameManuals).toHaveBeenCalledWith(7, true);
		});

		it("should not poll when rag is disabled", () => {
			renderWithTheme(<ManualsDialog {...defaultProps} ragEnabled={false} />);

			expect(mockUseGameManuals).toHaveBeenCalledWith(7, false);
		});

		it("should not poll when the dialog is closed", () => {
			renderWithTheme(<ManualsDialog {...defaultProps} open={false} />);

			expect(mockUseGameManuals).toHaveBeenCalledWith(7, false);
		});
	});

	describe("Empty state", () => {
		it("should show the empty message and a zero count", () => {
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			expect(screen.getByText("manuals.title (0)")).toBeInTheDocument();
			expect(screen.getByText("manuals.none")).toBeInTheDocument();
		});
	});

	describe("Manual list", () => {
		it("should render the manual title, size, date and count", () => {
			mockUseGameManuals.mockReturnValue(hookResult({ manuals: [createManual()] }));
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			expect(screen.getByText("manuals.title (1)")).toBeInTheDocument();
			expect(screen.getByText("Rulebook")).toBeInTheDocument();
			expect(screen.getByText("512 B · 2026-01-15")).toBeInTheDocument();
			expect(screen.queryByText("manuals.none")).not.toBeInTheDocument();
		});
	});

	describe("Status badge", () => {
		it.each([
			"pending",
			"indexing",
			"indexed",
			"failed",
		] as const)("should show the %s status when rag is enabled", (status) => {
			mockUseGameManuals.mockReturnValue(hookResult({ manuals: [createManual({ indexStatus: status })] }));
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			expect(screen.getByText(`manuals.status.${status}`)).toBeInTheDocument();
		});

		it("should not show a status badge when rag is disabled", () => {
			mockUseGameManuals.mockReturnValue(hookResult({ manuals: [createManual()] }));
			renderWithTheme(<ManualsDialog {...defaultProps} ragEnabled={false} />);

			expect(screen.queryByText("manuals.status.indexed")).not.toBeInTheDocument();
		});

		it("should expose the index error as tooltip on a failed badge", () => {
			mockUseGameManuals.mockReturnValue(
				hookResult({ manuals: [createManual({ indexStatus: "failed", indexError: "Parse error" })] }),
			);
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			expect(screen.getByText("manuals.status.failed")).toHaveAttribute("title", "Parse error");
		});

		it("should not set a tooltip on a failed badge without an error", () => {
			mockUseGameManuals.mockReturnValue(
				hookResult({ manuals: [createManual({ indexStatus: "failed", indexError: null })] }),
			);
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			expect(screen.getByText("manuals.status.failed")).not.toHaveAttribute("title");
		});

		it("should not set a tooltip on an indexed badge", () => {
			mockUseGameManuals.mockReturnValue(hookResult({ manuals: [createManual()] }));
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			expect(screen.getByText("manuals.status.indexed")).not.toHaveAttribute("title");
		});
	});

	describe("Reindex", () => {
		it("should call reindexManual with the manual id", async () => {
			const user = userEvent.setup();
			mockUseGameManuals.mockReturnValue(hookResult({ manuals: [createManual()] }));
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			await user.click(screen.getByTitle("manuals.reindex"));

			expect(reindexManual).toHaveBeenCalledWith(5);
		});

		it("should disable the reindex button while reindexing", () => {
			mockUseGameManuals.mockReturnValue(hookResult({ manuals: [createManual()], isReindexing: true }));
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			expect(screen.getByTitle("manuals.reindex")).toBeDisabled();
		});

		it("should hide the reindex button when the user cannot write", () => {
			mockUseGameManuals.mockReturnValue(hookResult({ manuals: [createManual()] }));
			renderWithTheme(<ManualsDialog {...defaultProps} canWrite={false} />);

			expect(screen.queryByTitle("manuals.reindex")).not.toBeInTheDocument();
		});

		it("should hide the reindex button when rag is disabled", () => {
			mockUseGameManuals.mockReturnValue(hookResult({ manuals: [createManual()] }));
			renderWithTheme(<ManualsDialog {...defaultProps} ragEnabled={false} />);

			expect(screen.queryByTitle("manuals.reindex")).not.toBeInTheDocument();
		});
	});

	describe("Download", () => {
		it("should call downloadManual with the manual id and title", async () => {
			const user = userEvent.setup();
			mockUseGameManuals.mockReturnValue(hookResult({ manuals: [createManual()] }));
			renderWithTheme(<ManualsDialog {...defaultProps} canWrite={false} />);

			await user.click(screen.getByTestId("download-icon").closest("button") as HTMLButtonElement);

			expect(downloadManual).toHaveBeenCalledWith(5, "Rulebook");
		});
	});

	describe("Delete", () => {
		it("should call deleteManual with the manual id", async () => {
			const user = userEvent.setup();
			mockUseGameManuals.mockReturnValue(hookResult({ manuals: [createManual()] }));
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			await user.click(screen.getByTestId("trash-icon").closest("button") as HTMLButtonElement);

			expect(deleteManual).toHaveBeenCalledWith(5);
		});

		it("should hide the delete button when the user cannot write", () => {
			mockUseGameManuals.mockReturnValue(hookResult({ manuals: [createManual()] }));
			renderWithTheme(<ManualsDialog {...defaultProps} canWrite={false} />);

			expect(screen.queryByTestId("trash-icon")).not.toBeInTheDocument();
		});
	});

	describe("Upload", () => {
		it("should hide the add button when the user cannot write", () => {
			renderWithTheme(<ManualsDialog {...defaultProps} canWrite={false} />);

			expect(screen.queryByText("manuals.add")).not.toBeInTheDocument();
		});

		it("should open the file picker when the add button is clicked", async () => {
			const user = userEvent.setup();
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			const input = document.querySelector('input[type="file"]') as HTMLInputElement;
			const clickSpy = vi.spyOn(input, "click");

			await user.click(screen.getByText("manuals.add"));

			expect(clickSpy).toHaveBeenCalledTimes(1);
		});

		it("should upload the selected files", () => {
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			const input = document.querySelector('input[type="file"]') as HTMLInputElement;
			const file = new File(["pdf"], "rules.pdf", { type: "application/pdf" });
			fireEvent.change(input, { target: { files: [file] } });

			expect(uploadManuals).toHaveBeenCalledWith([file]);
		});

		it("should not upload when no files are selected", () => {
			renderWithTheme(<ManualsDialog {...defaultProps} />);

			const input = document.querySelector('input[type="file"]') as HTMLInputElement;
			fireEvent.change(input, { target: { files: [] } });

			expect(uploadManuals).not.toHaveBeenCalled();
		});
	});
});
