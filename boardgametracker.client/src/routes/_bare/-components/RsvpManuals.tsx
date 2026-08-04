import { useTranslation } from "react-i18next";
import Download from "@/assets/icons/download.svg?react";
import List from "@/assets/icons/list.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import type { GameNightManuals } from "@/models";
import { manualInviteDownloadUrl } from "@/services/manualService";
import { formatFileSize } from "@/utils/numberUtils";

interface Props {
	linkId: string;
	manuals: GameNightManuals[];
}

export const RsvpManuals = ({ linkId, manuals }: Props) => {
	const { t } = useTranslation("rsvp");

	if (manuals.length === 0) {
		return null;
	}

	return (
		<BgtCard className="gap-3">
			<BgtText size="2" color="gray" className="flex items-center gap-3">
				<List className="size-5 text-primary" />
				{t("manuals")}
			</BgtText>
			<div className="flex flex-col gap-4">
				{manuals.map((group) => (
					<div key={group.gameId} className="flex flex-col gap-2">
						<BgtText size="2" weight="medium" color="white">
							{group.gameTitle}
						</BgtText>
						{group.manuals.map((manual) => (
							<a
								key={manual.id}
								href={manualInviteDownloadUrl(linkId, manual.id)}
								download
								className="flex items-center gap-3 bg-white/5 rounded-lg p-3 hover:bg-white/10 transition-colors"
							>
								<Download className="size-5 text-primary shrink-0" />
								<div className="flex-1 min-w-0">
									<BgtText size="2" color="white" className="truncate">
										{manual.title}
									</BgtText>
									<BgtText size="1" color="gray">
										{formatFileSize(manual.fileSizeBytes)}
									</BgtText>
								</div>
							</a>
						))}
					</div>
				))}
			</div>
		</BgtCard>
	);
};
