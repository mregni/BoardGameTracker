import { describe, expect, it } from "vitest";
import { render, screen, userEvent } from "@/test/test-utils";
import { DisabledActionHint } from "./DisabledActionHint";

describe("DisabledActionHint", () => {
	it("shows the reason after a touch on the disabled action", async () => {
		const user = userEvent.setup();
		render(<DisabledActionHint label="End night" hint="Finish the games first" />);
		expect(screen.getByRole("button", { name: "End night" })).toBeDisabled();

		await user.pointer({ keys: "[TouchA]", target: screen.getByTitle("Finish the games first") });

		expect(await screen.findByRole("tooltip")).toHaveTextContent("Finish the games first");
	});
});
