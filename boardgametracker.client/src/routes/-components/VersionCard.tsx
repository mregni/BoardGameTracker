import { useTranslation } from "react-i18next";

import Package from "@/assets/icons/package.svg?react";
import { BgtText } from "@/components/BgtText/BgtText";
import type { VersionInfo } from "@/models/Settings/VersionInfo";

interface Props {
	versionInfo: VersionInfo | undefined;
}

export const VersionCard = ({ versionInfo }: Props) => {
	const { t } = useTranslation("version");

	if (versionInfo === undefined) return null;

	if (versionInfo.updateAvailable) {
		return (
			<div className="mx-3 mb-3 rounded-lg bg-indigo-600 p-3 flex items-center gap-3">
				<div className="w-8 h-8 bg-white/20 rounded-lg flex items-center justify-center shrink-0">
					<Package className="size-5 text-white" />
				</div>
				<div className="flex-1 min-w-0">
					<BgtText size="2" weight="bold" color="white">
						{t("new")}
					</BgtText>
					<BgtText size="1" color="white" opacity={80} className="font-mono">
						v{versionInfo.latestVersion}
					</BgtText>
				</div>
			</div>
		);
	}

	return (
		<div className="flex items-center justify-between text-xs text-white/40 p-3 border-t border-white/10">
			<BgtText color="white" size="3" opacity={60}>
				{t("version")}
			</BgtText>
			<BgtText color="white" opacity={60} className="font-mono">
				v{versionInfo.currentVersion}
			</BgtText>
		</div>
	);
};
