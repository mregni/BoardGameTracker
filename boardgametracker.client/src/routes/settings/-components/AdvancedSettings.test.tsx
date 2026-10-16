import { describe, expect, it } from "vitest";
import { useAppForm } from "@/hooks/form";
import { renderWithProviders, screen, userEvent, within } from "@/test/test-utils";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { AdvancedSettings } from "./AdvancedSettings";

const Harness = () => {
	const form = useAppForm({
		...settingsFormOpts,
		defaultValues: { ...settingsFormOpts.defaultValues, versionTrack: "stable" },
	});

	return <AdvancedSettings form={form} disabled={false} overrides={{}} />;
};

describe("AdvancedSettings version tracks", () => {
	it("lays the stable and beta cards out as two equal, top-aligned columns", () => {
		renderWithProviders(<Harness />);

		const tracks = screen.getByTestId("version-tracks");
		expect(tracks).toHaveClass("md:grid-cols-2");
		const cards = within(tracks).getAllByRole("button");
		expect(cards).toHaveLength(2);
		for (const card of cards) {
			expect(card).toHaveClass("h-full", "flex-col", "justify-start", "items-start");
		}
	});

	it("marks the chosen track as pressed", async () => {
		const user = userEvent.setup();
		renderWithProviders(<Harness />);

		const [stable, beta] = within(screen.getByTestId("version-tracks")).getAllByRole("button");
		expect(stable).toHaveAttribute("aria-pressed", "true");
		expect(beta).toHaveAttribute("aria-pressed", "false");

		await user.click(beta);

		expect(beta).toHaveAttribute("aria-pressed", "true");
		expect(stable).toHaveAttribute("aria-pressed", "false");
	});
});
