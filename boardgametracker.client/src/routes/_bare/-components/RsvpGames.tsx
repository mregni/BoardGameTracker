import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import Download from "@/assets/icons/download.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { Game, GameNightManuals } from "@/models";
import { manualInviteDownloadUrl } from "@/services/manualService";

interface Props {
	linkId: string;
	games: Game[];
	manuals: GameNightManuals[];
}

export const RsvpGames = ({ linkId, games, manuals }: Props) => {
	const { t } = useTranslation("rsvp");
	const { fileSize } = useNumberFormat();
	const manualCount = manuals.reduce((total, group) => total + group.manuals.length, 0);

	if (games.length === 0 && manualCount === 0) {
		return null;
	}

	return (
		<BgtCard className="gap-3">
			{games.length > 0 && (
				<div className="flex flex-col gap-2">
					<BgtText size="4" weight="bold">
						{t("suggested-games")}
					</BgtText>
					<div className="flex flex-wrap gap-2">
						{games.map((game) => (
							<AvatarChip key={game.id} title={game.title} image={game.image} />
						))}
					</div>
				</div>
			)}

			{manualCount > 0 && (
				<details className="group rounded-lg border border-white/10 bg-white/5">
					<summary className="flex cursor-pointer list-none items-center gap-3 px-3 py-2.5 text-sm font-semibold [&::-webkit-details-marker]:hidden">
						<span className="flex-1">{t("manuals", { count: manualCount })}</span>
						<CaretRight className="size-4 text-gray-400 transition-transform group-open:rotate-90" />
					</summary>
					<div className="flex flex-col gap-4 border-t border-dashed border-white/10 p-3">
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
										className="flex min-h-12 items-center gap-3 rounded-lg bg-white/5 p-3 transition-colors hover:bg-white/10"
									>
										<Download className="size-5 shrink-0 text-primary" />
										<div className="min-w-0 flex-1">
											<BgtText size="2" color="white" className="truncate">
												{manual.title}
											</BgtText>
											<BgtText size="1" color="gray">
												{fileSize(manual.fileSizeBytes)}
											</BgtText>
										</div>
									</a>
								))}
							</div>
						))}
					</div>
				</details>
			)}
		</BgtCard>
	);
};
