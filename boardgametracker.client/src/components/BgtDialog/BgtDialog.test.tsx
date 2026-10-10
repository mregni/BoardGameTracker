import { describe, expect, it, vi } from "vitest";
import { render, renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { BgtDialog, BgtDialogClose, BgtDialogContent, BgtDialogDescription, BgtDialogTitle } from "./BgtDialog";

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

	describe("BgtDialogClose", () => {
		it("should render children", () => {
			render(
				<BgtDialogClose>
					<button type="button">Cancel</button>
					<button type="button">Confirm</button>
				</BgtDialogClose>,
			);
			expect(screen.getByText("Cancel")).toBeInTheDocument();
			expect(screen.getByText("Confirm")).toBeInTheDocument();
		});

		it("should apply custom className", () => {
			const { container } = render(
				<BgtDialogClose className="custom-close">
					<button type="button">Cancel</button>
				</BgtDialogClose>,
			);
			const wrapper = container.firstChild as HTMLElement;
			expect(wrapper).toHaveClass("custom-close");
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
						<BgtDialogClose>
							<button type="button">Cancel</button>
							<button type="button">Confirm</button>
						</BgtDialogClose>
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
