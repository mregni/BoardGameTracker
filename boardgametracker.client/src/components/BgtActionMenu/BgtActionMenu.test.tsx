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

	it("moves the focus into the menu and through the items with the arrow keys", async () => {
		const user = userEvent.setup();
		renderMenu();

		await user.click(screen.getByRole("button", { name: "Actions for jane" }));
		expect(screen.getByRole("menuitem", { name: "Edit" })).toHaveFocus();

		await user.keyboard("{ArrowDown}");
		expect(screen.getByRole("menuitem", { name: "Delete" })).toHaveFocus();
		await user.keyboard("{ArrowDown}");
		expect(screen.getByRole("menuitem", { name: "Edit" })).toHaveFocus();
		await user.keyboard("{End}");
		expect(screen.getByRole("menuitem", { name: "Delete" })).toHaveFocus();
		await user.keyboard("{Home}");
		expect(screen.getByRole("menuitem", { name: "Edit" })).toHaveFocus();
	});

	it("opens on the last item with ArrowUp from the trigger", async () => {
		const user = userEvent.setup();
		renderMenu();

		screen.getByRole("button", { name: "Actions for jane" }).focus();
		await user.keyboard("{ArrowUp}");

		expect(screen.getByRole("menuitem", { name: "Delete" })).toHaveFocus();
	});

	it("closes when Tab moves the focus away", async () => {
		const user = userEvent.setup();
		renderMenu();

		await user.click(screen.getByRole("button", { name: "Actions for jane" }));
		await user.tab();

		expect(screen.queryByRole("menu")).not.toBeInTheDocument();
	});

	it("separates the destructive item from the others", async () => {
		const user = userEvent.setup();
		renderMenu();

		await user.click(screen.getByRole("button", { name: "Actions for jane" }));

		expect(screen.getByRole("separator")).toBeInTheDocument();
	});

	it("toggles closed from the trigger", async () => {
		const user = userEvent.setup();
		renderMenu();

		const trigger = screen.getByRole("button", { name: "Actions for jane" });
		await user.click(trigger);
		await user.click(trigger);

		expect(screen.queryByRole("menu")).not.toBeInTheDocument();
	});
});
