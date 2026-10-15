import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { render, renderWithTheme, screen, userEvent } from "@/test/test-utils";
import {
	BgtDialog,
	BgtDialogClose,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogTitle,
	useDialogPortalContainer,
} from "./BgtDialog";

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

	describe("Accessibility", () => {
		const Opener = ({ onEscape }: { onEscape?: (event: KeyboardEvent) => void }) => {
			const [open, setOpen] = useState(false);
			return (
				<>
					<button type="button" onClick={() => setOpen(true)}>
						Open
					</button>
					<BgtDialog open={open} onClose={() => setOpen(false)}>
						<BgtDialogContent onEscapeKeyDown={onEscape}>
							<BgtDialogTitle>Edit loan</BgtDialogTitle>
							<BgtDialogDescription>Change the dates</BgtDialogDescription>
							<button type="button">Save</button>
						</BgtDialogContent>
					</BgtDialog>
				</>
			);
		};

		it("names and describes the dialog", async () => {
			const user = userEvent.setup();
			renderWithTheme(<Opener />);

			await user.click(screen.getByRole("button", { name: "Open" }));

			const dialog = screen.getByRole("dialog", { name: "Edit loan" });
			expect(dialog).toHaveAccessibleDescription("Change the dates");
		});

		it("gives focus back to the button that opened it after Escape", async () => {
			const user = userEvent.setup();
			const onEscape = vi.fn();
			renderWithTheme(<Opener onEscape={onEscape} />);
			const opener = screen.getByRole("button", { name: "Open" });

			await user.click(opener);
			expect(screen.getByRole("dialog")).toContainElement(document.activeElement as HTMLElement);
			await user.keyboard("{Escape}");

			expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
			expect(onEscape).toHaveBeenCalledTimes(1);
			expect(opener).toHaveFocus();
		});

		it("gives focus back to the opener after the close button", async () => {
			const user = userEvent.setup();
			renderWithTheme(<Opener />);
			const opener = screen.getByRole("button", { name: "Open" });

			await user.click(opener);
			await user.click(screen.getByRole("button", { name: "close" }));

			expect(opener).toHaveFocus();
		});

		it("leaves Escape to a popover that is open inside the dialog", async () => {
			const onClose = vi.fn();
			renderWithTheme(
				<BgtDialog open={true} onClose={onClose}>
					<BgtDialogContent>
						<BgtDialogTitle>Lend a game</BgtDialogTitle>
						<div data-trigger="DatePicker">
							<button type="button">16</button>
						</div>
					</BgtDialogContent>
				</BgtDialog>,
			);

			screen.getByRole("button", { name: "16" }).focus();
			await userEvent.keyboard("{Escape}");

			expect(onClose).not.toHaveBeenCalled();
		});

		it("offers its own element as the place for popovers", () => {
			const Probe = () => {
				const container = useDialogPortalContainer();
				return <span data-testid="probe">{container?.getAttribute("role")}</span>;
			};
			renderWithTheme(
				<BgtDialog open={true} onClose={vi.fn()}>
					<BgtDialogContent>
						<BgtDialogTitle>Lend a game</BgtDialogTitle>
						<Probe />
					</BgtDialogContent>
				</BgtDialog>,
			);

			expect(screen.getByTestId("probe")).toHaveTextContent("dialog");
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
