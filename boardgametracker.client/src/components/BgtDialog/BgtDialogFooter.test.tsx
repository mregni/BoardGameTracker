import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { BgtDialogFooter } from "./BgtDialogFooter";

const footerOf = (button: HTMLElement) => button.closest("[data-slot='dialog-footer']") as HTMLElement;

describe("BgtDialogFooter", () => {
	it("puts cancel on the left and the action on the right", () => {
		renderWithTheme(<BgtDialogFooter onCancel={vi.fn()} confirmLabel="Save" onConfirm={vi.fn()} />);

		const buttons = screen.getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "Save"]);
	});

	it("fills the row on phones and right-aligns natural-width buttons from md up", () => {
		renderWithTheme(<BgtDialogFooter onCancel={vi.fn()} confirmLabel="Save" />);

		const save = screen.getByRole("button", { name: "Save" });
		const cancel = screen.getByRole("button", { name: "common:cancel" });
		expect(footerOf(save)).toHaveClass("flex", "flex-row", "items-stretch", "gap-3", "md:justify-end");
		for (const button of [save, cancel]) {
			expect(button).toHaveClass("min-h-10", "flex-auto", "md:flex-none", "md:min-w-32");
		}
	});

	it("uses the cancel variant for cancel and primary for the action", () => {
		renderWithTheme(<BgtDialogFooter onCancel={vi.fn()} confirmLabel="Save" />);

		expect(screen.getByRole("button", { name: "common:cancel" })).toHaveClass("border-cancel/60");
		expect(screen.getByRole("button", { name: "Save" })).toHaveClass("bg-primary/60");
	});

	it("uses the red variant for destructive actions", () => {
		renderWithTheme(<BgtDialogFooter onCancel={vi.fn()} confirmLabel="Delete" variant="destructive" />);

		const remove = screen.getByRole("button", { name: "Delete" });
		expect(remove).toHaveClass("bg-error-strong");
		expect(remove).not.toHaveClass("bg-primary/60");
	});

	it("calls the handlers", async () => {
		const user = userEvent.setup();
		const onCancel = vi.fn();
		const onConfirm = vi.fn();
		renderWithTheme(<BgtDialogFooter onCancel={onCancel} confirmLabel="Save" onConfirm={onConfirm} />);

		await user.click(screen.getByRole("button", { name: "common:cancel" }));
		await user.click(screen.getByRole("button", { name: "Save" }));

		expect(onCancel).toHaveBeenCalledTimes(1);
		expect(onConfirm).toHaveBeenCalledTimes(1);
	});

	it("submits the linked form", async () => {
		const user = userEvent.setup();
		const onSubmit = vi.fn((event: React.FormEvent) => event.preventDefault());
		renderWithTheme(
			<>
				<form id="linked-form" onSubmit={onSubmit} />
				<BgtDialogFooter onCancel={vi.fn()} confirmLabel="Save" confirmType="submit" form="linked-form" />
			</>,
		);

		const save = screen.getByRole("button", { name: "Save" });
		expect(save).toHaveAttribute("type", "submit");
		expect(save).toHaveAttribute("form", "linked-form");
		await user.click(save);

		expect(onSubmit).toHaveBeenCalledTimes(1);
	});

	it("keeps cancel a plain button", () => {
		renderWithTheme(<BgtDialogFooter onCancel={vi.fn()} confirmLabel="Save" confirmType="submit" />);

		expect(screen.getByRole("button", { name: "common:cancel" })).toHaveAttribute("type", "button");
	});

	it("disables both buttons and shows a spinner while loading", () => {
		renderWithTheme(<BgtDialogFooter onCancel={vi.fn()} confirmLabel="Save" loading />);

		expect(screen.getByRole("button", { name: "common:cancel" })).toBeDisabled();
		expect(screen.getByRole("button", { name: "Save" })).toBeDisabled();
		expect(screen.getByTestId("bgt-spinner")).toBeInTheDocument();
	});

	it("disables each button on its own", () => {
		renderWithTheme(<BgtDialogFooter onCancel={vi.fn()} confirmLabel="Save" disabled />);

		expect(screen.getByRole("button", { name: "common:cancel" })).toBeEnabled();
		expect(screen.getByRole("button", { name: "Save" })).toBeDisabled();
	});

	it("can disable only cancel", () => {
		renderWithTheme(<BgtDialogFooter onCancel={vi.fn()} confirmLabel="Save" cancelDisabled />);

		expect(screen.getByRole("button", { name: "common:cancel" })).toBeDisabled();
		expect(screen.getByRole("button", { name: "Save" })).toBeEnabled();
	});

	it("renders only the action without a cancel handler", () => {
		renderWithTheme(<BgtDialogFooter confirmLabel="Done" />);

		expect(screen.getAllByRole("button")).toHaveLength(1);
		expect(screen.getByRole("button", { name: "Done" })).toBeInTheDocument();
	});

	it("renders only cancel without an action label", () => {
		renderWithTheme(<BgtDialogFooter onCancel={vi.fn()} cancelLabel="Close" />);

		expect(screen.getAllByRole("button")).toHaveLength(1);
		expect(screen.getByRole("button", { name: "Close" })).toBeInTheDocument();
	});

	it("accepts extra classes", () => {
		renderWithTheme(<BgtDialogFooter confirmLabel="Done" className="mt-4" />);

		expect(footerOf(screen.getByRole("button", { name: "Done" }))).toHaveClass("mt-4");
	});
});

describe("BgtDialogFooter focus colour", () => {
	it("gives the destructive action the bad focus ring and leaves cancel on the default", () => {
		renderWithTheme(<BgtDialogFooter onCancel={vi.fn()} confirmLabel="Delete" variant="destructive" />);

		expect(screen.getByRole("button", { name: "Delete" })).toHaveClass("[--focus-color:var(--color-bad)]");
		expect(screen.getByRole("button", { name: "common:cancel" }).className).not.toContain("--focus-color");
	});

	it("keeps the default focus ring on a primary action", () => {
		renderWithTheme(<BgtDialogFooter confirmLabel="Save" />);

		expect(screen.getByRole("button", { name: "Save" }).className).not.toContain("--focus-color");
	});
});
