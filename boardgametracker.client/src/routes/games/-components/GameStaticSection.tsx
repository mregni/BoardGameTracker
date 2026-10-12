import { useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useState } from "react";
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

const LONG_DESCRIPTION_LENGTH = 180;

export const GameStaticSection = (props: Props) => {
	const { game, manualCount, ragEnabled, canTrackPrice = false, onTrackPrice } = props;
	const { t } = useTranslation("game");
	const navigate = useNavigate();
	const [expanded, setExpanded] = useState(false);
	const isLong = game.description.length > LONG_DESCRIPTION_LENGTH;

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
			<div className="flex max-w-4xl flex-col items-start gap-1">
				<BgtText
					id={`game-description-${game.id}`}
					className={cx("text-white/70", !expanded && "line-clamp-3 xl:line-clamp-2")}
				>
					{game.description}
				</BgtText>
				{isLong && (
					<button
						type="button"
						aria-expanded={expanded}
						aria-controls={`game-description-${game.id}`}
						onClick={() => setExpanded((value) => !value)}
						className="text-sm text-primary hover:text-primary/80 cursor-pointer"
					>
						{expanded ? t("about.show-less") : t("about.show-more")}
					</button>
				)}
			</div>
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
