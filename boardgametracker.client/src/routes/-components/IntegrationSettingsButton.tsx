import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { INTEGRATION_SETTINGS_LABEL, useIntegrationSettings } from "@/routes/-hooks/useIntegrationSettings";

export const IntegrationSettingsButton = () => {
	const { t } = useTranslation();
	const { openIntegrationSettings } = useIntegrationSettings();

	return (
		<BgtButton type="button" variant="cancel" onClick={openIntegrationSettings}>
			{t(INTEGRATION_SETTINGS_LABEL)}
		</BgtButton>
	);
};
