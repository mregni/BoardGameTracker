import type { ComponentProps } from "react";
import { describe, expect, it, vi } from "vitest";
import { GameNightRsvpState } from "@/models";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { RsvpSuccessView } from "./RsvpSuccessView";

const props = (overrides: Partial<ComponentProps<typeof RsvpSuccessView>> = {}) => ({
	playerName: "Alice",
	response: GameNightRsvpState.Accepted,
	hostNotified: false,
	whenAndWhere: "Sunday 11-10-2026 · 19:30 · Thuis",
	calendarHref: "data:text/calendar;charset=utf-8,EVENT",
	calendarFileName: "Fun game.ics",
	onChangeResponse: vi.fn(),
	...overrides,
});

describe("RsvpSuccessView", () => {
	it("mentions the host notification only when the host is notified", () => {
		const { rerender } = renderWithTheme(<RsvpSuccessView {...props()} />);
		expect(screen.queryByText(/host-notified/)).not.toBeInTheDocument();

		rerender(<RsvpSuccessView {...props({ hostNotified: true })} />);
		expect(screen.getByText(/host-notified/)).toBeInTheDocument();
	});

	it.each([
		[GameNightRsvpState.Accepted, "answered-accepted"],
		[GameNightRsvpState.Maybe, "answered-maybe"],
		[GameNightRsvpState.Pending, "answered-pending"],
		[GameNightRsvpState.Declined, "answered-declined"],
	])("confirms a %s answer", (state, message) => {
		renderWithTheme(<RsvpSuccessView {...props({ response: state })} />);

		expect(screen.getByRole("heading", { name: message })).toBeInTheDocument();
	});

	it.each([
		[GameNightRsvpState.Accepted, true],
		[GameNightRsvpState.Maybe, true],
		[GameNightRsvpState.Declined, false],
	])("offers the calendar file after a %s answer: %s", (state, offered) => {
		renderWithTheme(<RsvpSuccessView {...props({ response: state })} />);

		const link = screen.queryByRole("link", { name: "add-to-calendar" });
		if (offered) {
			expect(link).toHaveAttribute("href", "data:text/calendar;charset=utf-8,EVENT");
			expect(link).toHaveAttribute("download", "Fun game.ics");
		} else {
			expect(link).not.toBeInTheDocument();
		}
	});

	it("follows up in the tone of the answer", () => {
		renderWithTheme(<RsvpSuccessView {...props({ response: GameNightRsvpState.Declined })} />);

		expect(screen.getByText(/followup-declined/)).toBeInTheDocument();
	});

	it("lets the responder go back and change the response", async () => {
		const user = userEvent.setup();
		const onChangeResponse = vi.fn();
		renderWithTheme(<RsvpSuccessView {...props({ response: GameNightRsvpState.Declined, onChangeResponse })} />);

		await user.click(screen.getByRole("button", { name: "change-response" }));

		expect(onChangeResponse).toHaveBeenCalledTimes(1);
	});
});

describe("RsvpSuccessView focus", () => {
	it("repeats when and where, and moves focus to the confirmation", () => {
		renderWithTheme(<RsvpSuccessView {...props()} />);

		expect(screen.getByTestId("rsvp-when-where")).toHaveTextContent("Sunday 11-10-2026 · 19:30 · Thuis");
		expect(screen.getByRole("heading", { name: "answered-accepted" })).toHaveFocus();
	});
});
