import { beforeEach, describe, expect, it, vi } from "vitest";
import { useAppForm } from "@/hooks/form";
import type { BggConfigStatus, ChangeDetectionConfigStatus } from "@/models";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { BggSettings } from "./BggSettings";

const { toastSuccess, toastError, getStatusesCall, testIntegrationCall } = vi.hoisted(() => ({
	toastSuccess: vi.fn(),
	toastError: vi.fn(),
	getStatusesCall: vi.fn(),
	testIntegrationCall: vi.fn(),
}));

vi.mock("sonner", () => ({
	toast: { success: toastSuccess, error: toastError },
}));

vi.mock("@/services/integrationService", () => ({
	getIntegrationStatusesCall: getStatusesCall,
	testIntegrationCall: testIntegrationCall,
}));

const createBggStatus = (overrides: Partial<BggConfigStatus> = {}): BggConfigStatus => ({
	isConfigured: false,
	isReadOnly: false,
	source: "db",
	...overrides,
});

const readOnlyChangeDetectionStatus: ChangeDetectionConfigStatus = {
	isConfigured: true,
	isReadOnly: true,
	source: "env",
};

interface HarnessProps {
	bggStatus: BggConfigStatus;
	disabled?: boolean;
	initialApiKey?: string | null;
	changeDetectionStatus?: ChangeDetectionConfigStatus;
	initialChangeDetectionApiKey?: string | null;
}

const Harness = ({
	bggStatus,
	disabled = false,
	initialApiKey = null,
	changeDetectionStatus = readOnlyChangeDetectionStatus,
	initialChangeDetectionApiKey = null,
}: HarnessProps) => {
	const form = useAppForm({
		...settingsFormOpts,
		defaultValues: {
			...settingsFormOpts.defaultValues,
			bggApiKey: initialApiKey,
			changeDetectionApiKey: initialChangeDetectionApiKey,
		},
	});

	return (
		<>
			<BggSettings
				form={form}
				bggStatus={bggStatus}
				changeDetectionStatus={changeDetectionStatus}
				disabled={disabled}
				timeFormat="HH:mm"
			/>
			<form.Subscribe selector={(state) => state.values.bggApiKey}>
				{(value) => <span data-testid="bggApiKey-value">{value === null ? "null" : String(value)}</span>}
			</form.Subscribe>
			<form.Subscribe selector={(state) => state.values.changeDetectionApiKey}>
				{(value) => <span data-testid="cdApiKey-value">{value === null ? "null" : String(value)}</span>}
			</form.Subscribe>
		</>
	);
};

const cdConfigured: ChangeDetectionConfigStatus = { isConfigured: true, isReadOnly: false, source: "db" };
const cdNotConfigured: ChangeDetectionConfigStatus = { isConfigured: false, isReadOnly: false, source: "none" };

