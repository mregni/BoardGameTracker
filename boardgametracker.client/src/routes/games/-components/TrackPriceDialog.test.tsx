import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent, waitFor, within } from "@/test/test-utils";
import { TrackPriceDialog } from "./TrackPriceDialog";

const renderDialog = (overrides: Partial<Parameters<typeof TrackPriceDialog>[0]> = {}) => {
	const props = {
		open: true,
		close: vi.fn(),
		initialUrl: null,
		isPending: false,
		onSubmit: vi.fn(() => Promise.resolve()),
		...overrides,
	};
	renderWithTheme(<TrackPriceDialog {...props} />);
	return props;
};

const footer = () => document.querySelector("[data-slot='dialog-footer']") as HTMLElement;
const submitButton = () => within(footer()).getByRole("button", { name: "game:track-price.submit" });
const urlInput = () => screen.getByRole("textbox", { name: "game:track-price.url-label" });

describe("TrackPriceDialog", () => {
	it("puts cancel on the left and the submit on the right", () => {
		renderDialog({ initialUrl: "https://shop.example.com/everdell" });

		const buttons = within(footer()).getAllByRole("button");
		expect(buttons.map((button) => button.textContent)).toEqual(["common:cancel", "game:track-price.submit"]);
		expect(buttons[1]).toHaveAttribute("type", "submit");
		expect(buttons[1]).toHaveClass("bg-primary/60");
	});

	it("closes when cancel is clicked", async () => {
		const user = userEvent.setup();
		const { close } = renderDialog();

		await user.click(within(footer()).getByRole("button", { name: "common:cancel" }));

		expect(close).toHaveBeenCalledTimes(1);
	});

	it("keeps the submit disabled until the url is a valid http address", async () => {
		const user = userEvent.setup();
		renderDialog();

		expect(submitButton()).toBeDisabled();
		await user.type(urlInput(), "shop.example.com/everdell");
		expect(submitButton()).toBeDisabled();

		await user.clear(urlInput());
		await user.type(urlInput(), "https://shop.example.com/everdell");
		expect(submitButton()).toBeEnabled();
	});

	it("submits the trimmed url", async () => {
		const user = userEvent.setup();
		const { onSubmit } = renderDialog();

		await user.type(urlInput(), "  https://shop.example.com/everdell  ");
		await user.click(submitButton());

		await waitFor(() => expect(onSubmit).toHaveBeenCalledWith("https://shop.example.com/everdell"));
	});

	it("starts from the url that is already tracked", () => {
		renderDialog({ initialUrl: "http://shop.example.com/root" });

		expect(urlInput()).toHaveValue("http://shop.example.com/root");
		expect(submitButton()).toBeEnabled();
	});

	it("disables the footer and the input while pending", () => {
		renderDialog({ initialUrl: "https://shop.example.com/everdell", isPending: true });

		expect(within(footer()).getByRole("button", { name: "common:cancel" })).toBeDisabled();
		expect(submitButton()).toBeDisabled();
		expect(urlInput()).toBeDisabled();
	});
});
