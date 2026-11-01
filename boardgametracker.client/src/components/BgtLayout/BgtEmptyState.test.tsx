import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { BgtEmptyState } from "./BgtEmptyState";

const Icon = (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="empty-icon" {...props} />;

describe("BgtEmptyState", () => {
	it("is one short line with an action when there is no description", async () => {
		const user = userEvent.setup();
		const onClick = vi.fn();
		const { container } = renderWithTheme(
			<BgtEmptyState icon={Icon} title="No players yet" action={{ label: "Add player", onClick }} />,
		);

		expect(screen.getByRole("heading", { name: "No players yet" })).toBeInTheDocument();
		expect(container.querySelector("p")).toBeNull();
		await user.click(screen.getByRole("button", { name: "Add player" }));
		expect(onClick).toHaveBeenCalled();
	});

	it("still shows a description when one is given", () => {
		renderWithTheme(<BgtEmptyState icon={Icon} title="No sessions" description="Pick another period." />);

		expect(screen.getByText("Pick another period.")).toBeInTheDocument();
	});

	it("can be the page heading when the page has no header", () => {
		renderWithTheme(<BgtEmptyState icon={Icon} title="Could not find game" titleAs="h1" />);

		expect(screen.getByRole("heading", { level: 1, name: "Could not find game" })).toBeInTheDocument();
	});
});
