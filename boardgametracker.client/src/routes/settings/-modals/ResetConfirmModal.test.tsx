import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent, within } from "@/test/test-utils";
import { ResetConfirmModal } from "./ResetConfirmModal";

const renderModal = (overrides: Partial<Parameters<typeof ResetConfirmModal>[0]> = {}) => {
	const props = {
		open: true,
		close: vi.fn(),
		onConfirm: vi.fn(() => Promise.resolve()),
		title: "Reset everything",
		description: "This removes all data",
		keyword: "RESET",
		inputLabel: "Type RESET",
		confirmLabel: "Reset",
		...overrides,
	};
	renderWithTheme(<ResetConfirmModal {...props} />);
	return props;
};

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;
const confirmButton = () => within(footer()).getByRole("button", { name: "Reset" });
const keywordInput = () => screen.getByRole("textbox", { name: "Type RESET" });

describe("ResetConfirmModal", () => {
	it("starts with the focus in the confirm field", async () => {
		renderModal();

		await vi.waitFor(() => expect(keywordInput()).toHaveFocus());
	});

	it("shows an optional note with a link that opens in a new tab", () => {
		renderModal({ note: { text: "The admin comes back.", linkLabel: "How?", href: "https://example.com/docs" } });

		expect(screen.getByText(/The admin comes back\./)).toBeInTheDocument();
		const link = screen.getByRole("link", { name: "How?" });
		expect(link).toHaveAttribute("href", "https://example.com/docs");
		expect(link).toHaveAttribute("target", "_blank");
	});

	it("shows no note when none is given", () => {
		renderModal();

		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});

	it("puts cancel on the left and the confirm on the right", () => {
		renderModal();

		expect(
			within(footer())
				.getAllByRole("button")
				.map((button) => button.textContent),
		).toEqual(["common:cancel", "Reset"]);
	});

	it("keeps the confirm disabled until the keyword is typed", async () => {
		const user = userEvent.setup();
		renderModal();

		expect(confirmButton()).toBeDisabled();
		await user.type(keywordInput(), "reset");
		expect(confirmButton()).toBeDisabled();

		await user.clear(keywordInput());
		await user.type(keywordInput(), "RESET");
		expect(confirmButton()).toBeEnabled();
	});

	it("uses the destructive variant once the confirm is enabled", async () => {
		const user = userEvent.setup();
		renderModal();

		await user.type(keywordInput(), "RESET");

		expect(confirmButton()).toHaveClass("bg-error-strong");
		expect(confirmButton()).not.toHaveClass("bg-primary/60");
	});

	it("confirms after the keyword is typed", async () => {
		const user = userEvent.setup();
		const { onConfirm } = renderModal();

		await user.type(keywordInput(), "RESET");
		await user.click(confirmButton());

		expect(onConfirm).toHaveBeenCalledTimes(1);
	});

	it("closes from cancel without confirming", async () => {
		const user = userEvent.setup();
		const { close, onConfirm } = renderModal();

		await user.click(within(footer()).getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalledTimes(1);
		expect(onConfirm).not.toHaveBeenCalled();
	});

	it("keeps the confirm disabled while loading", async () => {
		const user = userEvent.setup();
		renderModal({ isLoading: true });

		await user.type(keywordInput(), "RESET");

		expect(confirmButton()).toBeDisabled();
	});
});
