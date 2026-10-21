import { cva } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import AlertTriangle from "@/assets/icons/alert-triangle.svg?react";
import Check from "@/assets/icons/check.svg?react";
import { BgtText } from "@/components/BgtText/BgtText";
import {
	type IntegrationConnectionStatus,
	type IntegrationStatus,
	isIntegrationFailure,
} from "@/models/Settings/IntegrationStatus";
import { toDisplayTime } from "@/utils/dateUtils";

type Tone = "ok" | "bad" | "warn" | "muted";

const badgeVariants = cva(
	"inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-semibold whitespace-nowrap",
	{
		variants: {
			tone: {
				ok: "border-ok/30 bg-ok/15 text-ok",
				bad: "border-bad/30 bg-bad/15 text-bad",
				warn: "border-warn/30 bg-warn/15 text-warn",
				muted: "border-white/10 bg-white/5 text-white/60",
			},
		},
	},
);

const TONES: Record<IntegrationConnectionStatus, Tone> = {
	ok: "ok",
	unauthorized: "bad",
	unreachable: "warn",
	rateLimited: "warn",
	notConfigured: "muted",
};

interface Props {
	status: IntegrationStatus | undefined;
	isChecking: boolean;
	timeFormat: string;
}

export const IntegrationStatusBadge = ({ status, isChecking, timeFormat }: Props) => {
	const { t, i18n } = useTranslation("settings");

	if (!status) {
		return isChecking ? (
			<span className={badgeVariants({ tone: "muted" })} role="status">
				{t("integrations.checking")}
			</span>
		) : null;
	}

	const tone = TONES[status.status];
	const Icon = tone === "ok" ? Check : AlertTriangle;

	return (
		<div className="flex flex-col gap-1 @2xl:items-end @2xl:text-right" role="status" aria-busy={isChecking}>
			<div className="flex flex-row flex-wrap items-center gap-2 @2xl:justify-end">
				<span className={badgeVariants({ tone })} data-tone={tone}>
					{tone !== "muted" && <Icon className="size-3.5" aria-hidden="true" />}
					{t(`integrations.status.${status.status}`)}
				</span>
				{status.status !== "notConfigured" && (
					<BgtText size="1" color="white" opacity={50}>
						{isChecking
							? t("integrations.checking")
							: t("integrations.checked-at", {
									time: toDisplayTime(status.checkedAt, timeFormat, i18n.language),
								})}
					</BgtText>
				)}
			</div>
			{isIntegrationFailure(status) && (
				<BgtText size="1" color="white" opacity={70}>
					{t(`integrations.reason.${status.messageKey}`)}
				</BgtText>
			)}
		</div>
	);
};
