import { describe, expect, it } from "vitest";
import { useAppForm } from "@/hooks/form";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";
import { gameFormOpts } from "../-utils/gameFormOpts";
import { GameFormCoopFields } from "./GameFormCoopFields";

const Harness = ({ hasCoopMode, coopByDefault }: { hasCoopMode: boolean; coopByDefault: boolean }) => {
	const form = useAppForm({
		...gameFormOpts,
		defaultValues: { ...gameFormOpts.defaultValues, hasCoopMode, coopByDefault },
	});
	return <GameFormCoopFields form={form} disabled={false} />;
};

describe("GameFormCoopFields", () => {
	it("hides the co-op default until the game can be played co-op", async () => {
		const user = userEvent.setup();
		renderWithProviders(<Harness hasCoopMode={false} coopByDefault={false} />);

		expect(screen.getByRole("switch", { name: "coop.mode.label" })).not.toBeChecked();
		expect(screen.queryByRole("switch", { name: "coop.default.label" })).not.toBeInTheDocument();

		await user.click(screen.getByRole("switch", { name: "coop.mode.label" }));

		expect(screen.getByRole("switch", { name: "coop.default.label" })).not.toBeChecked();
		expect(screen.getByText("coop.hint")).toBeInTheDocument();
	});

	it("shows both switches on for a co-op only game", () => {
		renderWithProviders(<Harness hasCoopMode coopByDefault />);

		expect(screen.getByRole("switch", { name: "coop.mode.label" })).toBeChecked();
		expect(screen.getByRole("switch", { name: "coop.default.label" })).toBeChecked();
	});
});
