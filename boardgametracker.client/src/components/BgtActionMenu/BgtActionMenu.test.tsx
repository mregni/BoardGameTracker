import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { BgtActionMenu } from "./BgtActionMenu";

const renderMenu = () => {
	const edit = vi.fn();
	const remove = vi.fn();
	renderWithTheme(
		<div>
			<BgtActionMenu
				label="Actions for jane"
				items={[
					{ label: "Edit", onSelect: edit },
					{ label: "Delete", onSelect: remove, destructive: true },
				]}
			/>
			<button type="button">outside</button>
		</div>,
	);
	return { edit, remove };
};

describe("BgtActionMenu", () => {
	it("opens the items from the trigger and runs the chosen one", async () => {
		const user = userEvent.setup();
		const { edit, remove } = renderMenu();

		const trigger = screen.getByRole("button", { name: "Actions for jane" });
		expect(trigger).toHaveAttribute("aria-expanded", "false");
		await user.click(trigger);

		expect(trigger).toHaveAttribute("aria-expanded", "true");
		expect(screen.getByRole("menuitem", { name: "Delete" })).toHaveClass("text-bad");
		await user.click(screen.getByRole("menuitem", { name: "Edit" }));

		expect(edit).toHaveBeenCalled();
		expect(remove).not.toHaveBeenCalled();
		expect(screen.queryByRole("menu")).not.toBeInTheDocument();
	});

	it("closes on Escape and gives the focus back to the trigger", async () => {
		const user = userEvent.setup();
		renderMenu();

		const trigger = screen.getByRole("button", { name: "Actions for jane" });
		await user.click(trigger);
		await user.keyboard("{Escape}");

		expect(screen.queryByRole("menu")).not.toBeInTheDocument();
		expect(trigger).toHaveFocus();
	});

	it("closes when clicking elsewhere", async () => {
		const user = userEvent.setup();
		renderMenu();

		await user.click(screen.getByRole("button", { name: "Actions for jane" }));
		await user.click(screen.getByRole("button", { name: "outside" }));

		expect(screen.queryByRole("menu")).not.toBeInTheDocument();
	});

	it("toggles closed from the trigger", async () => {
		const user = userEvent.setup();
		renderMenu();

		const trigger = screen.getByRole("button", { name: "Actions for jane" });
		await user.click(trigger);
		await user.click(trigger);

		expect(screen.queryByRole("menu")).not.toBeInTheDocument();
	});

	it("moves the focus into the menu and through the items with the arrow keys", async () => {
		const user = userEvent.setup();
		renderMenu();

		await user.click(screen.getByRole("button", { name: "Actions for jane" }));
		const edit = screen.getByRole("menuitem", { name: "Edit" });
		const remove = screen.getByRole("menuitem", { name: "Delete" });
		expect(edit).toHaveFocus();

		await user.keyboard("{ArrowDown}");
		expect(remove).toHaveFocus();
		await user.keyboard("{ArrowDown}");
		expect(edit).toHaveFocus();
		await user.keyboard("{ArrowUp}");
		expect(remove).toHaveFocus();
		await user.keyboard("{Home}");
		expect(edit).toHaveFocus();
		await user.keyboard("{End}");
		expect(remove).toHaveFocus();
	});

	it("closes when tabbing out of the menu", async () => {
		const user = userEvent.setup();
		renderMenu();

		await user.click(screen.getByRole("button", { name: "Actions for jane" }));
		await user.tab();

		expect(screen.queryByRole("menu")).not.toBeInTheDocument();
	});
});
