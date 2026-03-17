import { useTranslation } from "react-i18next";
import Target from "@/assets/icons/target.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { RoundDecimal } from "@/utils/numberUtils";
import { usePlayerById } from "../../-hooks/usePlayerById";
import { SCORING_COLORS } from "../-utils/gameColorMappings";

interface ScoreRankItem {
	key: string;
	playerId: number;
	score: number;
}

interface Props {
	scoreRankChart: ScoreRankItem[];
}

export const ScoringResultsCard = (props: Props) => {
	const { scoreRankChart } = props;
	const { t } = useTranslation("game");
	const { playerById } = usePlayerById();

	if (scoreRankChart.length === 0) return null;

	const maxScore = scoreRankChart[0].score;

	return (
		<BgtCard title={t("titles.scoring-results")} icon={Target}>
			<div className="flex flex-col gap-3">
				{scoreRankChart.map((item, i) => (
					<div key={i} className="space-y-1">
						<div className="flex items-center justify-between text-sm">
							<div className="flex flex-col gap-0.5">
								<BgtText className="text-white/70 uppercase text-xs">{t(`charts.top-scoring.${item.key}`)}</BgtText>
								<BgtText color="primary">{playerById(item.playerId)?.name}</BgtText>
							</div>
							<BgtText size="5" weight="bold" color="cyan">
								{RoundDecimal(item.score)}
							</BgtText>
						</div>
						<div className="w-full bg-primary/10 rounded-full h-2 overflow-hidden">
							<div
								className="h-full rounded-full transition-all"
								style={{
									width: `${(item.score / maxScore) * 100}%`,
									backgroundColor: SCORING_COLORS[item.key],
								}}
							/>
						</div>
					</div>
				))}
			</div>
		</BgtCard>
	);
};
