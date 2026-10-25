import { useTranslation } from "react-i18next";
import Refresh from "@/assets/icons/refresh.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import type { IntegrationKind, IntegrationStatus } from "@/models/Settings/IntegrationStatus";
import { IntegrationStatusBadge } from "./IntegrationStatusBadge";

interface Props {
	integration: IntegrationKind;
	status: IntegrationStatus | undefined;
	isChecking: boolean;
	isTesting: boolean;
	canTest: boolean;
	timeFormat: string;
	onTest: (integration: IntegrationKind) => void;
}

export const IntegrationStatusRow = ({
	integration,
	status,
	isChecking,
	isTesting,
	canTest,
	timeFormat,
	onTest,
}: Props) => {
	const { t } = useTranslation("settings");
	const hint = t(
		integration === "changeDetection" ? "integrations.test-hint-change-detection" : "integrations.test-hint",
	);

	return (
		<div className="flex flex-row flex-wrap items-center gap-3 @2xl:flex-nowrap @2xl:justify-end">
			<IntegrationStatusBadge status={status} isChecking={isChecking || isTesting} timeFormat={timeFormat} />
			<div className="flex flex-col items-start gap-1 @2xl:items-end">
				<BgtButton
					variant="cancel"
					type="button"
					disabled={!canTest || isTesting}
					aria-busy={isTesting}
					title={canTest ? undefined : hint}
					onClick={() => onTest(integration)}
				>
					<Refresh className={isTesting ? "size-4 animate-spin" : "size-4"} aria-hidden="true" />
					{isTesting ? t("integrations.testing") : t("integrations.test")}
				</BgtButton>
			</div>
		</div>
	);
};
