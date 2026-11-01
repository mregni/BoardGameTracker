import { useTranslation } from "react-i18next";
import Target from "@/assets/icons/target.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtProgressBar } from "@/components/BgtProgressBar/BgtProgressBar";
import { BgtText } from "@/components/BgtText/BgtText";
import { useNumberFormat } from "@/hooks/useNumberFormat";
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
	const { integer } = useNumberFormat();

	if (scoreRankChart.length === 0) return null;

	const maxScore = scoreRankChart[0].score;

	return (
		<BgtCard title={t("titles.scoring-results")} icon={Target}>
			<div className="flex flex-col gap-3">
				{scoreRankChart.map((item) => (
					<div key={item.key} className="space-y-1">
						<div className="flex items-center justify-between text-sm">
							<div className="flex flex-col gap-0.5">
								<BgtText className="text-white/70 text-xs">{t(`charts.top-scoring.${item.key}`)}</BgtText>
								<BgtText color="primary">{playerById(item.playerId)?.name}</BgtText>
							</div>
							<BgtText size="5" weight="bold" color="cyan">
								{integer(item.score)}
							</BgtText>
						</div>
						<BgtProgressBar
							size="thin"
							track="primary"
							value={item.score}
							max={maxScore}
							color={SCORING_COLORS[item.key]}
							animateOnMount
							aria-label={t(`charts.top-scoring.${item.key}`)}
							aria-valuetext={integer(item.score)}
						/>
					</div>
				))}
			</div>
		</BgtCard>
	);
};
