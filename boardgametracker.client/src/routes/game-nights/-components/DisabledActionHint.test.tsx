import { describe, expect, it } from "vitest";
import { render, screen, userEvent } from "@/test/test-utils";
import { DisabledActionHint } from "./DisabledActionHint";

describe("DisabledActionHint", () => {
	it("shows the reason after a touch on the disabled action", async () => {
		const user = userEvent.setup();
		render(<DisabledActionHint label="End night" hint="Finish the games first" />);
		const button = screen.getByRole("button", { name: "End night" });
		expect(button).toHaveAttribute("aria-disabled", "true");

		await user.pointer({ keys: "[TouchA]", target: button });

		expect(await screen.findByRole("tooltip")).toHaveTextContent("Finish the games first");
	});

	it("shows the reason when the keyboard reaches the disabled action", async () => {
		const user = userEvent.setup();
		render(<DisabledActionHint label="End night" hint="Finish the games first" />);

		await user.tab();

		expect(screen.getByRole("button", { name: "End night" })).toHaveFocus();
		expect(await screen.findByRole("tooltip")).toHaveTextContent("Finish the games first");
	});
});
