import { describe, expect, it } from "vitest";
import { render, screen } from "@/test/test-utils";
import { SettingsSection } from "./SettingsSection";

describe("SettingsSection", () => {
	it("renders the title, description and content in a card", () => {
		render(
			<SettingsSection title="Title" description="Description">
				<p>Content</p>
			</SettingsSection>,
		);

		expect(screen.getByRole("heading", { name: "Title" })).toBeInTheDocument();
		expect(screen.getByText("Description")).toBeInTheDocument();
		expect(screen.getByText("Content")).toBeInTheDocument();
	});

	it("places the aside in the header next to the title and description", () => {
		render(
			<SettingsSection title="Title" description="Description" aside={<button type="button">Aside</button>}>
				<p>Content</p>
			</SettingsSection>,
		);

		const heading = screen.getByRole("heading", { name: "Title" });
		const header = heading.parentElement?.parentElement;
		expect(header).toContainElement(screen.getByText("Description"));
		expect(header).toContainElement(screen.getByRole("button", { name: "Aside" }));
		expect(header).not.toContainElement(screen.getByText("Content"));
	});

	it("renders an aside without a description", () => {
		render(
			<SettingsSection title="Title" aside={<span>Aside</span>}>
				<p>Content</p>
			</SettingsSection>,
		);

		expect(screen.getByRole("heading", { name: "Title" })).toBeInTheDocument();
		expect(screen.getByText("Aside")).toBeInTheDocument();
	});
});
