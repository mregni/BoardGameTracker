import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent, waitFor, within } from "@/test/test-utils";
import { ExpansionsDialog } from "./ExpansionsDialog";

const renderDialog = (overrides: Partial<Parameters<typeof ExpansionsDialog>[0]> = {}) => {
	const props = {
		expansions: [
			{ id: 1, title: "Pearlbrook", bggId: 123 },
			{ id: 2, title: "Homebrew", bggId: null },
		],
		open: true,
		close: vi.fn(),
		canWrite: true,
		bggEnabled: true,
		onAddExpansion: vi.fn(),
		onAddManualExpansion: vi.fn(() => Promise.resolve(true)),
		onDeleteExpansion: vi.fn(),
		...overrides,
	};
	renderWithTheme(<ExpansionsDialog {...props} />);
	return props;
};

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;
const footerLabels = () =>
	within(footer())
		.getAllByRole("button")
		.map((button) => button.textContent);

describe("ExpansionsDialog", () => {
	it("puts close on the left and add on the right when the user can write and bgg is enabled", () => {
		renderDialog();

		expect(footerLabels()).toEqual(["common:close", "expansions.add"]);
		expect(within(footer()).getByRole("button", { name: "expansions.add" })).toHaveClass("bg-primary/60");
	});

	it("only shows close when bgg is disabled", () => {
		renderDialog({ bggEnabled: false });

		expect(footerLabels()).toEqual(["common:close"]);
	});

	it("only shows close when the user cannot write", () => {
		renderDialog({ canWrite: false });

		expect(footerLabels()).toEqual(["common:close"]);
	});

	it("closes from the footer", async () => {
		const user = userEvent.setup();
		const { close } = renderDialog();

		await user.click(within(footer()).getByRole("button", { name: "common:close" }));

		expect(close).toHaveBeenCalledTimes(1);
	});

	it("starts adding an expansion from the footer", async () => {
		const user = userEvent.setup();
		const { onAddExpansion } = renderDialog();

		await user.click(within(footer()).getByRole("button", { name: "expansions.add" }));

		expect(onAddExpansion).toHaveBeenCalledTimes(1);
	});

	it("lists the expansions and marks the manual one", () => {
		renderDialog();

		expect(screen.getByText("expansions.title (2)")).toBeInTheDocument();
		expect(screen.getByText("Pearlbrook")).toBeInTheDocument();
		expect(screen.getByText("Homebrew")).toBeInTheDocument();
		expect(screen.getAllByText("expansions.manual.badge")).toHaveLength(1);
	});

	it("adds a manual expansion with the trimmed title", async () => {
		const user = userEvent.setup();
		const { onAddManualExpansion } = renderDialog();

		const input = screen.getByRole("textbox", { name: "expansions.manual.placeholder" });
		await user.type(input, "  Spirecrest  ");
		await user.click(screen.getByRole("button", { name: "expansions.manual.add" }));

		expect(onAddManualExpansion).toHaveBeenCalledWith("Spirecrest");
		await waitFor(() => expect(input).toHaveValue(""));
	});
});
