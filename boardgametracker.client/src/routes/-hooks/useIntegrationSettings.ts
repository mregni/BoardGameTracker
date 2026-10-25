import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";
import type { ToastAction } from "./useToasts";

export const INTEGRATION_SETTINGS_LABEL = "common:open-integration-settings";

export const useIntegrationSettings = () => {
	const navigate = useNavigate();

	const openIntegrationSettings = useCallback(() => {
		void navigate({ to: "/settings", search: { tab: "integrations" } });
	}, [navigate]);

	const integrationSettingsAction: ToastAction = {
		label: INTEGRATION_SETTINGS_LABEL,
		onClick: openIntegrationSettings,
	};

	return { openIntegrationSettings, integrationSettingsAction };
};
