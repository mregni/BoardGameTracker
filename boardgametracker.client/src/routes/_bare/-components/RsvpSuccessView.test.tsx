import { describe, expect, it, vi } from "vitest";
import { GameNightRsvpState } from "@/models";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { RsvpSuccessView } from "./RsvpSuccessView";

describe("RsvpSuccessView", () => {
	it("mentions the host notification only when the host is notified", () => {
		const { rerender } = renderWithTheme(
			<RsvpSuccessView
				playerName="Alice"
				response={GameNightRsvpState.Accepted}
				hostNotified={false}
				onChangeResponse={vi.fn()}
			/>,
		);
		expect(screen.queryByText(/submitted-notified/)).not.toBeInTheDocument();

		rerender(
			<RsvpSuccessView
				playerName="Alice"
				response={GameNightRsvpState.Accepted}
				hostNotified
				onChangeResponse={vi.fn()}
			/>,
		);
		expect(screen.getByText(/submitted-notified/)).toBeInTheDocument();
	});

	it("lets the responder go back and change the response", async () => {
		const user = userEvent.setup();
		const onChangeResponse = vi.fn();
		renderWithTheme(
			<RsvpSuccessView
				playerName="Alice"
				response={GameNightRsvpState.Declined}
				hostNotified={false}
				onChangeResponse={onChangeResponse}
			/>,
		);

		await user.click(screen.getByRole("button", { name: "change-response" }));

		expect(onChangeResponse).toHaveBeenCalledTimes(1);
	});
});