describe("BggSettings", () => {
	beforeEach(() => {
		getStatusesCall.mockResolvedValue([]);
	});

	describe("Section", () => {
		it("should render the section title", () => {
			renderWithProviders(<Harness bggStatus={createBggStatus()} />);
			expect(screen.getByText("bgg.title")).toBeInTheDocument();
		});

		it("should show the API key help as a field hint instead of an info box", () => {
			renderWithProviders(<Harness bggStatus={createBggStatus()} changeDetectionStatus={cdNotConfigured} />);
			expect(screen.queryByText("bgg.api-key.help-title")).not.toBeInTheDocument();
			expect(screen.getByText("changedetection.help-description")).toBeInTheDocument();
		});
	});

	describe("Not configured", () => {
		it("should not repeat the status in a warning banner", () => {
			renderWithProviders(<Harness bggStatus={createBggStatus({ isConfigured: false })} />);
			expect(screen.queryByText("bgg.status.not-configured")).not.toBeInTheDocument();
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
			expect(screen.getByText("bgg.api-key.clear-hint")).toBeInTheDocument();
		});

		it("should place the clear button on the same row as the API key input", () => {
			const { container } = renderWithProviders(<Harness bggStatus={status()} />);
			const input = container.querySelector('input[type="password"]');
			const row = screen.getByRole("button", { name: "bgg.api-key.clear" }).parentElement;
			expect(row).toContainElement(input as HTMLElement);
			expect(row).toContainElement(screen.getByText("bgg.api-key.label"));
		});
	});

	describe("Configured (read-only / env)", () => {
		const status = () => createBggStatus({ isConfigured: true, isReadOnly: true, source: "env" });

		it("should render the read-only info status", () => {
			renderWithProviders(<Harness bggStatus={status()} />);
			expect(screen.getByText("bgg.api-key.read-only")).toBeInTheDocument();
		});

		it("should not render the API key input field", () => {
			renderWithProviders(<Harness bggStatus={status()} />);
			expect(screen.queryByText("bgg.api-key.label")).not.toBeInTheDocument();
		});

		it("should not render the clear button", () => {
			renderWithProviders(<Harness bggStatus={status()} />);
			expect(screen.queryByRole("button", { name: "bgg.api-key.clear" })).not.toBeInTheDocument();
		});
	});

	describe("Change detection", () => {
		it("should render the API key input even when configured, so it can be rotated", () => {
			renderWithProviders(<Harness bggStatus={createBggStatus()} changeDetectionStatus={cdConfigured} />);
			expect(screen.getByText("changedetection.api-key.label")).toBeInTheDocument();
			expect(screen.getByRole("button", { name: "changedetection.api-key.clear" })).toBeInTheDocument();
		});

		it("should not render the clear button when not configured", () => {
			renderWithProviders(<Harness bggStatus={createBggStatus()} changeDetectionStatus={cdNotConfigured} />);
			expect(screen.getByText("changedetection.api-key.label")).toBeInTheDocument();
			expect(screen.queryByRole("button", { name: "changedetection.api-key.clear" })).not.toBeInTheDocument();
		});

		it("should null the API key on clear without hiding the input", async () => {
			const user = userEvent.setup();
			renderWithProviders(
				<Harness
					bggStatus={createBggStatus()}
					changeDetectionStatus={cdConfigured}
					initialChangeDetectionApiKey="existing-key"
				/>,
			);

			expect(screen.getByTestId("cdApiKey-value")).toHaveTextContent("existing-key");

			await user.click(screen.getByRole("button", { name: "changedetection.api-key.clear" }));

			expect(screen.getByTestId("cdApiKey-value")).toHaveTextContent("null");
			expect(screen.getByText("changedetection.api-key.label")).toBeInTheDocument();
			expect(screen.getByText("changedetection.api-key.clear-hint")).toBeInTheDocument();
		});

		it("should keep the base URL on its own row and the clear button next to the API key input", () => {
			renderWithProviders(<Harness bggStatus={createBggStatus()} changeDetectionStatus={cdConfigured} />);
			const row = screen.getByRole("button", { name: "changedetection.api-key.clear" }).parentElement;
			expect(row).toContainElement(screen.getByPlaceholderText("changedetection.api-key.placeholder"));
			expect(row).not.toContainElement(screen.getByPlaceholderText("changedetection.base-url.placeholder"));
		});

		it("should not show the clear hint before the key is cleared", () => {
			renderWithProviders(
				<Harness
					bggStatus={createBggStatus()}
					changeDetectionStatus={cdConfigured}
					initialChangeDetectionApiKey="existing-key"
				/>,
			);
			expect(screen.queryByText("changedetection.api-key.clear-hint")).not.toBeInTheDocument();
		});

		it("should not show the clear hint when nothing is configured", () => {
			renderWithProviders(<Harness bggStatus={createBggStatus()} changeDetectionStatus={cdNotConfigured} />);
			expect(screen.queryByText("changedetection.api-key.clear-hint")).not.toBeInTheDocument();
			expect(screen.queryByText("bgg.api-key.clear-hint")).not.toBeInTheDocument();
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

	describe("Integration status", () => {
		beforeEach(() => {
			vi.clearAllMocks();
			getStatusesCall.mockResolvedValue([
				{ integration: "bgg", status: "ok", checkedAt: "2026-10-16T09:30:00Z", messageKey: "ok" },
				{
					integration: "changeDetection",
					status: "notConfigured",
					checkedAt: "2026-10-16T09:30:00Z",
					messageKey: "not-configured",
				},
			]);
		});

		it("should show the status and test button in each card header", async () => {
			renderWithProviders(<Harness bggStatus={createBggStatus({ isConfigured: true })} />);
			await screen.findByText("integrations.status.ok");

			const header = screen.getByRole("heading", { name: "bgg.title" }).parentElement?.parentElement;
			expect(header).toContainElement(screen.getByText("bgg.description"));
			expect(header).toContainElement(screen.getByText("integrations.status.ok"));
			expect(header).toContainElement(screen.getAllByRole("button", { name: "integrations.test" })[0]);
			expect(header).not.toContainElement(screen.getByText("bgg.api-key.label"));
		});

		it("should show a status badge per integration when the tab opens", async () => {
			renderWithProviders(<Harness bggStatus={createBggStatus({ isConfigured: true })} />);

			expect(await screen.findByText("integrations.status.ok")).toBeInTheDocument();
			expect(screen.getByText("integrations.status.notConfigured")).toBeInTheDocument();
			expect(getStatusesCall).toHaveBeenCalledTimes(1);
		});

		it("should show the loading state while a test runs and the result inline afterwards", async () => {
			const user = userEvent.setup();
			let resolveTest: (value: unknown) => void = () => undefined;
			testIntegrationCall.mockReturnValue(
				new Promise((resolve) => {
					resolveTest = resolve;
				}),
			);
			renderWithProviders(<Harness bggStatus={createBggStatus({ isConfigured: true })} />);
			await screen.findByText("integrations.status.ok");

			await user.click(screen.getAllByRole("button", { name: "integrations.test" })[0]);

			const testing = await screen.findByRole("button", { name: "integrations.testing" });
			expect(testing).toBeDisabled();
			expect(testing).toHaveAttribute("aria-busy", "true");
			expect(testIntegrationCall).toHaveBeenCalledWith("bgg", expect.anything());

			resolveTest({
				integration: "bgg",
				status: "unauthorized",
				checkedAt: "2026-10-16T09:31:00Z",
				messageKey: "unauthorized",
			});

			expect(await screen.findByText("integrations.status.unauthorized")).toBeInTheDocument();
			expect(screen.getByText("integrations.reason.unauthorized")).toBeInTheDocument();
			expect(screen.queryByText("integrations.status.ok")).not.toBeInTheDocument();
			expect(screen.getAllByRole("button", { name: "integrations.test" })[0]).toBeEnabled();
			expect(toastError).not.toHaveBeenCalled();
		});

		it("should test changedetection.io on its own button", async () => {
			const user = userEvent.setup();
			testIntegrationCall.mockResolvedValue({
				integration: "changeDetection",
				status: "unreachable",
				checkedAt: "2026-10-16T09:31:00Z",
				messageKey: "timeout",
			});
			renderWithProviders(<Harness bggStatus={createBggStatus()} changeDetectionStatus={cdConfigured} />);
			await screen.findByText("integrations.status.ok");

			await user.click(screen.getAllByRole("button", { name: "integrations.test" })[1]);

			expect(await screen.findByText("integrations.status.unreachable")).toBeInTheDocument();
			expect(screen.getByText("integrations.reason.timeout")).toBeInTheDocument();
			expect(testIntegrationCall).toHaveBeenCalledWith("changeDetection", expect.anything());
		});

		it("should not allow a test before a key is saved", async () => {
			renderWithProviders(<Harness bggStatus={createBggStatus()} changeDetectionStatus={cdNotConfigured} />);
			await screen.findByText("integrations.status.ok");

			const buttons = screen.getAllByRole("button", { name: "integrations.test" });
			const hints = screen.getAllByText("integrations.test-hint");
			expect(hints).toHaveLength(2);
			buttons.forEach((button, index) => {
				expect(button).toBeDisabled();
				expect(button).toHaveAttribute("aria-describedby", hints[index].id);
			});
		});

		it("should not show the test hint once a key is saved", async () => {
			renderWithProviders(
				<Harness bggStatus={createBggStatus({ isConfigured: true })} changeDetectionStatus={cdConfigured} />,
			);
			await screen.findByText("integrations.status.ok");

			expect(screen.queryByText("integrations.test-hint")).not.toBeInTheDocument();
			for (const button of screen.getAllByRole("button", { name: "integrations.test" })) {
				expect(button).toBeEnabled();
				expect(button).not.toHaveAttribute("aria-describedby");
			}
		});

		it("should report a failure when the test request itself fails", async () => {
			const user = userEvent.setup();
			testIntegrationCall.mockRejectedValue(new Error("Network Error"));
			renderWithProviders(<Harness bggStatus={createBggStatus({ isConfigured: true })} />);
			await screen.findByText("integrations.status.ok");

			await user.click(screen.getAllByRole("button", { name: "integrations.test" })[0]);

			await vi.waitFor(() => expect(toastError).toHaveBeenCalledWith("integrations.test-failed"));
		});
	});
});
