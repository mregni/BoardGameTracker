import { useTranslation } from "react-i18next";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import type { CompareResult } from "@/models";
import { useGameById } from "@/routes/-hooks/useGameById";
import { toDisplay } from "@/utils/dateUtils";
import { useShortDuration } from "../-hooks/useShortDuration";
import { SectionTitle } from "./SectionTitle";

interface Props {
	compare: CompareResult;
	dateFormat: string;
	uiLanguage: string;
}

export const Highlights = ({ compare, dateFormat, uiLanguage }: Props) => {
	const { t } = useTranslation("compare");
	const { gameById } = useGameById();
	const duration = useShortDuration();

	const first = compare.firstGameTogether;
	const firstGame = first?.gameId ? gameById(first.gameId) : null;
	const longest = compare.longestSessionTogether;
	const longestGame = longest ? gameById(longest.gameId) : null;

	if (!firstGame && !longestGame) {
		return null;
	}

	return (
		<section aria-labelledby="highlights-title" className="flex flex-col gap-1.5">
			<SectionTitle id="highlights-title">{t("highlights.title")}</SectionTitle>
			<ul className="flex flex-wrap gap-1.5">
				{firstGame && first?.startDate && (
					<li>
						<AvatarChip title={firstGame.title} image={firstGame.image}>
							<span className="text-white/60">
								{t("highlights.first-game", { date: toDisplay(first.startDate, dateFormat, uiLanguage) })}
							</span>
						</AvatarChip>
					</li>
				)}
				{longestGame && longest && (
					<li>
						<AvatarChip title={longestGame.title} image={longestGame.image}>
							<span className="text-white/60">
								{t("highlights.longest-game", { duration: duration(longest.minutes) })}
							</span>
						</AvatarChip>
					</li>
				)}
			</ul>
		</section>
	);
};
