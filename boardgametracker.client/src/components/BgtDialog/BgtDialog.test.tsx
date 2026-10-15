import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { BgtDialog, BgtDialogContent, BgtDialogDescription, BgtDialogTitle } from "./BgtDialog";

describe("BgtDialog", () => {
	describe("BgtDialog Root", () => {
		it("should render children when open", () => {
			renderWithTheme(
				<BgtDialog open={true} onClose={vi.fn()}>
					<div data-testid="dialog-child">Dialog Content</div>
				</BgtDialog>,
			);
			expect(screen.getByTestId("dialog-child")).toBeInTheDocument();
		});

		it("should render children when closed", () => {
			renderWithTheme(
				<BgtDialog open={false} onClose={vi.fn()}>
					<div data-testid="dialog-child">Dialog Content</div>
				</BgtDialog>,
			);
			expect(screen.getByTestId("dialog-child")).toBeInTheDocument();
		});
	});

	describe("Sizes", () => {
		const renderSized = (size?: "default" | "wide" | "sheet") =>
			renderWithTheme(
				<BgtDialog open={true} onClose={vi.fn()}>
					<BgtDialogContent size={size}>
						<BgtDialogTitle>Sized</BgtDialogTitle>
					</BgtDialogContent>
				</BgtDialog>,
			);

		it("is a centred 600px dialog by default", () => {
			renderSized();
			const dialog = screen.getByRole("dialog");
			expect(dialog).toHaveClass("top-1/2", "max-w-[600px]");
			expect(dialog).not.toHaveClass("lg:max-w-5xl");
		});

		it("grows on large screens when wide", () => {
			renderSized("wide");
			expect(screen.getByRole("dialog")).toHaveClass("max-w-[600px]", "lg:max-w-5xl");
		});

		it("sits at the bottom on phones when it is a sheet", () => {
			renderSized("sheet");
			const dialog = screen.getByRole("dialog");
			expect(dialog).toHaveClass("bottom-0", "rounded-t-2xl", "md:top-1/2", "md:max-w-xl");
			expect(dialog).not.toHaveClass("top-1/2");
		});
	});

	describe("Nested dialogs", () => {
		it("should stack a child dialog above its parent", () => {
			renderWithTheme(
				<BgtDialog open={true} onClose={vi.fn()}>
					<BgtDialogContent>
						<BgtDialogTitle>Parent</BgtDialogTitle>
					</BgtDialogContent>
					<BgtDialog open={true} onClose={vi.fn()}>
						<BgtDialogContent>
							<BgtDialogTitle>Child</BgtDialogTitle>
						</BgtDialogContent>
					</BgtDialog>
				</BgtDialog>,
			);

			const parent = screen.getByText("Parent").closest("[role='dialog']") as HTMLElement;
			const child = screen.getByText("Child").closest("[role='dialog']") as HTMLElement;
			expect(Number(child.style.zIndex)).toBeGreaterThan(Number(parent.style.zIndex) + 10);
		});
	});

	describe("BgtDialogTitle", () => {
		it("should render title text", () => {
			renderWithTheme(
				<BgtDialog open={true} onClose={vi.fn()}>
					<BgtDialogContent>
						<BgtDialogTitle>My Dialog Title</BgtDialogTitle>
					</BgtDialogContent>
				</BgtDialog>,
			);
			expect(screen.getByText("My Dialog Title")).toBeInTheDocument();
		});

		it("should apply custom className", () => {
			renderWithTheme(
				<BgtDialog open={true} onClose={vi.fn()}>
					<BgtDialogContent>
						<BgtDialogTitle className="custom-title">Title</BgtDialogTitle>
					</BgtDialogContent>
				</BgtDialog>,
			);
			const title = screen.getByText("Title");
			expect(title).toHaveClass("custom-title");
		});
	});

	describe("BgtDialogDescription", () => {
		it("should render description text", () => {
			renderWithTheme(
				<BgtDialog open={true} onClose={vi.fn()}>
					<BgtDialogContent>
						<BgtDialogDescription>This is a description</BgtDialogDescription>
					</BgtDialogContent>
				</BgtDialog>,
			);
			expect(screen.getByText("This is a description")).toBeInTheDocument();
		});

		it("should apply custom className", () => {
			renderWithTheme(
				<BgtDialog open={true} onClose={vi.fn()}>
					<BgtDialogContent>
						<BgtDialogDescription className="custom-desc">Description</BgtDialogDescription>
					</BgtDialogContent>
				</BgtDialog>,
			);
			const desc = screen.getByText("Description");
			expect(desc).toHaveClass("custom-desc");
		});
	});

	describe("BgtDialogContent", () => {
		it("should close on Escape", async () => {
			const onClose = vi.fn();
			renderWithTheme(
				<BgtDialog open={true} onClose={onClose}>
					<BgtDialogContent>
						<BgtDialogTitle>Delete game</BgtDialogTitle>
					</BgtDialogContent>
				</BgtDialog>,
			);

			await userEvent.keyboard("{Escape}");

			expect(onClose).toHaveBeenCalledTimes(1);
		});

		it("should render children", () => {
			renderWithTheme(
				<BgtDialog open={true} onClose={vi.fn()}>
					<BgtDialogContent>
						<div data-testid="content-child">Content</div>
					</BgtDialogContent>
				</BgtDialog>,
			);
			expect(screen.getByTestId("content-child")).toBeInTheDocument();
		});

		it("should apply custom className", () => {
			renderWithTheme(
				<BgtDialog open={true} onClose={vi.fn()}>
					<BgtDialogContent className="custom-content" data-testid="dialog-content">
						Content
					</BgtDialogContent>
				</BgtDialog>,
			);
			const content = screen.getByTestId("dialog-content");
			expect(content).toHaveClass("custom-content");
		});
	});

	describe("Combined Usage", () => {
		it("should render full dialog structure", () => {
			renderWithTheme(
				<BgtDialog open={true} onClose={vi.fn()}>
					<BgtDialogContent>
						<BgtDialogTitle>Confirm Action</BgtDialogTitle>
						<BgtDialogDescription>Are you sure you want to proceed?</BgtDialogDescription>
						<button type="button">Cancel</button>
						<button type="button">Confirm</button>
					</BgtDialogContent>
				</BgtDialog>,
			);

			expect(screen.getByText("Confirm Action")).toBeInTheDocument();
			expect(screen.getByText("Are you sure you want to proceed?")).toBeInTheDocument();
			expect(screen.getByText("Cancel")).toBeInTheDocument();
			expect(screen.getByText("Confirm")).toBeInTheDocument();
		});
	});
});
