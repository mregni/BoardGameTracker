import { describe, expect, it } from "vitest";
import { useAppForm } from "@/hooks/form";
import type { BggConfigStatus } from "@/models";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { BggSettings } from "./BggSettings";

const createBggStatus = (overrides: Partial<BggConfigStatus> = {}): BggConfigStatus => ({
	isConfigured: false,
	isReadOnly: false,
	source: "db",
	...overrides,
});

interface HarnessProps {
	bggStatus: BggConfigStatus;
	disabled?: boolean;
	initialApiKey?: string | null;
}

const Harness = ({ bggStatus, disabled = false, initialApiKey = null }: HarnessProps) => {
	const form = useAppForm({
		...settingsFormOpts,
		defaultValues: { ...settingsFormOpts.defaultValues, bggApiKey: initialApiKey },
	});

	return (
		<>
			<BggSettings form={form} bggStatus={bggStatus} disabled={disabled} />
			<form.Subscribe selector={(state) => state.values.bggApiKey}>
				{(value) => <span data-testid="bggApiKey-value">{value === null ? "null" : String(value)}</span>}
			</form.Subscribe>
		</>
	);
};

describe("BggSettings", () => {
	describe("Section", () => {
		it("should render the section title", () => {
			renderWithProviders(<Harness bggStatus={createBggStatus()} />);
			expect(screen.getByText("bgg.title")).toBeInTheDocument();
		});

		it("should always render the API key help status", () => {
			renderWithProviders(<Harness bggStatus={createBggStatus()} />);
			expect(screen.getByText("bgg.api-key.help-title")).toBeInTheDocument();
		});
	});

	describe("Not configured", () => {
		it("should show the not-configured warning status", () => {
			renderWithProviders(<Harness bggStatus={createBggStatus({ isConfigured: false })} />);
			expect(screen.getByText("bgg.status.not-configured")).toBeInTheDocument();
			expect(screen.getByText("bgg.status.not-configured-description")).toBeInTheDocument();
		});

		it("should render the API key input field", () => {
			const { container } = renderWithProviders(<Harness bggStatus={createBggStatus({ isConfigured: false })} />);
			expect(screen.getByText("bgg.api-key.label")).toBeInTheDocument();
			expect(container.querySelector('input[type="password"]')).toBeInTheDocument();
		});

		it("should not render the clear button", () => {
			renderWithProviders(<Harness bggStatus={createBggStatus({ isConfigured: false })} />);
			expect(screen.queryByRole("button", { name: "bgg.api-key.clear" })).not.toBeInTheDocument();
		});
	});

	describe("Configured (editable)", () => {
		const status = () => createBggStatus({ isConfigured: true, isReadOnly: false });

		it("should show the configured success status with db source", () => {
			renderWithProviders(<Harness bggStatus={status()} />);
			expect(screen.getByText("bgg.status.configured")).toBeInTheDocument();
			expect(screen.getByText("bgg.status.source-db")).toBeInTheDocument();
		});

		it("should render the clear button", () => {
			renderWithProviders(<Harness bggStatus={status()} />);
			expect(screen.getByRole("button", { name: "bgg.api-key.clear" })).toBeInTheDocument();
		});

		it("should render the API key input field", () => {
			const { container } = renderWithProviders(<Harness bggStatus={status()} />);
			expect(container.querySelector('input[type="password"]')).toBeInTheDocument();
		});

		it("should clear the API key when the clear button is clicked", async () => {
			const user = userEvent.setup();
			renderWithProviders(<Harness bggStatus={status()} initialApiKey="existing-key" />);

			expect(screen.getByTestId("bggApiKey-value")).toHaveTextContent("existing-key");

			await user.click(screen.getByRole("button", { name: "bgg.api-key.clear" }));

			expect(screen.getByTestId("bggApiKey-value")).toHaveTextContent("null");
		});
	});

	describe("Configured (read-only / env)", () => {
		const status = () => createBggStatus({ isConfigured: true, isReadOnly: true, source: "env" });

		it("should show the configured success status with env source", () => {
			renderWithProviders(<Harness bggStatus={status()} />);
			expect(screen.getByText("bgg.status.configured")).toBeInTheDocument();
			expect(screen.getByText("bgg.status.source-env")).toBeInTheDocument();
		});

		it("should render the read-only info status", () => {
			renderWithProviders(<Harness bggStatus={status()} />);
			expect(screen.getByText("bgg.api-key.read-only")).toBeInTheDocument();
		});

		it("should not render the API key input field", () => {
			const { container } = renderWithProviders(<Harness bggStatus={status()} />);
			expect(container.querySelector('input[type="password"]')).not.toBeInTheDocument();
		});

		it("should not render the clear button", () => {
			renderWithProviders(<Harness bggStatus={status()} />);
			expect(screen.queryByRole("button", { name: "bgg.api-key.clear" })).not.toBeInTheDocument();
		});
	});

	describe("Disabled state", () => {
		it("should disable the API key input when disabled is true", () => {
			const { container } = renderWithProviders(
				<Harness bggStatus={createBggStatus({ isConfigured: false })} disabled />,
			);
			expect(container.querySelector('input[type="password"]')).toBeDisabled();
		});
	});
});
