import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { render, screen, userEvent, waitFor } from "@/test/test-utils";
import { handleFormSubmit } from "./formUtils";

const scrollIntoView = vi.fn();
Element.prototype.scrollIntoView = scrollIntoView;

const RefusedForm = () => {
	const [refused, setRefused] = useState(false);

	return (
		<form onSubmit={handleFormSubmit({ handleSubmit: async () => setRefused(true) })}>
			<input aria-label="free" />
			<div>
				<input aria-label="first" />
				{refused && <div data-slot="form-error">first error</div>}
			</div>
			<div>
				<input aria-label="second" />
				{refused && <div data-slot="form-error">second error</div>}
			</div>
			<button type="submit">save</button>
		</form>
	);
};

describe("handleFormSubmit", () => {
	it("scrolls to the first error and focuses its field once a refused submit has rendered", async () => {
		const user = userEvent.setup();
		render(<RefusedForm />);

		await user.click(screen.getByRole("button", { name: "save" }));

		await waitFor(() => expect(screen.getByLabelText("first")).toHaveFocus());
		expect(scrollIntoView.mock.contexts).toEqual([screen.getByText("first error")]);
	});

	it("leaves the page alone when the form has no errors", async () => {
		const user = userEvent.setup();
		scrollIntoView.mockClear();
		const handleSubmit = vi.fn().mockResolvedValue(undefined);
		render(
			<form onSubmit={handleFormSubmit({ handleSubmit })}>
				<button type="submit">save</button>
			</form>,
		);

		await user.click(screen.getByRole("button", { name: "save" }));

		await waitFor(() => expect(handleSubmit).toHaveBeenCalled());
		await new Promise(requestAnimationFrame);
		expect(scrollIntoView).not.toHaveBeenCalled();
		expect(screen.getByRole("button", { name: "save" })).toHaveFocus();
	});
});
