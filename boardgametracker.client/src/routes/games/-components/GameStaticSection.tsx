import { useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Target from "@/assets/icons/target.svg?react";
import { BgtBadge } from "@/components/BgtBadge/BgtBadge";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtText } from "@/components/BgtText/BgtText";
import type { Game } from "@/models";
import { RulebookChatButton } from "./RulebookChatButton";

interface Props {
	game: Game;
	manualCount: number;
	ragEnabled: boolean;
	canTrackPrice?: boolean;
	onTrackPrice?: () => void;
}

export const GameStaticSection = (props: Props) => {
	const { game, manualCount, ragEnabled, canTrackPrice = false, onTrackPrice } = props;
	const { t } = useTranslation("game");
	const navigate = useNavigate();

	return (
		<div className="flex flex-col gap-2">
			{game.categories.length > 0 && (
				<div className="flex flex-wrap gap-2">
					{game.categories.map((cat) => (
						<BgtBadge
							key={cat.id}
							color="primary"
							variant="soft"
							onClick={() =>
								navigate({
									to: "/games",
									search: () => ({ category: cat.name }),
								})
							}
						>
							{cat.name}
						</BgtBadge>
					))}
				</div>
			)}
			<BgtText className="max-w-4xl line-clamp-3 text-white/70 xl:line-clamp-2">{game.description}</BgtText>
			{(ragEnabled || canTrackPrice) && (
				<div className="flex gap-2">
					{ragEnabled && <RulebookChatButton gameId={game.id} disabled={manualCount === 0} />}
					{canTrackPrice && (
						<BgtButton variant="cancel" size="1" onClick={onTrackPrice}>
							<Target className="size-4" />
							{t("track-price.button")}
						</BgtButton>
					)}
				</div>
			)}
		</div>
	);
};
