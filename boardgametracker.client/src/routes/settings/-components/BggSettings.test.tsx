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

			for (const button of screen.getAllByRole("button", { name: "integrations.test" })) {
				expect(button).toBeDisabled();
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
