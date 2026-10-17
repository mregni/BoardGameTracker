import { useId } from "react";
import { useTranslation } from "react-i18next";
import Refresh from "@/assets/icons/refresh.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtText } from "@/components/BgtText/BgtText";
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
	const hintId = useId();

	return (
		<div className="flex flex-row flex-wrap items-start justify-between gap-3">
			<IntegrationStatusBadge status={status} isChecking={isChecking || isTesting} timeFormat={timeFormat} />
			<div className="flex flex-col items-end gap-1">
				<BgtButton
					variant="cancel"
					type="button"
					disabled={!canTest || isTesting}
					aria-busy={isTesting}
					aria-describedby={canTest ? undefined : hintId}
					onClick={() => onTest(integration)}
				>
					<Refresh className={isTesting ? "size-4 animate-spin" : "size-4"} aria-hidden="true" />
					{isTesting ? t("integrations.testing") : t("integrations.test")}
				</BgtButton>
				{!canTest && (
					<BgtText id={hintId} size="1" color="white" opacity={50}>
						{t("integrations.test-hint")}
					</BgtText>
				)}
			</div>
		</div>
	);
};
